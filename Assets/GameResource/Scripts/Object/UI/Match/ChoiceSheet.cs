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

        private static readonly string[] SuitLabels = { "♠", "♥", "♦", "♣", "★", "☾" };

        private static readonly Color SuitBlack = new Color(0.18f, 0.18f, 0.2f, 1f);
        private static readonly Color SuitRed = new Color(0.72f, 0.18f, 0.2f, 1f);
        private static readonly Color SuitBlue = new Color(0.16f, 0.32f, 0.72f, 1f);

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

        private readonly CommonButton[] _suitButtons = new CommonButton[6];
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
        /// 프리팹 미배선이어도 시트를 채운다.
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

            var rt = CachedRectTransform;
            rt.anchorMin = new Vector2(0.5f, 0f);
            rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(0f, 430f);
            rt.sizeDelta = new Vector2(720f, 200f);

            if (!TryGetComponent(out Image bg))
            {
                bg = CachedGameObject.AddComponent<Image>();
            }

            bg.color = new Color(0.08f, 0.1f, 0.14f, 0.92f);
            bg.raycastTarget = true;

            _title = FindOrCreateText("Title", new Vector2(0.5f, 1f), new Vector2(0f, -28f), new Vector2(680f, 40f), 30f);
            _suitGrid = FindOrCreateSuitGrid();
            _queenRow = FindOrCreateChoiceRow("QueenRow",
                ("Reverse", "뒤집기"), ("Give", "주기"),
                out _queenReverseButton, out _queenGiveButton);
            _kingRow = FindOrCreateChoiceRow("KingRow",
                ("Extra", "한장더"), ("Hide", "숨기기"),
                out _kingExtraButton, out _kingHideButton);
            _confirmButton = FindOrCreateButton("Confirm", "확정", new Vector2(0f, -64f), new Vector2(220f, 72f));

            for (var i = 0; i < _suitButtons.Length; i++)
            {
                var suit = SuitCodes[i];
                BindButton(_suitButtons[i], () => SuitClicked?.Invoke(suit));
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

            _title.text = TitleFor(prompt);
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

        private GameObject FindOrCreateSuitGrid()
        {
            var existing = CachedTransform.Find("SuitGrid");
            GameObject go;
            if (existing != null)
            {
                go = existing.gameObject;
            }
            else
            {
                go = new GameObject("SuitGrid", typeof(RectTransform), typeof(GridLayoutGroup));
                go.transform.SetParent(CachedTransform, false);
            }

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(0f, -12f);
            rt.sizeDelta = new Vector2(400f, 160f);

            if (!go.TryGetComponent(out GridLayoutGroup grid))
            {
                grid = go.AddComponent<GridLayoutGroup>();
            }

            grid.cellSize = new Vector2(120f, 72f);
            grid.spacing = new Vector2(8f, 8f);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 3;
            grid.childAlignment = TextAnchor.MiddleCenter;
            grid.startAxis = GridLayoutGroup.Axis.Horizontal;

            for (var i = 0; i < SuitCodes.Length; i++)
            {
                _suitButtons[i] = FindOrCreateChildButton(go.transform, "Suit_" + SuitCodes[i], SuitLabels[i], SuitTint(i));
            }

            return go;
        }

        private GameObject FindOrCreateChoiceRow(
            string name,
            (string id, string label) left,
            (string id, string label) right,
            out CommonButton leftButton,
            out CommonButton rightButton)
        {
            var existing = CachedTransform.Find(name);
            GameObject go;
            if (existing != null)
            {
                go = existing.gameObject;
            }
            else
            {
                go = new GameObject(name, typeof(RectTransform), typeof(HorizontalLayoutGroup));
                go.transform.SetParent(CachedTransform, false);
            }

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(0f, -12f);
            rt.sizeDelta = new Vector2(520f, 80f);
            if (!go.TryGetComponent(out HorizontalLayoutGroup layout))
            {
                layout = go.AddComponent<HorizontalLayoutGroup>();
            }

            layout.spacing = 16f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            leftButton = FindOrCreateChildButton(go.transform, name + left.id, left.label, new Color(0.2f, 0.2f, 0.22f, 0.95f));
            rightButton = FindOrCreateChildButton(go.transform, name + right.id, right.label, new Color(0.2f, 0.2f, 0.22f, 0.95f));
            return go;
        }

        private CommonButton FindOrCreateButton(string name, string label, Vector2 pos, Vector2 size)
        {
            var existing = CachedTransform.Find(name);
            GameObject go;
            if (existing != null)
            {
                go = existing.gameObject;
            }
            else
            {
                go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(CommonButton));
                go.transform.SetParent(CachedTransform, false);
            }

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            if (go.TryGetComponent(out Image image))
            {
                image.color = new Color(0.2f, 0.2f, 0.22f, 0.95f);
            }

            if (!go.TryGetComponent(out CommonButton button))
            {
                button = go.AddComponent<CommonButton>();
            }

            button.useSound = false;
            EnsureButtonLabel(go.transform, label, 28f);
            return button;
        }

        private CommonButton FindOrCreateChildButton(Transform parent, string name, string label, Color tint)
        {
            var existing = parent.Find(name);
            GameObject go;
            if (existing != null)
            {
                go = existing.gameObject;
            }
            else
            {
                go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(CommonButton));
                go.transform.SetParent(parent, false);
            }

            go.GetComponent<RectTransform>().sizeDelta = new Vector2(220f, 72f);
            if (go.TryGetComponent(out Image image))
            {
                image.color = tint;
            }

            if (!go.TryGetComponent(out CommonButton button))
            {
                button = go.AddComponent<CommonButton>();
            }

            button.useSound = false;
            EnsureButtonLabel(go.transform, label, 28f);
            var text = go.GetComponentInChildren<Text>();
            if (text != null && name.StartsWith("Suit_", StringComparison.Ordinal))
            {
                text.color = LabelColor(name[name.Length - 1]);
            }

            return button;
        }

        private Text FindOrCreateText(string name, Vector2 anchor, Vector2 pos, Vector2 size, float fontSize)
        {
            var existing = CachedTransform.Find(name);
            GameObject go;
            if (existing != null)
            {
                go = existing.gameObject;
            }
            else
            {
                go = new GameObject(name, typeof(RectTransform), typeof(Text));
                go.transform.SetParent(CachedTransform, false);
            }

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            if (!go.TryGetComponent(out Text text))
            {
                text = go.AddComponent<Text>();
            }

            text.fontSize = (int)fontSize;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.raycastTarget = false;
            if (_font != null)
            {
                text.font = _font;
            }

            return text;
        }

        private void EnsureButtonLabel(Transform parent, string label, float fontSize)
        {
            var existing = parent.Find("Label");
            GameObject go;
            if (existing != null)
            {
                go = existing.gameObject;
            }
            else
            {
                go = new GameObject("Label", typeof(RectTransform), typeof(Text));
                go.transform.SetParent(parent, false);
            }

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            if (!go.TryGetComponent(out Text text))
            {
                text = go.AddComponent<Text>();
            }

            text.text = label;
            text.fontSize = (int)fontSize;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.raycastTarget = false;
            if (_font != null)
            {
                text.font = _font;
            }
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

        private static Color SuitTint(int index)
        {
            switch (index)
            {
                case 1:
                case 2:
                    return new Color(0.42f, 0.16f, 0.18f, 0.95f);
                case 4:
                case 5:
                    return new Color(0.16f, 0.22f, 0.42f, 0.95f);
                default:
                    return new Color(0.2f, 0.2f, 0.22f, 0.95f);
            }
        }

        private static Color LabelColor(char suit)
        {
            switch (suit)
            {
                case 'H':
                case 'D':
                    return SuitRed;
                case 'R':
                case 'M':
                    return SuitBlue;
                default:
                    return SuitBlack;
            }
        }
    }
}
