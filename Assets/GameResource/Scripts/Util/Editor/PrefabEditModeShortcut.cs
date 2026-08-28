using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using System.Collections.Generic;

namespace cyKimUnityExtensions
{
    public static class PrefabEditModeShortcut
    {
        private static Stack<string> prefabHistory = new Stack<string>(); // 프리팹 경로 기록 저장
        private static Dictionary<string, string> prefabInstanceSelection = new Dictionary<string, string>(); // 선택했던 프리팹 인스턴스 저장

        [MenuItem("Tools/Util/Prefab/Open Selected Prefab &e")] 
        private static void OpenSelectedPrefab()
        {
            GameObject selectedObject = Selection.activeGameObject;

            if (selectedObject == null)
            {
                Debug.LogWarning("선택된 오브젝트가 없습니다.");
                return;
            }

            string assetPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(selectedObject);
            if (string.IsNullOrEmpty(assetPath))
            {
                Debug.LogWarning("선택한 오브젝트는 프리팹이 아닙니다.");
                return;
            }

            var currentStage = PrefabStageUtility.GetCurrentPrefabStage();
            if (currentStage != null)
            {
                // 현재 프리팹 경로를 스택에 저장 (이전 프리팹으로 돌아가기 위함)
                prefabHistory.Push(currentStage.assetPath);

                // 현재 선택한 프리팹 인스턴스 경로 저장 (나중에 돌아왔을 때 선택을 위해)
                string instancePath = GetGameObjectPath(selectedObject);
                prefabInstanceSelection[currentStage.assetPath] = instancePath;
            }

            // 프리팹 편집 모드로 진입
            PrefabStageUtility.OpenPrefab(assetPath);
        }

        [MenuItem("Tools/Util/Prefab/Open Selected Prefab &e", true)]
        private static bool ValidateOpenSelectedPrefab()
        {
            GameObject selectedObject = Selection.activeGameObject;
            return selectedObject != null && PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(selectedObject) != "";
        }

        [MenuItem("Tools/Util/Prefab/Exit Prefab Edit Mode %&e")] 
        private static void ExitPrefabEditMode()
        {
            if (PrefabStageUtility.GetCurrentPrefabStage() != null)
            {
                if (prefabHistory.Count > 0)
                {
                    // 이전 프리팹으로 복귀
                    string previousPrefabPath = prefabHistory.Pop();
                    PrefabStageUtility.OpenPrefab(previousPrefabPath);

                    // 이전 프리팹에서 선택했던 오브젝트가 있으면 자동 선택
                    if (prefabInstanceSelection.TryGetValue(previousPrefabPath, out string instancePath))
                    {
                        GameObject instanceObject = FindGameObjectByPathInPrefabStage(instancePath);
                        if (instanceObject != null)
                        {
                            Selection.activeGameObject = instanceObject;
                        }
                    }
                }
                else
                {
                    // 프리팹 기록이 없으면 씬으로 복귀
                    StageUtility.GoBackToPreviousStage();
                }

                // 하이어라키 창 업데이트 (폴딩 상태 유지)
                EditorApplication.ExecuteMenuItem("Window/General/Hierarchy");
            }
            else
            {
                Debug.LogWarning("현재 프리팹 편집 모드가 아닙니다.");
            }
        }

        [MenuItem("Tools/Util/Prefab/Exit Prefab Edit Mode %&e", true)]
        private static bool ValidateExitPrefabEditMode()
        {
            return PrefabStageUtility.GetCurrentPrefabStage() != null;
        }

        /// <summary>
        /// 게임오브젝트의 하이어라키 경로를 가져옴
        /// </summary>
        private static string GetGameObjectPath(GameObject obj)
        {
            if (obj == null) return string.Empty;
            string path = obj.name;
            while (obj.transform.parent != null)
            {
                obj = obj.transform.parent.gameObject;
                path = obj.name + "/" + path;
            }
            return path;
        }

        /// <summary>
        /// 현재 열린 프리팹 편집 모드의 Scene 내부에서 특정 경로에 해당하는 게임오브젝트를 찾음
        /// </summary>
        private static GameObject FindGameObjectByPathInPrefabStage(string path)
        {
            var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
            if (prefabStage == null) return null;

            // 현재 프리팹 편집 씬의 모든 루트 오브젝트 가져오기
            GameObject[] rootObjects = prefabStage.scene.GetRootGameObjects();

            string[] splitPath = path.Split('/');
            GameObject current = null;

            // 루트 오브젝트에서 탐색 시작
            foreach (GameObject root in rootObjects)
            {
                if (root.name == splitPath[0])
                {
                    current = root;
                    break;
                }
            }

            // 하위 오브젝트 탐색
            for (int i = 1; i < splitPath.Length; i++)
            {
                if (current == null) return null;
                Transform child = current.transform.Find(splitPath[i]);
                current = child != null ? child.gameObject : null;
            }
            return current;
        }
    }
}