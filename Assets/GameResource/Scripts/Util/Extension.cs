using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Util
{
    public static class Extension
    {
        public static Vector2 ToXZ(this Vector3 vector3)
        {
            return new Vector2(vector3.x, vector3.z);
        }

        public static T RandomPick<T>(this List<T> list)
        {
            var randomI = Random.Range(0, list.Count);
            return list[randomI];
        }

        public static T RandomPick<T>(this IEnumerable<T> enumerable)
        {
            var list = enumerable.ToList();
            var randomI = Random.Range(0, list.Count);

            return list[randomI];
        }

        public static async UniTask WaitCurrentStateCompleteAsync(this Animator animator, int layer = 0)
        {
            await UniTask.NextFrame();
            while (animator != null && animator.isActiveAndEnabled &&
                   animator.GetCurrentAnimatorStateInfo(layer).normalizedTime < 1f)
            {
                await UniTask.NextFrame();
            }
        }
    }
}
