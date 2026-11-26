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
        private readonly ObjectPool<T> _pool;
        private readonly T _prefab;
        private readonly Transform _parent;
        private readonly Action<T> _onGet;
        private readonly Action<T> _onRelease;

        private AsyncOperationHandle<GameObject>? _addressableHandle;
        private readonly HashSet<T> _activeObjects = new HashSet<T>();

        public int CountActive => _pool.CountActive;
        public int CountInactive => _pool.CountInactive;
        public int CountAll => _pool.CountAll;

        public Pooling(
            T prefab, Transform parent = null, int defaultCapacity = 10, int maxSize = 100,
            Action<T> onGet = null, Action<T> onRelease = null, bool collectionCheck = true)
        {
            _prefab = prefab;
            _parent = parent;
            _onGet = onGet;
            _onRelease = onRelease;

            _pool = new ObjectPool<T>(
                CreatePooledItem, OnGetFromPool, OnReleaseToPool, OnDestroyPooledItem,
                collectionCheck, defaultCapacity, maxSize
            );
        }

        internal void SetAddressableHandle(AsyncOperationHandle<GameObject> handle)
        {
            _addressableHandle = handle;
        }

        // 💡 개선 1: CancellationToken 추가 및 ToUniTask() 연동
        public static async UniTask<Pooling<T>> CreateAsync(
            string addressableKey, Transform parent = null, int defaultCapacity = 10, int maxSize = 100,
            Action<T> onGet = null, Action<T> onRelease = null, bool collectionCheck = true,
            CancellationToken token = default)
        {
            if (string.IsNullOrEmpty(addressableKey))
            {
                Debug.LogError("[Pooling] Addressable key is null or empty.");
                return null;
            }

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

        public T Get() => _pool.Get();
        public PooledObject<T> Get(out T instance) => _pool.Get(out instance);

        public void Release(T element)
        {
            if (element == null) return;
            _pool.Release(element);
        }

        public void Clear()
        {
            _activeObjects.Clear();
            _pool.Clear();
        }

        public void Dispose()
        {
            _activeObjects.Clear();
            _pool.Clear();

            if (_addressableHandle.HasValue && _addressableHandle.Value.IsValid())
            {
                Addressables.Release(_addressableHandle.Value);
                _addressableHandle = null;
            }
        }

        private T CreatePooledItem()
        {
            var instance = UnityEngine.Object.Instantiate(_prefab, _parent);
            instance.gameObject.SetActive(false);
            return instance;
        }

        private void OnGetFromPool(T element)
        {
            _activeObjects.Add(element);
            element.gameObject.SetActive(true);
            _onGet?.Invoke(element);
        }

        private void OnReleaseToPool(T element)
        {
            _activeObjects.Remove(element);
            _onRelease?.Invoke(element);
            element.gameObject.SetActive(false);
        }

        private void OnDestroyPooledItem(T element)
        {
            _activeObjects.Remove(element);
            if (element != null && element.gameObject != null)
            {
                UnityEngine.Object.Destroy(element.gameObject);
            }
        }

        public List<T> GetAllActive()
        {
            var result = ListPool<T>.Get();
            foreach (var obj in _activeObjects)
            {
                result.Add(obj);
            }
            return result; // ※ 사용 후 ListPool<T>.Release(result) 를 호출해줘야 합니다.
        }
    }
}