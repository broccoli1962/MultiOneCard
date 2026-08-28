using GoogleSpreadSheetLoader.OneButton;
using UnityEditor;
using UnityEngine;
using static GoogleSpreadSheetLoader.GSSL_State;

namespace GoogleSpreadSheetLoader.Setting
{
    public class TabbedView
    {
        private enum eTab
        {
            전체최신화,
            개별최신화
        }
        
        private eTab _currentTab = eTab.전체최신화;
        private readonly IntegratedView _integratedView = new();
        private readonly IndividualView _individualView = new();
        
        public void DrawTabbedView()
        {
            // 윈도우 상단 여백
            EditorGUILayout.Space(5);
            
            DrawTabButtons();
            
            EditorGUILayout.Space(5);
            
            // 현재 선택된 탭에 따라 다른 뷰 그리기
            switch (_currentTab)
            {
                case eTab.전체최신화:
                    _integratedView.DrawIntegratedView();
                    break;
                case eTab.개별최신화:
                    _individualView.DrawIndividualView();
                    break;
            }
            
            // 윈도우 하단 여백
            EditorGUILayout.Space(5);
        }
        
        private void DrawTabButtons()
        {
            EditorGUILayout.BeginHorizontal();
            
            // 좌측 여백
            EditorGUILayout.Space(5);
            
            // 전체 최신화 탭
            var allUpdateStyle = _currentTab == eTab.전체최신화 ? EditorStyles.miniButtonLeft : EditorStyles.miniButtonLeft;
            if (_currentTab == eTab.전체최신화)
            {
                GUI.backgroundColor = Color.cyan;
            }
            
            if (GUILayout.Button("전체 최신화", allUpdateStyle, GUILayout.Width(150)))
            {
                _currentTab = eTab.전체최신화;
            }
            
            // 색상 리셋
            GUI.backgroundColor = Color.white;
            
            // 개별 최신화 탭
            var individualStyle = _currentTab == eTab.개별최신화 ? EditorStyles.miniButtonRight : EditorStyles.miniButtonRight;
            if (_currentTab == eTab.개별최신화)
            {
                GUI.backgroundColor = Color.cyan;
            }
            
            if (GUILayout.Button("개별 최신화", individualStyle, GUILayout.Width(150)))
            {
                _currentTab = eTab.개별최신화;
            }
            
            // 색상 리셋
            GUI.backgroundColor = Color.white;
            
            // 우측 여백
            GUILayout.FlexibleSpace();
            EditorGUILayout.Space(5);
            
            EditorGUILayout.EndHorizontal();
        }
    }
}
