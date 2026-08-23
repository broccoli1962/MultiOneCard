using System;
using Backend.Net;
using UnityEngine;
using UnityEngine.UI;

namespace Backend.Object.UI
{
    /// <summary>
    /// 7 문양 2×3, Q Reverse/Give, K Extra/Hide, 미러 버림, Q 지급 시트.
    /// 입력만 올리고 판결하지 않는다. 커맨드는 GamePointer 가 낸다.
    /// </summary>
    public sealed class ChoiceSheet : UIView
    {
        private static readonly string[] SuitCodes =
        {
            SuitCode.Spade, SuitCode.Heart, SuitCode.Diamond, SuitCode.Club, SuitCode.Star, SuitCode.Moon,
        };

        [SerializeField] private Font _font;
        [SerializeField] private Text _title;
        [SerializeField] private GameObject _suitGrid;
        [SerializeField] private GameObject _queenRow;
        [SerializeField] private GameObject _kingRow;
        [SerializeField] private CommonButton _confirmButton;
        [SerializeField] private CommonButton _queenReverseButton;
        [SerializeField] private CommonButton _queenGiveButton;
        [SerializeField] private CommonButton _kingExtraButton;
        [SerializeField] private CommonButton _kingHideButton;
        [SerializeField] private CommonButton[] _suitButtons = new CommonButton[6];

        private bool _layoutReady;

        /// <summary>7 이후 문양. 값은 SuitCode.</summary>
        public event Action<string> SuitClicked;

        /// <summary>Q Reverse|Give.</summary>
        public event Action<string> QueenModeClicked;

        /// <summary>K Extra|Hide.</summary>
        public event Action<string> KingModeClicked;

        /// <summary>지급·미러 확정. GamePointer.Confirm.</summary>
        public event Action ConfirmClicked;

        /// <summary>
        /// 프리팹 자식에 묶인 시트를 찾아 이벤트를 묶는다.
        /// </summary>
        public void EnsureLayout(Font font = null)
        {
            if (font != null)
            {
                _font = font;
            }

            if (_layoutReady && _suitGrid != null && _confirmButton != null)
            {
                return;
            }

            _title ??= FindOrCreateText("Title");
            _suitGrid ??= FindOrCreate("SuitGrid");
            BindSuitButtons();
            _queenRow ??= FindOrCreate("QueenRow");
            _kingRow ??= FindOrCreate("KingRow");
            _queenReverseButton ??= FindOrCreateChildButton(_queenRow, "QueenRowReverse");
            _queenGiveButton ??= FindOrCreateChildButton(_queenRow, "QueenRowGive");
            _kingExtraButton ??= FindOrCreateChildButton(_kingRow, "KingRowExtra");
            _kingHideButton ??= FindOrCreateChildButton(_kingRow, "KingRowHide");
            _confirmButton ??= FindOrCreateButton("Confirm");

            for (var i = 0; i < SuitCodes.Length; i++)
            {
                var suit = SuitCodes[i];
                BindButton(SuitButton(i), () => SuitClicked?.Invoke(suit));
            }

            BindButton(_queenReverseButton, () => QueenModeClicked?.Invoke(QueenModeName.Reverse));
            BindButton(_queenGiveButton, () => QueenModeClicked?.Invoke(QueenModeName.Give));
            BindButton(_kingExtraButton, () => KingModeClicked?.Invoke(KingModeName.Extra));
            BindButton(_kingHideButton, () => KingModeClicked?.Invoke(KingModeName.Hide));
            BindButton(_confirmButton, () => ConfirmClicked?.Invoke());

            _layoutReady = true;
            Apply(MatchPrompt.None);
        }

        /// <summary>
        /// 호스트가 요구한 시트만 보여 준다. 시간 초과는 화면이 판결하지 않는다.
        /// </summary>
        public void Apply(MatchPrompt prompt)
        {
            EnsureLayout();

            var show = prompt == MatchPrompt.Suit
                || prompt == MatchPrompt.QueenMode
                || prompt == MatchPrompt.KingMode
                || prompt == MatchPrompt.GiveCards
                || prompt == MatchPrompt.MirrorDiscard;

            if (!show)
            {
                Hide();
                return;
            }

            if (_title != null)
            {
                _title.text = TitleFor(prompt);
            }
            SetActive(_suitGrid, prompt == MatchPrompt.Suit);
            SetActive(_queenRow, prompt == MatchPrompt.QueenMode);
            SetActive(_kingRow, prompt == MatchPrompt.KingMode);
            if (_confirmButton != null)
            {
                var confirm = prompt == MatchPrompt.GiveCards || prompt == MatchPrompt.MirrorDiscard;
                _confirmButton.CachedGameObject.SetActive(confirm);
                var label = _confirmButton.GetComponentInChildren<Text>();
                if (label != null)
                {
                    label.text = prompt == MatchPrompt.GiveCards ? "지급" : "버리기";
                }
            }

            Show();
        }

        private void BindSuitButtons()
        {
            if (_suitButtons == null || _suitButtons.Length != SuitCodes.Length)
            {
                _suitButtons = new CommonButton[SuitCodes.Length];
            }

            if (_suitGrid == null)
            {
                return;
            }

            for (var i = 0; i < SuitCodes.Length; i++)
            {
                _suitButtons[i] ??= FindOrCreateChildButton(_suitGrid, "Suit_" + SuitCodes[i]);
            }
        }

        private CommonButton SuitButton(int index)
        {
            return _suitButtons != null && index >= 0 && index < _suitButtons.Length
                ? _suitButtons[index]
                : null;
        }

        private CommonButton FindOrCreateButton(string name)
        {
            var go = FindOrCreate(name);
            if (go == null || !go.TryGetComponent(out CommonButton button))
            {
                return null;
            }

            button.useSound = false;
            return button;
        }

        private CommonButton FindOrCreateChildButton(GameObject parent, string name)
        {
            if (parent == null)
            {
                return null;
            }

            var existing = parent.transform.Find(name);
            if (existing == null || !existing.TryGetComponent(out CommonButton button))
            {
                return null;
            }

            button.useSound = false;
            return button;
        }

        private Text FindOrCreateText(string name)
        {
            var go = FindOrCreate(name);
            return go != null && go.TryGetComponent(out Text text) ? text : null;
        }

        private GameObject FindOrCreate(string name)
        {
            var existing = CachedTransform.Find(name);
            return existing != null ? existing.gameObject : null;
        }

        private static void BindButton(CommonButton button, UnityEngine.Events.UnityAction action)
        {
            if (button == null)
            {
                return;
            }

            if (button.OnClick == null)
            {
                button.OnClick = new UnityEngine.Events.UnityEvent();
            }

            button.OnClick.RemoveAllListeners();
            button.OnClick.AddListener(action);
        }

        private static void SetActive(GameObject go, bool active)
        {
            if (go != null && go.activeSelf != active)
            {
                go.SetActive(active);
            }
        }

        private static string TitleFor(MatchPrompt prompt)
        {
            switch (prompt)
            {
                case MatchPrompt.Suit:
                    return "무늬 고르기";
                case MatchPrompt.QueenMode:
                    return "Q 고르기";
                case MatchPrompt.KingMode:
                    return "K 고르기";
                case MatchPrompt.GiveCards:
                    return "지급할 장";
                case MatchPrompt.MirrorDiscard:
                    return "버릴 장";
                default:
                    return string.Empty;
            }
        }

    }
}
