using UnityEngine;
using UnityEngine.UI;

namespace cyKimUnityExtensions
{
    namespace UnityEngine
    {
        namespace UI
        {
            public static class LayoutUtil
            {   
                /// <summary>
                /// 지정된 Transform 내에서 LayoutGroup과 ContentSizeFitter를 하위 자식부터 순차적으로 Rebuild합니다.
                /// </summary>
                /// <param name="root">Rebuild를 수행할 Transform의 루트</param>
                public static void RebuildLayoutsFromBottom(this Transform root)
                {
                    if (root == null) return;

                    // 하위 자식부터 재귀적으로 탐색하며 Rebuild 실행
                    foreach (Transform child in root)
                    {
                        RebuildLayoutsFromBottom(child);
                    }

                    // LayoutGroup과 ContentSizeFitter를 가진 경우 Rebuild 실행
                    if (root.TryGetComponent<LayoutGroup>(out var _) ||
                        root.TryGetComponent<ContentSizeFitter>(out var _))
                    {
                        LayoutRebuilder.ForceRebuildLayoutImmediate(root as RectTransform);
                    }
                }
            }
        }
    }
}