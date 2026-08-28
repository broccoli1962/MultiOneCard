using System;
using UnityEngine;

namespace Backend.Object.UI
{
    /// <summary>
    /// 특수 카드 목록 View. 닫기만 담당한다. 행은 프리팹에 정적 배치한다.
    /// </summary>
    public sealed class CardListPanel : UIPopup<CardListPresenter>
    {
        [SerializeField] private CommonButton _closeButton;

        private bool _layoutReady;

        /// <summary>닫기.</summary>
        public event Action CloseClicked;

        protected override void Awake()
        {
            if (Application.isPlaying)
            {
                EnsureLayout();
            }

            base.Awake();
        }

        /// <summary>
        /// 프리팹 자식에 묶인 위젯을 찾아 이벤트를 묶는다.
        /// </summary>
        public void EnsureLayout()
        {
            if (_layoutReady && _closeButton != null)
            {
                return;
            }

            _closeButton ??= FindButton("Close");
            BindButton(_closeButton, () => CloseClicked?.Invoke());
            _layoutReady = true;
        }

        private CommonButton FindButton(string name)
        {
            var existing = CachedTransform.Find(name);
            if (existing == null || !existing.TryGetComponent(out CommonButton button))
            {
                return null;
            }

            button.useSound = false;
            return button;
        }

        private static void BindButton(CommonButton button, UnityEngine.Events.UnityAction action)
        {
            if (button == null)
            {
                return;
            }

            button.OnClick.RemoveAllListeners();
            button.OnClick.AddListener(action);
        }
    }
}
