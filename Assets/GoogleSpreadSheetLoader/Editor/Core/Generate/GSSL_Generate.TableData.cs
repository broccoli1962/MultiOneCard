using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using TableData;
using UnityEditor;
using UnityEngine;

namespace GoogleSpreadSheetLoader.Generate
{
    public static partial class GSSL_Generate
    {
        public static void GenerateTableScripts(List<SheetData> sheets)
        {
            var tableScriptSavePath = GSSL_Path.GetPath(ePath.TableScript);
            var dataScriptSavePath = GSSL_Path.GetPath(ePath.DataScript);
            
            foreach (var sheet in sheets)
            {
                var tableTitle = sheet.title;
                var dataClassName = tableTitle + "Data";
                var tableClassName = tableTitle + "Table";
                var dataFilePath = dataScriptSavePath + dataClassName + ".cs";
                var tableFilePath = tableScriptSavePath + tableClassName + ".cs";

                var variableDeclarations = new List<string>();

                var sheetRows = JsonConvert.DeserializeObject<List<List<string>>>(sheet.data);

                if (sheetRows == null || sheetRows.Count < 2) continue;

                var setData = "";
                var headers = sheetRows[0];
                for (var i = 0; i < headers.Count; i++)
                {
                    if (string.IsNullOrWhiteSpace(headers[i]) || !headers[i].Contains("-"))
                        continue;

                    var splitHeader = headers[i].Split('-');
                    if (splitHeader.Length < 2) continue;

                    var varName = splitHeader[0].Trim();
                    var varType = ConvertToCSharpType(splitHeader[1].Trim());

                    variableDeclarations.Add($"    public {varType} {varName} => _{varName};\n");
                    variableDeclarations.Add($"    [SerializeField] private {varType} _{varName};\n\n");

                    if (varType == "string")
                    {
                        // 문자열의 경우, 셀이 비어있거나 행의 길이가 짧아도 안전하게 빈 문자열로 처리
                        setData += $"\t\t_{varName} = data.Count > {i} ? data[{i}] : string.Empty;\n";
                    }
                    else if (CheckEnumType(varType))
                    {
                        // Enum 및 기타 타입은 인덱스 범위와 빈 문자열을 체크한 뒤에만 파싱
                        setData += $"\t\tif (data.Count > {i} && !string.IsNullOrEmpty(data[{i}]))\n";
                        setData += "\t\t{\n";
                        setData += $"\t\t\t_{varName} = {varType}.Parse<{varType}>(data[{i}]);\n";
                        setData += "\t\t}\n";
                    }
                    else
                    {
                        // 숫자/기타 기본형도 인덱스와 빈 문자열을 체크해서 안전하게 처리
                        setData += $"\t\tif (data.Count > {i} && !string.IsNullOrEmpty(data[{i}]))\n";
                        setData += "\t\t{\n";
                        setData += $"\t\t\t_{varName} = {varType}.Parse(data[{i}]);\n";
                        setData += "\t\t}\n";
                    }
                }

                setData = "\tpublic void SetData(List<string> data)\n\t{\n" + $"{setData}" + "\t}\n";

                var dataClassTemplate = $"using System;\n"
                                           + $"using System.Collections.Generic;\n"
                                           + "using TableData;\n"
                                           + "using UnityEngine;\n"
                                           + "\n"
                                           + "[Serializable]\n"
                                           + $"public partial class {dataClassName} : {nameof(IData)}\n{{\n"
                                           + string.Join("", variableDeclarations)
                                           + string.Join("", setData)
                                           + "}\n";

                File.WriteAllText(dataFilePath, dataClassTemplate);

                setData = "\tpublic void SetData(List<List<string>> data)\n\t{\n"
                          + $"\t\tdataList = new List<{dataClassName}>();\n"
                          + $"\t\tforeach (var item in data)\n"
                          + $"\t\t{{\n\t\t\t{dataClassName} newData = new();\n"
                          + $"\t\t\tnewData.SetData(item);\n"
                          + $"\t\t\tdataList.Add(newData);\n\t\t}}"
                          + "\n\t}\n";

                var tableTemplate = $"using System.Collections.Generic;\n"
                                       + "using TableData;\n"
                                       + "using UnityEngine;\n"
                                       + "\n"
                                       + $"[CreateAssetMenu(fileName = \"{tableClassName}\", menuName = \"Tables/{tableClassName}\")]\n"
                                       + $"public class {tableClassName} : ScriptableObject, {nameof(ITable)}\n{{\n"
                                       + $"    public List<{dataClassName}> dataList = new List<{dataClassName}>();\n\n"
                                       + string.Join("", setData)
                                       + "}\n";

                File.WriteAllText(tableFilePath, tableTemplate);
            }
        }

        public static void GenerateTableData(List<SheetData> listSheet)
        {
            var dataSavePath = GSSL_Path.GetPath(ePath.TableData);
            foreach (var sheet in listSheet)
            {
                var tableAssetPath = dataSavePath + sheet.title + "Table.asset";
                var tableClassName = sheet.title + "Table";

                var sheetRows = JsonConvert.DeserializeObject<List<List<string>>>(sheet.data);

                if (sheetRows == null || sheetRows.Count < 2) continue;

                var tableType = FindTypeByName(tableClassName);
                if (tableType == null)
                {
                    Debug.LogError($"Failed to find type: {tableClassName}");
                    continue;
                }
                
                // 기존 파일이 있으면 삭제
                if (AssetDatabase.AssetPathExists(tableAssetPath))
                {
                    AssetDatabase.DeleteAsset(tableAssetPath);
                }
                
                var tableAsset = ScriptableObject.CreateInstance(tableType);
                if (tableAsset == null)
                {
                    Debug.LogError($"Failed to create instance of {tableClassName}");
                    continue;
                }

                sheetRows.RemoveAt(0);

                ((ITable)tableAsset).SetData(sheetRows);

                AssetDatabase.CreateAsset(tableAsset, tableAssetPath);
            }
        }

        private static string ConvertToCSharpType(string type)
        {
            return type switch
            {
                "int" => "int",
                "float" => "float",
                "bool" => "bool",
                "long" => "long",
                "double" => "double",
                "string" => "string",
                
                _ => type,
            };
        }
        
        private static bool CheckEnumType(string type)
        {
            return type switch
            {
                "int" => false,
                "float" => false,
                "bool" => false,
                "long" => false,
                "double" => false,
                "string" => false,
                
                _ => true,
            };
        }

        private static Type FindTypeByName(string typeName)
        {
            // 모든 로드된 어셈블리에서 검색
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    var type = assembly.GetType(typeName);
                    if (type != null)
                    {
                        Debug.Log($"Found type {typeName} in assembly {assembly.FullName}");
                        return type;
                    }
                    
                    // 어셈블리 내의 모든 타입을 검색
                    var types = assembly.GetTypes();
                    foreach (var t in types)
                    {
                        if (t.Name == typeName)
                        {
                            Debug.Log($"Found type {typeName} in assembly {assembly.FullName} by name search");
                            return t;
                        }
                    }
                }
                catch (Exception ex)
                {
                    // 일부 어셈블리에서 GetTypes()가 실패할 수 있으므로 무시
                    Debug.LogWarning($"Failed to get types from assembly {assembly.FullName}: {ex.Message}");
                }
            }
            
            Debug.LogError($"Type not found: {typeName}");
            return null;
        }
    }
}