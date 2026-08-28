using System;

namespace GoogleSpreadSheetLoader
{
    [Serializable]
    public class SheetData
    {
        public enum eTableStyle
        {
            Common,
            EnumType,
            Localization,
        }
        
        public string spreadSheetId;
        public string title;
        public eTableStyle tableStyle;
        public string data;
    }
}