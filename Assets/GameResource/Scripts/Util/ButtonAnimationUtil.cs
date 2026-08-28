using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using LitMotion;
using LitMotion.Extensions;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Backend.Util
{
    public static class ButtonAnimationUtil
    {
        private static readonly Dictionary<int, CancellationTokenSource> _ctsDictionary = new();

#if UNITY_EDITOR
        [InitializeOnLoadMethod]
        private static void InitializeOnLoadMethod()
        {
            _ctsDictionary.Clear();
        }
#endif

        /// <summary>
        /// 버튼 눌림 스케일 애니메이션 재생 (0.9 → 1.0, OutElastic)
        /// </summary>
        public static void ButtonAnimation(this GameObject gObj)
        {
            try
            {
                var instanceId = gObj.GetInstanceID();
                if (_ctsDictionary.TryGetValue(instanceId, out var existing))
                {
                    existing.Cancel();
                    _ctsDictionary.Remove(instanceId);
                }

                gObj.transform.localScale = Vector3.one;

                var cts = new CancellationTokenSource();
                _ctsDictionary.TryAdd(instanceId, cts);

                ButtonAnimationProcess(gObj, cts.Token).Forget();
            }
            catch (Exception)
            {
                gObj?.SetActive(false);
            }
        }

        /// <summary>
        /// 진행 중인 버튼 애니메이션 중단 및 스케일 복원
        /// </summary>
        public static void StopButtonAnimation(this GameObject gObj)
        {
            var instanceId = gObj.GetInstanceID();
            if (_ctsDictionary.TryGetValue(instanceId, out var cts))
            {
                cts.Cancel();
                _ctsDictionary.Remove(instanceId);
                gObj.transform.localScale = Vector3.one;
            }
        }

        private static async UniTask ButtonAnimationProcess(GameObject gObj, CancellationToken token)
        {
            var tr = gObj.transform;
            try
            {
                await LMotion.Create(Vector3.one, Vector3.one * 0.9f, 0.05f)
                    .WithEase(Ease.Linear)
                    .BindToLocalScale(tr)
                    .ToUniTask(token);

                if (token.IsCancellationRequested) return;

                tr.localScale = Vector3.one * 0.9f;

                await LMotion.Create(Vector3.one * 0.9f, Vector3.one, 0.3f)
                    .WithEase(Ease.OutElastic)
                    .BindToLocalScale(tr)
                    .ToUniTask(token);
            }
            catch (OperationCanceledException)
            {
                tr.localScale = Vector3.one;
            }
            finally
            {
                _ctsDictionary.Remove(gObj.GetInstanceID());
                if (gObj != null)
                    gObj.transform.localScale = Vector3.one;
            }
        }
    }
}
