using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;

namespace GoogleSpreadSheetLoader.Generate
{
    public static partial class GSSL_Generate
    {
        private static readonly string localizePath = "Assets/Resources/";
        
        public static void GenerateLocalize(List<SheetData> sheets)
        {
            if (!Directory.Exists(localizePath))
            {
                Directory.CreateDirectory(localizePath);
            }

            var dicLocalizeHeader = new Dictionary<string, int>();
            var dicLocalize = new Dictionary<int, List<string>>(); 
            
            foreach (var sheet in sheets)
            {
                var validColumns = new List<int>();

                var sheetRows = JsonConvert.DeserializeObject<List<List<string>>>(sheet.data);

                if (sheetRows == null || sheetRows.Count < 2) continue;

                var headers = sheetRows[0];
                for (var i = 0; i < headers.Count; i++)
                {
                    if (string.IsNullOrWhiteSpace(headers[i]) || !headers[i].Contains("-"))
                        continue;

                    var splitHeader = headers[i].Split('-');
                    if (splitHeader.Length < 2) continue;

                    var varName = splitHeader[0].Trim();
                    dicLocalizeHeader.TryAdd(varName, i);
                    dicLocalize.TryAdd(i, new List<string>());

                    validColumns.Add(i);
                }

                sheetRows.RemoveAt(0);
                foreach (var row in sheetRows)
                {
                    foreach (var column in validColumns)
                    {
                        dicLocalize[column].Add(row[column]);
                    }
                }
            }

            var checkedId = false;
            var idList = new List<string>();
            foreach (var (header, idx) in dicLocalizeHeader)
            {
                if (!checkedId && header.ToLower() == "id")
                {
                    checkedId = true;
                    idList = dicLocalize[idx];
                    continue;
                }
                
                var localizeList = new List<LocalizeKeyValue>();
                var usedIds = new HashSet<string>();
                var targetList = dicLocalize[idx];
                
                for (var i = 0; i < idList.Count; i++)
                {
                    if (!usedIds.Add(idList[i]))
                    {
                        Debug.LogError($"Duplicate localize id - {idList[i]}");
                        continue;
                    }

                    localizeList.Add(new LocalizeKeyValue
                    {
                        Key = idList[i],
                        Value = targetList[i]
                    });
                }

                var contents = JsonConvert.SerializeObject(localizeList);
                
                File.WriteAllText(localizePath + $"Localize_{header}.json", contents);
            }
        }
    }
}