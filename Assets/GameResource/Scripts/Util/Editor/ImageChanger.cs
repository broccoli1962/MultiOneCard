using System.IO;
using UnityEditor;
using UnityEngine;

// 기존 이미지를 불러와 외곽선을 추가/수정하는 도구
// Tools/ImageGenerator/ImageChanger 에서 열 수 있음
public class ImageChanger : EditorWindow
{
    // ─────────────────────────────────────────
    //  열거형
    // ─────────────────────────────────────────

    private enum ShapeSource     { ImageSilhouette, SDFShape }
    private enum ShapeType       { RoundedRect, Circle, Diamond, Hexagon }
    private enum BorderPlacement { Outside, Centered, Inside }

    // ─────────────────────────────────────────
    //  소스 이미지
    // ─────────────────────────────────────────

    private Texture2D _src;
    private Color[]   _srcPix;
    private int       _srcW, _srcH;
    private float[]   _silSDF;   // 이미지 알파에서 계산한 Signed Distance Field

    // ─────────────────────────────────────────
    //  테두리 모양
    // ─────────────────────────────────────────

    private ShapeSource     _shapeSource = ShapeSource.ImageSilhouette;
    private ShapeType       _sdfShape    = ShapeType.RoundedRect;
    private float           _rLarge = 0.40f, _rSmall = 0.10f;
    private BorderPlacement _placement   = BorderPlacement.Outside;

    // ─────────────────────────────────────────
    //  테두리 레이어 (바깥 → 안)
    // ─────────────────────────────────────────

    private Color _col0 = Color.black; private float _t0 = 0.025f;
    private Color _col1 = Color.white; private float _t1 = 0.050f;
    private Color _col2 = Color.black; private float _t2 = 0.025f;

    // ─────────────────────────────────────────
    //  출력
    // ─────────────────────────────────────────

    private string _outPath = "";

    // ─────────────────────────────────────────
    //  미리보기
    // ─────────────────────────────────────────

    private Texture2D _previewTex;
    private bool      _dirty = true;
    private Vector2   _scroll;

    // ─────────────────────────────────────────

    [MenuItem("Tools/ImageGenerator/ImageChanger")]
    public static void Open()
    {
        var w = GetWindow<ImageChanger>("Image Changer");
        w.minSize = new Vector2(420, 700);
    }

    // ─────────────────────────────────────────
    //  GUI
    // ─────────────────────────────────────────

