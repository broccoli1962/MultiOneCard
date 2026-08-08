using Backend.Util.Management;
using TableData;
using UnityEngine;

namespace Backend.Object.Management
{
    public partial class TableManager : SingletonGameObject<TableManager>
    {
        //private TableLinker _tableLinker;

        private bool _isInitialized = false;
        public static bool IsInitialized => Instance != null && Instance._isInitialized;

        public static void Init() => Instance.Init_Internal();

        private void Init_Internal()
        {
            if (_isInitialized) return;

            //_tableLinker = Resources.Load<TableLinker>("TableLinker");

            //if (_tableLinker == null)
            //{
                //Debug.LogError("[TableManager] TableLinker를 Resources에서 찾을 수 없습니다.");
                //return;
            //}

            _isInitialized = true;
        }
    }
}
