using Backend.Object.Management;
using Backend.Util;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Backend.Object.UI
{
    /// <summary>
    /// 모든 UI 의 공통 베이스. View 책임만 가지며, Model 은 추후 Presenter 가 DataManager 로 조회.
    /// 공통 라이프사이클(Open SFX / Close Animation / Back Button) 을 처리한다.
    /// </summary>
    public abstract class UIBase : CachedMonobehaviour
    {
        [Header("Open Sound")]
        [SerializeField] private bool _useOpenSound;
        [ShowIf(ActionOnConditionFail.JustDisable, ConditionOperator.And, nameof(_useOpenSound))]
        [SerializeField] private string _openSoundKey;

        [Header("Close Animation")]
        [SerializeField] private bool _useCloseAnimation;
        [ShowIf(ActionOnConditionFail.JustDisable, ConditionOperator.And, nameof(_useCloseAnimation))]
        [SerializeField] private float _closeAnimDelay = 0.2f;
        [ShowIf(ActionOnConditionFail.JustDisable, ConditionOperator.And, nameof(_useCloseAnimation))]
        [SerializeField] private string _closeAnimTrigger = "close";

        [Header("Back Button")]
        [SerializeField] private bool _handleBackButton;

        private Animator _animator;
        private bool _isClosing;

        public abstract UILayer Layer { get; }
        public bool HandleBackButton => _handleBackButton;

        /// <summary>
        /// 컴포넌트가 GameObject 에 처음 부착될 때 _handleBackButton 의 기본값.
        /// 서브클래스에서 override 하여 디폴트를 지정한다 (예: UIPopup = true).
        /// </summary>
        protected virtual bool DefaultHandleBackButton => false;

        protected virtual void Awake()
        {
            TryGetComponent(out _animator);
        }

        /// <summary>
        /// Inspector 에서 컴포넌트가 처음 추가/Reset 될 때 호출되어 디폴트 값을 채운다.
        /// </summary>
        protected virtual void Reset()
        {
            _handleBackButton = DefaultHandleBackButton;
        }

        internal void HandleOpen()
        {
            PlayOpenSfx();
            OnOpen();
        }

        protected virtual void OnOpen() { }
        protected virtual void OnClose() { }

        /// <summary>
        /// 뒤로가기/ESC 입력 시 호출.
        /// true 반환 → UIManager 가 자동으로 이 UI 를 Close.
        /// false 반환 → 닫지 않고 자체 처리(예: 확인 팝업 띄우기) 수행.
        /// </summary>
        public virtual bool OnBackPressed() => true;

        /// <summary>
        /// 닫기 애니메이션을 재생하고 OnClose 를 호출. 풀 반환은 UIManager 가 담당.
        /// </summary>
        internal async UniTask CloseAsync()
        {
            if (_isClosing) return;
            _isClosing = true;

            if (_useCloseAnimation && _animator != null && !string.IsNullOrEmpty(_closeAnimTrigger))
            {
                _animator.SetTrigger(_closeAnimTrigger);
                if (_closeAnimDelay > 0f)
                {
                    await UniTask.WaitForSeconds(_closeAnimDelay);
                }
            }

            OnClose();
            _isClosing = false;
        }

        private void PlayOpenSfx()
        {
            if (!_useOpenSound || string.IsNullOrEmpty(_openSoundKey))
                return;

            AudioManager.PlaySfx(_openSoundKey);
        }
    }
}
