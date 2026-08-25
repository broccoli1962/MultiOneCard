using UnityEngine;
using UnityEditor;

namespace Backend.Util.Addressable
{
    [CreateAssetMenu(fileName = "AddressableGenSettings", menuName = "Addressables/Generator Settings")]
    public class AddressableGenSettings : ScriptableObject
    {
        [Header("Set Root Namespace")]
        public string rootNameSpace = "Backend";

        [Header("키 스크립트가 생성될 폴더")]
        public DefaultAsset targetFolder;

        public string GetFolderPath()
        {
            if (targetFolder != null)
            {
                return AssetDatabase.GetAssetPath(targetFolder);
            }
            return "Assets";
        }
    }
}