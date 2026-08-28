using System;
using System.Collections.Generic;
using Backend.AddressableKey;
using Backend.Object.UI;
using Backend.Util.Management;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Pool;
using UnityEngine.UI;

namespace Backend.Object.Management
{
    /// <summary>
    /// UI 의 생성/오픈/닫기/뒤로가기 스택을 통합 관리한다.
    /// - Open / Close: 풀이 이미 만들어진 UI 의 동기 오픈/닫기
    /// - OpenAsync (동적 오픈): Addressable 로 풀을 만들고 첫 인스턴스 반환
    /// - CloseDynamic (동적 닫기): 닫음과 동시에 해당 UI 의 풀 자체를 해제
    /// - PopBack: 모바일 뒤로가기 / PC ESC. PuzzleControl.UI.Cancel 액션으로 직접 구독.
    /// </summary>
    public class UIManager : SingletonGameObject<UIManager>
    {
        private readonly struct UILifecycle
        {
            public readonly Action Release;
            public readonly Action ReleasePool;

            public UILifecycle(Action release, Action releasePool)
            {
                Release = release;
                ReleasePool = releasePool;
            }
        }

        [Header("Refs")]
        [SerializeField] private UIRegistry _registry;

        private UniTaskCompletionSource _registryReady;

        private readonly Dictionary<Type, UIBase> _active = new();
        private readonly Dictionary<UIBase, UILifecycle> _lifecycles = new();
        private readonly Stack<UIBase> _backStack = new();
        private readonly Subject<Unit> _onBackEmpty = new();

        private GameObject _blockerRoot;
        private Action<InputAction.CallbackContext> _onCancelPerformed;

        /// <summary> 백 스택이 비어있을 때 뒤로가기 입력이 들어오면 발행되는 이벤트. </summary>
        public static Observable<Unit> OnBackEmpty => Instance._onBackEmpty;

        protected override void OnAwake()
        {
            base.OnAwake();

            _registryReady = new UniTaskCompletionSource();

            if (_registry != null)
                _registryReady.TrySetResult();
            else
                InitRegistryAsync().Forget();
        }

        private async UniTaskVoid InitRegistryAsync()
        {
            var prefab = await ResourceManager.LoadResourceAsync<GameObject>(AddressableKeys.UI.Get("UIRoot"));
            if (prefab == null)
            {
                Debug.LogError("[UIManager] UIRoot 프리팹 로드 실패. _registry 없이 동작합니다.");
                _registryReady.TrySetResult();
                return;
            }

            var go = Instantiate(prefab);
            DontDestroyOnLoad(go);
            _registry = go.GetComponent<UIRegistry>();

            if (_registry == null)
                Debug.LogError("[UIManager] UIRoot 프리팹에 UIRegistry 컴포넌트가 없습니다.");

            _registryReady.TrySetResult();
        }

        private void OnDestroy()
        {
            _onBackEmpty?.Dispose();
        }

        #region Open Internal

        /// <summary>
        /// 풀이 이미 만들어져 있는 UI 를 동기로 오픈한다.
        /// </summary>
        private T Open_Internal<T>() where T : UIBase
        {
            if (!ObjectPoolManager.HasPool<T>())
            {
                Debug.LogError($"[UIManager] Pool for {typeof(T).Name} not created. Use OpenAsync first.");
                return null;
            }

            var ui = ObjectPoolManager.Get<T>();
            if (ui == null)
            {
                Debug.LogError($"[UIManager] Failed to get {typeof(T).Name} from pool.");
                return null;
            }

            RegisterLifecycle(ui, ui);
            Activate(ui);
            return ui;
        }

