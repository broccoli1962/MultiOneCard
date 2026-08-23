#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Backend.Object.UI.Editor
{
    /// <summary>
    /// TitlePanel / LobbyPanel / RoomPanel / MatchPanel / ResultPanel
    /// 고정 위젯이 프리팹 자식 + SerializeField 로 배선됐는지 검사한다.
    /// EnsureLayout / FindOrCreate 는 GameObject 를 만들지 않고 이미 있는 ChoiceSheet·CardView 만 찾는다.
    /// </summary>
    public static class PanelFixedWidgetGuard
    {
        private static readonly string[] PanelPaths =
        {
            "Assets/GameResource/Prefab/UI/TitlePanel.prefab",
            "Assets/GameResource/Prefab/UI/LobbyPanel.prefab",
            "Assets/GameResource/Prefab/UI/RoomPanel.prefab",
            "Assets/GameResource/Prefab/UI/MatchPanel.prefab",
            "Assets/GameResource/Prefab/UI/ResultPanel.prefab",
        };

        [MenuItem("Tools/OneTable/Validate Panel Fixed Widgets")]
        public static bool Validate()
        {
            foreach (var path in PanelPaths)
            {
                var root = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (root == null || root.transform.childCount == 0)
                {
                    return false;
                }

                if (!HasWiredWidgetFields(root))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool HasWiredWidgetFields(GameObject root)
        {
            var behaviours = root.GetComponentsInChildren<MonoBehaviour>(true);
            for (var i = 0; i < behaviours.Length; i++)
            {
                var behaviour = behaviours[i];
                if (behaviour == NoneRelevant(behaviour))
                {
                    continue;
                }

                var so = new SerializedObject(behaviour);
                var prop = so.GetIterator();
                var enterChildren = true;
                while (prop.NextVisible(enterChildren))
                {
                    enterChildren = prop.isArray;
                    if (!prop.name.StartsWith("_") || prop.name == "_font")
                    {
                        continue;
                    }

                    if (prop.propertyType == SerializedPropertyType.ObjectReference
                        && prop.objectReferenceValue == null)
                    {
                        return false;
                    }

                    if (prop.isArray)
                    {
                        for (var index = 0; index < prop.arraySize; index++)
                        {
                            var element = prop.GetArrayElementAtIndex(index);
                            if (element.propertyType == SerializedPropertyType.ObjectReference
                                && element.objectReferenceValue == null)
                            {
                                return false;
                            }
                        }
                    }
                }
            }

            return true;
        }

        private static MonoBehaviour NoneRelevant(MonoBehaviour behaviour)
        {
            if (behaviour == null)
            {
                return behaviour;
            }

            var typeName = behaviour.GetType().Name;
            if (typeName == "TitlePanel" || typeName == "LobbyPanel" || typeName == "RoomPanel"
                || typeName == "MatchPanel" || typeName == "ResultPanel" || typeName == "ChatView"
                || typeName == "ChoiceSheet" || typeName == "MatchHud" || typeName == "CardView")
            {
                return null;
            }

            return behaviour;
        }
    }
}
#endif
