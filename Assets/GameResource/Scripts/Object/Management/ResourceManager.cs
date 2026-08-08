using System.Collections.Generic;
using Backend.Util.Management;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Backend.Object.Management
{
    public class ResourceManager : SingletonGameObject<ResourceManager>
    {
        private Dictionary<string, AsyncOperationHandle> _resourceCache = new();

        #region #static Method
        /// <summary>
        /// ResourceManager.LoadResource<T>(string key) — 동기 로드
        /// ResourceManager.LoadResourceAsync<T>(string key) — 비동기 로드
        /// 단 타입이 Component인 경우에는 ResourceManager.LoadComponent<T>(string key), ResourceManager.LoadComponentAsync<T>(string key) 를 사용함.
        /// </summary>
        public static T LoadResource<T>(string key) where T : UnityEngine.Object
        {
            return Instance.LoadResource_Internal<T>(key);
        }

        public static UniTask<T> LoadResourceAsync<T>(string key) where T : UnityEngine.Object
        {
            return Instance.LoadResourceAsync_Internal<T>(key);
        }

        /// <summary>
        /// Loads a prefab as a GameObject and returns the requested Component on it.
        /// Addressables treats a prefab's main asset as GameObject, so the component type
        /// cannot be loaded directly. This shares the GameObject-keyed cache and extracts
        /// the component via GetComponent.
        /// </summary> 
        public static T LoadComponent<T>(string key) where T : Component
        {
            return Instance.LoadComponent_Internal<T>(key);
        }

        public static UniTask<T> LoadComponentAsync<T>(string key) where T : Component
        {
            return Instance.LoadComponentAsync_Internal<T>(key);
        }


        public static void ReleaseResource(string key)
        {
            Instance.ReleaseResource_Internal(key);
        }
        #endregion

        #region #Internal Method
        private T LoadResource_Internal<T>(string key) where T : UnityEngine.Object
        {
            if (_resourceCache.TryGetValue(key, out AsyncOperationHandle cachedHandle))
            {
                return cachedHandle.Result as T;
            }

            AsyncOperationHandle<T> handle = Addressables.LoadAssetAsync<T>(key);
            T resource = handle.WaitForCompletion();

            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                _resourceCache.Add(key, handle);
                return resource;
            }
            else
            {
                Debug.LogError($"Asset Load Fail! Key : {key}");
                Addressables.Release(handle);
                return null;
            }
        }

        private void ReleaseResource_Internal(string key)
        {
            if (_resourceCache.TryGetValue(key, out AsyncOperationHandle handle))
            {
                Addressables.Release(handle);
                _resourceCache.Remove(key);
            }
            else
            {
                Debug.LogWarning($"[ResourceManager] Release Resource Fail! Key : {key}");
            }
        }

        private async UniTask<T> LoadResourceAsync_Internal<T>(string key) where T : UnityEngine.Object
        {
            if (_resourceCache.TryGetValue(key, out AsyncOperationHandle cachedHandle))
            {
                if (!cachedHandle.IsDone)
                {
                    await cachedHandle.ToUniTask();
                }
                return cachedHandle.Result as T;
            }

            AsyncOperationHandle<T> handle = Addressables.LoadAssetAsync<T>(key);

            _resourceCache.Add(key, handle);

            T resource = await handle.ToUniTask();

            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                return resource;
            }
            else
            {
                Debug.LogError($"Asset Load Fail! Key : {key}");
                _resourceCache.Remove(key);
                Addressables.Release(handle);
                return null;
            }
        }

        private async UniTask<T> LoadComponentAsync_Internal<T>(string key) where T : Component
        {
            GameObject prefab = await LoadResourceAsync_Internal<GameObject>(key);
            if (prefab == null)
            {
                return null;
            }

            T component = prefab.GetComponent<T>();
            if (component == null)
            {
                Debug.LogError($"[ResourceManager] Component {typeof(T).Name} not found on prefab! Key : {key}");
            }

            return component;
        }

        private T LoadComponent_Internal<T>(string key) where T : Component
        {
            GameObject prefab = LoadResource_Internal<GameObject>(key);
            if (prefab == null)
            {
                return null;
            }

            T component = prefab.GetComponent<T>();
            if (component == null)
            {
                Debug.LogError($"[ResourceManager] Component {typeof(T).Name} not found on prefab! Key : {key}");
            }

            return component;
        }

        private void OnDestroy()
        {
            foreach (var handle in _resourceCache.Values)
            {
                Addressables.Release(handle);
            }
            _resourceCache.Clear();
        }
        #endregion
    }
}