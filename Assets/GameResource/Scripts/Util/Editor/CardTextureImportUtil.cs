using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Backend.Util.Editor
{
    /// <summary>
    /// 인게임 카드 PNG TextureImporter 를 선명 설정으로 맞춘다.
    /// GPU 압축·Crunch 를 끄고(이중 압축 방지), Bilinear+밉맵으로 손패 축소 시 문양을 읽기 쉽게 한다.
    /// </summary>
    public static class CardTextureImportUtil
    {
        private const string CardsFolder = "Assets/GameResource/Data/Cards";
        private const int MaxTextureSize = 2048;
        private static readonly string[] Platforms = { "Standalone", "Android", "WebGL", "iPhone" };

        /// <summary>
        /// Cards 폴더(Source 제외) PNG 전부포트 설정을 일괄 적용한다.
        /// </summary>
        [MenuItem("Tools/Cards/Apply Sharp Texture Import Settings")]
        public static void ApplySharpSettingsMenu()
        {
            var count = ApplySharpSettings();
            EditorUtility.DisplayDialog(
                "Card Texture Import",
                $"Applied sharp import settings to {count} card textures.",
                "OK");
        }

        /// <summary>
        /// Cards 폴더 PNG 에 Uncompressed / Bilinear / mipmaps 를 적용하고 재임포트한다.
        /// </summary>
        public static int ApplySharpSettings()
        {
            var guids = AssetDatabase.FindAssets("t:Texture2D", new[] { CardsFolder });
            var paths = new List<string>();
            for (var i = 0; i < guids.Length; i++)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (path.Contains("/Source/") || !path.EndsWith(".png", System.StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                paths.Add(path);
            }

            try
            {
                AssetDatabase.StartAssetEditing();
                for (var i = 0; i < paths.Count; i++)
                {
                    var path = paths[i];
                    EditorUtility.DisplayProgressBar(
                        "Card Texture Import",
                        path,
                        paths.Count == 0 ? 1f : (float)i / paths.Count);
                    ApplyToPath(path, reimport: false);
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                EditorUtility.ClearProgressBar();
            }

            for (var i = 0; i < paths.Count; i++)
            {
                AssetDatabase.ImportAsset(paths[i], ImportAssetOptions.ForceUpdate);
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[CardTextureImportUtil] Applied sharp settings to {paths.Count} textures under {CardsFolder}");
            return paths.Count;
        }

        /// <summary>
        /// 단일 카드 텍스처 경로에 선명 임포트 설정을 적용한다.
        /// </summary>
        public static bool ApplyToPath(string path, bool reimport = true)
        {
            var ti = AssetImporter.GetAtPath(path) as TextureImporter;
            if (ti == null)
            {
                return false;
            }

            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.alphaIsTransparency = true;
            ti.mipmapEnabled = true;
            ti.mipMapBias = -0.5f;
            ti.filterMode = FilterMode.Bilinear;
            ti.anisoLevel = 1;
            ti.wrapMode = TextureWrapMode.Clamp;
            ti.npotScale = TextureImporterNPOTScale.None;
            ti.isReadable = false;
            ti.sRGBTexture = true;
            ti.maxTextureSize = MaxTextureSize;

            var settings = new TextureImporterSettings();
            ti.ReadTextureSettings(settings);
            settings.textureType = TextureImporterType.Sprite;
            settings.spriteMode = (int)SpriteImportMode.Single;
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.alphaIsTransparency = true;
            settings.mipmapEnabled = true;
            settings.filterMode = FilterMode.Bilinear;
            settings.aniso = 1;
            settings.wrapMode = TextureWrapMode.Clamp;
            settings.npotScale = TextureImporterNPOTScale.None;
            ti.SetTextureSettings(settings);

            var def = ti.GetDefaultPlatformTextureSettings();
            ApplyPlatformUncompressed(def, overridden: false);
            ti.SetPlatformTextureSettings(def);

            for (var i = 0; i < Platforms.Length; i++)
            {
                var ps = ti.GetPlatformTextureSettings(Platforms[i]);
                ApplyPlatformUncompressed(ps, overridden: true);
                ti.SetPlatformTextureSettings(ps);
            }

            if (reimport)
            {
                ti.SaveAndReimport();
            }
            else
            {
                EditorUtility.SetDirty(ti);
            }

            return true;
        }

        private static void ApplyPlatformUncompressed(TextureImporterPlatformSettings ps, bool overridden)
        {
            ps.overridden = overridden;
            ps.maxTextureSize = MaxTextureSize;
            ps.resizeAlgorithm = TextureResizeAlgorithm.Mitchell;
            ps.format = TextureImporterFormat.RGBA32;
            ps.textureCompression = TextureImporterCompression.Uncompressed;
            ps.crunchedCompression = false;
            ps.compressionQuality = 100;
            ps.allowsAlphaSplitting = false;
        }
    }
}
