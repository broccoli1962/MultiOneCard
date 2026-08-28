using System.Collections.Generic;

namespace GoogleSpreadSheetLoader
{
    public static class GSSL_DownloadedSheet
    {
        // <SpreadSheetId, List<SheetData>>
        private static readonly Dictionary<string, List<SheetData>> _dicDownloadedSheet = new();

        public static List<SheetData> GetAllSheetData()
        {
            var allSheetData = new List<SheetData>();
            foreach (var sheetList in _dicDownloadedSheet.Values)
            {
                allSheetData.AddRange(sheetList);
            }
            
            return allSheetData;
        }

        public static void AddSheetData(SheetData sheetData)
        {
            _dicDownloadedSheet.TryAdd(sheetData.spreadSheetId, new());
            _dicDownloadedSheet[sheetData.spreadSheetId].Add(sheetData);
        }

        public static void ClearAllSheetData()
        {
            _dicDownloadedSheet.Clear();
        }
    }
}