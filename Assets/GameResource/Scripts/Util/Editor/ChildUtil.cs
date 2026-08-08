using System.Collections.Generic;
using UnityEngine;

namespace cyKimUnityExtensions
{
    namespace UnityEngine
    {
        public static class ChildUtil
        {
            public static Transform FindChildByName(this Transform parent, string targetName, int recursiveCount = -1)
            {
                // 부모의 이름이 타겟 이름과 같은지 확인
                if (parent.name == targetName)
                    return parent;

                if (recursiveCount-- == 0)
                    return null;
                
                // 자식들을 순회하며 재귀적으로 탐색
                foreach (Transform child in parent)
                {
                    Transform result = FindChildByName(child, targetName, recursiveCount);
                    if (result != null)
                        return result;
                }

                // 일치하는 항목이 없으면 null 반환
                return null;
            }
            
            public static void GetTransformIncludeAllChild(this Transform parent, ref List<Transform> listResult)
            {
                if (listResult == null)
                    listResult = new List<Transform>();
                
                listResult.Add(parent);

                // 자식들을 순회하며 재귀적으로 탐색
                foreach (Transform child in parent)
                {
                    GetTransformIncludeAllChild(child, ref listResult);
                }
            }
        }
    }
}