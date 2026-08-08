using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.Pool;
using Backend.Util.Interface;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.AddressableAssets;

namespace Backend.Object.Management.Pool
{
    public class Pooling<T> : IClearable where T : Component
    {
        private readonly ObjectPool<T> pool;
        private readonly T prefab;
        private readonly Transform parent;
        private readonly Action<T> onGet;
        private readonly Action<T> onRelease;

        private AsyncOperationHandle<GameObject>? addressableHandle;
        private readonly HashSet<T> activeObjects = new HashSet<T>();

        public int CountActive => pool.CountActive;
        public int CountInactive => pool.CountInactive;
        public int CountAll => pool.CountAll;

        public Pooling(
            T prefab, Transform parent = null, int defaultCapacity = 10, int maxSize = 100,
            Action<T> onGet = null, Action<T> onRelease = null, bool collectionCheck = true)
        {
            this.prefab = prefab;
            this.parent = parent;
            this.onGet = onGet;
            this.onRelease = onRelease;

            pool = new ObjectPool<T>(
                CreatePooledItem, OnGetFromPool, OnReleaseToPool, OnDestroyPooledItem,
                collectionCheck, defaultCapacity, maxSize
            );
        }

        internal void SetAddressableHandle(AsyncOperationHandle<GameObject> handle)
        {
            addressableHandle = handle;
        }

        // 💡 개선 1: CancellationToken 추가 및 ToUniTask() 연동
        public static async UniTask<Pooling<T>> CreateAsync(
            string addressableKey, Transform parent = null, int defaultCapacity = 10, int maxSize = 100,
            Action<T> onGet = null, Action<T> onRelease = null, bool collectionCheck = true,
            CancellationToken token = default)
        {
            var handle = Addressables.LoadAssetAsync<GameObject>(addressableKey);

            GameObject prefabGameObject;
            try
            {
                prefabGameObject = await handle.ToUniTask(cancellationToken: token);
            }
            catch (OperationCanceledException)
            {
                Addressables.Release(handle); // 취소 시 메모리 누수 방지
                throw;
            }

            if (prefabGameObject == null)
            {
                Debug.LogError($"Failed to load addressable: {addressableKey}");
                return null;
            }

            if (!prefabGameObject.TryGetComponent<T>(out var prefabComponent))
            {
                Debug.LogError($"Component {typeof(T).Name} not found on addressable: {addressableKey}");
                Addressables.Release(handle);
                return null;
            }

            var newPool = new Pooling<T>(
                prefabComponent, parent, defaultCapacity, maxSize, onGet, onRelease, collectionCheck
            );

            newPool.SetAddressableHandle(handle);
            return newPool;
        }

        public T Get() => pool.Get();
        public PooledObject<T> Get(out T instance) => pool.Get(out instance);

        public void Release(T element)
        {
            if (element == null) return;
            pool.Release(element);
        }

        public void Clear()
        {
            activeObjects.Clear();
            pool.Clear();
        }

        public void Dispose()
        {
            activeObjects.Clear();
            pool.Clear();

            if (addressableHandle.HasValue && addressableHandle.Value.IsValid())
            {
                Addressables.Release(addressableHandle.Value);
                addressableHandle = null;
            }
        }

        private T CreatePooledItem()
        {
            var instance = UnityEngine.Object.Instantiate(prefab, parent);
            instance.gameObject.SetActive(false);
            return instance;
        }

        private void OnGetFromPool(T element)
        {
            activeObjects.Add(element);
            element.gameObject.SetActive(true);
            onGet?.Invoke(element);
        }

        private void OnReleaseToPool(T element)
        {
            activeObjects.Remove(element);
            onRelease?.Invoke(element);
            element.gameObject.SetActive(false);
        }

        private void OnDestroyPooledItem(T element)
        {
            activeObjects.Remove(element);
            if (element != null && element.gameObject != null)
            {
                UnityEngine.Object.Destroy(element.gameObject);
            }
        }

        public List<T> GetAllActive()
        {
            var result = ListPool<T>.Get();
            foreach (var obj in activeObjects)
            {
                result.Add(obj);
            }
            return result; // ※ 사용 후 ListPool<T>.Release(result) 를 호출해줘야 합니다.
        }
    }
}