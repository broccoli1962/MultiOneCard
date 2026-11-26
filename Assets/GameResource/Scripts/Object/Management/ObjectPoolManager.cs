using System;
using System.Collections.Generic;
using System.Threading;
using Backend.Object.Management.Pool;
using Backend.Util.Interface;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Pool;

namespace Backend.Object.Management
{
    public static class ObjectPoolManager
    {
        private static readonly Dictionary<Type, object> _pools = new();
        private static readonly Dictionary<Type, IClearable> _clearablePools = new();

        private static readonly Dictionary<string, object> _namedPools = new();
        private static readonly Dictionary<string, IClearable> _namedClearablePools = new();

        // 💡 동시성(Race Condition) 제어를 위한 로딩 상태 추적 딕셔너리
        private static readonly HashSet<Type> _loadingTypes = new();
        private static readonly HashSet<string> _loadingNames = new();

        public static Pooling<T> GetPool<T>() where T : Component
        {
            return _pools.TryGetValue(typeof(T), out var pool) ? pool as Pooling<T> : null;
        }

        public static bool HasPool<T>() where T : Component => _pools.ContainsKey(typeof(T));

        #region 프리팹 기반 풀 생성 (기존과 동일)
        public static Pooling<T> CreatePool<T>(
            T prefab, Transform parent = null, int defaultCapacity = 10, int maxSize = 100,
            Action<T> onGet = null, Action<T> onRelease = null) where T : Component
        {
            var type = typeof(T);
            if (_pools.TryGetValue(type, out var existingPool))
                return existingPool as Pooling<T>;

            var newPool = new Pooling<T>(prefab, parent, defaultCapacity, maxSize, onGet, onRelease);
            _pools[type] = newPool;
            _clearablePools[type] = newPool;
            return newPool;
        }

        public static Pooling<T> GetOrCreatePool<T>(
            T prefab, Transform parent = null, int defaultCapacity = 10, int maxSize = 100,
            Action<T> onGet = null, Action<T> onRelease = null) where T : Component
        {
            return GetPool<T>() ?? CreatePool(prefab, parent, defaultCapacity, maxSize, onGet, onRelease);
        }
        #endregion

        #region Addressable 기반 비동기 풀 생성 (Type 기반)

        public static async UniTask<Pooling<T>> CreatePoolAsync<T>(
            string addressableKey, Transform parent = null, int defaultCapacity = 10, int maxSize = 100,
            Action<T> onGet = null, Action<T> onRelease = null, CancellationToken token = default) where T : Component
        {
            var type = typeof(T);

            if (_pools.TryGetValue(type, out var existingPool))
                return existingPool as Pooling<T>;

            // 💡 개선 2: 동일 타입 중복 비동기 로딩 방지 (대기열 처리)
            if (_loadingTypes.Contains(type))
            {
                await UniTask.WaitWhile(() => _loadingTypes.Contains(type), cancellationToken: token);
                return GetPool<T>();
            }

            _loadingTypes.Add(type);
            try
            {
                var newPool = await Pooling<T>.CreateAsync(
                    addressableKey, parent, defaultCapacity, maxSize, onGet, onRelease, true, token);

                if (newPool == null) return null;

                _pools[type] = newPool;
                _clearablePools[type] = newPool;
                return newPool;
            }
            finally
            {
                _loadingTypes.Remove(type); // 성공/실패 무관하게 로딩 상태 해제
            }
        }

        public static async UniTask<Pooling<T>> GetOrCreatePoolAsync<T>(
            string addressableKey, Transform parent = null, int defaultCapacity = 10, int maxSize = 100,
            Action<T> onGet = null, Action<T> onRelease = null, CancellationToken token = default) where T : Component
        {
            var existingPool = GetPool<T>();
            return existingPool ?? await CreatePoolAsync(addressableKey, parent, defaultCapacity, maxSize, onGet, onRelease, token);
        }

        public static async UniTask<Pooling<T>> GetOrCreatePoolAsync<T>(
            string addressableKey, int preloadCount, Transform parent = null, int defaultCapacity = 10, int maxSize = 100,
            Action<T> onGet = null, Action<T> onRelease = null, CancellationToken token = default) where T : Component
        {
            var existingPool = GetPool<T>();
            if (existingPool != null) return existingPool;

            var newPool = await CreatePoolAsync(addressableKey, parent, defaultCapacity, maxSize, onGet, onRelease, token);
            if (newPool == null || preloadCount <= 0) return newPool;

            PreloadObjects(newPool, preloadCount); // 리팩토링된 프리로드 로직 호출

            return newPool;
        }
        #endregion

        #region 이름 기반 Addressable 풀 생성 (Name 기반)

        public static Pooling<T> GetPool<T>(string name) where T : Component
        {
            return _namedPools.TryGetValue(name, out var pool) ? pool as Pooling<T> : null;
        }

