using Backend.Util;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Backend.Object.UI
{
    /// <summary>
    /// 단일 패널 내부에서 탭 전환으로 교체되는 View 의 베이스.
    /// UIManager 가 관리하지 않으며, 부모 Panel 이 Show / Hide 를 직접 호출한다.
    /// </summary>
    public abstract class UIView : CachedMonobehaviour
    {
        [Header("Transition")]
        [SerializeField] private bool _useAnimation;
        [ShowIf(ActionOnConditionFail.JustDisable, ConditionOperator.And, nameof(_useAnimation))]
        [SerializeField] private string _showAnimTrigger = "show";
        [ShowIf(ActionOnConditionFail.JustDisable, ConditionOperator.And, nameof(_useAnimation))]
        [SerializeField] private string _hideAnimTrigger = "hide";
        [ShowIf(ActionOnConditionFail.JustDisable, ConditionOperator.And, nameof(_useAnimation))]
        [SerializeField] private float _hideAnimDelay = 0.2f;

        private Animator _animator;

        protected virtual void Awake()
        {
            TryGetComponent(out _animator);
        }

        public void Show()
        {
            CachedGameObject.SetActive(true);

            if (_useAnimation && _animator != null)
                _animator.SetTrigger(_showAnimTrigger);

            OnShow();
        }

        public void Hide()
        {
            HideAsync().Forget();
        }

        private async UniTaskVoid HideAsync()
        {
            if (_useAnimation && _animator != null && !string.IsNullOrEmpty(_hideAnimTrigger))
            {
                _animator.SetTrigger(_hideAnimTrigger);
                if (_hideAnimDelay > 0f)
                    await UniTask.WaitForSeconds(_hideAnimDelay);
            }

            OnHide();
            CachedGameObject.SetActive(false);
        }

        /// <summary> View 가 표시될 때 호출. 데이터 바인딩 등 진입 처리. </summary>
        protected virtual void OnShow() { }

        /// <summary> View 가 숨겨지기 직전 호출. 상태 정리 처리. </summary>
        protected virtual void OnHide() { }
    }
}
