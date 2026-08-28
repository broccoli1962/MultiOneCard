using System.Collections.Generic;
using System.IO;
using Unity.Plastic.Newtonsoft.Json;
// ReSharper disable CheckNamespace

namespace GoogleSpreadSheetLoader.Generate
{
    public static partial class GSSL_Generate
    {
        public static void GenerateEnumDef(List<SheetData> sheets)
        {
            var listEnumInfo = new List<EnumInfo>();
            var enumDefSavePath = GSSL_Path.GetPath(ePath.Enum);
            
            foreach (var sheet in sheets)
            {
                var validColumns = new List<int>();
                var dicTargetList = new Dictionary<int, List<string>>();
                var sheetRows = JsonConvert.DeserializeObject<List<List<string>>>(sheet.data);

                if (sheetRows == null || sheetRows.Count < 2) continue;

                // 종류 별로 일단 담은
                var headers = sheetRows[0];
                for (var i = 0; i < headers.Count; i++)
                {
                    var enumName = headers[i];
                    var isIdx = enumName.Contains('-');
                    var enumTitle = isIdx ? enumName.Split('-')[0]: enumName;
                    
                    if (string.IsNullOrWhiteSpace(enumName))
                        continue;
                    
                    var info = listEnumInfo.Find(x => x.enumTitle == enumTitle);
                    if (info == null)
                    {
                        info = new EnumInfo()
                        {
                            enumTitle = enumName,
                            listName = new(),
                            listIdx = new(),
                        };

                        listEnumInfo.Add(info);
                    }

                    dicTargetList.Add(i, !isIdx ? info.listName : info.listIdx);
                    
                    validColumns.Add(i);
                }

                // 맨 앞줄은 이름이었으니 제외
                sheetRows.RemoveAt(0);
                
                // 그 후 enum들을 추가로 넣어줌
                foreach (var row in sheetRows)
                {
                    foreach (var column in validColumns)
                    {
                        if (row.Count <= column)
                        {
                            break;
                        }
                        
                        if(string.IsNullOrEmpty(row[column]))
                            continue;

                        dicTargetList[column].Add(row[column]);
                    }
                }
            }
            
            // 만든 string List를 토대로 enum 작성
            foreach (var info in listEnumInfo)
            {
                var data = "\n";
                var dataFilePath = enumDefSavePath + $"{info.enumTitle}.cs";
                var listName = info.listName;
                var listIdx = info.listIdx;
                    
                data += $"\npublic enum {info.enumTitle}\n{{\n";
                for (var i = 0; i < listName.Count; i++)
                {
                    data += $"\t{listName[i]} = {listIdx[i]},\n";
                }
                data += $"}}\n";
                File.WriteAllText(dataFilePath, data);
            }
        }
    }

    internal class EnumInfo
    {
        public string enumTitle;

        public List<string> listName;
        public List<string> listIdx;
    }
}