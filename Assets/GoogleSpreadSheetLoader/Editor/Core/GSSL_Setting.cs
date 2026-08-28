using UnityEditor;
using UnityEngine;
// ReSharper disable InconsistentNaming
// ReSharper disable ClassNeverInstantiated.Global

namespace GoogleSpreadSheetLoader.Setting
{
    public class GSSL_Setting
    {
        private static readonly string _settingDataAssetName = "SettingData.asset";
        public static bool AdvanceMode => true;
        public static SettingData SettingData => _settingData;
        private static SettingData _settingData;
        

        [InitializeOnLoadMethod]
        private static void ResetStaticInstance()
        {
            _settingData = null;
        }

        public static bool CheckAndCreate()
        {
            var settingDataPath = GSSL_Path.GetPath(ePath.SettingData) + _settingDataAssetName;

            if (_settingData == null)
            {
                // 없으면 파일 생성
                if (!AssetDatabase.AssetPathExists(settingDataPath))
                {
                    var obj = ScriptableObject.CreateInstance<SettingData>();
                    AssetDatabase.CreateAsset(obj, settingDataPath);
                }

                _settingData = AssetDatabase.LoadAssetAtPath<SettingData>(settingDataPath);
            }

            return _settingData != null;
        }
    }
}