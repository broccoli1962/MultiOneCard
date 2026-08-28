using GoogleSpreadSheetLoader.Auth;
using GoogleSpreadSheetLoader.OneButton;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using static GoogleSpreadSheetLoader.GSSL_State;

namespace GoogleSpreadSheetLoader.Setting
{
    public class IntegratedView
    {
        private bool _isEditMode = false;
        private SettingData _tempSettingData; // 임시 저장용
        private ReorderableList _spreadSheetReorderableList;

        public void DrawIntegratedView()
        {
            // SettingData가 초기화되지 않았으면 초기화
            if (_tempSettingData == null)
            {
                InitializeTempData();
            }

            // 윈도우 상단 여백
            EditorGUILayout.Space(5);

            DrawSettingsBox();

            // 박스와 액션 사이 여백
            EditorGUILayout.Space(5);

            DrawActionButtons();

            // 윈도우 하단 여백
            EditorGUILayout.Space(5);
        }

        private void DrawSettingsBox()
        {
            // 박스 좌우 여백
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.Space(5);

            // 설정 박스 시작
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // 설정 제목
            EditorGUILayout.LabelField("설정", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            DrawServiceAccountAuth();
            DrawSpreadSheetsInfos();
            DrawSheetSettings();

            EditorGUILayout.Space(5);
            DrawEditControls();
            EditorGUILayout.Space(10);

            // 설정 박스 끝
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(5);
            EditorGUILayout.EndHorizontal();
        }

        private void DrawServiceAccountAuth()
        {
            EditorGUILayout.LabelField("서비스 계정 JSON 경로");

            var jsonPath = _tempSettingData.serviceAccountJsonPath;

            if (_isEditMode)
            {
                EditorGUILayout.BeginHorizontal();
                _tempSettingData.serviceAccountJsonPath = EditorGUILayout.TextField(_tempSettingData.serviceAccountJsonPath);
                if (GUILayout.Button("찾아보기", GUILayout.Width(70)))
                {
                    var selectedPath = EditorUtility.OpenFilePanel("서비스 계정 JSON 선택", "", "json");
                    if (!string.IsNullOrEmpty(selectedPath))
                        _tempSettingData.serviceAccountJsonPath = selectedPath;
                }
                EditorGUILayout.EndHorizontal();
            }
            else
            {
                var displayText = string.IsNullOrWhiteSpace(jsonPath) ? "(미지정)" : jsonPath;
                EditorGUILayout.LabelField(displayText, EditorStyles.wordWrappedLabel);
            }

            if (!string.IsNullOrWhiteSpace(jsonPath) && !GSSL_ServiceAccountAuth.IsJsonPathValid(jsonPath))
            {
                EditorGUILayout.HelpBox("지정한 경로에서 JSON 파일을 찾을 수 없습니다.", MessageType.Warning);
            }

            var serviceAccountEmail = _isEditMode
                ? GSSL_ServiceAccountAuth.TryGetClientEmailFromPath(_tempSettingData.serviceAccountJsonPath)
                : GSSL_ServiceAccountAuth.GetServiceAccountEmail();

            if (!string.IsNullOrEmpty(serviceAccountEmail))
            {
                EditorGUILayout.HelpBox(
                    $"각 스프레드시트를 아래 이메일과 공유하세요 (뷰어 이상):\n{serviceAccountEmail}",
                    MessageType.Info);
            }
            else if (!_isEditMode)
            {
                EditorGUILayout.HelpBox(
                    "서비스 계정 JSON 파일의 절대 경로를 지정하면 비공개 스프레드시트도 다운로드할 수 있습니다.",
                    MessageType.Warning);
            }

            EditorGUILayout.Space(5);
        }

        private void DrawSpreadSheetsInfos()
        {
            EnsureSpreadSheetReorderableList();
            _spreadSheetReorderableList.displayAdd = _isEditMode;
            _spreadSheetReorderableList.displayRemove = _isEditMode;
            _spreadSheetReorderableList.DoLayoutList();
            EditorGUILayout.Space(5);
        }

        private void EnsureSpreadSheetReorderableList()
        {
            if (_spreadSheetReorderableList != null &&
                _spreadSheetReorderableList.list == _tempSettingData.listSpreadSheetInfo)
            {
                return;
            }

            _spreadSheetReorderableList = new ReorderableList(
                _tempSettingData.listSpreadSheetInfo,
                typeof(SpreadSheetInfo),
                true,
                true,
                true,
                true)
            {
                drawHeaderCallback = rect => EditorGUI.LabelField(rect, "스프레드 시트 데이터"),
                elementHeight = EditorGUIUtility.singleLineHeight + 4,
                drawElementCallback = DrawSpreadSheetElement,
                onAddCallback = list =>
                {
                    list.list.Add(new SpreadSheetInfo
                    {
                        spreadSheetName = "Name Here",
                        spreadSheetId = "Id Here",
                    });
                },
                onRemoveCallback = list =>
                {
                    if (list.index < 0 || list.index >= list.list.Count)
                        return;

                    list.list.RemoveAt(list.index);
                },
                onReorderCallback = _ =>
                {
                    if (!_isEditMode)
                        SaveSpreadSheetOrder();
                },
            };
        }

        private void DrawSpreadSheetElement(Rect rect, int index, bool isActive, bool isFocused)
        {
            var info = _tempSettingData.listSpreadSheetInfo[index];
            rect.y += 2;
            rect.height = EditorGUIUtility.singleLineHeight;

            if (_isEditMode)
            {
                var nameRect = new Rect(rect.x, rect.y, 100, rect.height);
                var idRect = new Rect(rect.x + 105, rect.y, rect.width - 105, rect.height);
                info.spreadSheetName = EditorGUI.TextField(nameRect, info.spreadSheetName);
                info.spreadSheetId = EditorGUI.TextField(idRect, info.spreadSheetId);
                return;
            }

            var nameLabelRect = new Rect(rect.x, rect.y, 100, rect.height);
            var idRectWidth = rect.width - 100 - 130;
            var idLabelRect = new Rect(rect.x + 105, rect.y, idRectWidth, rect.height);
            var openRect = new Rect(rect.x + rect.width - 125, rect.y, 60, rect.height);
            var syncRect = new Rect(rect.x + rect.width - 60, rect.y, 60, rect.height);

            EditorGUI.LabelField(nameLabelRect, info.spreadSheetName);

            var displayId = info.spreadSheetId.Length > 10
                ? info.spreadSheetId.Substring(0, 10) + "..."
                : info.spreadSheetId;
            EditorGUI.LabelField(idLabelRect, displayId);

            if (GUI.Button(openRect, "열기"))
            {
                Application.OpenURL(string.Format(GSSL_URL.SpreadSheetOpenUrl, info.spreadSheetId));
            }

            using (new EditorGUI.DisabledScope(CurrState != eGSSL_State.None))
            {
                if (GUI.Button(syncRect, "최신화"))
                {
                    _ = GSSL_OneButton.OneButtonProcessSingleSpreadSheet(info.spreadSheetId);
                }
            }
        }

        private void SaveSpreadSheetOrder()
        {
            CopySettingData(_tempSettingData, GSSL_Setting.SettingData);
            EditorUtility.SetDirty(GSSL_Setting.SettingData);
            AssetDatabase.SaveAssets();
        }

        private void DrawSheetSettings()
        {
            EditorGUILayout.LabelField("시트 설정");

            if (_isEditMode)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Box(" 시트의 제목에서", GUILayout.Width(100));
                _tempSettingData.sheetTargetStr = EditorGUILayout.TextField(_tempSettingData.sheetTargetStr, GUILayout.Width(50));
                GUILayout.Box("문구가 포함되어 있을 경우, 대상 시트에서 ", GUILayout.Width(250));
                _tempSettingData.sheetTarget = (SettingData.eSheetTargetStandard)EditorGUILayout.EnumPopup(_tempSettingData.sheetTarget, GUILayout.Width(60));
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                GUILayout.Box(" 다음의 문구가 있다면 enum타입을 정의하는 테이블로 분류합니다.");
                _tempSettingData.sheet_enumTypeStr = EditorGUILayout.TextField(_tempSettingData.sheet_enumTypeStr, GUILayout.Width(100));
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                GUILayout.Box(" 다음의 같은 문구가 있다면 Localization테이블로 분류합니다.");
                _tempSettingData.sheet_localizationTypeStr = EditorGUILayout.TextField(_tempSettingData.sheet_localizationTypeStr, GUILayout.Width(100));
                EditorGUILayout.EndHorizontal();
            }
            else
            {
                EditorGUILayout.LabelField($" 시트의 제목에서 \"{_tempSettingData.sheetTargetStr}\" 문구가 포함되어 있을경우, 대상 시트에서 \"{_tempSettingData.sheetTarget.ToString()}\"");
                EditorGUILayout.LabelField($" 다음의 문구가 있다면 enum타입을 정의하는 테이블로 분류합니다. \"{_tempSettingData.sheet_enumTypeStr}\"");
                EditorGUILayout.LabelField($" 다음의 같은 문구가 있다면 Localization테이블로 분류합니다. \"{_tempSettingData.sheet_localizationTypeStr}\"");
            }

            EditorGUILayout.Space(5);
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

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("최신화 Only", GUILayout.Width(200)))
            {
                _ = GSSL_OneButton.OneButtonProcessSpreadSheet(false);
            }
            EditorGUILayout.Space(30);
            if (GUILayout.Button("클린 & 최신화", GUILayout.Width(200)))
            {
                _ = GSSL_OneButton.OneButtonProcessSpreadSheet(true);
            }
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        private void DrawEditControls()
        {
            EditorGUILayout.Separator();

            if (!_isEditMode)
            {
                // 편집 모드가 아닐 때 - 편집 버튼만 표시
                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("설정 편집", GUILayout.Width(150)))
                {
                    EnterEditMode();
                }
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
            }
            else
            {
                // 편집 모드일 때 - 적용/취소 버튼 표시
                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();

                if (GUILayout.Button("취소", GUILayout.Width(100)))
                {
                    CancelEditMode();
                }

                EditorGUILayout.Space(20);

                if (GUILayout.Button("적용", GUILayout.Width(100)))
                {
                    ApplyChanges();
                }

                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
            }
        }

