using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;

namespace Backend.Object.UI.Editor
{
    /// <summary>카드와 오페라 UI 원본을 용도별 아틀라스에 연결한다.</summary>
    internal sealed class OneCardSpriteAtlases : IPreprocessBuildWithReport
    {
        private const string ATLAS_DIR = "Assets/GameResource/Atlas";
        private const string CARDS_DIR = "Assets/GameResource/Data/Cards";
        private const string OPERA_DIR = "Assets/GameResource/Images/GameUI/OperaMinimal";

        public int callbackOrder => -1000;

        /// <summary>빌드 전에 추가되거나 빠진 원본만 반영한다.</summary>
        public void OnPreprocessBuild(BuildReport report) => Synchronize();

        /// <summary>기존 스프라이트와 Addressables 키를 유지한 채 패킹 대상을 동기화한다.</summary>
        public static void Synchronize()
        {
            var groups = new SortedDictionary<string, List<string>>(StringComparer.Ordinal);
            void Add(string key, string path)
            {
                if (!groups.TryGetValue(key, out var assets))
                    groups[key] = assets = new List<string>();
                assets.Add(path.Replace('\\', '/'));
            }

            foreach (var path in Directory.GetFiles(CARDS_DIR, "*.png"))
            {
                var name = Path.GetFileNameWithoutExtension(path);
                string group;
                if (name == "BACK") group = "Back";
                else if (name.StartsWith("SPEC_", StringComparison.Ordinal) || name.StartsWith("JOKER_", StringComparison.Ordinal)) group = "Special";
                else group = name[0] switch
                {
                    'S' => "Spades", 'H' => "Hearts", 'D' => "Diamonds", 'C' => "Clubs",
                    'R' => "Stars", 'M' => "Moons",
                    _ => throw new InvalidDataException($"Unknown card sprite: {path}")
                };
                Add("Cards_" + group, path);
            }
            foreach (var path in Directory.GetFiles(OPERA_DIR, "*.png"))
            {
                var name = Path.GetFileNameWithoutExtension(path);
                Add(name == "TableFelt" || name == "TableRim" ? "Opera_Table" : "Opera_Controls", path);
            }
            foreach (var group in groups)
                UpdateAtlas(group.Key, group.Value);
        }

        private static void UpdateAtlas(string name, List<string> paths)
        {
            var path = $"{ATLAS_DIR}/{name}.spriteatlasv2";
            paths.Sort(StringComparer.Ordinal);
            var existing = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(path);
            if (existing != null && existing.GetPackables().Select(AssetDatabase.GetAssetPath)
                    .OrderBy(value => value, StringComparer.Ordinal).SequenceEqual(paths))
                return;

            var atlas = new SpriteAtlasAsset();
            atlas.Add(paths.Select(AssetDatabase.LoadAssetAtPath<Texture2D>).Cast<UnityEngine.Object>().ToArray());
            SpriteAtlasAsset.Save(atlas, path);
            AssetDatabase.ImportAsset(path);
            var importer = (SpriteAtlasImporter)AssetImporter.GetAtPath(path);
            var packing = importer.packingSettings;
            packing.enableRotation = false;
            packing.enableTightPacking = false;
            packing.padding = 4;
            importer.packingSettings = packing;
            var texture = importer.textureSettings;
            texture.filterMode = FilterMode.Bilinear;
            texture.generateMipMaps = false;
            importer.textureSettings = texture;
            var platform = importer.GetPlatformSettings("DefaultTexturePlatform");
            platform.maxTextureSize = name.StartsWith("Cards_", StringComparison.Ordinal) ? 4096 : 2048;
            importer.SetPlatformSettings(platform);
            importer.includeInBuild = true;
            importer.SaveAndReimport();
        }
    }
}
