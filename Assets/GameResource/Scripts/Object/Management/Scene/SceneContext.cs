using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Backend.Object.Management
{
    /// <summary>
    /// 각 씬의 진입점(씬 부트). 앱 코어(Boot) 초기화가 끝난 뒤 씬 전용 초기화를 수행한다.
    /// 씬마다 이 클래스를 상속한 컴포넌트를 하나 배치한다.
    /// - 진입: Boot 준비 완료를 기다린 뒤 OnEnterAsync 호출
    /// - 이탈: 씬 언로드 시 OnExit 호출 (앱 종료 중에는 호출하지 않음)
    /// </summary>
    public abstract class SceneContext : MonoBehaviour
    {
        private void Start()
        {
            EnterAsync().Forget();
        }

        private async UniTaskVoid EnterAsync()
        {
            await Boot.WaitUntilReadyAsync();
            await OnEnterAsync();
        }

        private void OnDestroy()
        {
            if (GameStateUtil.IsQuitting) return;
            OnExit();
        }

        /// <summary> 코어 초기화 완료 후 호출되는 씬 전용 진입 처리. </summary>
        protected abstract UniTask OnEnterAsync();

        /// <summary> 씬 이탈(언로드) 시 호출되는 정리. 앱 종료 중에는 호출되지 않는다. </summary>
        protected virtual void OnExit() { }
    }
}