        private void InitializeTempData()
        {
            _tempSettingData = new SettingData();
            CopySettingData(GSSL_Setting.SettingData, _tempSettingData);
            _spreadSheetReorderableList = null;
        }

        private void EnterEditMode()
        {
            _isEditMode = true;
        }

        private void CancelEditMode()
        {
            _isEditMode = false;
            CopySettingData(GSSL_Setting.SettingData, _tempSettingData);
            _spreadSheetReorderableList = null;
        }

        private void ApplyChanges()
        {
            CopySettingData(_tempSettingData, GSSL_Setting.SettingData);

            EditorUtility.SetDirty(GSSL_Setting.SettingData);
            AssetDatabase.SaveAssets();

            GSSL_ServiceAccountAuth.ClearCache();

            _isEditMode = false;
            _spreadSheetReorderableList = null;

            Debug.Log("설정이 성공적으로 저장되었습니다.");
        }

        private void CopySettingData(SettingData source, SettingData target)
        {
            target.serviceAccountJsonPath = source.serviceAccountJsonPath;
            target.sheetTarget = source.sheetTarget;
            target.sheetTargetStr = source.sheetTargetStr;
            target.sheet_enumTypeStr = source.sheet_enumTypeStr;
            target.sheet_localizationTypeStr = source.sheet_localizationTypeStr;

            // 스프레드시트 정보도 깊은 복사
            target.listSpreadSheetInfo.Clear();
            foreach (var info in source.listSpreadSheetInfo)
            {
                target.listSpreadSheetInfo.Add(new SpreadSheetInfo
                {
                    spreadSheetName = info.spreadSheetName,
                    spreadSheetId = info.spreadSheetId
                });
            }
        }
    }
}
