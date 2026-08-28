using GoogleSpreadSheetLoader.Setting;
using UnityEngine;

namespace GoogleSpreadSheetLoader
{
    public static class GSSL_Log
    {
        public static void Log(string message)
        {
            if(!GSSL_Setting.AdvanceMode) return;
            
            Debug.Log(message);
        }
        
        public static void LogError(string message)
        {
            if(!GSSL_Setting.AdvanceMode) return;
            
            Debug.LogError(message);
        }
    }
}