        public static bool HasPool(string name) => _namedPools.ContainsKey(name);

        public static async UniTask<Pooling<T>> CreatePoolAsync<T>(
            string name, string addressableKey, Transform parent = null, int defaultCapacity = 10, int maxSize = 100,
            Action<T> onGet = null, Action<T> onRelease = null, CancellationToken token = default) where T : Component
        {
            if (_namedPools.TryGetValue(name, out var existingPool))
                return existingPool as Pooling<T>;

            // 💡 개선 2: 동일 이름 중복 비동기 로딩 방지 (가장 흔하게 발생하는 렉의 주범)
            if (_loadingNames.Contains(name))
            {
                await UniTask.WaitWhile(() => _loadingNames.Contains(name), cancellationToken: token);
                return GetPool<T>(name);
            }

            _loadingNames.Add(name);
            try
            {
                var newPool = await Pooling<T>.CreateAsync(
                    addressableKey, parent, defaultCapacity, maxSize, onGet, onRelease, true, token);

                if (newPool == null) return null;

                _namedPools[name] = newPool;
                _namedClearablePools[name] = newPool;
                return newPool;
            }
            finally
            {
                _loadingNames.Remove(name);
            }
        }

        public static async UniTask<Pooling<T>> GetOrCreatePoolAsync<T>(
            string name, string addressableKey, Transform parent = null, int defaultCapacity = 10, int maxSize = 100,
            Action<T> onGet = null, Action<T> onRelease = null, CancellationToken token = default) where T : Component
        {
            return GetPool<T>(name) ?? await CreatePoolAsync(name, addressableKey, parent, defaultCapacity, maxSize, onGet, onRelease, token);
        }

        public static async UniTask<Pooling<T>> GetOrCreatePoolAsync<T>(
            string name, string addressableKey, int preloadCount, Transform parent = null, int defaultCapacity = 10, int maxSize = 100,
            Action<T> onGet = null, Action<T> onRelease = null, CancellationToken token = default) where T : Component
        {
            var existingPool = GetPool<T>(name);
            if (existingPool != null) return existingPool;

            var newPool = await CreatePoolAsync(name, addressableKey, parent, defaultCapacity, maxSize, onGet, onRelease, token);
            if (newPool == null || preloadCount <= 0) return newPool;

            PreloadObjects(newPool, preloadCount);

            return newPool;
        }
        #endregion

        #region 유틸리티 & 풀 해제 (기존과 거의 동일)

        // 💡 개선 3: ListPool을 사용한 GC 없는 프리로드
        private static void PreloadObjects<T>(Pooling<T> pool, int preloadCount) where T : Component
        {
            var preloadList = ListPool<T>.Get();

            for (var i = 0; i < preloadCount; i++)
                preloadList.Add(pool.Get());

            foreach (var obj in preloadList)
                pool.Release(obj);

            ListPool<T>.Release(preloadList); // 메모리 반납 (GC 발생 X)
        }

        public static T Get<T>() where T : Component => GetPool<T>()?.Get();
        public static void Release<T>(T element) where T : Component { if (element != null) GetPool<T>()?.Release(element); }

        public static T Get<T>(string name) where T : Component => GetPool<T>(name)?.Get();
        public static void Release<T>(string name, T element) where T : Component { if (element != null) GetPool<T>(name)?.Release(element); }

        public static void ReleasePool<T>() where T : Component
        {
            var type = typeof(T);
            if (_clearablePools.TryGetValue(type, out var disposable))
            {
                disposable.Dispose();
                _clearablePools.Remove(type);
            }
            _pools.Remove(type);
        }

        public static void ReleasePool(string name)
        {
            if (_namedClearablePools.TryGetValue(name, out var disposable))
            {
                disposable.Dispose();
                _namedClearablePools.Remove(name);
            }
            _namedPools.Remove(name);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetSession()
        {
            // Pools retain callbacks and Unity objects from the previous Play session.
            _pools.Clear();
            _clearablePools.Clear();
            _namedPools.Clear();
            _namedClearablePools.Clear();
            _loadingTypes.Clear();
            _loadingNames.Clear();
        }

        public static void ReleaseAllPools()
        {
            foreach (var disposable in _clearablePools.Values) disposable.Dispose();
            foreach (var disposable in _namedClearablePools.Values) disposable.Dispose();

            ResetSession();
        }

        public static void ClearAllInactivePools()
        {
            foreach (var pool in _clearablePools.Values) pool.Clear();
            foreach (var pool in _namedClearablePools.Values) pool.Clear();
        }

        public static int PoolCount => _pools.Count;
        public static int NamedPoolCount => _namedPools.Count;
        public static IEnumerable<Type> RegisteredPoolTypes => _pools.Keys;
        public static IEnumerable<string> RegisteredPoolNames => _namedPools.Keys;

        #endregion
    }
}