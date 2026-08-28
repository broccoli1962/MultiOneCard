using UnityEditor;
using UnityEngine;
using UnityEditor.SceneManagement;

public class CustomCreateGameObject
{
    [MenuItem("GameObject/Custom Create GameObject _%#n")] // Ctrl+Shift+N (Mac: Cmd+Shift+N)
    public static void CreateGameObject()
    {
        GameObject[] selectedObjects = Selection.gameObjects;
        
        // 프리팹 편집 모드인지 확인
        PrefabStage prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
        Transform parentTransform = null;
        
        // 프리팹 편집 모드에서 아무것도 선택하지 않았을 때의 처리
        if (prefabStage != null && selectedObjects.Length == 0)
        {
            parentTransform = prefabStage.prefabContentsRoot.transform;
        }
        else if (selectedObjects.Length == 1)
        {
            parentTransform = selectedObjects[0].transform.parent;
        }

        // 선택된 오브젝트가 하나이고, RectTransform을 가지고 있는 경우
        if (selectedObjects.Length == 1 && selectedObjects[0].GetComponent<RectTransform>() != null)
        {
            GameObject uiObject = new GameObject("UI_GameObject", typeof(RectTransform));
            
            // 부모가 Canvas가 되도록 설정
            Canvas canvas = selectedObjects[0].GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                canvas = new GameObject("Canvas", typeof(Canvas), typeof(UnityEngine.UI.CanvasScaler), typeof(UnityEngine.UI.GraphicRaycaster)).GetComponent<Canvas>();
                // Canvas를 선택된 오브젝트의 부모 아래에 생성
                canvas.transform.SetParent(selectedObjects[0].transform.parent, false);
            }
            
            // UI 오브젝트를 선택된 오브젝트의 형제로 설정
            uiObject.transform.SetParent(selectedObjects[0].transform.parent, false);
            // 선택된 오브젝트 다음에 위치하도록 설정
            uiObject.transform.SetSiblingIndex(selectedObjects[0].transform.GetSiblingIndex() + 1);
            
            // 기본 UI 설정
            RectTransform rectTransform = uiObject.GetComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(100, 100); // 기본 크기 설정
            rectTransform.anchoredPosition = Vector2.zero; // 부모 기준 중앙에 배치
            
            // 선택된 오브젝트로 설정
            Selection.activeGameObject = uiObject;
            Undo.RegisterCreatedObjectUndo(uiObject, "Create UI GameObject");
        }
        else
        {
            // 기본 Transform으로 오브젝트 생성 (기본 동작)
            GameObject gameObject = new GameObject("GameObject");
            
            // 부모 설정 (프리팹 편집 모드 또는 선택된 오브젝트)
            if (parentTransform != null)
            {
                gameObject.transform.SetParent(parentTransform, false);
                // 선택된 오브젝트가 있는 경우, 그 다음에 위치하도록 설정
                if (selectedObjects.Length == 1)
                {
                    gameObject.transform.SetSiblingIndex(selectedObjects[0].transform.GetSiblingIndex() + 1);
                }
            }
            
            Selection.activeGameObject = gameObject;
            Undo.RegisterCreatedObjectUndo(gameObject, "Create Empty GameObject");
        }
    }
}