    private void OnGUI()
    {
        _scroll = EditorGUILayout.BeginScrollView(_scroll);

        // ── 소스 이미지 ──────────────────────────────────────────────
        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("소스 이미지", EditorStyles.boldLabel);
        EditorGUI.BeginChangeCheck();
        var newSrc = (Texture2D)EditorGUILayout.ObjectField(
            "이미지 선택", _src, typeof(Texture2D), false);
        if (EditorGUI.EndChangeCheck() && newSrc != _src)
        {
            _src = newSrc;
            LoadSource();
            if (_src != null && string.IsNullOrEmpty(_outPath))
                SuggestOutputPath();
            _dirty = true;
        }
        if (_src != null)
            EditorGUILayout.LabelField(
                $"{_srcW} × {_srcH}  |  {AssetDatabase.GetAssetPath(_src)}",
                EditorStyles.miniLabel);

        // ── 테두리 모양 ──────────────────────────────────────────────
        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("테두리 모양", EditorStyles.boldLabel);
        EditorGUI.BeginChangeCheck();

        _shapeSource = (ShapeSource)EditorGUILayout.EnumPopup("모양 기준", _shapeSource);
        EditorGUILayout.HelpBox(
            _shapeSource == ShapeSource.ImageSilhouette
                ? "이미지의 알파 채널 윤곽선을 따라 테두리를 그립니다."
                : "선택한 SDF 도형의 경계를 기준으로 테두리를 그립니다.",
            MessageType.None);

        if (_shapeSource == ShapeSource.SDFShape)
        {
            _sdfShape = (ShapeType)EditorGUILayout.EnumPopup("도형", _sdfShape);
            if (_sdfShape == ShapeType.RoundedRect)
            {
                _rLarge = EditorGUILayout.Slider("TL/BR 곡선", _rLarge, 0.05f, 0.50f);
                _rSmall = EditorGUILayout.Slider("TR/BL 곡선", _rSmall, 0.01f, 0.30f);
            }
        }
        _placement = (BorderPlacement)EditorGUILayout.EnumPopup("테두리 위치", _placement);
        if (EditorGUI.EndChangeCheck()) _dirty = true;

        // ── 테두리 레이어 ────────────────────────────────────────────
        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("테두리 레이어  (바깥 → 안)", EditorStyles.boldLabel);
        if (DrawLayer("레이어 1", ref _col0, ref _t0)) _dirty = true;
        if (DrawLayer("레이어 2", ref _col1, ref _t1)) _dirty = true;
        if (DrawLayer("레이어 3", ref _col2, ref _t2)) _dirty = true;

        float totalThick = _t0 + _t1 + _t2;
        int refPx = _srcW > 0 ? _srcW : 1024;
        EditorGUILayout.LabelField(
            $"전체 두께: {totalThick:F3}  ({totalThick * refPx:F0} px @ {refPx}px 기준)",
            EditorStyles.miniLabel);

        // ── 미리보기 ─────────────────────────────────────────────────
        EditorGUILayout.Space(8);
        if (_dirty) { RebuildPreview(); _dirty = false; }

        if (_previewTex != null)
        {
            float sz = Mathf.Min(position.width - 20, 260);
            var r = GUILayoutUtility.GetRect(sz, sz);
            r.x = (position.width - sz) * 0.5f;
            r.width = r.height = sz;
            EditorGUI.DrawTextureTransparent(r, _previewTex);
        }
        else
        {
            var helpRect = GUILayoutUtility.GetRect(0, 80);
            EditorGUI.HelpBox(helpRect, "소스 이미지를 선택하면 미리보기가 표시됩니다.", MessageType.Info);
        }

        // ── 출력 경로 ────────────────────────────────────────────────
        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("출력 경로", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        _outPath = EditorGUILayout.TextField(_outPath);
        if (GUILayout.Button("...", GUILayout.Width(26)))
        {
            string dir  = Path.GetDirectoryName(_outPath)?.Replace("Assets/", "") ?? "GameResource/Images/GameUI";
            string file = Path.GetFileNameWithoutExtension(_outPath);
            string res  = EditorUtility.SaveFilePanelInProject(
                "저장 위치 선택", string.IsNullOrEmpty(file) ? "output" : file,
                "png", "PNG 저장 위치", "Assets/" + dir);
            if (!string.IsNullOrEmpty(res)) _outPath = res;
        }
        EditorGUILayout.EndHorizontal();
        if (_src != null && GUILayout.Button("소스 이름 기반으로 자동 설정", EditorStyles.miniButton))
            SuggestOutputPath();

        // ── 저장 버튼 ────────────────────────────────────────────────
        EditorGUILayout.Space(8);
        GUI.enabled = _src != null && _srcPix != null;
        GUI.backgroundColor = new Color(0.4f, 0.8f, 0.4f);
        if (GUILayout.Button("이미지 저장", GUILayout.Height(36))) ExportFull();
        GUI.backgroundColor = Color.white;
        GUI.enabled = true;

        EditorGUILayout.EndScrollView();
    }

    // ─────────────────────────────────────────
    //  레이어 UI 헬퍼
    // ─────────────────────────────────────────

    private bool DrawLayer(string label, ref Color col, ref float thickness)
    {
        bool changed = false;
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(label, GUILayout.Width(56));
        EditorGUI.BeginChangeCheck();
        col = EditorGUILayout.ColorField(GUIContent.none, col, true, true, false, GUILayout.Width(52));
        if (EditorGUI.EndChangeCheck()) changed = true;
        EditorGUI.BeginChangeCheck();
        thickness = EditorGUILayout.Slider(thickness, 0f, 0.15f);
        if (EditorGUI.EndChangeCheck()) changed = true;
        EditorGUILayout.EndHorizontal();
        return changed;
    }

    private void SuggestOutputPath()
    {
        if (_src == null) return;
        string sp   = AssetDatabase.GetAssetPath(_src);
        string dir  = Path.GetDirectoryName(sp)?.Replace("\\", "/") ?? "Assets";
        string name = Path.GetFileNameWithoutExtension(sp);
        _outPath = $"{dir}/{name}_bordered.png";
    }

    // ─────────────────────────────────────────
    //  소스 이미지 로딩
    // ─────────────────────────────────────────

    private void LoadSource()
    {
        _srcPix = null; _silSDF = null; _srcW = _srcH = 0;
        if (_src == null) return;

        // Read/Write 설정 없이도 읽을 수 있도록 RenderTexture 경유
        var rt   = RenderTexture.GetTemporary(_src.width, _src.height, 0, RenderTextureFormat.ARGB32);
        Graphics.Blit(_src, rt);
        var prev = RenderTexture.active;
        RenderTexture.active = rt;
        var tmp  = new Texture2D(_src.width, _src.height, TextureFormat.RGBA32, false);
        tmp.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        tmp.Apply();
        RenderTexture.active = prev;
        RenderTexture.ReleaseTemporary(rt);

        _srcPix = tmp.GetPixels();
        _srcW   = tmp.width;
        _srcH   = tmp.height;
        DestroyImmediate(tmp);

        // 실루엣 SDF 사전 계산
        _silSDF = ComputeSilhouetteSDF(_srcPix, _srcW, _srcH);
    }

    // ─────────────────────────────────────────
    //  실루엣 SDF — Chamfer 3-4 DT
    // ─────────────────────────────────────────

    // 이미지 알파를 기반으로 Signed Distance Field 계산
    // 결과 범위: 음수=내부, 양수=외부, 0=경계
    private static float[] ComputeSilhouetteSDF(Color[] pix, int w, int h)
    {
        bool[] inside = new bool[w * h];
        for (int i = 0; i < pix.Length; i++) inside[i] = pix[i].a > 0.5f;

        // outside: 외부 픽셀 → 가장 가까운 내부 픽셀까지의 거리
        // inner:   내부 픽셀 → 가장 가까운 외부 픽셀까지의 거리
        int[] outsideDist = ChamferDT(inside, w, h, seedInside: true);
        int[] insideDist  = ChamferDT(inside, w, h, seedInside: false);

        float scale = 3f * Mathf.Max(w, h);
        var sdf = new float[w * h];
        for (int i = 0; i < sdf.Length; i++)
            sdf[i] = (outsideDist[i] - insideDist[i]) / scale;
        return sdf;
    }

    // Chamfer 3-4 Distance Transform (2-패스 스캔라인)
    // seedInside=true  → 내부 픽셀을 시드로, 외부 픽셀의 거리 계산
    // seedInside=false → 외부 픽셀을 시드로, 내부 픽셀의 거리 계산
    private static int[] ChamferDT(bool[] mask, int w, int h, bool seedInside)
    {
        const int INF = int.MaxValue / 2;
        var d = new int[w * h];
        for (int i = 0; i < d.Length; i++)
            d[i] = (mask[i] == seedInside) ? 0 : INF;

        // 정방향 (좌상 → 우하)
        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
        {
            int i = y * w + x;
            if (y > 0 && x > 0   && d[(y-1)*w+(x-1)] < INF) d[i] = Mathf.Min(d[i], d[(y-1)*w+(x-1)] + 4);
            if (y > 0            && d[(y-1)*w+ x   ] < INF) d[i] = Mathf.Min(d[i], d[(y-1)*w+ x   ] + 3);
            if (y > 0 && x < w-1 && d[(y-1)*w+(x+1)] < INF) d[i] = Mathf.Min(d[i], d[(y-1)*w+(x+1)] + 4);
            if (x > 0            && d[    y *w+(x-1)] < INF) d[i] = Mathf.Min(d[i], d[    y *w+(x-1)] + 3);
        }
        // 역방향 (우하 → 좌상)
        for (int y = h-1; y >= 0; y--)
        for (int x = w-1; x >= 0; x--)
        {
            int i = y * w + x;
            if (y < h-1 && x < w-1 && d[(y+1)*w+(x+1)] < INF) d[i] = Mathf.Min(d[i], d[(y+1)*w+(x+1)] + 4);
            if (y < h-1            && d[(y+1)*w+ x   ] < INF) d[i] = Mathf.Min(d[i], d[(y+1)*w+ x   ] + 3);
            if (y < h-1 && x > 0  && d[(y+1)*w+(x-1)] < INF) d[i] = Mathf.Min(d[i], d[(y+1)*w+(x-1)] + 4);
            if (x < w-1            && d[    y *w+(x+1)] < INF) d[i] = Mathf.Min(d[i], d[    y *w+(x+1)] + 3);
        }
        return d;
    }

    // ─────────────────────────────────────────
    //  SDF 조회
    // ─────────────────────────────────────────

    private float GetSDF(int idx, float px, float py)
    {
        if (_shapeSource == ShapeSource.ImageSilhouette && _silSDF != null)
            return _silSDF[idx];
        return ShapeSDF(px, py);
    }

    private float ShapeSDF(float px, float py)
    {
        return _sdfShape switch
        {
            ShapeType.RoundedRect => SDF_RoundedRect(px, py),
            ShapeType.Circle      => SDF_Circle(px, py),
            ShapeType.Diamond     => SDF_Diamond(px, py),
            ShapeType.Hexagon     => SDF_Hexagon(px, py),
            _                     => SDF_RoundedRect(px, py),
        };
    }

    private float SDF_RoundedRect(float px, float py)
    {
        float r  = (px < 0 == py >= 0) ? _rLarge : _rSmall;
        float qx = Mathf.Abs(px) - (0.5f - r);
        float qy = Mathf.Abs(py) - (0.5f - r);
        return Mathf.Sqrt(Mathf.Max(qx, 0) * Mathf.Max(qx, 0) +
                          Mathf.Max(qy, 0) * Mathf.Max(qy, 0))
               + Mathf.Min(Mathf.Max(qx, qy), 0f) - r;
    }

    private static float SDF_Circle(float px, float py)
        => Mathf.Sqrt(px * px + py * py) - 0.5f;

    private static float SDF_Diamond(float px, float py)
        => Mathf.Abs(px) + Mathf.Abs(py) - 0.5f;

    private static float SDF_Hexagon(float px, float py)
    {
        const float kx = -0.866025404f, ky = 0.5f, kz = 0.577350269f;
        const float r  = 0.5f;
        float ax = Mathf.Abs(px), ay = Mathf.Abs(py);
        float m  = 2f * Mathf.Min(kx * ax + ky * ay, 0f);
        ax -= m * kx; ay -= m * ky;
        ax -= Mathf.Clamp(ax, -kz * r, kz * r);
        ay -= r;
        return Mathf.Sqrt(ax * ax + ay * ay) * Mathf.Sign(ay);
    }

    // ─────────────────────────────────────────
    //  테두리 색상 계산
    // ─────────────────────────────────────────

    // SDF 거리 d에서의 테두리 색(col)과 불투명도(a)를 반환
    private (Color col, float a) GetBorderAt(float d, float aa)
    {
        float total  = _t0 + _t1 + _t2;
        float offset = _placement switch
        {
            BorderPlacement.Outside  =>  0f,
            BorderPlacement.Centered => -total * 0.5f,
            BorderPlacement.Inside   => -total,
            _                        =>  0f,
        };
        float sd = d - offset;

        float a0 = SmoothBand(sd, 0f,       _t0,         aa);
        float a1 = SmoothBand(sd, _t0,      _t0 + _t1,   aa);
        float a2 = SmoothBand(sd, _t0+_t1,  total,       aa);
        float wa  = a0 + a1 + a2;
        float ta  = Mathf.Clamp01(wa);
        if (ta < 0.001f) return (Color.clear, 0f);

        Color col = (_col0 * a0 + _col1 * a1 + _col2 * a2) / (wa + 0.00001f);
        col.a = 1f;
        return (col, ta);
    }

    // aa 폭을 가진 [lo, hi] 구간에서 부드럽게 1이 되는 함수
    private static float SmoothBand(float sd, float lo, float hi, float aa)
        => Mathf.Clamp01((sd - lo) / aa + 0.5f) * Mathf.Clamp01((hi - sd) / aa + 0.5f);

    // Porter-Duff "src over dst" 합성
    private static Color AlphaOver(Color dst, Color src, float srcAlpha)
    {
        float a = srcAlpha + dst.a * (1f - srcAlpha);
        if (a < 0.001f) return new Color(0, 0, 0, 0);
        float inv = 1f / a;
        return new Color(
            (src.r * srcAlpha + dst.r * dst.a * (1f - srcAlpha)) * inv,
            (src.g * srcAlpha + dst.g * dst.a * (1f - srcAlpha)) * inv,
            (src.b * srcAlpha + dst.b * dst.a * (1f - srcAlpha)) * inv,
            a);
    }

    // ─────────────────────────────────────────
    //  미리보기 생성 (200px)
    // ─────────────────────────────────────────

    private void RebuildPreview()
    {
        const int P = 200;
        if (_previewTex == null || _previewTex.width != P)
            _previewTex = new Texture2D(P, P, TextureFormat.RGBA32, false);

        Color[] srcAtP;
        float[] sdfAtP;

        if (_srcPix != null)
        {
            srcAtP = Bilinear(_srcPix, _srcW, _srcH, P, P);
            sdfAtP = (_shapeSource == ShapeSource.ImageSilhouette)
                ? ComputeSilhouetteSDF(srcAtP, P, P)
                : null;
        }
        else
        {
            srcAtP = new Color[P * P];
            sdfAtP = null;
        }

        float aa = 0.003f;
        var pixels = new Color[P * P];

        for (int y = 0; y < P; y++)
        for (int x = 0; x < P; x++)
        {
            float px = (x + 0.5f) / P - 0.5f;
            float py = (y + 0.5f) / P - 0.5f;
            int   idx = y * P + x;

            float d = (sdfAtP != null) ? sdfAtP[idx] : ShapeSDF(px, py);
            var (bCol, bA) = GetBorderAt(d, aa);

            Color composite = AlphaOver(srcAtP[idx], bCol, bA);

            // 체커보드 배경으로 투명도 시각화
            bool ck = ((x / 10 + y / 10) % 2 == 0);
            var bg = new Color(ck ? 0.8f : 0.6f, ck ? 0.8f : 0.6f, ck ? 0.8f : 0.6f, 1f);
            composite = AlphaOver(bg, composite, composite.a);
            composite.a = 1f;
            pixels[idx] = composite;
        }

        _previewTex.SetPixels(pixels);
        _previewTex.Apply();
    }

    // ─────────────────────────────────────────
    //  풀 해상도 내보내기
    // ─────────────────────────────────────────

    private void ExportFull()
    {
        if (_srcPix == null) return;

        float aa = 2.5f / Mathf.Max(_srcW, _srcH);
        var pixels = new Color[_srcW * _srcH];

        for (int y = 0; y < _srcH; y++)
        for (int x = 0; x < _srcW; x++)
        {
            float px = (x + 0.5f) / _srcW - 0.5f;
            float py = (y + 0.5f) / _srcH - 0.5f;
            int   idx = y * _srcW + x;

            float d = GetSDF(idx, px, py);
            var (bCol, bA) = GetBorderAt(d, aa);
            pixels[idx] = AlphaOver(_srcPix[idx], bCol, bA);
        }

        WritePng(_outPath, pixels, _srcW, _srcH);
        AssetDatabase.Refresh();

        if (_outPath.StartsWith("Assets/"))
        {
            var ti = AssetImporter.GetAtPath(_outPath) as TextureImporter;
            if (ti != null)
            {
                ti.textureType         = TextureImporterType.Sprite;
                ti.alphaIsTransparency = true;
                ti.mipmapEnabled       = false;
                ti.spritePixelsPerUnit = 100;
                ti.SaveAndReimport();
            }
        }

        Debug.Log($"[ImageChanger] 저장 완료: {_outPath}");
        EditorUtility.RevealInFinder(_outPath);
    }

    // ─────────────────────────────────────────
    //  유틸리티
    // ─────────────────────────────────────────

    // 바이리니어 다운스케일
    private static Color[] Bilinear(Color[] src, int sw, int sh, int dw, int dh)
    {
        var dst = new Color[dw * dh];
        float scaleX = (float)sw / dw, scaleY = (float)sh / dh;
        for (int y = 0; y < dh; y++)
        for (int x = 0; x < dw; x++)
        {
            float fx = (x + 0.5f) * scaleX - 0.5f;
            float fy = (y + 0.5f) * scaleY - 0.5f;
            int x0 = Mathf.Clamp((int)fx,     0, sw-1);
            int x1 = Mathf.Clamp((int)fx + 1, 0, sw-1);
            int y0 = Mathf.Clamp((int)fy,     0, sh-1);
            int y1 = Mathf.Clamp((int)fy + 1, 0, sh-1);
            float tx = fx - Mathf.Floor(fx), ty = fy - Mathf.Floor(fy);
            dst[y * dw + x] = Color.Lerp(
                Color.Lerp(src[y0*sw+x0], src[y0*sw+x1], tx),
                Color.Lerp(src[y1*sw+x0], src[y1*sw+x1], tx), ty);
        }
        return dst;
    }

    private static void WritePng(string path, Color[] pixels, int w, int h)
    {
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.SetPixels(pixels);
        tex.Apply();
        string dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
        File.WriteAllBytes(path, tex.EncodeToPNG());
        DestroyImmediate(tex);
    }
}
