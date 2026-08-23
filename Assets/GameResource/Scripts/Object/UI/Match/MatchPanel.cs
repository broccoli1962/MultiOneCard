using System;
using System.Collections.Generic;
using Backend.App;
using Backend.Net;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Backend.Object.UI
{
    /// <summary>
    /// 더미 매치 테이블 View. 표시와 입력만 담당한다.
    /// 카드 확정은 <see cref="GamePointer"/> 가 맡고, 7·Q·K·미러 시트는 <see cref="ChoiceSheet"/> 다.
    /// </summary>
    public sealed class MatchPanel : UIPanel<MatchPresenter>, IPointerClickHandler
    {
        [SerializeField] private Font _font;
        [SerializeField] private Text _hudText;
        [SerializeField] private Text _statusText;
        [SerializeField] private Text _resultText;
        [SerializeField] private CardView _discardView;
        [SerializeField] private CardView _deckView;
        [SerializeField] private CardView _opponentView;
        [SerializeField] private RectTransform _handContainer;
        [SerializeField] private HandLayout _handLayout;
        [SerializeField] private CardView _cardPrefab;
        [SerializeField] private CanvasGroup _inputGroup;
        [SerializeField] private CommonButton _drawButton;
        [SerializeField] private CommonButton _acceptButton;
        [SerializeField] private CommonButton _surrenderButton;
        [SerializeField] private ChoiceSheet _choiceSheet;

        private readonly List<CardView> _handCards = new List<CardView>();
        private bool _layoutReady;
        private long _deadlineMs;
        private string _hudBase = string.Empty;

        /// <summary>손패 풀용 템플릿.</summary>
        public CardView CardPrefab => _cardPrefab;

        /// <summary>손패를 붙일 컨테이너.</summary>
        public RectTransform HandContainer => _handContainer;

        /// <summary>손패 배치.</summary>
        public HandLayout HandLayout => _handLayout;

        /// <summary>7·Q·K·미러·지급 선택 시트.</summary>
        public ChoiceSheet ChoiceSheet => _choiceSheet;

        /// <summary>드로우 버튼.</summary>
        public event Action DrawClicked;

        /// <summary>공격·Q 감수 버튼.</summary>
        public event Action AcceptClicked;

        /// <summary>지급·미러 버림 확정.</summary>
        public event Action ConfirmClicked;

        /// <summary>기권 버튼. 두 번 확인은 Presenter.</summary>
        public event Action SurrenderClicked;

        /// <summary>7 이후 문양. 값은 SuitCode.</summary>
        public event Action<string> SuitClicked;

        /// <summary>Q Reverse|Give.</summary>
        public event Action<string> QueenModeClicked;

        /// <summary>K Extra|Hide.</summary>
        public event Action<string> KingModeClicked;

        /// <summary>손패 카드 탭. instanceId. GamePointer 로 넘긴다.</summary>
        public event Action<int> CardClicked;

        /// <summary>빈곳 탭 또는 우클릭. GamePointer.Cancel.</summary>
        public event Action CancelPressed;

        protected override void Awake()
        {
            if (Application.isPlaying)
            {
                EnsureLayout();
            }

            base.Awake();
        }

        private void Update()
        {
            Presenter?.Tick();
            RefreshTimer();
        }

        /// <summary>
        /// 프리팹 미배선이어도 더미 테이블을 만들 수 있게 자식을 채운다.
        /// </summary>
        public void EnsureLayout()
        {
            if (_layoutReady && _handLayout != null && _cardPrefab != null)
            {
                return;
            }

            EnsureEventSystem();

            var rt = CachedRectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            if (!TryGetComponent(out Image bg))
            {
                bg = CachedGameObject.AddComponent<Image>();
            }

            bg.color = new Color(0.08f, 0.28f, 0.18f, 1f);
            bg.raycastTarget = true;

            if (_inputGroup == null && !TryGetComponent(out _inputGroup))
            {
                _inputGroup = CachedGameObject.AddComponent<CanvasGroup>();
            }

            _hudText = FindOrCreateText("Hud", new Vector2(0.5f, 1f), new Vector2(0f, -80f), new Vector2(1000f, 140f), 34f);
            _statusText = FindOrCreateText("Status", new Vector2(0.5f, 1f), new Vector2(0f, -190f), new Vector2(1000f, 56f), 28f);
            _resultText = FindOrCreateText("Result", new Vector2(0.5f, 0.55f), new Vector2(0f, 80f), new Vector2(900f, 220f), 36f);

            _opponentView = FindOrCreateCard("OpponentCard", new Vector2(0.5f, 1f), new Vector2(0f, -360f), new Vector2(140f, 196f));
            _discardView = FindOrCreateCard("DiscardTop", new Vector2(0.5f, 0.55f), new Vector2(0f, 40f), new Vector2(160f, 224f));
            _deckView = FindOrCreateCard("Deck", new Vector2(0.5f, 0.55f), new Vector2(-220f, 40f), new Vector2(140f, 196f));
            _deckView.Clicked -= OnDeckClicked;
            _deckView.Clicked += OnDeckClicked;

            if (_cardPrefab == null)
            {
                var templateGo = FindOrCreate("CardTemplate", typeof(RectTransform), typeof(Image), typeof(CardView), typeof(CommonButton));
                var templateRt = templateGo.GetComponent<RectTransform>();
                templateRt.sizeDelta = new Vector2(130f, 182f);
                templateGo.SetActive(false);
                _cardPrefab = templateGo.GetComponent<CardView>();
                _cardPrefab.EnsureParts(_font);
            }

            if (_handContainer == null || _handLayout == null)
            {
                var handGo = FindOrCreate("Hand", typeof(RectTransform));
                _handContainer = handGo.GetComponent<RectTransform>();
                StretchBottom(_handContainer, 260f, 20f);
                if (handGo.TryGetComponent(out HorizontalLayoutGroup group))
                {
                    group.enabled = false;
                }

                if (_handLayout == null && !handGo.TryGetComponent(out _handLayout))
                {
                    _handLayout = handGo.AddComponent<HandLayout>();
                }
            }

            if (_handLayout != null)
            {
                _handLayout.Bind(_cardPrefab, _font);
                _handLayout.CardClicked -= OnHandCardClicked;
                _handLayout.CardClicked += OnHandCardClicked;
            }

            _drawButton = FindOrCreateActionButton("Draw", "드로우", new Vector2(0.2f, 0f), new Vector2(-80f, 300f));
            _acceptButton = FindOrCreateActionButton("Accept", "받기", new Vector2(0.4f, 0f), new Vector2(40f, 300f));
            _surrenderButton = FindOrCreateActionButton("Surrender", "기권", new Vector2(0.8f, 0f), new Vector2(280f, 300f));

            if (_choiceSheet == null)
            {
                var sheetGo = FindOrCreate("ChoiceSheet", typeof(RectTransform), typeof(ChoiceSheet));
                _choiceSheet = sheetGo.GetComponent<ChoiceSheet>();
            }

            _choiceSheet.EnsureLayout(_font);
            _choiceSheet.SuitClicked -= OnChoiceSuitClicked;
            _choiceSheet.QueenModeClicked -= OnChoiceQueenClicked;
            _choiceSheet.KingModeClicked -= OnChoiceKingClicked;
            _choiceSheet.ConfirmClicked -= OnChoiceConfirmClicked;
            _choiceSheet.SuitClicked += OnChoiceSuitClicked;
            _choiceSheet.QueenModeClicked += OnChoiceQueenClicked;
            _choiceSheet.KingModeClicked += OnChoiceKingClicked;
            _choiceSheet.ConfirmClicked += OnChoiceConfirmClicked;

            BindButton(_drawButton, () => DrawClicked?.Invoke());
            BindButton(_acceptButton, () => AcceptClicked?.Invoke());
            BindButton(_surrenderButton, () => SurrenderClicked?.Invoke());
            HideLegacyChoiceRows();

            ShowPrompt(MatchPrompt.None);
            _layoutReady = true;
        }

        /// <summary>
        /// 공개 상태와 핫시트 손패를 그린다.
        /// </summary>
        public void Render(
            PublicMatchView match,
            int viewingSeat,
            IReadOnlyList<int> handIds,
            IReadOnlyList<string> handDefs,
            IReadOnlyCollection<int> selectedIds,
            IReadOnlyList<bool> legalFlags,
            MatchPrompt prompt,
            string status,
            string result,
            bool inputLocked)
        {
            EnsureLayout();

            _hudBase = FormatHud(match, viewingSeat);
            _deadlineMs = match != null ? match.deadlineMs : 0;
            RefreshTimer();

            _statusText.text = status ?? string.Empty;
            _resultText.text = result ?? string.Empty;
            _resultText.gameObject.SetActive(!string.IsNullOrEmpty(result));

            if (match != null)
            {
                _discardView.BindDiscard(match.discardTop);
                if (_deckView != null)
                {
                    _deckView.BindBack(match.deckCount);
                    _deckView.SetInteractable(!inputLocked && prompt == MatchPrompt.None);
                }

                var opponentSeat = viewingSeat == 0 ? 1 : 0;
                var opponentCount = match.handCounts != null && opponentSeat < match.handCounts.Length
                    ? match.handCounts[opponentSeat]
                    : 0;
                _opponentView.BindBack(opponentCount);
            }

            RenderHand(handIds, handDefs, selectedIds, legalFlags, prompt, inputLocked);
            ShowPrompt(prompt);
            SetAcceptLabel(match, prompt);
            SetInputLocked(inputLocked);
        }

        /// <summary>
        /// 현재 표시 중인 손패 카드.
        /// </summary>
        public IReadOnlyList<CardView> HandCards => _handLayout != null ? _handLayout.Cards : _handCards;

        /// <summary>
        /// 손패를 풀에 돌려준다.
        /// </summary>
        public void ReleaseHand()
        {
            if (_handLayout != null)
            {
                _handLayout.Release();
            }

            _handCards.Clear();
        }

        /// <summary>
        /// ack 대기 중 입력을 잠근다.
        /// </summary>
        public void SetInputLocked(bool locked)
        {
            if (_inputGroup == null)
            {
                return;
            }

            _inputGroup.interactable = !locked;
            _inputGroup.blocksRaycasts = true;
        }

        /// <summary>
        /// 빈곳 좌클릭·우클릭은 선택을 해제한다. 카드는 자식이 먼저 받는다.
        /// </summary>
        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData == null)
            {
                return;
            }

            if (eventData.button == PointerEventData.InputButton.Right
                || eventData.button == PointerEventData.InputButton.Left)
            {
                CancelPressed?.Invoke();
            }
        }

        private void RenderHand(
            IReadOnlyList<int> handIds,
            IReadOnlyList<string> handDefs,
            IReadOnlyCollection<int> selectedIds,
            IReadOnlyList<bool> legalFlags,
            MatchPrompt prompt,
            bool inputLocked)
        {
            if (_handLayout == null)
            {
                return;
            }

            var cardsSelectable = !inputLocked
                && prompt != MatchPrompt.Suit
                && prompt != MatchPrompt.QueenMode
                && prompt != MatchPrompt.KingMode;
            var playHint = prompt == MatchPrompt.None;
            _handLayout.Render(
                handIds,
                handDefs,
                selectedIds,
                playHint ? legalFlags : null,
                cardsSelectable);
        }

        private void OnHandCardClicked(int instanceId)
        {
            CardClicked?.Invoke(instanceId);
        }

        private void OnDeckClicked(CardView _)
        {
            DrawClicked?.Invoke();
        }

        private void OnChoiceSuitClicked(string suit)
        {
            SuitClicked?.Invoke(suit);
        }

        private void OnChoiceQueenClicked(string queenMode)
        {
            QueenModeClicked?.Invoke(queenMode);
        }

        private void OnChoiceKingClicked(string kingMode)
        {
            KingModeClicked?.Invoke(kingMode);
        }

        private void OnChoiceConfirmClicked()
        {
            ConfirmClicked?.Invoke();
        }

        private void ShowPrompt(MatchPrompt prompt)
        {
            if (_choiceSheet != null)
            {
                _choiceSheet.Apply(prompt);
            }
        }

        private void HideLegacyChoiceRows()
        {
            HideChild("SuitRow");
            HideChild("QueenRow");
            HideChild("KingRow");
            HideChild("Confirm");
        }

        private void HideChild(string name)
        {
            var child = CachedTransform.Find(name);
            if (child != null)
            {
                child.gameObject.SetActive(false);
            }
        }

        private void SetAcceptLabel(PublicMatchView match, MatchPrompt prompt)
        {
            var showAttack = match != null && match.attackStack > 0 && prompt == MatchPrompt.None;
            var showQueen = match != null && match.queenStack > 0 && prompt == MatchPrompt.None;
            if (_acceptButton != null)
            {
                _acceptButton.CachedGameObject.SetActive(showAttack || showQueen);
                var label = _acceptButton.GetComponentInChildren<Text>();
                if (label != null)
                {
                    if (showAttack)
                    {
                        label.text = $"받기 ({match.attackStack}장)";
                    }
                    else if (showQueen)
                    {
                        label.text = $"받기 (Q×{match.queenStack})";
                    }
                }
            }
        }

        private void RefreshTimer()
        {
            if (_hudText == null)
            {
                return;
            }

            var remain = 0;
            if (_deadlineMs > 0)
            {
                var left = _deadlineMs - DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                remain = left > 0 ? (int)(left / 1000L) : 0;
            }

            _hudText.text = string.IsNullOrEmpty(_hudBase)
                ? string.Empty
                : $"{_hudBase}  {remain}초";
        }

        private static string FormatHud(PublicMatchView match, int viewingSeat)
        {
            if (match == null)
            {
                return "대기";
            }

            var dir = match.direction < 0 ? "시계" : "반시계";
            var suit = string.IsNullOrEmpty(match.requiredSuit) ? "-" : match.requiredSuit;
            var color = string.IsNullOrEmpty(match.requiredColor) ? "-" : match.requiredColor;
            var spear = match.spearInStack ? " 죽창" : string.Empty;
            return $"보기P{viewingSeat} 턴P{match.currentSeat} {dir} 무늬{suit} 색{color} +{match.attackStack}{spear} Q×{match.queenStack} 조커 {match.jokerBw}/{match.jokerColor}/{match.jokerMoon} 덱{match.deckCount}";
        }

        private static void EnsureEventSystem()
        {
            if (EventSystem.current != null)
            {
                return;
            }

            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            go.AddComponent<InputSystemUIInputModule>();
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
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            if (_font != null)
            {
                text.font = _font;
            }

            return text;
        }

        private CardView FindOrCreateCard(string name, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            var existing = CachedTransform.Find(name);
            GameObject go;
            if (existing != null)
            {
                go = existing.gameObject;
            }
            else
            {
                go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(CardView), typeof(CommonButton));
                go.transform.SetParent(CachedTransform, false);
            }

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;

            if (!go.TryGetComponent(out CardView card))
            {
                card = go.AddComponent<CardView>();
            }

            card.EnsureParts(_font);
            return card;
        }

        private CommonButton FindOrCreateActionButton(string name, string label, Vector2 anchor, Vector2 pos)
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
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(200f, 72f);
            if (go.TryGetComponent(out Image image))
            {
                image.color = new Color(0.16f, 0.16f, 0.18f, 0.92f);
            }

            if (!go.TryGetComponent(out CommonButton button))
            {
                button = go.AddComponent<CommonButton>();
            }

            button.useSound = false;
            EnsureButtonLabel(go.transform, label, 30f);
            return button;
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

        private GameObject FindOrCreate(string name, params Type[] components)
        {
            var existing = CachedTransform.Find(name);
            if (existing != null)
            {
                return existing.gameObject;
            }

            var go = new GameObject(name, components);
            go.transform.SetParent(CachedTransform, false);
            return go;
        }

        private static void StretchBottom(RectTransform rt, float height, float bottom)
        {
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = new Vector2(0f, bottom);
            rt.sizeDelta = new Vector2(0f, height);
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

    /// <summary>
    /// 호스트 Reject·이벤트 이후 화면에 띄울 선택. 판결이 아니다.
    /// </summary>
    public enum MatchPrompt
    {
        None,
        Suit,
        QueenMode,
        KingMode,
        HideUnder,
        GiveCards,
        MirrorDiscard,
    }
}
