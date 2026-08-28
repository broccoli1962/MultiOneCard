using System;
using GoogleSpreadSheetLoader.Setting;
using UnityEditor;
using UnityEngine;

namespace GoogleSpreadSheetLoader
{
    public class GSSL_EditorWindow : EditorWindow
    {
        private readonly TabbedView _tabbedView = new();
        
        [MenuItem("Tools/GSSL/Open Window", priority = 0)]
        public static void ShowWindow()
        {
            var window = GetWindow<GSSL_EditorWindow>(true, "Google Spread Sheet Loader");
            window.minSize = new Vector2(530, 600);
            window.ShowUtility();
        }

        private void OnEnable()
        {
            EditorApplication.update -= UpdateEditor;
            EditorApplication.update += UpdateEditor;
        }

        private void OnDisable()
        {
            EditorApplication.update -= UpdateEditor;
        }

        private DateTime _checkTime;
        
        private void UpdateEditor()
        {
            if((DateTime.Now - _checkTime).TotalSeconds > 1)
            {
                _checkTime = DateTime.Now;
                Repaint();
            }
        }
        
        private void OnGUI()
        {
            try
            {
                if (!GSSL_Setting.CheckAndCreate())
                {
                    EditorGUILayout.LabelField("설정 데이터를 초기화할 수 없습니다.", EditorStyles.centeredGreyMiniLabel);
                    return;
                }
                
                _tabbedView.DrawTabbedView();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"GSSL Editor Window Error: {e.Message}");
                EditorGUILayout.LabelField("에러가 발생했습니다. 콘솔을 확인해주세요.", EditorStyles.centeredGreyMiniLabel);
            }
        }
    }
}