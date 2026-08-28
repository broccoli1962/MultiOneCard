using System;
using System.IO;
using System.Linq;
using System.Reflection;
using GoogleSpreadSheetLoader.OneButton;
using UnityEditor;
using UnityEngine;

namespace GoogleSpreadSheetLoader.Generate
{
    public static partial class GSSL_Generate
    {
        public static void GenerateTableLinkerScript()
        {
            var sheetDataList = GSSL_DownloadedSheet.GetAllSheetData()
                .Where(x=>x.tableStyle == SheetData.eTableStyle.Common);
            var tableLinkerScriptPath = GSSL_Path.GetPath(ePath.TableLinkerScript);
            var declaration = "";

            foreach (var sheetData in sheetDataList)
            {
                var className = sheetData.title + "Table";

                declaration += $"\t\t public {className} {className};\n";
            }

            var contents =
                "using System.Collections.Generic;\n" +
                "using UnityEngine;\n" +
                "using UnityEngine.Serialization;\n\n" +
                "namespace TableData\n" +
                "{\n" +
                "    [CreateAssetMenu(fileName = \"TableLinker\", menuName = \"Tables/TableLinker\")]\n" +
                "    public class TableLinker : ScriptableObject\n" +
                "    {\n"
                + declaration
                + "\n    }\n}";
            var path = tableLinkerScriptPath + "TableLinker.cs";

            File.WriteAllText(path, contents);
        }
        
        public static void GenerateTableLinkerData()
        {
            var tableLinkerDataPath = GSSL_Path.GetPath(ePath.TableLinkerData);
            var tableLinkerAssetPath = tableLinkerDataPath + "TableLinker.asset";
            
            // 기존 파일이 있으면 삭제
            if (AssetDatabase.AssetPathExists(tableLinkerAssetPath))
            {
                AssetDatabase.DeleteAsset(tableLinkerAssetPath);
            }
            
            var tableLinkerAsset = ScriptableObject.CreateInstance("TableLinker");
            
            AssetDatabase.CreateAsset(tableLinkerAsset, tableLinkerAssetPath);

            if (AssignFirstMatchingAssets(tableLinkerAsset))
            {
                GSSL_OneButton.TableLinkerFlag = false;
            }
            
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
        
        /// <summary>
        /// 기존 테이블링커에 테이블들을 다시 연결합니다.
        /// </summary>
        public static void ReconnectTableLinker()
        {
            var tableLinkerDataPath = GSSL_Path.GetPath(ePath.TableLinkerData);
            var tableLinkerAssetPath = tableLinkerDataPath + "TableLinker.asset";
            
            // 기존 테이블링커가 있는지 확인
            if (!AssetDatabase.AssetPathExists(tableLinkerAssetPath))
            {
                Debug.LogWarning("테이블링커 asset이 존재하지 않습니다. 먼저 전체 최신화를 실행하세요.");
                return;
            }
            
            var tableLinkerAsset = AssetDatabase.LoadAssetAtPath(tableLinkerAssetPath, typeof(UnityEngine.Object));
            
            if (tableLinkerAsset == null)
            {
                Debug.LogWarning("테이블링커 asset을 로드할 수 없습니다.");
                return;
            }
            
            // 테이블 연결 재처리
            if (AssignFirstMatchingAssets(tableLinkerAsset))
            {
                Debug.Log("테이블링커 연결이 완료되었습니다.");
            }
            else
            {
                Debug.Log("연결할 새로운 테이블이 없습니다.");
            }
            
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
        
        private static bool AssignFirstMatchingAssets(UnityEngine.Object target)
        {
            if (target == null)
            {
                Debug.LogWarning("타겟 오브젝트가 null입니다.");
                return false;
            }

            Type targetType = target.GetType();

            SerializedObject serializedObject = new SerializedObject(target);
            SerializedProperty property = serializedObject.GetIterator();

            property.Next(true);
            property.Next(true);

            var anySet = false;
            while (property.NextVisible(false))
            {
                if (property.propertyType != SerializedPropertyType.ObjectReference) continue;
                
                Type propertyType = GetFieldType(targetType, property.name);

                if (propertyType == null || !typeof(UnityEngine.Object).IsAssignableFrom(propertyType)) continue;
                
                string[] guids = AssetDatabase.FindAssets($"t:{propertyType.Name}");

                if (guids.Length <= 0) continue;
                
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                UnityEngine.Object asset = AssetDatabase.LoadAssetAtPath(path, propertyType);
                property.objectReferenceValue = asset;
                anySet = true;
            }

            serializedObject.ApplyModifiedProperties();

            return anySet;
        }

        private static Type GetFieldType(Type targetType, string propertyName)
        {
            FieldInfo field = targetType.GetField(propertyName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
            if (field != null)
            {
                return field.FieldType;
            }

            // SerializedProperty는 필드명이 그대로 오지 않으므로 대체 시도
            foreach (var f in targetType.GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance))
            {
                if (ObjectNames.NicifyVariableName(f.Name) == propertyName)
                {
                    return f.FieldType;
                }
            }

            return null;
        }
    }
}