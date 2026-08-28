using System;
using System.Collections.Generic;
using System.Linq;
using GoogleSpreadSheetLoader.OneButton;
using UnityEditor;
using UnityEngine;
using static GoogleSpreadSheetLoader.GSSL_State;

namespace GoogleSpreadSheetLoader.Setting
{
    public class IndividualView
    {
        private Vector2 _scrollPosition;
        private readonly Dictionary<string, bool> _selectedSheets = new();
        private bool _selectAll = false;
        private string _searchFilter = "";
        
        public void DrawIndividualView()
        {
            DrawCachedSheetsBox();
            EditorGUILayout.Space(5);
            DrawActionButtons();
        }
        
        private void DrawCachedSheetsBox()
        {
            // 박스 좌우 여백
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.Space(5);
            
            // 캐시된 시트 박스 시작
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            // 제목
            EditorGUILayout.LabelField("다운로드된 시트 목록", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);
            
            DrawSearchAndControls();
            
            EditorGUILayout.Space(5);
            
            DrawCachedSheetsList();
            
            EditorGUILayout.Space(5);
            
            // 캐시된 시트 박스 끝
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.Space(5);
            EditorGUILayout.EndHorizontal();
        }
        
        private void DrawSearchAndControls()
        {
            // 검색 필터
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("검색:", GUILayout.Width(40));
            _searchFilter = EditorGUILayout.TextField(_searchFilter);
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(3);
            
            // 전체 선택/해제 및 캐시 관리
            EditorGUILayout.BeginHorizontal();
            
            bool newSelectAll = EditorGUILayout.Toggle("전체 선택", _selectAll, GUILayout.Width(80));
            if (newSelectAll != _selectAll)
            {
                _selectAll = newSelectAll;
                UpdateAllSelection();
            }
            
            GUILayout.FlexibleSpace();
            
            if (GUILayout.Button("캐시 새로고침", GUILayout.Width(100)))
            {
                RefreshCacheList();
            }
            
            if (GUILayout.Button("캐시 전체 삭제", GUILayout.Width(100)))
            {
                if (EditorUtility.DisplayDialog("캐시 삭제 확인", "모든 캐시된 시트를 삭제하시겠습니까?", "삭제", "취소"))
                {
                    GSSL_CacheManager.ClearCache();
                    RefreshCacheList();
                }
            }
            
            EditorGUILayout.EndHorizontal();
        }
        
        private void DrawCachedSheetsList()
        {
            var filteredSheets = GetFilteredSheets();
            
            if (filteredSheets == null || filteredSheets.Count == 0)
            {
                if (string.IsNullOrEmpty(_searchFilter))
                {
                    EditorGUILayout.LabelField("다운로드된 시트가 없습니다.", EditorStyles.centeredGreyMiniLabel);
                    EditorGUILayout.LabelField("먼저 '전체 최신화' 탭에서 스프레드시트를 다운로드하세요.", EditorStyles.centeredGreyMiniLabel);
                }
                else
                {
                    EditorGUILayout.LabelField("검색 결과가 없습니다.", EditorStyles.centeredGreyMiniLabel);
                }
                return;
            }
            
            // 스크롤 영역 시작
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, GUILayout.Height(300));
            
            foreach (var cacheInfo in filteredSheets)
            {
                DrawSheetItem(cacheInfo);
            }
            
            EditorGUILayout.EndScrollView();
            
            // 선택된 시트 수 표시 (필터링된 시트 중에서만)
            var selectedCount = filteredSheets.Count(sheet => _selectedSheets.GetValueOrDefault(sheet.sheetName, false));
            EditorGUILayout.LabelField($"선택된 시트: {selectedCount} / {filteredSheets.Count}", EditorStyles.centeredGreyMiniLabel);
        }
        
        private void DrawSheetItem(GSSL_CacheManager.CacheInfo cacheInfo)
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            
            // 체크박스
            bool isSelected = _selectedSheets.GetValueOrDefault(cacheInfo.sheetName, false);
            bool newSelection = EditorGUILayout.Toggle(isSelected, GUILayout.Width(20));
            if (newSelection != isSelected)
            {
                _selectedSheets[cacheInfo.sheetName] = newSelection;
                UpdateSelectAllState();
            }
            