        /// <summary>
        /// Addressable 에서 비동기 로드하여 풀을 생성한 뒤 첫 인스턴스를 오픈한다.
        /// addressableKey 가 null 이면 AddressableKeys.UI.Get&lt;T&gt;() 를 사용.
        /// </summary>
        private async UniTask<T> OpenAsync_Internal<T>(string addressableKey) where T : UIBase
        {
            await _registryReady.Task;

            if (!ObjectPoolManager.HasPool<T>())
            {
                var key = addressableKey ?? AddressableKeys.UI.Get<T>();
                if (string.IsNullOrEmpty(key))
                {
                    Debug.LogError($"[UIManager] Addressable key for {typeof(T).Name} is empty.");
                    return null;
                }

                var pool = await ObjectPoolManager.GetOrCreatePoolAsync<T>(
                    addressableKey: key,
                    parent: null,
                    onGet: instance => Reparent(instance),
                    onRelease: null);

                if (pool == null)
                {
                    Debug.LogError($"[UIManager] Failed to create pool for {typeof(T).Name} (key={key}).");
                    return null;
                }
            }

            return Open_Internal<T>();
        }

        #endregion

        #region Close Internal

        private void Close_Internal<T>(T ui) where T : UIBase
        {
            if (ui == null) return;
            RunCloseAsync(ui, releasePool: false).Forget();
        }

        private void CloseDynamic_Internal<T>(T ui) where T : UIBase
        {
            if (ui == null) return;
            RunCloseAsync(ui, releasePool: true).Forget();
        }

        private async UniTaskVoid RunCloseAsync(UIBase target, bool releasePool)
        {
            if (target == null) return;

            await target.CloseAsync();

            _active.Remove(target.GetType());
            RemoveFromBackStack(target);

            if (_lifecycles.TryGetValue(target, out var lifecycle))
            {
                _lifecycles.Remove(target);
                lifecycle.Release?.Invoke();
                if (releasePool)
                {
                    lifecycle.ReleasePool?.Invoke();
                }
            }
            else
            {
                Debug.LogWarning($"[UIManager] Lifecycle missing for {target.GetType().Name}. Falling back to deactivate.");
                target.gameObject.SetActive(false);
            }
        }

        #endregion

        #region Back Stack Internal

        /// <summary>
        /// 뒤로가기 처리. InputActionHandler 콜백 또는 외부에서 직접 호출 가능.
        /// </summary>
        private void PopBack_Internal()
        {
            while (_backStack.Count > 0)
            {
                var top = _backStack.Peek();
                if (top == null)
                {
                    _backStack.Pop();
                    continue;
                }

                if (!top.OnBackPressed())
                {
                    return;
                }

                _backStack.Pop();
                RunCloseAsync(top, releasePool: false).Forget();
                return;
            }

            _onBackEmpty.OnNext(Unit.Default);
        }

        private void RemoveFromBackStack(UIBase ui)
        {
            if (ui == null || _backStack.Count == 0 || !ui.HandleBackButton) return;

            if (ReferenceEquals(_backStack.Peek(), ui))
            {
                _backStack.Pop();
                return;
            }

            var temp = ListPool<UIBase>.Get();
            try
            {
                while (_backStack.Count > 0)
                {
                    var item = _backStack.Pop();
                    if (ReferenceEquals(item, ui))
                    {
                        break;
                    }
                    temp.Add(item);
                }

                for (int i = temp.Count - 1; i >= 0; i--)
                {
                    _backStack.Push(temp[i]);
                }
            }
            finally
            {
                ListPool<UIBase>.Release(temp);
            }
        }

        #endregion

        #region Helpers

        private void Activate(UIBase ui)
        {
            Reparent(ui);
            _active[ui.GetType()] = ui;

            if (ui.HandleBackButton)
            {
                _backStack.Push(ui);
            }

            ui.HandleOpen();
        }

        private void Reparent(UIBase ui)
        {
            if (ui == null || _registry == null) return;

            var root = _registry.GetRoot(ui.Layer);
            if (root == null)
            {
                Debug.LogWarning($"[UIManager] No root mapped for layer '{ui.Layer}'. {ui.GetType().Name} will stay at scene root.");
                return;
            }

            ui.transform.SetParent(root, false);
            ui.transform.SetAsLastSibling();
        }

