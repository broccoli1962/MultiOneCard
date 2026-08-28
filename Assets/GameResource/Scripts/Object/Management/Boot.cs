using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Backend.Object.Management
{
    public class Boot : MonoBehaviour
    {
        private static Boot _instance;
        private static UniTaskCompletionSource _readySource = new UniTaskCompletionSource();

        /// <summary>
        /// 부트스트랩 초기화가 완료되었는지 여부.
        /// </summary>
        public static bool IsReady { get; private set; }

        /// <summary>
        /// 부트스트랩 초기화가 끝날 때까지 대기한다. 이미 완료된 경우 즉시 반환한다.
        /// </summary>
        public static UniTask WaitUntilReadyAsync()
        {
            if (IsReady)
                return UniTask.CompletedTask;

            return _readySource.Task;
        }

        /// <summary>
        /// 도메인 리로드 비활성화/씬 재진입 등에서 잔존하는 정적 상태를 안전하게 초기화한다.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            IsReady = false;
            _readySource = new UniTaskCompletionSource();
            _instance = null;
        }

        private void Awake()
        {
            // 앱 단위 부트는 1회만 존재한다. 씬 재진입 시 새로 생성된 Boot 은 제거한다.
            if (_instance != null)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);

            InitializeAsync().Forget();
        }

        private async UniTaskVoid InitializeAsync()
        {
            try
            {
                await GameManager.InitializeCore();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[Boot] Initialization failed: {e}");
                _readySource.TrySetException(e);
                return;
            }

            IsReady = true;
            _readySource.TrySetResult();
        }
    }
}