            // 시트 정보
            EditorGUILayout.BeginVertical();
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"시트: {cacheInfo.sheetName}", EditorStyles.boldLabel, GUILayout.Width(200));
            EditorGUILayout.LabelField($"타입: {cacheInfo.tableStyle}", GUILayout.Width(120));
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"스프레드시트: {cacheInfo.spreadSheetName}", GUILayout.Width(200));
            EditorGUILayout.LabelField($"최종 업데이트: {cacheInfo.LastUpdatedDateTime:yyyy-MM-dd HH:mm}", GUILayout.Width(150));
            GUILayout.FlexibleSpace();
            
            // 개별 액션 버튼들
            if (GUILayout.Button("열기", GUILayout.Width(50)))
            {
                Application.OpenURL(string.Format(GSSL_URL.SpreadSheetOpenUrl, cacheInfo.spreadSheetId));
            }
            
            if (GUILayout.Button("삭제", GUILayout.Width(50)))
            {
                if (EditorUtility.DisplayDialog("캐시 삭제 확인", $"'{cacheInfo.sheetName}' 시트를 캐시에서 삭제하시겠습니까?", "삭제", "취소"))
                {
                    GSSL_CacheManager.RemoveFromCache(cacheInfo.sheetName);
                    _selectedSheets.Remove(cacheInfo.sheetName);
                    RefreshCacheList();
                }
            }
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.EndHorizontal();
        }
        
        private void DrawActionButtons()
        {
            if (CurrState != eGSSL_State.None)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                EditorGUILayout.LabelField(ProgressText, GUILayout.Width(ProgressText.Length * 12));
                EditorGUILayout.Space(30);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.Space(30);

                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("취소", GUILayout.Width(180)))
                {
                    GSSL_OneButton.CancelCurrentProcess();
                }
                EditorGUILayout.Space(30);
                EditorGUILayout.EndHorizontal();

                return;
            }
            
            var selectedSheets = _selectedSheets.Where(kvp => kvp.Value).Select(kvp => kvp.Key).ToList();
            
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            GUI.enabled = selectedSheets.Count > 0;
            
            if (GUILayout.Button($"선택된 시트 최신화 ({selectedSheets.Count}개)", GUILayout.Width(200)))
            {
                _ = GSSL_OneButton.IndividualUpdateSelectedSheets(selectedSheets);
            }
            
            EditorGUILayout.Space(30);
            
            if (GUILayout.Button("선택된 시트 스크립터블 오브젝트 재생성", GUILayout.Width(250)))
            {
                _ = GSSL_OneButton.RegenerateSelectedSheets(selectedSheets);
            }
            
            GUI.enabled = true;
            
            EditorGUILayout.Space(10);
            
            if (GUILayout.Button("테이블링커 연결", GUILayout.Width(120)))
            {
                _ = GSSL_OneButton.ReconnectTableLinker();
            }
            
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);

            DrawQuickUpdateButtons();
        }

        /// <summary>
        /// 특정 네이밍 규칙(string / Type)이 붙은 시트들을 한 번에 최신화하는 빠른 버튼들
        /// </summary>
        private void DrawQuickUpdateButtons()
        {
            var cachedSheets = GSSL_CacheManager.GetAllCachedSheets();
            if (cachedSheets == null || cachedSheets.Count == 0)
            {
                return;
            }

            var stringSheets = cachedSheets
                .Where(sheet => sheet.sheetName != null &&
                                sheet.sheetName.ToLower().Contains("string"))
                .Select(sheet => sheet.sheetName)
                .Distinct()
                .ToList();

            var typeSheets = cachedSheets
                .Where(sheet => sheet.sheetName != null &&
                                sheet.sheetName.ToLower().Contains("type"))
                .Select(sheet => sheet.sheetName)
                .Distinct()
                .ToList();

            if (stringSheets.Count == 0 && typeSheets.Count == 0)
            {
                return;
            }

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            // "string" 이 붙은 시트들 재생성
            GUI.enabled = stringSheets.Count > 0;
            if (GUILayout.Button($"\"string\" 시트 최신화 ({stringSheets.Count}개)", GUILayout.Width(220)))
            {
                _ = GSSL_OneButton.RegenerateSelectedSheets(stringSheets);
            }

            EditorGUILayout.Space(10);

            // "Type" 이 붙은 시트들 재생성
            GUI.enabled = typeSheets.Count > 0;
            if (GUILayout.Button($"\"Type\" 시트 최신화 ({typeSheets.Count}개)", GUILayout.Width(220)))
            {
                _ = GSSL_OneButton.RegenerateSelectedSheets(typeSheets);
            }

            GUI.enabled = true;

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }
        
        private List<GSSL_CacheManager.CacheInfo> GetFilteredSheets()
        {
            var cachedSheets = GSSL_CacheManager.GetAllCachedSheets();
            
            if (cachedSheets == null || cachedSheets.Count == 0)
            {
                return new List<GSSL_CacheManager.CacheInfo>();
            }
            
            // 검색 필터 적용
            if (!string.IsNullOrEmpty(_searchFilter))
            {
                return cachedSheets.Where(sheet => 
                    sheet.sheetName.ToLower().Contains(_searchFilter.ToLower()) ||
                    sheet.spreadSheetName.ToLower().Contains(_searchFilter.ToLower())
                ).ToList();
            }
            
            return cachedSheets;
        }
        
        private void UpdateAllSelection()
        {
            // 검색 필터가 적용된 시트들만 선택/해제
            var filteredSheets = GetFilteredSheets();
            
            foreach (var sheet in filteredSheets)
            {
                _selectedSheets[sheet.sheetName] = _selectAll;
            }
        }
        
        private void UpdateSelectAllState()
        {
            // 검색 필터가 적용된 시트들을 기준으로 전체선택 상태 확인
            var filteredSheets = GetFilteredSheets();
            if (filteredSheets.Count == 0)
            {
                _selectAll = false;
                return;
            }
            
            var selectedCount = filteredSheets.Count(sheet => _selectedSheets.GetValueOrDefault(sheet.sheetName, false));
            _selectAll = selectedCount == filteredSheets.Count;
        }
        
        private void RefreshCacheList()
        {
            _selectedSheets.Clear();
            _selectAll = false;
        }
    }
}
