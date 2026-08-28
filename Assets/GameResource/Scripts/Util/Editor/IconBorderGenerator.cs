using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

// SDF 수학으로 도형을 처음부터 생성하는 도구
// Tools/ImageGenerator/SDFGenerator 에서 열 수 있음
public class SDFGenerator : EditorWindow
{
    // ─────────────────────────────────────────
    //  데이터 타입
    // ─────────────────────────────────────────

    public enum ShapeType { RoundedRect, Circle, Diamond, Hexagon }

    [Serializable]
    public class BorderConfig
    {
        public string    label      = "New Config";
        public ShapeType shape      = ShapeType.RoundedRect;

        public float rLarge = 0.40f;
        public float rSmall = 0.10f;

        public float  bOuter     = 0.020f;
        public Color  colOuter   = new Color(0f, 0f, 0f, 1f);
        public float  bWhite     = 0.050f;
        public Color  colWhite   = new Color(1f, 1f, 1f, 1f);
        public float  bInner     = 0.020f;
        public Color  colInner   = new Color(0f, 0f, 0f, 1f);

        public int textureSize = 1024;

        public string borderPath   = "Assets/GameResource/Images/GameUI/UIPanelIcon.png";
        public bool   generateMask = true;
        public string maskPath     = "Assets/GameResource/Images/GameUI/UIPanelIconFillMask.png";

        public BorderConfig Clone() => (BorderConfig)MemberwiseClone();
    }

    // ─────────────────────────────────────────
    //  창 상태
    // ─────────────────────────────────────────

    private enum Tab { Single, Batch }
    private Tab _tab = Tab.Single;

    private BorderConfig       _single       = new BorderConfig();
    private Texture2D          _previewTex;
    private bool               _previewDirty = true;
    private Vector2            _singleScroll;

    private List<BorderConfig> _batch        = new List<BorderConfig>();
    private int                _batchSel     = -1;
    private Vector2            _batchScroll;
    private Vector2            _detailScroll;

    [MenuItem("Tools/ImageGenerator/SDFGenerator")]
    public static void Open()
    {
        var win = GetWindow<SDFGenerator>("SDF Generator");
        win.minSize = new Vector2(420, 600);
    }

    // ─────────────────────────────────────────
    //  GUI 진입점
    // ─────────────────────────────────────────

    private void OnGUI()
    {
        EditorGUILayout.Space(4);
        var newTab = (Tab)GUILayout.Toolbar((int)_tab, new[] { "단일 생성", "배치 생성" });
        if (newTab != _tab) { _tab = newTab; _previewDirty = true; }
        EditorGUILayout.Space(6);

        if (_tab == Tab.Single) DrawSingleTab();
        else                    DrawBatchTab();
    }

    // ─────────────────────────────────────────
    //  단일 탭
    // ─────────────────────────────────────────

    private void DrawSingleTab()
    {
        _singleScroll = EditorGUILayout.BeginScrollView(_singleScroll);

        bool dirty = DrawConfigFields(_single, showPaths: true);
        if (dirty) _previewDirty = true;

        EditorGUILayout.Space(8);

        // ── 미리보기 (모든 도형 지원) ──
        if (_previewDirty) { RebuildPreview(_single); _previewDirty = false; }
        float previewSize = Mathf.Min(position.width - 20, 220);
        var rect = GUILayoutUtility.GetRect(previewSize, previewSize);
        rect.x = (position.width - previewSize) * 0.5f;
        rect.width = rect.height = previewSize;
        EditorGUI.DrawTextureTransparent(rect, _previewTex);

        EditorGUILayout.Space(6);
        GUI.backgroundColor = new Color(0.4f, 0.8f, 0.4f);
        if (GUILayout.Button("이미지 생성", GUILayout.Height(36))) GenerateImages(_single);
        GUI.backgroundColor = Color.white;

        EditorGUILayout.EndScrollView();
    }

    // ─────────────────────────────────────────
    //  배치 탭
    // ─────────────────────────────────────────

