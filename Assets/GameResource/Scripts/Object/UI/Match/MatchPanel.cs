using System;
using System.Collections.Generic;
using System.Threading;
using Backend.App;
using Backend.Net;
using Backend.Object.Management;
using Cysharp.Threading.Tasks;
using LitMotion;
using LitMotion.Extensions;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Backend.Object.UI
{
    /// <summary>
    /// 더미 매치 테이블 View. 표시와 입력만 담당한다.
    /// 카드 내기는 손패 드래그, 지급·미러는 탭. 확정은 <see cref="GamePointer"/>, 7·Q·K 시트는 <see cref="ChoiceSheet"/> 다.
    /// </summary>
    public sealed class MatchPanel : UIPanel<MatchPresenter>, IPointerClickHandler
    {
        [SerializeField] private TMP_FontAsset _font;
        [SerializeField] private MatchHud _matchHud;
        [SerializeField] private TextMeshProUGUI _statusText;
        [SerializeField] private TextMeshProUGUI _lastPlayText;
        [SerializeField] private TextMeshProUGUI _resultText;
        [SerializeField] private CardView _discardView;
        [SerializeField] private CardView _deckView;
        [SerializeField] private TextMeshProUGUI _handCountText;
        [SerializeField] private CardView _opponentView;
        [SerializeField] private RectTransform _handContainer;
        [SerializeField] private HandLayout _handLayout;
        [SerializeField] private CardView _cardPrefab;
        [SerializeField] private CanvasGroup _inputGroup;
        [SerializeField] private CommonButton _drawButton;
        [SerializeField] private TextMeshProUGUI _turnNickText;
        [SerializeField] private CommonButton _acceptButton;
        [SerializeField] private CommonButton _surrenderButton;
        [SerializeField] private ChatView _chatView;
        [SerializeField] private CommonButton _chatButton;
        [SerializeField] private ChoiceSheet _choiceSheet;
        [SerializeField] private RectTransform _previewRoot;
        [SerializeField] private CardView _previewView;
        [SerializeField] private TextMeshProUGUI _previewTitle;
        [SerializeField] private TextMeshProUGUI _previewAbility;
        [SerializeField] private JokerGaugeView _jokerGauge;
        [SerializeField] private SuitAnnounceView _suitAnnounce;
        [SerializeField] private CardView[] _opponentViews = new CardView[5];

        private readonly List<CardView> _handCards = new List<CardView>();
        private readonly List<CardView> _flightCards = new List<CardView>();
        private readonly SeatAnchor[] _seatScratch = new SeatAnchor[5];
        private bool _layoutReady;
        private int _seatCount = 2;
        private int _viewingSeat;
        private Canvas _canvas;
        private CancellationTokenSource _flightCts;
        private int _lastQueenFlightSeq = int.MinValue;
        private int _lastSuitAnnounceSeq = int.MinValue;

        /// <summary>손패 풀용 템플릿.</summary>
        public CardView CardPrefab => _cardPrefab;

        /// <summary>손패를 붙일 컨테이너.</summary>
        public RectTransform HandContainer => _handContainer;

        /// <summary>손패 배치.</summary>
        public HandLayout HandLayout => _handLayout;

        /// <summary>7·Q·K·미러·지급 선택 시트.</summary>
        public ChoiceSheet ChoiceSheet => _choiceSheet;

        /// <summary>턴·조커값·공격 스택 HUD.</summary>
        public MatchHud MatchHud => _matchHud;

        /// <summary>채팅 서브뷰.</summary>
        public ChatView Chat => _chatView;

        /// <summary>드로우 버튼.</summary>
        public event Action DrawClicked;

        /// <summary>공격·Q 감수 버튼.</summary>
        public event Action AcceptClicked;

        /// <summary>지급·미러 버림 확정.</summary>
        public event Action ConfirmClicked;

        /// <summary>기권 버튼. 두 번 확인은 Presenter.</summary>
        public event Action SurrenderClicked;

        /// <summary>채팅 패널 토글.</summary>
        public event Action ChatClicked;

        /// <summary>7 이후 문양. 값은 SuitCode.</summary>
        public event Action<string> SuitClicked;

        /// <summary>Q Reverse|Give.</summary>
        public event Action<string> QueenModeClicked;

        /// <summary>K Extra|Hide.</summary>
        public event Action<string> KingModeClicked;

        /// <summary>손패 카드 탭. instanceId. 지급·미러·숨김만 쓴다.</summary>
        public event Action<int> CardClicked;

        /// <summary>손패 호버. 미리보기용 instanceId.</summary>
        public event Action<int> CardHovered;

        /// <summary>손패에서 호버가 끝남.</summary>
        public event Action<int> CardUnhovered;

        /// <summary>손패 드래그 시작. 미리보기 선택.</summary>
        public event Action<int> CardDragStarted;

        /// <summary>손패를 테이블에 놓음. PlayCard instanceId.</summary>
        public event Action<int> CardPlayDropped;

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
            if (_matchHud != null)
            {
                _matchHud.Tick();
            }

            if (_jokerGauge != null)
            {
                _jokerGauge.Tick();
            }
        }

        private void OnRectTransformDimensionsChange()
        {
            if (_layoutReady)
            {
                PlaceOpponentsDynamic();
            }
        }

        /// <summary>
        /// 프리팹 자식에 묶인 고정 위젯을 찾아 이벤트를 묶는다.
        /// </summary>
        public void EnsureLayout()
        {
            if (_layoutReady && _handLayout != null && _cardPrefab != null)
            {
                return;
            }

            if (_inputGroup == null)
            {
                TryGetComponent(out _inputGroup);
            }

            _matchHud ??= FindOrCreateComponent<MatchHud>("MatchHud");
            if (_matchHud != null)
            {
                _matchHud.EnsureLayout(_font);
            }

            HideChild("Hud");
            _statusText ??= FindOrCreateText("Status");
            _lastPlayText ??= FindOrCreateText("LastPlay");
            HideChild("Status");
            HideChild("LastPlay");
            _resultText ??= FindOrCreateText("Result");
            _jokerGauge ??= FindOrCreateComponent<JokerGaugeView>("JokerGauge");
            if (_jokerGauge != null)
            {
                _jokerGauge.EnsureLayout();
            }

            EnsureOpponentViews();
            _discardView ??= FindOrCreateCard("DiscardTop");
            _deckView ??= FindOrCreateCard("Deck");
            if (_deckView != null)
            {
                _deckView.Clicked -= OnDeckClicked;
                _deckView.Clicked += OnDeckClicked;
            }

            _handCountText ??= FindOrCreateText("HandCount");
            if (_handCountText != null)
            {
                _handCountText.alignment = TextAlignmentOptions.Center;
                _handCountText.textWrappingMode = TextWrappingModes.NoWrap;
                _handCountText.overflowMode = TextOverflowModes.Overflow;
                _handCountText.raycastTarget = false;
                if (_font != null)
                {
                    _handCountText.font = _font;
                }

                if (_handCountText.TryGetComponent(out RectTransform handCountRt))
                {
                    handCountRt.sizeDelta = new Vector2(240f, 40f);
                }
            }

            _cardPrefab ??= FindOrCreateCard("CardTemplate");
            if (_cardPrefab != null)
            {
                _cardPrefab.EnsureParts(_font);
                _cardPrefab.CachedGameObject.SetActive(false);
            }

            if (_handContainer == null || _handLayout == null)
            {
                var handGo = FindOrCreate("Hand");
                if (handGo != null)
                {
                    _handContainer = handGo.GetComponent<RectTransform>();
                    if (handGo.TryGetComponent(out HorizontalLayoutGroup group))
                    {
                        group.enabled = false;
                    }

                    if (_handLayout == null)
                    {
                        handGo.TryGetComponent(out _handLayout);
                    }
                }
            }

            if (_handLayout != null)
            {
                _handLayout.Bind(_cardPrefab, _font, _deckView != null ? _deckView.CachedRectTransform : null);
                _handLayout.CardClicked -= OnHandCardClicked;
                _handLayout.CardClicked += OnHandCardClicked;
                _handLayout.CardHovered -= OnHandCardHovered;
                _handLayout.CardHovered += OnHandCardHovered;
                _handLayout.CardUnhovered -= OnHandCardUnhovered;
                _handLayout.CardUnhovered += OnHandCardUnhovered;
                _handLayout.CardDragStarted -= OnHandCardDragStarted;
                _handLayout.CardDragStarted += OnHandCardDragStarted;
                _handLayout.CardPlayDropped -= OnHandCardPlayDropped;
                _handLayout.CardPlayDropped += OnHandCardPlayDropped;
            }

            EnsurePreview();
            _drawButton ??= FindOrCreateButton("Draw");
            _turnNickText ??= FindOrCreateText("TurnNick");
            if (_turnNickText != null)
            {
                _turnNickText.alignment = TextAlignmentOptions.Center;
                _turnNickText.textWrappingMode = TextWrappingModes.NoWrap;
                _turnNickText.overflowMode = TextOverflowModes.Overflow;
                _turnNickText.raycastTarget = false;
                if (_font != null)
                {
                    _turnNickText.font = _font;
                }
            }

            _acceptButton ??= FindOrCreateButton("Accept");
            _surrenderButton ??= FindOrCreateButton("Surrender");
            _chatButton ??= FindOrCreateButton("Chat");
            _chatView ??= FindOrCreateComponent<ChatView>("ChatView");
            if (_chatView != null)
            {
                _chatView.EnsureLayout(_font);
            }

            _choiceSheet ??= FindOrCreateComponent<ChoiceSheet>("ChoiceSheet");
            if (_choiceSheet != null)
            {
                _choiceSheet.EnsureLayout(_font);
                _choiceSheet.SuitClicked -= OnChoiceSuitClicked;
                _choiceSheet.QueenModeClicked -= OnChoiceQueenClicked;
                _choiceSheet.KingModeClicked -= OnChoiceKingClicked;
                _choiceSheet.ConfirmClicked -= OnChoiceConfirmClicked;
                _choiceSheet.SuitClicked += OnChoiceSuitClicked;
                _choiceSheet.QueenModeClicked += OnChoiceQueenClicked;
                _choiceSheet.KingModeClicked += OnChoiceKingClicked;
                _choiceSheet.ConfirmClicked += OnChoiceConfirmClicked;
            }

            EnsureSuitAnnounce();

            BindButton(_drawButton, () => DrawClicked?.Invoke());
            BindButton(_acceptButton, () => AcceptClicked?.Invoke());
            BindButton(_surrenderButton, () => SurrenderClicked?.Invoke());
            BindButton(_chatButton, () => ChatClicked?.Invoke());
            if (_surrenderButton != null)
            {
                if (!_surrenderButton.TryGetComponent(out CanvasGroup surrenderGroup))
                {
                    surrenderGroup = _surrenderButton.CachedGameObject.AddComponent<CanvasGroup>();
                }

                surrenderGroup.ignoreParentGroups = true;
                surrenderGroup.blocksRaycasts = true;
                surrenderGroup.interactable = true;
                _surrenderButton.CachedTransform.SetAsLastSibling();
            }

            if (_chatView != null)
            {
                _chatView.CachedTransform.SetAsLastSibling();
            }

            if (_chatButton != null)
            {
                _chatButton.CachedTransform.SetAsLastSibling();
            }

            HideLegacyChoiceRows();

            ShowPrompt(MatchPrompt.None);
            _layoutReady = true;
            PlaceOpponentsDynamic();
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
            string lastPlay,
            int lastActSeat,
            string result,
            bool inputLocked,
            string[] nicks = null,
            int hoverPreviewId = -1)
        {
            EnsureLayout();
            _ = status;
            _viewingSeat = viewingSeat;
            _seatCount = match != null && match.handCounts != null && match.handCounts.Length >= 2
                ? match.handCounts.Length
                : 2;
            if (_seatCount > 6)
            {
                _seatCount = 6;
            }

            PlaceOpponentsDynamic();

            if (_matchHud != null)
            {
                _matchHud.Bind(match, lastPlay, nicks);
            }

            if (_jokerGauge != null && match != null)
            {
                _jokerGauge.Bind(match.jokerBw, match.jokerColor, match.jokerMoon);
            }

            if (_resultText != null)
            {
                _resultText.text = result ?? string.Empty;
                _resultText.gameObject.SetActive(!string.IsNullOrEmpty(result));
            }

            if (match != null)
            {
                if (_discardView != null)
                {
                    var justPlayed = lastActSeat >= 0 && !string.IsNullOrEmpty(lastPlay) && lastPlay.IndexOf("냄", StringComparison.Ordinal) >= 0;
                    _discardView.BindDiscard(match.discardTop, justPlayed);
                }
                if (_deckView != null)
                {
                    _deckView.BindBack(match.deckCount);
                    _deckView.SetInteractable(!inputLocked && prompt == MatchPrompt.None);
                }

                BindHandCount(match, viewingSeat);
                BindOpponents(match, viewingSeat, lastActSeat, nicks);
            }

            RenderHand(handIds, handDefs, selectedIds, legalFlags, prompt, inputLocked);
            BindPreview(handIds, handDefs, selectedIds, hoverPreviewId);
            BindTurnNick(match, nicks);
            ShowPrompt(prompt);
            SetAcceptLabel(match, prompt);
            SetInputLocked(inputLocked);
        }

        /// <summary>
        /// 호버 미리보기만 갱신한다. 손패 배치는 다시 깔지 않는다.
        /// </summary>
        public void BindHoverPreview(
            IReadOnlyList<int> handIds,
            IReadOnlyList<string> handDefs,
            IReadOnlyCollection<int> selectedIds,
            int hoverPreviewId)
        {
            EnsureLayout();
            BindPreview(handIds, handDefs, selectedIds, hoverPreviewId);
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
            CancelQueenFlights();
            if (_suitAnnounce != null)
            {
                _suitAnnounce.Cancel();
            }

            if (_handLayout != null)
            {
                _handLayout.Release();
            }

            _handCards.Clear();
        }

        /// <summary>
        /// Q 지급으로 들어오는 손패를 덱이 아니라 fromSeat 위치에서 출발시킨다.
        /// </summary>
        public void ArmQueenReceive(int fromSeat)
        {
            EnsureLayout();
            if (_handLayout == null)
            {
                return;
            }

            _handLayout.ArmTravelOrigin(SeatVisual(fromSeat));
        }

        /// <summary>
        /// 관계없는 좌석용 Q 지급 연출. 뒷면 카드가 fromSeat 에서 toSeat 로 이동한다.
        /// </summary>
        public void PlayQueenGiveFlight(int fromSeat, int toSeat, int count, int seq)
        {
            if (count <= 0 || fromSeat == toSeat || seq == _lastQueenFlightSeq)
            {
                return;
            }

            _lastQueenFlightSeq = seq;
            PlayQueenGiveFlightAsync(fromSeat, toSeat, count).Forget();
        }

        /// <summary>
        /// 7 문양 지정 안내. 모든 좌석에 중앙 스케일 연출을 보여 준다.
        /// </summary>
        public void PlaySuitChanged(string suit, int seq)
        {
            if (string.IsNullOrEmpty(suit) || seq == _lastSuitAnnounceSeq)
            {
                return;
            }

            _lastSuitAnnounceSeq = seq;
            EnsureLayout();
            var announce = EnsureSuitAnnounce();
            if (announce == null)
            {
                return;
            }

            var sprite = _choiceSheet != null ? _choiceSheet.SuitSprite(suit) : null;
            announce.Play(suit, sprite);
        }

        private void OnDisable()
        {
            CancelQueenFlights();
            if (_suitAnnounce != null)
            {
                _suitAnnounce.Cancel();
            }
        }

        /// <summary>
        /// 내기·드로우만 잠근다. 루트 CanvasGroup 은 기권이 받도록 열어 둔다.
        /// </summary>
        public void SetInputLocked(bool locked)
        {
            if (_inputGroup != null)
            {
                _inputGroup.interactable = true;
                _inputGroup.blocksRaycasts = true;
            }

            if (_drawButton != null)
            {
                _drawButton.interactable = !locked;
            }

            if (_acceptButton != null)
            {
                _acceptButton.interactable = !locked;
            }
        }

        /// <summary>
        /// 채팅 패널을 보이거나 숨긴다. 토글 버튼은 항상 켠다.
        /// </summary>
        public void SetChatVisible(bool visible)
        {
            EnsureLayout();
            if (_chatView != null)
            {
                _chatView.CachedGameObject.SetActive(visible);
                if (visible)
                {
                    _chatView.CachedTransform.SetAsLastSibling();
                }
            }

            if (_chatButton != null)
            {
                _chatButton.CachedTransform.SetAsLastSibling();
                var label = _chatButton.GetComponentInChildren<TextMeshProUGUI>(true);
                if (label != null)
                {
                    label.text = visible ? "채팅 닫기" : "채팅";
                }
            }

            ChatView.SetUnreadDot(_chatButton, false);
        }

        /// <summary>
        /// 채팅 패널이 닫혀 있을 때 새 메시지 레드닷을 켠다.
        /// </summary>
        public void NotifyChatArrived()
        {
            EnsureLayout();
            if (_chatView != null && _chatView.CachedGameObject.activeSelf)
            {
                return;
            }

            ChatView.SetUnreadDot(_chatButton, true);
        }

        /// <summary>
        /// 기권 두 번 확인 라벨. 버튼 입력은 잠그지 않는다.
        /// </summary>
        public void SetSurrenderArmed(bool armed)
        {
            if (_surrenderButton == null)
            {
                return;
            }

            _surrenderButton.interactable = true;
            var label = _surrenderButton.GetComponentInChildren<TextMeshProUGUI>(true);
            if (label != null)
            {
                label.text = armed ? "기권 확인" : "기권";
            }
        }

        /// <summary>
        /// 끌고 있던 손패를 자리에 되돌린다.
        /// </summary>
        public void CancelHandDrag()
        {
            if (_handLayout != null)
            {
                _handLayout.CancelDrag();
            }
        }

        /// <summary>
        /// 빈곳 좌클릭·우클릭은 선택을 해제한다. 카드는 자식이 먼저 받는다.
        /// </summary>
        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData == null || (_handLayout != null && _handLayout.IsDragging))
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
            var dragEnabled = playHint
                || prompt == MatchPrompt.GiveCards
                || prompt == MatchPrompt.HideUnder
                || prompt == MatchPrompt.MirrorDiscard;
            _handLayout.Render(
                handIds,
                handDefs,
                selectedIds,
                legalFlags,
                cardsSelectable,
                dragEnabled);
        }

        private void BindPreview(
            IReadOnlyList<int> handIds,
            IReadOnlyList<string> handDefs,
            IReadOnlyCollection<int> selectedIds,
            int hoverPreviewId)
        {
            var def = FindDefById(handIds, handDefs, hoverPreviewId);
            if (string.IsNullOrEmpty(def))
            {
                def = FindSelectedDef(handIds, handDefs, selectedIds);
            }
            var show = !string.IsNullOrEmpty(def);
            if (_previewRoot != null)
            {
                _previewRoot.gameObject.SetActive(show);
            }

            if (!show)
            {
                return;
            }

            if (_previewView != null)
            {
                _previewView.EnsureParts(_font);
                _previewView.BindFront(-1, def, false);
                _previewView.SetInteractable(false);
                _previewView.CachedRectTransform.localRotation = Quaternion.identity;
                _previewView.CachedTransform.localScale = Vector3.one;
            }

            if (_previewAbility != null)
            {
                var ability = CardAbilityText.Describe(def);
                _previewAbility.text = ability;
                _previewAbility.gameObject.SetActive(!string.IsNullOrEmpty(ability));
            }
        }

        private void EnsurePreview()
        {
            if (_previewRoot == null)
            {
                var root = FindOrCreate("Preview");
                if (root != null)
                {
                    _previewRoot = root.GetComponent<RectTransform>();
                }
            }

            if (_previewRoot == null)
            {
                return;
            }

            if (_previewView == null)
            {
                var cardTf = _previewRoot.Find("CardPreview");
                if (cardTf != null && cardTf.TryGetComponent(out CardView card))
                {
                    _previewView = card;
                }
            }

            if (_previewView != null)
            {
                _previewView.EnsureParts(_font);
                _previewView.SetInteractable(false);
            }

            if (_previewTitle == null)
            {
                var titleTf = _previewRoot.Find("PreviewTitle");
                if (titleTf != null)
                {
                    titleTf.TryGetComponent(out _previewTitle);
                }
            }

            if (_previewTitle != null)
            {
                _previewTitle.text = "선택한 카드";
                _previewTitle.alignment = TextAlignmentOptions.Center;
                if (_font != null)
                {
                    _previewTitle.font = _font;
                }
            }

            if (_previewAbility == null)
            {
                var abilityTf = _previewRoot.Find("PreviewAbility");
                if (abilityTf != null)
                {
                    abilityTf.TryGetComponent(out _previewAbility);
                }
            }

            if (_previewAbility == null)
            {
                var abilityGo = new GameObject("PreviewAbility", typeof(RectTransform));
                abilityGo.transform.SetParent(_previewRoot, false);
                _previewAbility = abilityGo.AddComponent<TextMeshProUGUI>();
            }

            if (_previewAbility != null)
            {
                _previewAbility.alignment = TextAlignmentOptions.Top;
                _previewAbility.fontSize = 20;
                _previewAbility.textWrappingMode = TextWrappingModes.Normal;
                _previewAbility.overflowMode = TextOverflowModes.Overflow;
                _previewAbility.raycastTarget = false;
                _previewAbility.color = Color.white;
                if (_font != null)
                {
                    _previewAbility.font = _font;
                }
            }

            _previewRoot.gameObject.SetActive(false);
        }

        private static string FindDefById(
            IReadOnlyList<int> handIds,
            IReadOnlyList<string> handDefs,
            int instanceId)
        {
            if (handIds == null || handDefs == null || instanceId < 0)
            {
                return null;
            }

            for (var i = 0; i < handIds.Count; i++)
            {
                if (handIds[i] != instanceId || i >= handDefs.Count)
                {
                    continue;
                }

                return handDefs[i];
            }

            return null;
        }

        private static string FindSelectedDef(
            IReadOnlyList<int> handIds,
            IReadOnlyList<string> handDefs,
            IReadOnlyCollection<int> selectedIds)
        {
            if (handIds == null || handDefs == null || selectedIds == null)
            {
                return null;
            }

            for (var i = 0; i < handIds.Count; i++)
            {
                if (!ContainsSelected(selectedIds, handIds[i]) || i >= handDefs.Count)
                {
                    continue;
                }

                return handDefs[i];
            }

            return null;
        }

        private static bool ContainsSelected(IReadOnlyCollection<int> ids, int id)
        {
            foreach (var value in ids)
            {
                if (value == id)
                {
                    return true;
                }
            }

            return false;
        }

        private void OnHandCardClicked(int instanceId)
        {
            CardClicked?.Invoke(instanceId);
        }

        private void OnHandCardHovered(int instanceId)
        {
            CardHovered?.Invoke(instanceId);
        }

        private void OnHandCardUnhovered(int instanceId)
        {
            CardUnhovered?.Invoke(instanceId);
        }

        private void OnHandCardDragStarted(int instanceId)
        {
            CardDragStarted?.Invoke(instanceId);
        }

        private void OnHandCardPlayDropped(int instanceId)
        {
            CardPlayDropped?.Invoke(instanceId);
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

        private void BindHandCount(PublicMatchView match, int viewingSeat)
        {
            if (_handCountText == null)
            {
                return;
            }

            var count = match != null && match.handCounts != null
                && viewingSeat >= 0 && viewingSeat < match.handCounts.Length
                ? match.handCounts[viewingSeat]
                : 0;
            _handCountText.text = "현재 카드 : " + count;
        }

        private void BindTurnNick(PublicMatchView match, string[] nicks)
        {
            if (_turnNickText == null)
            {
                return;
            }

            if (match == null)
            {
                _turnNickText.text = string.Empty;
                return;
            }

            _turnNickText.text = NickOf(nicks, match.currentSeat) + "의 턴";
        }

        private static string NickOf(string[] nicks, int seat)
        {
            if (nicks != null && seat >= 0 && seat < nicks.Length && !string.IsNullOrEmpty(nicks[seat]))
            {
                return nicks[seat];
            }

            return "P" + seat;
        }

        private void SetAcceptLabel(PublicMatchView match, MatchPrompt prompt)
        {
            var showQueen = match != null
                && match.queenStack > 0
                && !match.pendingGive
                && prompt == MatchPrompt.None
                && match.currentSeat == _viewingSeat;
            var showAttack = match != null
                && match.attackStack > 0
                && prompt == MatchPrompt.None
                && match.currentSeat == _viewingSeat;
            var show = showQueen || showAttack;
            if (_acceptButton != null)
            {
                _acceptButton.CachedGameObject.SetActive(show);
                var label = _acceptButton.GetComponentInChildren<TextMeshProUGUI>(true);
                if (label != null && show)
                {
                    var n = showQueen ? match.queenStack : match.attackStack;
                    label.text = $"받기 ({n}장)";
                }
            }
        }

        private TextMeshProUGUI FindOrCreateText(string name)
        {
            var go = FindOrCreate(name);
            return go != null && go.TryGetComponent(out TextMeshProUGUI text) ? text : null;
        }

        private CardView FindOrCreateCard(string name)
        {
            var go = FindOrCreate(name);
            if (go == null || !go.TryGetComponent(out CardView card))
            {
                return null;
            }

            card.EnsureParts(_font);
            return card;
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

        private SuitAnnounceView EnsureSuitAnnounce()
        {
            if (_suitAnnounce != null)
            {
                _suitAnnounce.EnsureLayout(_font);
                return _suitAnnounce;
            }

            var existing = CachedTransform.Find("SuitAnnounce");
            if (existing != null)
            {
                if (!existing.TryGetComponent(out _suitAnnounce))
                {
                    _suitAnnounce = existing.gameObject.AddComponent<SuitAnnounceView>();
                }
            }
            else
            {
                var go = new GameObject("SuitAnnounce", typeof(RectTransform));
                go.transform.SetParent(CachedTransform, false);
                _suitAnnounce = go.AddComponent<SuitAnnounceView>();
            }

            _suitAnnounce.EnsureLayout(_font);
            return _suitAnnounce;
        }

        private T FindOrCreateComponent<T>(string name) where T : Component
        {
            var go = FindOrCreate(name);
            return go != null && go.TryGetComponent(out T component) ? component : null;
        }

        private GameObject FindOrCreate(string name)
        {
            var existing = CachedTransform.Find(name);
            return existing != null ? existing.gameObject : null;
        }

        private RectTransform SeatVisual(int seat)
        {
            if (seat == _viewingSeat)
            {
                return _handLayout != null ? _handLayout.CachedRectTransform : _handContainer;
            }

            var placed = LayoutPresetUtil.PlaceOpponents(_seatCount, _viewingSeat, _seatScratch);
            for (var i = 0; i < placed; i++)
            {
                if (_seatScratch[i].Seat != seat || _opponentViews == null || i >= _opponentViews.Length)
                {
                    continue;
                }

                var view = _opponentViews[i];
                return view != null ? view.CachedRectTransform : null;
            }

            return _deckView != null ? _deckView.CachedRectTransform : null;
        }

        private async UniTaskVoid PlayQueenGiveFlightAsync(int fromSeat, int toSeat, int count)
        {
            CancelQueenFlights();
            if (_cardPrefab == null || GameStateUtil.IsQuitting)
            {
                return;
            }

            EnsureLayout();
            var fromRt = SeatVisual(fromSeat);
            var toRt = SeatVisual(toSeat);
            if (fromRt == null || toRt == null)
            {
                return;
            }

            if (!TryPanelLocal(fromRt, out var fromPos) || !TryPanelLocal(toRt, out var toPos))
            {
                return;
            }

            ObjectPoolManager.GetOrCreatePool(_cardPrefab, CachedTransform);
            var fromSize = fromRt.rect.size;
            if (fromSize.x < 8f)
            {
                fromSize = new Vector2(96f, 134f);
            }

            var toSize = toRt.rect.size;
            if (toSize.x < 8f)
            {
                toSize = fromSize;
            }

            _flightCts = new CancellationTokenSource();
            var token = _flightCts.Token;
            var spawned = new List<CardView>(count);
            var tasks = new List<UniTask>(count);
            try
            {
                for (var i = 0; i < count; i++)
                {
                    var offset = new Vector2(i * 14f, i * 10f);
                    var card = SpawnFlightCard(fromPos + offset, fromSize);
                    if (card == null)
                    {
                        continue;
                    }

                    spawned.Add(card);
                    _flightCards.Add(card);
                    tasks.Add(AnimateFlightAsync(
                        card,
                        fromPos + offset,
                        toPos + offset * 0.35f,
                        fromSize,
                        toSize,
                        i * HandLayout.DrawStagger,
                        token));
                }

                if (tasks.Count > 0)
                {
                    await UniTask.WhenAll(tasks);
                }
            }
            catch (OperationCanceledException)
            {
            }
            finally
            {
                ReleaseFlightCards(spawned);
            }
        }

        private CardView SpawnFlightCard(Vector2 start, Vector2 size)
        {
            var card = ObjectPoolManager.Get<CardView>();
            if (card == null)
            {
                return null;
            }

            card.CachedTransform.SetParent(CachedTransform, false);
            card.CachedTransform.SetAsLastSibling();
            var rt = card.CachedRectTransform;
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = start;
            rt.sizeDelta = size;
            rt.localRotation = Quaternion.identity;
            card.CachedTransform.localScale = Vector3.one;
            card.EnsureParts(_font);
            card.SetTraveling(true);
            card.BindBack(1, " ", false);
            card.SetHoverEnabled(false);
            card.SetDragEnabled(false);
            card.SetInteractable(false);
            return card;
        }

        private async UniTask AnimateFlightAsync(
            CardView card,
            Vector2 fromPos,
            Vector2 toPos,
            Vector2 fromSize,
            Vector2 toSize,
            float delay,
            CancellationToken token)
        {
            if (card == null)
            {
                return;
            }

            if (delay > 0f)
            {
                await UniTask.Delay(TimeSpan.FromSeconds(delay), cancellationToken: token);
            }

            token.ThrowIfCancellationRequested();
            AudioManager.PlaySfx(HandLayout.DrawSfxKey);
            var rt = card.CachedRectTransform;
            var duration = HandLayout.DrawDuration;
            if (duration <= 0f)
            {
                rt.anchoredPosition = toPos;
                rt.sizeDelta = toSize;
                return;
            }

            var posHandle = LMotion.Create(fromPos, toPos, duration)
                .WithEase(Ease.OutCubic)
                .Bind(v =>
                {
                    if (rt != null)
                    {
                        rt.anchoredPosition = v;
                    }
                });
            var sizeHandle = LMotion.Create(fromSize, toSize, duration)
                .WithEase(Ease.OutCubic)
                .Bind(v =>
                {
                    if (rt != null)
                    {
                        rt.sizeDelta = v;
                    }
                });

            try
            {
                await UniTask.WhenAll(
                    posHandle.ToUniTask(token),
                    sizeHandle.ToUniTask(token));
                if (rt != null)
                {
                    rt.anchoredPosition = toPos;
                    rt.sizeDelta = toSize;
                }
            }
            catch (OperationCanceledException)
            {
                if (posHandle.IsActive())
                {
                    posHandle.Cancel();
                }

                if (sizeHandle.IsActive())
                {
                    sizeHandle.Cancel();
                }

                throw;
            }
        }

        private void CancelQueenFlights()
        {
            _flightCts?.Cancel();
            _flightCts?.Dispose();
            _flightCts = null;
            ReleaseFlightCards(_flightCards);
        }

        private void ReleaseFlightCards(List<CardView> cards)
        {
            if (cards == null || cards.Count == 0)
            {
                return;
            }

            var snapshot = cards == _flightCards ? new List<CardView>(cards) : cards;
            for (var i = 0; i < snapshot.Count; i++)
            {
                var card = snapshot[i];
                if (card == null || !_flightCards.Remove(card) || GameStateUtil.IsQuitting)
                {
                    continue;
                }

                card.SetTraveling(false);
                ObjectPoolManager.Release(card);
            }

            cards.Clear();
        }

        private bool TryPanelLocal(RectTransform visual, out Vector2 local)
        {
            local = default;
            if (visual == null)
            {
                return false;
            }

            var cam = EventCamera();
            var world = visual.TransformPoint(visual.rect.center);
            var screen = RectTransformUtility.WorldToScreenPoint(cam, world);
            return RectTransformUtility.ScreenPointToLocalPointInRectangle(
                CachedRectTransform,
                screen,
                cam,
                out local);
        }

        private Camera EventCamera()
        {
            if (_canvas == null)
            {
                _canvas = GetComponentInParent<Canvas>();
            }

            if (_canvas == null || _canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                return null;
            }

            return _canvas.worldCamera;
        }

        private void EnsureOpponentViews()
        {
            if (_opponentViews == null || _opponentViews.Length != 5)
            {
                _opponentViews = new CardView[5];
            }

            for (var i = 0; i < _opponentViews.Length; i++)
            {
                var name = i == 0 ? "OpponentCard" : "Opponent" + i;
                _opponentViews[i] ??= FindOrCreateCard(name);
                if (_opponentViews[i] != null)
                {
                    _opponentViews[i].LayoutCaptionBelow();
                }
            }

            _opponentView = _opponentViews[0];
        }

        private void BindOpponents(PublicMatchView match, int viewingSeat, int lastActSeat, string[] nicks)
        {
            var placed = LayoutPresetUtil.PlaceOpponents(_seatCount, viewingSeat, _seatScratch);
            for (var i = 0; i < _opponentViews.Length; i++)
            {
                var view = _opponentViews[i];
                if (view == null)
                {
                    continue;
                }

                var show = i < placed;
                view.CachedGameObject.SetActive(show);
                if (!show)
                {
                    continue;
                }

                var seat = _seatScratch[i].Seat;
                var count = match.handCounts != null && seat >= 0 && seat < match.handCounts.Length
                    ? match.handCounts[seat]
                    : 0;
                var acted = lastActSeat == seat;
                var caption = NickOf(nicks, seat) + "\n남은 덱 수 : " + count + "장";
                view.BindBack(count, caption, acted);
            }
        }

        /// <summary>
        /// 좌석 수·시점 좌석에 따라 상대 카드만 코드로 배치. 그 외 UI는 프리팹 고정 레이아웃.
        /// </summary>
        private void PlaceOpponentsDynamic()
        {
            var preset = LayoutPresetUtil.Resolve(Screen.width, Screen.height);
            var safe = Screen.safeArea;
            var fitter = new SafeAreaFitter(Screen.width, Screen.height, safe.x, safe.y, safe.width, safe.height);
            LayoutPresetUtil.OpponentCardSize(preset, out var w, out var h);
            var size = new Vector2(w, h);
            var placed = LayoutPresetUtil.PlaceOpponents(_seatCount, _viewingSeat, _seatScratch);
            for (var i = 0; i < _opponentViews.Length; i++)
            {
                var view = _opponentViews[i];
                if (view == null)
                {
                    continue;
                }

                var show = i < placed;
                view.CachedGameObject.SetActive(show);
                if (!show)
                {
                    continue;
                }

                fitter.MapPoint(_seatScratch[i].Nx, _seatScratch[i].Ny, out var x, out var y);
                PlaceAnchored(view.CachedRectTransform, x, y, size);
            }
        }

        private static void PlaceAnchored(RectTransform rt, float nx, float ny, Vector2 size)
        {
            if (rt == null)
            {
                return;
            }

            rt.anchorMin = new Vector2(nx, ny);
            rt.anchorMax = new Vector2(nx, ny);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = size;
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
