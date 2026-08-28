using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace GoogleSpreadSheetLoader
{
    public enum ePath
    {
        None,
        SettingData,
        TableScript,
        DataScript,
        Enum,
        TableData,
        TableLinkerScript,
        TableLinkerData,
    }
        
    public static class GSSL_Path
    {
        private static readonly Dictionary<ePath, string> _dicPath = new()
        {
            { ePath.SettingData, "Assets/GoogleSpreadSheetLoader/"},
            {ePath.TableScript, "Assets/GoogleSpreadSheetLoader/Generated/Script/TableScript/"},
            {ePath.DataScript, "Assets/GoogleSpreadSheetLoader/Generated/Script/DataScript/"},
            {ePath.Enum, "Assets/GoogleSpreadSheetLoader/Generated/Script/Enum/"},
            {ePath.TableData, "Assets/GoogleSpreadSheetLoader/Generated/SerializeObject/TableData/"},
            {ePath.TableLinkerScript, "Assets/GoogleSpreadSheetLoader/Generated/Script/"},
            {ePath.TableLinkerData, "Assets/Resources/"},
        };

        public static string GetPath(ePath path)
        {
            if (_dicPath.TryGetValue(path, out var result))
            {
                if (!Directory.Exists(result))
                    Directory.CreateDirectory(result);
                
                return result;
            }
            
            Debug.LogError($"GSSL_Path : 정의 되지 않은 패스 ePath({path})");
            return "";
        }

        public static void ClearGeneratedFolder()
        {
            var generatedPath = "Assets/GoogleSpreadSheetLoader/Generated";
            
            if (Directory.Exists(generatedPath))
            {
                // Generated 폴더 내의 모든 파일과 폴더 삭제
                var files = Directory.GetFiles(generatedPath, "*", SearchOption.AllDirectories);
                foreach (var file in files)
                {
                    if (file.EndsWith(".meta")) continue; // .meta 파일은 건너뛰기
                    
                    var assetPath = file.Replace('\\', '/');
                    AssetDatabase.DeleteAsset(assetPath);
                }
                
                // 빈 폴더들 삭제 (깊은 폴더부터 삭제하기 위해 역순으로)
                var directories = Directory.GetDirectories(generatedPath, "*", SearchOption.AllDirectories).Reverse();
                foreach (var dir in directories)
                {
                    var dirPath = dir.Replace('\\', '/');
                    if (Directory.Exists(dirPath) && Directory.GetFiles(dirPath).Length == 0 && Directory.GetDirectories(dirPath).Length == 0)
                    {
                        AssetDatabase.DeleteAsset(dirPath);
                    }
                }
                
                AssetDatabase.Refresh();
                Debug.Log("Generated 폴더가 정리되었습니다.");
            }
        }
    }
}