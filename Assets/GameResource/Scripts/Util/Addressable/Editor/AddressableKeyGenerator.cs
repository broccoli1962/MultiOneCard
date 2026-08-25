using Backend.Util.Addressable;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEngine;

namespace Backend.Editor
{
    public static class AddressableKeyGenerator
    {
        private const string FileName = "AddressableKeys.cs";
        private const string ClassName = "AddressableKeys";

        [MenuItem("Tools/Addressables/Force Generate Keys")]
        public static void Generate()
        {
            string[] guids = AssetDatabase.FindAssets("t:AddressableGenSettings");
            if (guids.Length == 0)
            {
                Debug.LogError("AddressableGenSettings 에셋을 생성해주세요.");
                return;
            }

            var genSettings = AssetDatabase.LoadAssetAtPath<AddressableGenSettings>(AssetDatabase.GUIDToAssetPath(guids[0]));
            var addrSettings = AddressableAssetSettingsDefaultObject.Settings;

            if (addrSettings == null) return;

            string folderPath = genSettings.GetFolderPath();
            string rootNameSpace = genSettings.rootNameSpace.Trim();
            string subNameSpace = "AddressableKey";
            string finalNameSpace = string.IsNullOrEmpty(rootNameSpace) ? $"Backend.AddressableKey" : $"{rootNameSpace}.{subNameSpace}";

            bool useNamespace = !string.IsNullOrEmpty(finalNameSpace);
            string tap = useNamespace ? "    " : "";

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("// Auto Generate Code.");
            sb.AppendLine("using System.Collections.Generic;");
            sb.AppendLine();

            if (useNamespace)
            {
                sb.AppendLine($"namespace {finalNameSpace}");
                sb.AppendLine("{");
            }

            sb.AppendLine($"{tap}public static class {ClassName}");
            sb.AppendLine($"{tap}{{");

            foreach (var group in addrSettings.groups)
            {
                if (group == null || group.ReadOnly) continue;

                sb.AppendLine($"{tap}    public static class {FormatName(group.Name)}");
                sb.AppendLine($"{tap}    {{");

                sb.AppendLine($"{tap}        private static readonly Dictionary<string, string> Keys = new Dictionary<string, string>()");
                sb.AppendLine($"{tap}        {{");

                // 생성된 키 추적 (이름이 같은 파일이 다른 하위 폴더에 있을 때 변수명 중복 방지)
                HashSet<string> generatedKeys = new HashSet<string>();

                foreach (var entry in group.entries)
                {
                    // 💡 엔트리가 '폴더'인지 검사
                    if (AssetDatabase.IsValidFolder(entry.AssetPath))
                    {
                        // 폴더 내부의 모든 파일 탐색
                        string[] assetGuids = AssetDatabase.FindAssets("", new[] { entry.AssetPath });
                        foreach (var guid in assetGuids)
                        {
                            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                            if (AssetDatabase.IsValidFolder(assetPath)) continue; // 하위 폴더 자체는 스킵

                            // 파일 이름을 추출해 변수명으로 사용 (예: "Hero.prefab" -> "Hero")
                            string fileName = Path.GetFileNameWithoutExtension(assetPath);
                            string keyName = FormatName(fileName);

                            // 중복 이름 방지 로직 (같은 이름이면 Hero_1, Hero_2 식으로 넘버링)
                            int suffix = 1;
                            string finalKeyName = keyName;
                            while (generatedKeys.Contains(finalKeyName))
                            {
                                finalKeyName = $"{keyName}_{suffix}";
                                suffix++;
                            }
                            generatedKeys.Add(finalKeyName);

                            var resolveEntry = addrSettings.FindAssetEntry(guid, true);
                            if (resolveEntry == null || string.IsNullOrEmpty(resolveEntry.address))
                                continue;

                            if (group.Name == "Cards")
                            {
                                finalKeyName = KeyNameForEntry(group.Name, resolveEntry.address);
                                if (!generatedKeys.Add(finalKeyName))
                                    continue;
                            }

                            // 폴더로 넣었을 때 내부 에셋의 어드레스는 '전체 경로'임
                            sb.AppendLine($"{tap}            {{ \"{finalKeyName}\", \"{resolveEntry.address}\" }},");
                        }
                    }
                    else
                    {
                        // 💡 엔트리가 '개별 파일'인 경우 기존 로직
                        string keyName = KeyNameForEntry(group.Name, entry.address);
                        if (!generatedKeys.Contains(keyName))
                        {
                            generatedKeys.Add(keyName);
                            sb.AppendLine($"{tap}            {{ \"{keyName}\", \"{entry.address}\" }},");
                        }
                    }
                }

                sb.AppendLine($"{tap}        }};");
                sb.AppendLine();
                sb.AppendLine($"{tap}        public static string Get<T>() => Keys.TryGetValue(typeof(T).Name, out var key) ? key : null;");
                sb.AppendLine($"{tap}        public static string Get(string keyName) => Keys.TryGetValue(keyName, out var key) ? key : null;");
                sb.AppendLine($"{tap}    }}");
                sb.AppendLine();
            }
            sb.AppendLine($"{tap}}}");

            if (useNamespace) sb.AppendLine("}");

            string relativePath = Path.Combine(folderPath, FileName).Replace('\\', '/');
            string fullPath = Path.Combine(Directory.GetCurrentDirectory(), relativePath);
            
            string directory = Path.GetDirectoryName(fullPath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(fullPath, sb.ToString(), Encoding.UTF8);

            AssetDatabase.ImportAsset(relativePath);
            AssetDatabase.Refresh();

            Debug.Log($"[Addressables] {relativePath} 생성 완료!");
        }

        private static string KeyNameForEntry(string groupName, string address)
        {
            if (groupName == "Cards" && !string.IsNullOrEmpty(address) && address.StartsWith("Cards/"))
            {
                return address.Substring("Cards/".Length);
            }

            return FormatName(address);
        }

        private static string FormatName(string name)
        {
            if (string.IsNullOrEmpty(name)) return "Unknown";
            string result = name.Replace(" ", "_").Replace("-", "_").Replace(".", "_").Replace("/", "_");
            if (char.IsDigit(result[0])) result = "_" + result;
            return result;
        }
    }
}