        /// <summary>
        /// Open&lt;T&gt; 시점에 컴파일 타임 타입 T 를 캡처한 Release/ReleasePool 델리게이트를 등록.
        /// 백 스택을 통해 닫힐 때처럼 런타임 타입만 알아도 정확한 풀 메서드를 호출할 수 있게 해준다.
        /// </summary>
        private void RegisterLifecycle<T>(UIBase key, T target) where T : UIBase
        {
            _lifecycles[key] = new UILifecycle(
                release: () => ObjectPoolManager.Release(target),
                releasePool: () => ObjectPoolManager.ReleasePool<T>());
        }

        #endregion

        #region Block / Close All Internal

        private UniTask BlockUI_Internal()
        {
            if (_blockerRoot != null)
            {
                _blockerRoot.SetActive(true);
                return UniTask.CompletedTask;
            }

            _blockerRoot = new GameObject("[UIManager] InputBlocker");
            DontDestroyOnLoad(_blockerRoot);

            var canvas = _blockerRoot.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = short.MaxValue;
            _blockerRoot.AddComponent<GraphicRaycaster>();

            var imageGo = new GameObject("Image");
            imageGo.transform.SetParent(_blockerRoot.transform, false);
            var img = imageGo.AddComponent<Image>();
            img.color = new Color(0f, 0f, 0f, 0f);
            var rt = img.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            return UniTask.CompletedTask;
        }

        private void UnblockUI_Internal()
        {
            if (_blockerRoot != null) _blockerRoot.SetActive(false);
        }

        private void CloseAllUI_Internal()
        {
            var snapshot = ListPool<UIBase>.Get();
            try
            {
                snapshot.AddRange(_active.Values);
                foreach (var ui in snapshot)
                {
                    Close_Internal(ui);
                }
            }
            finally
            {
                ListPool<UIBase>.Release(snapshot);
            }

            _backStack.Clear();
            UnblockUI_Internal();
        }

        #endregion

        #region Static Public Methods

        /// <summary>
        /// 풀이 이미 만들어져 있는 UI 를 동기로 오픈한다.
        /// </summary>
        public static T Open<T>() where T : UIBase
            => Instance.Open_Internal<T>();

        /// <summary>
        /// Addressable 에서 비동기 로드하여 풀을 생성한 뒤 첫 인스턴스를 오픈한다.
        /// addressableKey 가 null 이면 AddressableKeys.UI.Get&lt;T&gt;() 를 사용.
        /// </summary>
        public static UniTask<T> OpenAsync<T>(string addressableKey = null) where T : UIBase
            => Instance.OpenAsync_Internal<T>(addressableKey);

        /// <summary>
        /// UI 를 닫고 풀로 반환한다 (풀은 유지).
        /// </summary>
        public static void Close<T>(T ui) where T : UIBase
            => Instance.Close_Internal(ui);

        /// <summary>
        /// UI 를 닫고 해당 타입의 풀까지 해제한다 (Addressable 핸들도 반환).
        /// </summary>
        public static void CloseDynamic<T>(T ui) where T : UIBase
            => Instance.CloseDynamic_Internal(ui);

        /// <summary>
        /// 뒤로가기 처리. InputActionHandler 콜백 또는 외부에서 직접 호출 가능.
        /// </summary>
        public static void PopBack()
            => Instance.PopBack_Internal();

        /// <summary>
        /// 입력을 받지 않는 풀스크린 블로커를 활성화한다. 씬 전환 중 입력 차단 용도.
        /// </summary>
        public static UniTask BlockUI()
            => Instance.BlockUI_Internal();

        /// <summary>
        /// BlockUI 로 활성화된 블로커를 비활성화한다.
        /// </summary>
        public static void UnblockUI()
            => Instance.UnblockUI_Internal();

        /// <summary>
        /// 현재 활성화된 모든 UI 를 닫고 백 스택과 블로커를 정리한다.
        /// </summary>
        public static void CloseAllUI()
            => Instance.CloseAllUI_Internal();

        #endregion
    }
}
