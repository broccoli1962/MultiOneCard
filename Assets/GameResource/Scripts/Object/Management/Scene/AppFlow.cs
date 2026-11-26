using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Backend.Object.Management
{
    public static class AppFlow
    {
        private static UniTaskCompletionSource _ready;
        private static SceneContext _current;
        private static bool _transitioning;

        public static bool IsReady { get; private set; }
        /// <summary>코어 초기화와 첫 씬 진입이 완료될 때까지 기다린다.</summary>
        public static UniTask WaitUntilReadyAsync() => _ready.Task;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            IsReady = false;
            _ready = new UniTaskCompletionSource();
            _current = null;
            _transitioning = false;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Run() => RunAsync().Forget();

        private static async UniTask RunAsync()
        {
            var curtain = CreateCurtain();
            try
            {
                // BeforeSceneLoad also runs when entering Play directly from GameScene.
                await UniTask.NextFrame();
                await GameManager.InitializeCore();
                var name = SceneManager.GetActiveScene().name;
                if (Enum.TryParse(name, out SceneId id))
                {
                    _current = SceneContexts.Create(id);
                    await _current.PreloadAsync();
                    await _current.EnterAsync();
                }
                IsReady = true;
                _ready.TrySetResult();
            }
            catch (Exception e)
            {
                _ready.TrySetException(e);
                Debug.LogException(e);
            }
            finally
            {
                UnityEngine.Object.Destroy(curtain);
            }
        }

        /// <summary>대상 화면을 준비하고 현재 씬을 정리한 뒤 새 씬에 진입한다.</summary>
        public static async UniTask LoadSceneAsync(SceneId next)
        {
            await WaitUntilReadyAsync();
            if (_transitioning || SceneManager.GetActiveScene().name == next.ToString())
                return;

            if (!Application.CanStreamedLevelBeLoaded(next.ToString()))
                throw new InvalidOperationException($"Scene is missing from build settings: {next}");

            var context = SceneContexts.Create(next);
            var curtain = CreateCurtain();
            _transitioning = true;
            try
            {
                // Finish Addressables while the old context is still usable. Holding
                // allowSceneActivation at false can stall queued Addressables requests.
                await context.PreloadAsync();
                if (_current != null)
                    await _current.ExitAsync();
                await UIManager.CloseAllUIAsync();

                await SceneManager.LoadSceneAsync(next.ToString()).ToUniTask();
                _current = context;
                await context.EnterAsync();
            }
            finally
            {
                UnityEngine.Object.Destroy(curtain);
                _transitioning = false;
            }
        }

        private static GameObject CreateCurtain()
        {
            var root = new GameObject("[AppFlow] Curtain", typeof(Canvas), typeof(GraphicRaycaster));
            UnityEngine.Object.DontDestroyOnLoad(root);
            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = short.MaxValue;
            var image = new GameObject("Backdrop", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            image.transform.SetParent(root.transform, false);
            image.rectTransform.anchorMin = Vector2.zero;
            image.rectTransform.anchorMax = Vector2.one;
            image.rectTransform.offsetMin = image.rectTransform.offsetMax = Vector2.zero;
            image.color = Color.black;
            return root;
        }
    }
}