    private void DrawBatchTab()
    {
        EditorGUILayout.BeginHorizontal();

        EditorGUILayout.BeginVertical(GUILayout.Width(150));
        _batchScroll = EditorGUILayout.BeginScrollView(_batchScroll);
        for (int i = 0; i < _batch.Count; i++)
        {
            GUI.backgroundColor = (i == _batchSel) ? new Color(0.6f, 0.8f, 1f) : Color.white;
            if (GUILayout.Button(_batch[i].label, GUILayout.Height(28))) _batchSel = i;
        }
        GUI.backgroundColor = Color.white;
        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space(4);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("+", GUILayout.Width(40)))
        {
            var cfg = _single.Clone();
            cfg.label = $"Config {_batch.Count + 1}";
            _batch.Add(cfg);
            _batchSel = _batch.Count - 1;
        }
        GUI.enabled = _batchSel >= 0 && _batchSel < _batch.Count;
        if (GUILayout.Button("-", GUILayout.Width(40)))
        {
            _batch.RemoveAt(_batchSel);
            _batchSel = Mathf.Clamp(_batchSel - 1, -1, _batch.Count - 1);
        }
        GUI.enabled = true;
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();

        EditorGUILayout.BeginVertical();
        if (_batchSel >= 0 && _batchSel < _batch.Count)
        {
            _detailScroll = EditorGUILayout.BeginScrollView(_detailScroll);
            var cfg = _batch[_batchSel];
            cfg.label = EditorGUILayout.TextField("이름", cfg.label);
            DrawConfigFields(cfg, showPaths: true);
            EditorGUILayout.EndScrollView();
        }
        else
        {
            EditorGUILayout.HelpBox("왼쪽 목록에서 설정을 선택하거나 + 로 추가하세요.", MessageType.Info);
        }
        EditorGUILayout.EndVertical();
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(8);
        GUI.enabled = _batch.Count > 0;
        GUI.backgroundColor = new Color(0.4f, 0.8f, 0.4f);
        if (GUILayout.Button($"전체 {_batch.Count}개 생성", GUILayout.Height(36)))
        {
            foreach (var cfg in _batch) GenerateImages(cfg);
            Debug.Log($"[SDFGenerator] {_batch.Count}개 배치 완료");
        }
        GUI.backgroundColor = Color.white;
        GUI.enabled = true;
    }

    // ─────────────────────────────────────────
    //  공용 설정 UI
    // ─────────────────────────────────────────

    private bool DrawConfigFields(BorderConfig cfg, bool showPaths)
    {
        bool changed = false;

        EditorGUI.BeginChangeCheck();
        cfg.shape = (ShapeType)EditorGUILayout.EnumPopup("모양", cfg.shape);
        if (EditorGUI.EndChangeCheck()) changed = true;

        if (cfg.shape == ShapeType.RoundedRect)
        {
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("모서리 반경", EditorStyles.boldLabel);
            if (DrawSlider("TL / BR (큰 곡선)", ref cfg.rLarge, 0.01f, 0.50f)) changed = true;
            if (DrawSlider("TR / BL (작은 곡선)", ref cfg.rSmall, 0.01f, 0.50f)) changed = true;
        }

        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("선 색상 / 두께", EditorStyles.boldLabel);
        if (DrawColorLine("외곽선", ref cfg.colOuter, ref cfg.bOuter, 0f, 0.10f)) changed = true;
        if (DrawColorLine("중앙선", ref cfg.colWhite, ref cfg.bWhite, 0f, 0.20f)) changed = true;
        if (DrawColorLine("내부선", ref cfg.colInner, ref cfg.bInner, 0f, 0.10f)) changed = true;
        EditorGUILayout.LabelField($"전체 두께: {(cfg.bOuter + cfg.bWhite + cfg.bInner):F3}", EditorStyles.miniLabel);

        EditorGUILayout.Space(4);
        EditorGUI.BeginChangeCheck();
        cfg.textureSize = EditorGUILayout.IntPopup("텍스처 해상도", cfg.textureSize,
            new[] { "512", "1024", "2048" }, new[] { 512, 1024, 2048 });
        if (EditorGUI.EndChangeCheck()) changed = true;

        if (showPaths)
        {
            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("출력 경로", EditorStyles.boldLabel);
            DrawPathField("Border PNG", ref cfg.borderPath, "UIPanelIcon");

            EditorGUILayout.Space(4);
            EditorGUI.BeginChangeCheck();
            cfg.generateMask = EditorGUILayout.Toggle("Mask 생성", cfg.generateMask);
            if (EditorGUI.EndChangeCheck()) changed = true;

            if (cfg.generateMask)
                DrawPathField("Mask PNG", ref cfg.maskPath, "UIPanelIconFillMask");
        }

        return changed;
    }

    private bool DrawSlider(string label, ref float value, float min, float max)
    {
        EditorGUI.BeginChangeCheck();
        float next = EditorGUILayout.Slider(label, value, min, max);
        if (EditorGUI.EndChangeCheck()) { value = next; return true; }
        return false;
    }

    // 컬러 피커 + 두께 슬라이더를 한 줄에 표시
    private static bool DrawColorLine(string label, ref Color col, ref float thickness, float min, float max)
    {
        bool changed = false;
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(label, GUILayout.Width(44));
        EditorGUI.BeginChangeCheck();
        col = EditorGUILayout.ColorField(GUIContent.none, col, true, true, false, GUILayout.Width(52));
        if (EditorGUI.EndChangeCheck()) changed = true;
        EditorGUI.BeginChangeCheck();
        thickness = EditorGUILayout.Slider(thickness, min, max);
        if (EditorGUI.EndChangeCheck()) changed = true;
        EditorGUILayout.EndHorizontal();
        return changed;
    }

    // 경로 필드: ObjectField(드래그&드롭) + "..." 저장 다이얼로그 + 전체 경로 텍스트 표시
    private static void DrawPathField(string label, ref string path, string defaultName)
    {
        EditorGUILayout.LabelField(label, EditorStyles.miniLabel);
        EditorGUILayout.BeginHorizontal();

        // ObjectField로 기존 에셋 선택
        var existing = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        EditorGUI.BeginChangeCheck();
        var picked = (Texture2D)EditorGUILayout.ObjectField(
            existing, typeof(Texture2D), false, GUILayout.Width(52), GUILayout.Height(52));
        if (EditorGUI.EndChangeCheck() && picked != null)
            path = AssetDatabase.GetAssetPath(picked);

        // 경로 텍스트 + "..." 버튼
        EditorGUILayout.BeginVertical();
        EditorGUI.BeginChangeCheck();
        // WordWrap 스타일로 경로가 잘리지 않게 표시
        var wrapStyle = new GUIStyle(EditorStyles.textField) { wordWrap = true };
        string edited = EditorGUILayout.TextField(path, wrapStyle, GUILayout.ExpandWidth(true), GUILayout.MinHeight(36));
        if (EditorGUI.EndChangeCheck()) path = edited;

        if (GUILayout.Button("... 저장 위치 선택", EditorStyles.miniButton))
        {
            string dir  = Path.GetDirectoryName(path)?.Replace("Assets/", "") ?? "GameResource/Images/GameUI";
            string file = Path.GetFileNameWithoutExtension(path);
            if (string.IsNullOrEmpty(file)) file = defaultName;
            string result = EditorUtility.SaveFilePanelInProject(
                "저장 위치 선택", file, "png", "PNG 저장 위치를 선택하세요.", "Assets/" + dir);
            if (!string.IsNullOrEmpty(result)) path = result;
        }
        EditorGUILayout.EndVertical();

        EditorGUILayout.EndHorizontal();
    }

    // ─────────────────────────────────────────
    //  미리보기 (모든 도형 지원)
    // ─────────────────────────────────────────

    private void RebuildPreview(BorderConfig cfg)
    {
        const int P = 128;
        if (_previewTex == null)
            _previewTex = new Texture2D(P, P, TextureFormat.RGBA32, false);

        float aa = 2.5f / P;
        float d0 = 0f,   d1 = -cfg.bOuter,
              d2 = -(cfg.bOuter + cfg.bWhite),
              d3 = -(cfg.bOuter + cfg.bWhite + cfg.bInner);

        var pixels = new Color[P * P];
        for (int y = 0; y < P; y++)
        for (int x = 0; x < P; x++)
        {
            float px = (x + 0.5f) / P - 0.5f;
            float py = (y + 0.5f) / P - 0.5f;
            float dist = SDF(px, py, cfg);

            float aBO = Clamp01((d0 - dist + aa * 0.5f) / aa) * Clamp01((dist - d1 + aa * 0.5f) / aa);
            float aW  = Clamp01((d1 - dist + aa * 0.5f) / aa) * Clamp01((dist - d2 + aa * 0.5f) / aa);
            float aBI = Clamp01((d2 - dist + aa * 0.5f) / aa) * Clamp01((dist - d3 + aa * 0.5f) / aa);

            bool checker = ((x / 8 + y / 8) % 2 == 0);
            var bg = checker ? new Color(0.8f, 0.8f, 0.8f, 1f) : new Color(0.6f, 0.6f, 0.6f, 1f);

            Color blended = BlendLayers(aBO, cfg.colOuter, aW, cfg.colWhite, aBI, cfg.colInner);
            pixels[y * P + x] = Color.Lerp(bg, blended, blended.a);
        }

        _previewTex.SetPixels(pixels);
        _previewTex.Apply();
    }

    // ─────────────────────────────────────────
    //  이미지 생성
    // ─────────────────────────────────────────

    private static void GenerateImages(BorderConfig cfg)
    {
        int sz = cfg.textureSize;
        float aa = 2.5f / sz;
        float d0 = 0f,   d1 = -cfg.bOuter,
              d2 = -(cfg.bOuter + cfg.bWhite),
              d3 = -(cfg.bOuter + cfg.bWhite + cfg.bInner);

        var bPix = new Color[sz * sz];
        var mPix = cfg.generateMask ? new Color[sz * sz] : null;

        for (int y = 0; y < sz; y++)
        for (int x = 0; x < sz; x++)
        {
            float px = (x + 0.5f) / sz - 0.5f;
            float py = (y + 0.5f) / sz - 0.5f;
            float dist = SDF(px, py, cfg);

            float aBO = Clamp01((d0 - dist + aa * 0.5f) / aa) * Clamp01((dist - d1 + aa * 0.5f) / aa);
            float aW  = Clamp01((d1 - dist + aa * 0.5f) / aa) * Clamp01((dist - d2 + aa * 0.5f) / aa);
            float aBI = Clamp01((d2 - dist + aa * 0.5f) / aa) * Clamp01((dist - d3 + aa * 0.5f) / aa);

            bPix[y * sz + x] = BlendLayers(aBO, cfg.colOuter, aW, cfg.colWhite, aBI, cfg.colInner);

            if (mPix != null)
            {
                float mA = Clamp01((-dist - (cfg.bOuter + cfg.bWhite + cfg.bInner) + aa * 0.5f) / aa);
                mPix[y * sz + x] = new Color(1, 1, 1, mA);
            }
        }

        WritePng(cfg.borderPath, sz, bPix);
        if (mPix != null) WritePng(cfg.maskPath, sz, mPix);

        AssetDatabase.Refresh();

        SetupImporter(cfg.borderPath, sz);
        if (cfg.generateMask) SetupImporter(cfg.maskPath, sz);

        string maskInfo = cfg.generateMask ? $"\nMask:   {cfg.maskPath}" : "";
        Debug.Log($"[SDFGenerator] 생성 완료: {cfg.label}\nBorder: {cfg.borderPath}{maskInfo}");
    }

    // ─────────────────────────────────────────
    //  SDF 디스패처
    // ─────────────────────────────────────────

    private static float SDF(float px, float py, BorderConfig cfg)
    {
        return cfg.shape switch
        {
            ShapeType.RoundedRect => SDF_RoundedRect(px, py, cfg.rLarge, cfg.rSmall),
            ShapeType.Circle      => SDF_Circle(px, py),
            ShapeType.Diamond     => SDF_Diamond(px, py),
            ShapeType.Hexagon     => SDF_Hexagon(px, py),
            _                     => SDF_RoundedRect(px, py, cfg.rLarge, cfg.rSmall),
        };
    }

    private static float SDF_RoundedRect(float px, float py, float rLarge, float rSmall)
    {
        float r = (px < 0 == py >= 0) ? rLarge : rSmall;
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

    // IQ의 정육각형 SDF (inradius = 0.5, 플랫-탑 방향)
    private static float SDF_Hexagon(float px, float py)
    {
        const float kx = -0.866025404f, ky = 0.5f, kz = 0.577350269f;
        const float r  = 0.5f;
        float ax = Mathf.Abs(px), ay = Mathf.Abs(py);
        float m  = 2f * Mathf.Min(kx * ax + ky * ay, 0f);
        ax -= m * kx;
        ay -= m * ky;
        ax -= Mathf.Clamp(ax, -kz * r, kz * r);
        ay -= r;
        return Mathf.Sqrt(ax * ax + ay * ay) * Mathf.Sign(ay);
    }

    // ─────────────────────────────────────────
    //  유틸리티
    // ─────────────────────────────────────────

    private static void WritePng(string path, int sz, Color[] pixels)
    {
        var tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false);
        tex.SetPixels(pixels);
        tex.Apply();
        string dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
        File.WriteAllBytes(path, tex.EncodeToPNG());
        DestroyImmediate(tex);
    }

    private static void SetupImporter(string path, int size)
    {
        var ti = AssetImporter.GetAtPath(path) as TextureImporter;
        if (ti == null) return;
        ti.textureType         = TextureImporterType.Sprite;
        ti.spriteImportMode    = SpriteImportMode.Single;
        ti.spritePivot         = new Vector2(0.5f, 0.5f);
        ti.alphaIsTransparency = true;
        ti.filterMode          = FilterMode.Bilinear;
        ti.mipmapEnabled       = false;
        ti.maxTextureSize      = 2048;
        ti.spritePixelsPerUnit = 100;
        var ps = ti.GetDefaultPlatformTextureSettings();
        ps.maxTextureSize     = 2048;
        ps.textureCompression = TextureImporterCompression.Uncompressed;
        ti.SetPlatformTextureSettings(ps);
        ti.SaveAndReimport();
    }

    // 3개 레이어를 alpha-over 방식으로 합성 → 최종 RGBA 반환
    // 각 레이어는 [0,1] 알파 가중치(aX)와 색상(cX)을 가짐
    private static Color BlendLayers(float a0, Color c0, float a1, Color c1, float a2, Color c2)
    {
        // 레이어 순서: 외곽(0) → 중앙(1) → 내부(2), 모두 동일 레벨에서 가중 평균
        float wa   = a0 * c0.a + a1 * c1.a + a2 * c2.a;
        float total = Mathf.Clamp01(wa);
        if (total < 0.001f) return Color.clear;

        float invW = 1f / (wa + 0.00001f);
        Color col;
        col.r = (c0.r * a0 * c0.a + c1.r * a1 * c1.a + c2.r * a2 * c2.a) * invW;
        col.g = (c0.g * a0 * c0.a + c1.g * a1 * c1.a + c2.g * a2 * c2.a) * invW;
        col.b = (c0.b * a0 * c0.a + c1.b * a1 * c1.a + c2.b * a2 * c2.a) * invW;
        col.a = total;
        return col;
    }

    private static float Clamp01(float v) => Mathf.Clamp01(v);
}
