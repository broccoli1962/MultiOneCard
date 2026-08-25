using System;
using Backend.Net;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Backend.Object.UI
{
    /// <summary>
    /// 7 문양 2×3, Q Reverse/Give, K Extra/Hide, 지급·숨김·미러 안내.
    /// 지급·숨김·미러 확정은 손패 드래그. 입력만 올리고 판결하지 않는다.
    /// </summary>
    public sealed class ChoiceSheet : UIView
    {
        private static readonly string[] SuitCodes =
        {
            SuitCode.Spade, SuitCode.Heart, SuitCode.Diamond, SuitCode.Club, SuitCode.Star, SuitCode.Moon,
        };

        [SerializeField] private TMP_FontAsset _font;
        [SerializeField] private TextMeshProUGUI _title;
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

        /// <summary>지급·미러 확정 버튼. 드래그로 바꾸어 쓰지 않는다.</summary>
        public event Action ConfirmClicked;

        /// <summary>
        /// 프리팹 자식에 묶인 시트를 찾아 이벤트를 묶는다.
        /// </summary>
        public void EnsureLayout(TMP_FontAsset font = null)
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
            PaintSuitButtons();

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
                || prompt == MatchPrompt.HideUnder
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
            if (prompt == MatchPrompt.Suit)
            {
                PaintSuitButtons();
            }

            SetActive(_queenRow, prompt == MatchPrompt.QueenMode);
            SetActive(_kingRow, prompt == MatchPrompt.KingMode);
            if (_confirmButton != null)
            {
                _confirmButton.CachedGameObject.SetActive(false);
            }

            Show();
        }

        /// <summary>
        /// 시트 버튼에 묶인 문양 스프라이트. 없으면 null.
        /// </summary>
        public Sprite SuitSprite(string suit)
        {
            EnsureLayout();
            var index = SuitIndex(suit);
            var button = SuitButton(index);
            if (button == null || !button.TryGetComponent(out Image image))
            {
                return null;
            }

            return image.sprite;
        }

        /// <summary>문양 글리프. ♠♥♦♣★☾.</summary>
        public static string SuitGlyph(string suit)
        {
            switch (suit)
            {
                case SuitCode.Spade:
                    return "♠";
                case SuitCode.Heart:
                    return "♥";
                case SuitCode.Diamond:
                    return "♦";
                case SuitCode.Club:
                    return "♣";
                case SuitCode.Star:
                    return "★";
                case SuitCode.Moon:
                    return "☾";
                default:
                    return suit ?? string.Empty;
            }
        }

        /// <summary>글리프 색.</summary>
        public static Color SuitForeground(string suit)
        {
            switch (suit)
            {
                case SuitCode.Heart:
                case SuitCode.Diamond:
                case SuitCode.Star:
                case SuitCode.Moon:
                    return Color.white;
                default:
                    return new Color(0.08f, 0.08f, 0.1f, 1f);
            }
        }

        /// <summary>스프라이트가 없을 때 배경색.</summary>
        public static Color SuitBackground(string suit)
        {
            switch (suit)
            {
                case SuitCode.Heart:
                case SuitCode.Diamond:
                    return new Color(0.55f, 0.16f, 0.18f, 1f);
                case SuitCode.Star:
                case SuitCode.Moon:
                    return new Color(0.16f, 0.28f, 0.55f, 1f);
                default:
                    return new Color(0.92f, 0.93f, 0.95f, 1f);
            }
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

        private void PaintSuitButtons()
        {
            Sprite sharedSprite = null;
            for (var i = 0; i < SuitCodes.Length; i++)
            {
                var button = SuitButton(i);
                if (button != null && button.TryGetComponent(out Image image) && image.sprite != null)
                {
                    sharedSprite = image.sprite;
                    break;
                }
            }

            for (var i = 0; i < SuitCodes.Length; i++)
            {
                var button = SuitButton(i);
                if (button == null)
                {
                    continue;
                }

                if (button.TryGetComponent(out Image image))
                {
                    if (image.sprite == null && sharedSprite != null)
                    {
                        image.sprite = sharedSprite;
                    }

                    image.color = image.sprite != null ? Color.white : SuitBackground(SuitCodes[i]);
                    image.preserveAspect = image.sprite != null;
                }

                var label = button.GetComponentInChildren<TextMeshProUGUI>(true);
                if (label == null)
                {
                    continue;
                }

                var painted = button.TryGetComponent(out Image paintedImage) && paintedImage.sprite != null;
                label.enabled = !painted;
                label.text = SuitGlyph(SuitCodes[i]);
                label.color = SuitForeground(SuitCodes[i]);
                label.alignment = TextAlignmentOptions.Center;
                label.raycastTarget = false;
            }
        }

        private static int SuitIndex(string suit)
        {
            for (var i = 0; i < SuitCodes.Length; i++)
            {
                if (SuitCodes[i] == suit)
                {
                    return i;
                }
            }

            return -1;
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

        private TextMeshProUGUI FindOrCreateText(string name)
        {
            var go = FindOrCreate(name);
            return go != null && go.TryGetComponent(out TextMeshProUGUI text) ? text : null;
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
                case MatchPrompt.HideUnder:
                    return "숨길 장";
                case MatchPrompt.MirrorDiscard:
                    return "버릴 장";
                default:
                    return string.Empty;
            }
        }

    }
}
