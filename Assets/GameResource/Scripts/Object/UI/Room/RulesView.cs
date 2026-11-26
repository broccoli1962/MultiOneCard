using System;
using Backend.App;
using Game.Rules;
using TMPro;
using UnityEngine;

namespace Backend.Object.UI
{
    /// <summary>
    /// 대기실 규칙 오버레이. 현재 룰 요약과 카드 목록 진입만 담당한다.
    /// </summary>
    public sealed class RulesView : UIView
    {
        private static readonly Color _bodyColor = new Color32(255, 232, 200, 255);

        [SerializeField] private TextMeshProUGUI _summaryText;
        [SerializeField] private GameObject _hostRoot;
        [SerializeField] private CommonButton _cardListButton;
        [SerializeField] private CommonButton _closeButton;

        private bool _layoutReady;

        /// <summary>특수 카드 목록 패널.</summary>
        public event Action CardListClicked;

        /// <summary>규칙 오버레이 닫기.</summary>
        public event Action CloseClicked;

        /// <summary>
        /// 프리팹 자식에 묶인 위젯을 찾아 이벤트를 묶는다.
        /// </summary>
        public void EnsureLayout()
        {
            if (_layoutReady && _summaryText != null && _cardListButton != null && _closeButton != null)
            {
                return;
            }

            if (_summaryText == null)
            {
                var body = CachedTransform.Find("RulesBody")
                    ?? CachedTransform.Find("RulesScroll/Viewport/Content/RulesBody");
                if (body != null)
                {
                    body.TryGetComponent(out _summaryText);
                }
            }

            _hostRoot ??= FindChild("HostSettings");
            _cardListButton ??= FindButton("CardList");
            _closeButton ??= FindButton("Close");
            BindButton(_cardListButton, () => CardListClicked?.Invoke());
            BindButton(_closeButton, () => CloseClicked?.Invoke());
            if (_hostRoot != null)
            {
                _hostRoot.SetActive(false);
            }

            _layoutReady = true;
        }

        /// <summary>
        /// 현재 하우스룰 요약을 검은 글씨로 그린다. 방장 편집 UI는 숨긴다.
        /// </summary>
        public void Render(HouseRules rules)
        {
            EnsureLayout();
            if (_hostRoot != null)
            {
                _hostRoot.SetActive(false);
            }

            if (_summaryText == null)
            {
                return;
            }

            _summaryText.color = _bodyColor;
            _summaryText.alignment = TextAlignmentOptions.TopLeft;
            _summaryText.lineSpacing = 10f;
            _summaryText.textWrappingMode = TextWrappingModes.Normal;
            _summaryText.overflowMode = TextOverflowModes.Overflow;
            _summaryText.text = HouseRulesText.Format(rules);
            _summaryText.ForceMeshUpdate();
            var height = Mathf.Max(_summaryText.preferredHeight + 16f, 400f);
            var bodyRt = _summaryText.rectTransform;
            bodyRt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
            var content = bodyRt.parent as RectTransform;
            if (content != null)
            {
                content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
            }
        }

        protected override void OnShow()
        {
            EnsureLayout();
            Render(HouseRules.Official);
            base.OnShow();
        }

    }
}
