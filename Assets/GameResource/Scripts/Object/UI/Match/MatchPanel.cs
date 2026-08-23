using System;
using System.Collections.Generic;
using Backend.App;
using Backend.Net;
using UnityEngine;
using UnityEngine.EventSystems;
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
        [SerializeField] private MatchHud _matchHud;
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
        [SerializeField] private CardView[] _opponentViews = new CardView[5];

        private readonly List<CardView> _handCards = new List<CardView>();
        private readonly SeatAnchor[] _seatScratch = new SeatAnchor[5];
        private bool _layoutReady;
        private int _seatCount = 2;
        private int _viewingSeat;
        private int _fitWidth;
        private int _fitHeight;
        private float _fitSafeX;
        private float _fitSafeY;
        private float _fitSafeW;
        private float _fitSafeH;

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
            if (_matchHud != null)
            {
                _matchHud.Tick();
            }

            if (_layoutReady && NeedsSafeRefit())
            {
                ApplySafeLayout();
            }
        }

        private void OnRectTransformDimensionsChange()
        {
            if (_layoutReady)
            {
                ApplySafeLayout();
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
            _resultText ??= FindOrCreateText("Result");

            EnsureOpponentViews();
            _discardView ??= FindOrCreateCard("DiscardTop");
            _deckView ??= FindOrCreateCard("Deck");
            if (_deckView != null)
            {
                _deckView.Clicked -= OnDeckClicked;
                _deckView.Clicked += OnDeckClicked;
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
                _handLayout.Bind(_cardPrefab, _font);
                _handLayout.CardClicked -= OnHandCardClicked;
                _handLayout.CardClicked += OnHandCardClicked;
            }

            _drawButton ??= FindOrCreateButton("Draw");
            _acceptButton ??= FindOrCreateButton("Accept");
            _surrenderButton ??= FindOrCreateButton("Surrender");

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

            BindButton(_drawButton, () => DrawClicked?.Invoke());
            BindButton(_acceptButton, () => AcceptClicked?.Invoke());
            BindButton(_surrenderButton, () => SurrenderClicked?.Invoke());
            HideLegacyChoiceRows();

            ShowPrompt(MatchPrompt.None);
            _layoutReady = true;
            ApplySafeLayout();
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
            _viewingSeat = viewingSeat;
            _seatCount = match != null && match.handCounts != null && match.handCounts.Length >= 2
                ? match.handCounts.Length
                : 2;
            if (_seatCount > 6)
            {
                _seatCount = 6;
            }

            ApplySafeLayout();

            if (_matchHud != null)
            {
                _matchHud.Bind(match, viewingSeat);
            }

            if (_statusText != null)
            {
                _statusText.text = status ?? string.Empty;
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
                    _discardView.BindDiscard(match.discardTop);
                }
                if (_deckView != null)
                {
                    _deckView.BindBack(match.deckCount);
                    _deckView.SetInteractable(!inputLocked && prompt == MatchPrompt.None);
                }

                BindOpponents(match, viewingSeat);
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

        private Text FindOrCreateText(string name)
        {
            var go = FindOrCreate(name);
            return go != null && go.TryGetComponent(out Text text) ? text : null;
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
            }

            _opponentView = _opponentViews[0];
        }

        private void BindOpponents(PublicMatchView match, int viewingSeat)
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
                view.BindBack(count);
            }
        }

        private bool NeedsSafeRefit()
        {
            var safe = Screen.safeArea;
            return _fitWidth != Screen.width
                || _fitHeight != Screen.height
                || _fitSafeX != safe.x
                || _fitSafeY != safe.y
                || _fitSafeW != safe.width
                || _fitSafeH != safe.height;
        }

        private void ApplySafeLayout()
        {
            var preset = LayoutPresetUtil.Resolve(Screen.width, Screen.height);
            var safe = Screen.safeArea;
            var fitter = new SafeAreaFitter(Screen.width, Screen.height, safe.x, safe.y, safe.width, safe.height);
            _fitWidth = Screen.width;
            _fitHeight = Screen.height;
            _fitSafeX = safe.x;
            _fitSafeY = safe.y;
            _fitSafeW = safe.width;
            _fitSafeH = safe.height;

            var handHeight = LayoutPresetUtil.HandHeight(preset);
            PlaceHand(fitter, handHeight);
            PlaceHud(fitter);
            PlaceStatus(fitter);
            PlaceResult(fitter);
            PlaceTableCards(preset, fitter);
            PlaceOpponents(preset, fitter);
            PlaceActions(fitter, handHeight);
            PlaceChoiceSheet(fitter, handHeight);
        }

        private void PlaceHand(SafeAreaFitter fitter, float handHeight)
        {
            if (_handContainer == null)
            {
                return;
            }

            fitter.GetHandAnchors(out var minX, out var minY, out var maxX, out var maxY);
            var rt = _handContainer;
            rt.anchorMin = new Vector2(minX, minY);
            rt.anchorMax = new Vector2(maxX, maxY);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(0f, handHeight);
        }

        private void PlaceHud(SafeAreaFitter fitter)
        {
            if (_matchHud == null)
            {
                return;
            }

            var rt = _matchHud.CachedRectTransform;
            rt.anchorMin = new Vector2(0.5f, fitter.AnchorMaxY);
            rt.anchorMax = new Vector2(0.5f, fitter.AnchorMaxY);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(1000f, 140f);
        }

        private void PlaceStatus(SafeAreaFitter fitter)
        {
            if (_statusText == null)
            {
                return;
            }

            var rt = _statusText.rectTransform;
            rt.anchorMin = new Vector2(0.5f, fitter.AnchorMaxY);
            rt.anchorMax = new Vector2(0.5f, fitter.AnchorMaxY);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -148f);
            rt.sizeDelta = new Vector2(1000f, 56f);
        }

        private void PlaceResult(SafeAreaFitter fitter)
        {
            if (_resultText == null)
            {
                return;
            }

            fitter.MapPoint(0.5f, 0.55f, out var x, out var y);
            var rt = _resultText.rectTransform;
            rt.anchorMin = new Vector2(x, y);
            rt.anchorMax = new Vector2(x, y);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(900f, 220f);
        }

        private void PlaceTableCards(LayoutPreset preset, SafeAreaFitter fitter)
        {
            var ny = LayoutPresetUtil.DiscardNormalizedY(preset);
            fitter.MapPoint(0.5f, ny, out var cx, out var cy);
            PlaceAnchored(_discardView != null ? _discardView.CachedRectTransform : null, cx, cy, new Vector2(160f, 224f));
            fitter.MapPoint(preset == LayoutPreset.MobilePortrait ? 0.28f : 0.32f, ny, out var dx, out var dy);
            PlaceAnchored(_deckView != null ? _deckView.CachedRectTransform : null, dx, dy, new Vector2(140f, 196f));
        }

        private void PlaceOpponents(LayoutPreset preset, SafeAreaFitter fitter)
        {
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

        private void PlaceActions(SafeAreaFitter fitter, float handHeight)
        {
            var y = handHeight + 48f;
            PlaceActionButton(_drawButton, 0.22f, fitter.AnchorMinY, y);
            PlaceActionButton(_acceptButton, 0.5f, fitter.AnchorMinY, y);
            PlaceActionButton(_surrenderButton, 0.78f, fitter.AnchorMinY, y);
        }

        private static void PlaceActionButton(CommonButton button, float nx, float anchorY, float y)
        {
            if (button == null)
            {
                return;
            }

            var rt = button.CachedRectTransform;
            rt.anchorMin = new Vector2(nx, anchorY);
            rt.anchorMax = new Vector2(nx, anchorY);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = new Vector2(0f, y);
            rt.sizeDelta = new Vector2(200f, 72f);
        }

        private void PlaceChoiceSheet(SafeAreaFitter fitter, float handHeight)
        {
            if (_choiceSheet == null)
            {
                return;
            }

            var rt = _choiceSheet.CachedRectTransform;
            rt.anchorMin = new Vector2(0.5f, fitter.AnchorMinY);
            rt.anchorMax = new Vector2(0.5f, fitter.AnchorMinY);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = new Vector2(0f, handHeight + 130f);
            rt.sizeDelta = new Vector2(720f, 200f);
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
