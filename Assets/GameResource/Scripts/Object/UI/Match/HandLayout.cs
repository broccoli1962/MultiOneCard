using System;
using System.Collections.Generic;
using System.Threading;
using Backend.Object.Management;
using Cysharp.Threading.Tasks;
using Game.Rules;
using LitMotion;
using LitMotion.Extensions;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Backend.Object.UI
{
    /// <summary>
    /// 내 손패 부채꼴 배치. 합법 100% / 불법은 더 투명·선택 불가 / 호버 시 살짝 올림.
    /// 상대 앞면은 붙이지 않는다. 일반 내기는 테이블로 드래그한다.
    /// </summary>
    public sealed class HandLayout : UIView
    {
        public const float CardWidth = 202f;
        public const float CardHeight = 283f;
        public const float FanStep = 67f;
        public const float MinFanStep = 38f;
        public const float MaxFanAngle = 20f;
        public const float RestY = 8f;
        public const float RowGap = 108f;
        public const float PlayDropLift = 150f;
        public const float DrawDuration = 0.3f;
        public const float DrawStagger = 0.08f;
        public const float LayoutDuration = 0.2f;
        public const float TravelStartScale = 0.4f;
        public const string DrawSfxKey = "Card_Flip";

        private readonly List<CardView> _cards = new List<CardView>();
        private readonly List<CardView> _drawnScratch = new List<CardView>();
        private readonly Dictionary<int, CardView> _byIdScratch = new Dictionary<int, CardView>();

        [SerializeField] private CardView _prefab;
        [SerializeField] private TMP_FontAsset _font;
        [SerializeField] private RectTransform _drawOrigin;

        private RectTransform _travelOrigin;
        private CardView _dragCard;
        private CardView _hoverCard;
        private IReadOnlyCollection<int> _selectedIds;
        private Vector2 _dragGrabOffset;
        private bool _dragToPlay;
        private bool _inputEnabled;
        private Canvas _canvas;
        private int _clickFrame = -1;
        private CancellationTokenSource _travelCts;

        /// <summary>손패 탭. 불법 장은 올리지 않는다. 일반 내기는 보내지 않는다.</summary>
        public event Action<int> CardClicked;

        /// <summary>손패 호버. 미리보기용 instanceId.</summary>
        public event Action<int> CardHovered;

        /// <summary>손패에서 호버가 끝남. 미리보기 해제용 instanceId.</summary>
        public event Action<int> CardUnhovered;

        /// <summary>손패 드래그 시작. 미리보기용 instanceId.</summary>
        public event Action<int> CardDragStarted;

        /// <summary>손패를 테이블 쪽으로 끌어 놓음. PlayCard instanceId.</summary>
        public event Action<int> CardPlayDropped;

        /// <summary>현재 손패 카드.</summary>
        public IReadOnlyList<CardView> Cards => _cards;

        /// <summary>카드를 끌고 있으면 true.</summary>
        public bool IsDragging => _dragCard != null;

        /// <summary>
        /// 풀 템플릿과 폰트, 드로우 출발점(덱)을 받는다.
        /// </summary>
        public void Bind(CardView prefab, TMP_FontAsset font, RectTransform drawOrigin = null)
        {
            if (prefab != null)
            {
                _prefab = prefab;
            }

            if (font != null)
            {
                _font = font;
            }

            if (drawOrigin != null)
            {
                _drawOrigin = drawOrigin;
            }

            if (TryGetComponent(out HorizontalLayoutGroup group))
            {
                group.enabled = false;
            }
        }

        /// <summary>
        /// 다음 신규 손패의 출발점을 덱 대신 이 트랜스폼으로 쓴다. 신규 장이 있을 때 한 번만 쓴다.
        /// </summary>
        public void ArmTravelOrigin(RectTransform origin)
        {
            _travelOrigin = origin;
        }

        /// <summary>
        /// 손패를 깔고 합법/선택 시각을 적용한다. 같은 장이면 풀을 다시 쓰지 않는다.
        /// legalFlags 가 null 이면 모두 선택 가능(지급·미러·숨김).
        /// dragToPlay 면 테이블로 끌어 확정한다(내기·지급·숨김·미러).
        /// 기존 손패와 겹치는 장이 하나도 없으면(좌석 전환) 덱 드로우 연출을 쓰지 않는다.
        /// </summary>
        public void Render(
            IReadOnlyList<int> handIds,
            IReadOnlyList<string> handDefs,
            IReadOnlyCollection<int> selectedIds,
            IReadOnlyList<bool> legalFlags,
            bool interactable,
            bool dragToPlay)
        {
            _dragToPlay = dragToPlay;
            _inputEnabled = interactable;
            var count = handIds != null ? handIds.Count : 0;
            if (count == 0 || _prefab == null)
            {
                CancelDrag();
                Release();
                return;
            }

            if (_dragCard != null)
            {
                if (SameHand(handIds))
                {
                    return;
                }

                CancelDrag();
            }

            if (SameHand(handIds))
            {
                ApplyState(handIds, handDefs, selectedIds, legalFlags);
                return;
            }

            Sync(handIds, handDefs, selectedIds, legalFlags);
        }

        /// <summary>
        /// 끌고 있던 카드를 자리에 되돌린다. 커맨드는 보내지 않는다.
        /// </summary>
        public void CancelDrag()
        {
            var card = _dragCard;
            _dragCard = null;
            if (card == null)
            {
                return;
            }

            card.RestoreRest(false);
        }

        /// <summary>
        /// 손패를 풀에 돌려준다.
        /// </summary>
        public void Release()
        {
            CancelTravel();
            CancelDrag();
            _hoverCard = null;
            _selectedIds = null;
            _travelOrigin = null;
            for (var i = 0; i < _cards.Count; i++)
            {
                var card = _cards[i];
                if (card == null)
                {
                    continue;
                }

                UnbindCard(card);
                ObjectPoolManager.Release(card);
            }

            _cards.Clear();
        }

        private void OnDisable()
        {
            CancelTravel();
        }

        private void Sync(
            IReadOnlyList<int> handIds,
            IReadOnlyList<string> handDefs,
            IReadOnlyCollection<int> selectedIds,
            IReadOnlyList<bool> legalFlags)
        {
            CancelTravel();
            var previousCount = _cards.Count;
            _byIdScratch.Clear();
            for (var i = 0; i < _cards.Count; i++)
            {
                var card = _cards[i];
                if (card == null || card.InstanceId < 0 || _byIdScratch.ContainsKey(card.InstanceId))
                {
                    continue;
                }

                _byIdScratch[card.InstanceId] = card;
            }

            ObjectPoolManager.GetOrCreatePool(_prefab, CachedTransform);
            var next = new List<CardView>(handIds.Count);
            _drawnScratch.Clear();
            for (var i = 0; i < handIds.Count; i++)
            {
                var id = handIds[i];
                if (_byIdScratch.TryGetValue(id, out var kept))
                {
                    _byIdScratch.Remove(id);
                    next.Add(kept);
                    continue;
                }

                var spawned = SpawnCard(i);
                if (spawned == null)
                {
                    continue;
                }

                next.Add(spawned);
                _drawnScratch.Add(spawned);
            }

            var keptCount = next.Count - _drawnScratch.Count;
            if (previousCount > 0 && keptCount == 0 && _drawnScratch.Count > 0)
            {
                _drawnScratch.Clear();
                _travelOrigin = null;
            }

            for (var i = 0; i < _cards.Count; i++)
            {
                var leftover = _cards[i];
                if (leftover == null || ContainsCard(next, leftover))
                {
                    continue;
                }

                leftover.SetTraveling(false);
                UnbindCard(leftover);
                ObjectPoolManager.Release(leftover);
            }

            _byIdScratch.Clear();
            _cards.Clear();
            _cards.AddRange(next);

            var animateDraws = _drawnScratch.Count > 0;
            if (animateDraws)
            {
                for (var i = 0; i < _cards.Count; i++)
                {
                    _cards[i].SetTraveling(true);
                }
            }

            ApplyState(handIds, handDefs, selectedIds, legalFlags);
            if (!animateDraws)
            {
                return;
            }

            var origin = ResolveTravelAnchored(true);
            for (var i = 0; i < _drawnScratch.Count; i++)
            {
                var card = _drawnScratch[i];
                if (card == null)
                {
                    continue;
                }

                card.CachedTransform.SetAsLastSibling();
                card.CachedRectTransform.anchoredPosition = origin;
                card.CachedRectTransform.localRotation = Quaternion.identity;
                card.CachedTransform.localScale = Vector3.one * TravelStartScale;
            }

            PlayDraws(new List<CardView>(_drawnScratch), origin).Forget();
        }

        private CardView SpawnCard(int siblingIndex)
        {
            var card = ObjectPoolManager.Get<CardView>();
            if (card == null)
            {
                return null;
            }

            card.CachedTransform.SetParent(CachedTransform, false);
            card.CachedRectTransform.sizeDelta = new Vector2(CardWidth, CardHeight);
            card.CachedTransform.SetSiblingIndex(siblingIndex);
            card.SetTraveling(false);
            BindCard(card);
            return card;
        }

        private void ApplyState(
            IReadOnlyList<int> handIds,
            IReadOnlyList<string> handDefs,
            IReadOnlyCollection<int> selectedIds,
            IReadOnlyList<bool> legalFlags)
        {
            _selectedIds = selectedIds;
            var count = _cards.Count;
            for (var i = 0; i < count; i++)
            {
                var card = _cards[i];
                if (card == null)
                {
                    continue;
                }

                var id = handIds[i];
                var def = handDefs != null && i < handDefs.Count ? handDefs[i] : "?";
                var selected = ContainsId(selectedIds, id);
                var legal = legalFlags == null || (i < legalFlags.Count && legalFlags[i]);
                var canUse = _inputEnabled && legal && !card.IsTraveling;

                card.CachedRectTransform.sizeDelta = new Vector2(CardWidth, CardHeight);
                Place(card, i, count);
                card.EnsureParts(_font);
                card.BindFront(id, def, selected);
                card.SetLegal(legal);
                card.SetInteractable(canUse);
                card.SetHoverEnabled(canUse);
                if (!canUse && _hoverCard == card)
                {
                    _hoverCard = null;
                }

                card.SetDragEnabled(canUse && _dragToPlay);
            }

            ApplyDrawOrder(selectedIds);
        }

        private void Place(CardView card, int index, int count)
        {
            var rt = card.CachedRectTransform;
            rt.anchorMin = new Vector2(0.5f, 0f);
            rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);

            ResolveRow(index, count, out var indexInRow, out var rowCount, out var row);
            var step = FanStepFor(rowCount);
            var t = rowCount == 1 ? 0.5f : indexInRow / (float)(rowCount - 1);
            var maxAngle = rowCount <= 1 ? 0f : Mathf.Min(MaxFanAngle, 2.8f * (rowCount - 1));
            var angle = Mathf.Lerp(maxAngle, -maxAngle, t);
            var x = (indexInRow - (rowCount - 1) * 0.5f) * step;
            var y = RestY + row * RowGap;
            card.SetRest(new Vector2(x, y), angle);
            if (!card.IsTraveling)
            {
                rt.anchoredPosition = new Vector2(x, y);
                rt.localRotation = Quaternion.Euler(0f, 0f, angle);
            }
        }

        private float FanStepFor(int rowCount)
        {
            var step = FanStep;
            var width = CachedRectTransform.rect.width;
            if (width < 1f)
            {
                width = 1080f;
            }

            if (rowCount > 1)
            {
                var needed = (rowCount - 1) * FanStep + CardWidth;
                if (needed > width)
                {
                    step = (width - CardWidth) / (rowCount - 1);
                    if (step < MinFanStep)
                    {
                        step = MinFanStep;
                    }
                }
            }

            return step;
        }

        /// <summary>
        /// 파산 장수의 절반을 넘으면 앞줄·뒷줄로 나눈다. 앞줄이 한 장 더 가질 수 있다.
        /// </summary>
        private static void ResolveRow(int index, int count, out int indexInRow, out int rowCount, out int row)
        {
            var perRow = MatchState.BankruptHandCount / 2;
            if (count <= perRow)
            {
                row = 0;
                indexInRow = index;
                rowCount = count;
                return;
            }

            var frontCount = (count + 1) / 2;
            if (index < frontCount)
            {
                row = 0;
                indexInRow = index;
                rowCount = frontCount;
                return;
            }

            row = 1;
            indexInRow = index - frontCount;
            rowCount = count - frontCount;
        }

        private void ApplyDrawOrder(IReadOnlyCollection<int> selectedIds)
        {
            var count = _cards.Count;
            var perRow = MatchState.BankruptHandCount / 2;
            if (count > perRow)
            {
                var frontCount = (count + 1) / 2;
                for (var i = frontCount; i < count; i++)
                {
                    BringToFront(_cards[i]);
                }

                for (var i = 0; i < frontCount; i++)
                {
                    BringToFront(_cards[i]);
                }
            }
            else
            {
                for (var i = 0; i < count; i++)
                {
                    BringToFront(_cards[i]);
                }
            }

            for (var i = 0; i < count; i++)
            {
                if (_cards[i] != null && ContainsId(selectedIds, _cards[i].InstanceId))
                {
                    BringToFront(_cards[i]);
                }
            }

            if (_hoverCard != null)
            {
                BringToFront(_hoverCard);
            }

            if (_dragCard != null)
            {
                BringToFront(_dragCard);
            }
        }

        private static void BringToFront(CardView card)
        {
            if (card != null)
            {
                card.CachedTransform.SetAsLastSibling();
            }
        }

        private void BindCard(CardView card)
        {
            card.Clicked -= OnCardClicked;
            card.Clicked += OnCardClicked;
            card.Hovered -= OnCardHovered;
            card.Hovered += OnCardHovered;
            card.Unhovered -= OnCardUnhovered;
            card.Unhovered += OnCardUnhovered;
            card.DragBegan -= OnDragBegan;
            card.DragBegan += OnDragBegan;
            card.DragMoved -= OnDragMoved;
            card.DragMoved += OnDragMoved;
            card.DragEnded -= OnDragEnded;
            card.DragEnded += OnDragEnded;
        }

        private void UnbindCard(CardView card)
        {
            card.Clicked -= OnCardClicked;
            card.Hovered -= OnCardHovered;
            card.Unhovered -= OnCardUnhovered;
            card.SetHoverEnabled(false);
            card.DragBegan -= OnDragBegan;
            card.DragMoved -= OnDragMoved;
            card.DragEnded -= OnDragEnded;
            card.SetDragEnabled(false);
        }

        private void OnCardClicked(CardView card)
        {
            if (card == null || _dragCard != null || Time.frameCount == _clickFrame)
            {
                return;
            }

            _clickFrame = Time.frameCount;
            CardClicked?.Invoke(card.InstanceId);
        }

        private void OnCardHovered(CardView card)
        {
            if (card == null || _dragCard != null)
            {
                return;
            }

            if (_hoverCard != null && _hoverCard != card)
            {
                _hoverCard.ClearHover();
            }

            _hoverCard = card;
            ApplyDrawOrder(_selectedIds);
            CardHovered?.Invoke(card.InstanceId);
        }

        private void OnCardUnhovered(CardView card)
        {
            if (card == null)
            {
                return;
            }

            if (_hoverCard == card)
            {
                _hoverCard = null;
            }

            ApplyDrawOrder(_selectedIds);
            CardUnhovered?.Invoke(card.InstanceId);
        }

        private void OnDragBegan(CardView card, PointerEventData eventData)
        {
            if (card == null || eventData == null || !_dragToPlay)
            {
                return;
            }

            _dragCard = card;
            card.CachedTransform.SetAsLastSibling();
            if (TryScreenToHand(eventData.position, out var local))
            {
                _dragGrabOffset = card.CachedRectTransform.anchoredPosition - local;
            }
            else
            {
                _dragGrabOffset = Vector2.zero;
            }

            CardDragStarted?.Invoke(card.InstanceId);
        }

        private void OnDragMoved(CardView card, PointerEventData eventData)
        {
            if (card == null || card != _dragCard || eventData == null)
            {
                return;
            }

            if (!TryScreenToHand(eventData.position, out var local))
            {
                return;
            }

            card.FollowAnchored(local + _dragGrabOffset);
        }

        private void OnDragEnded(CardView card, PointerEventData eventData)
        {
            if (card == null || card != _dragCard)
            {
                return;
            }

            var play = _dragToPlay && IsPlayDrop(card, eventData);
            var id = card.InstanceId;
            _dragCard = null;
            if (play)
            {
                CardPlayDropped?.Invoke(id);
                return;
            }

            card.RestoreRest(true);
        }

        private bool IsPlayDrop(CardView card, PointerEventData eventData)
        {
            if (card != null && card.CachedRectTransform.anchoredPosition.y > card.RestAnchored.y + PlayDropLift)
            {
                return true;
            }

            if (eventData == null || card == null || !TryScreenToHand(eventData.position, out var local))
            {
                return false;
            }

            var tableLine = Mathf.Max(CachedRectTransform.rect.yMax, card.RestAnchored.y + CardHeight) + 12f;
            return local.y > tableLine;
        }

        private void StopTravelTweens()
        {
            _travelCts?.Cancel();
            _travelCts?.Dispose();
            _travelCts = null;
        }

        private void CancelTravel()
        {
            StopTravelTweens();
            for (var i = 0; i < _cards.Count; i++)
            {
                var card = _cards[i];
                if (card == null || !card.IsTraveling)
                {
                    continue;
                }

                card.SetTraveling(false);
                FinishCard(card);
            }
        }

        private async UniTaskVoid PlayDraws(
            List<CardView> drawn,
            Vector2 origin)
        {
            _travelCts?.Cancel();
            _travelCts?.Dispose();
            _travelCts = new CancellationTokenSource();
            var token = _travelCts.Token;
            var layoutTasks = new List<UniTask>(_cards.Count);

            try
            {
                for (var i = 0; i < _cards.Count; i++)
                {
                    var card = _cards[i];
                    if (card == null || ContainsCard(drawn, card))
                    {
                        continue;
                    }

                    var from = card.CachedRectTransform.anchoredPosition;
                    var fromZ = SignedZ(card.CachedRectTransform.localEulerAngles.z);
                    var to = card.RestAnchored;
                    if ((from - to).sqrMagnitude < 1f
                        && Mathf.Abs(Mathf.DeltaAngle(fromZ, card.RestZ)) < 0.5f)
                    {
                        continue;
                    }

                    card.SetTraveling(true);
                    layoutTasks.Add(AnimateCardAsync(
                        card,
                        from,
                        fromZ,
                        card.CachedTransform.localScale,
                        to,
                        card.RestZ,
                        Vector3.one,
                        LayoutDuration,
                        Ease.OutCubic,
                        token));
                }

                if (layoutTasks.Count > 0)
                {
                    await UniTask.WhenAll(layoutTasks);
                }

                for (var i = 0; i < _cards.Count; i++)
                {
                    var card = _cards[i];
                    if (card == null || ContainsCard(drawn, card) || !card.IsTraveling)
                    {
                        continue;
                    }

                    card.SetTraveling(false);
                    FinishCard(card);
                }

                token.ThrowIfCancellationRequested();

                for (var d = 0; d < drawn.Count; d++)
                {
                    var card = drawn[d];
                    if (card == null)
                    {
                        continue;
                    }

                    card.CachedTransform.SetAsLastSibling();
                    card.CachedRectTransform.anchoredPosition = origin;
                    card.CachedRectTransform.localRotation = Quaternion.identity;
                    card.CachedTransform.localScale = Vector3.one * TravelStartScale;
                    AudioManager.PlaySfx(DrawSfxKey);
                    await AnimateCardAsync(
                        card,
                        origin,
                        0f,
                        Vector3.one * TravelStartScale,
                        card.RestAnchored,
                        card.RestZ,
                        Vector3.one,
                        DrawDuration,
                        Ease.OutBack,
                        token);

                    card.SetTraveling(false);
                    FinishCard(card);
                    if (DrawStagger > 0f && d < drawn.Count - 1)
                    {
                        await UniTask.Delay(TimeSpan.FromSeconds(DrawStagger), cancellationToken: token);
                    }
                }
            }
            catch (OperationCanceledException)
            {
            }
        }

        private void FinishCard(CardView card)
        {
            if (card == null)
            {
                return;
            }

            var canUse = _inputEnabled && card.IsLegal;
            card.SetInteractable(canUse);
            card.SetHoverEnabled(canUse);
            card.SetDragEnabled(canUse && _dragToPlay);
        }

        private async UniTask AnimateCardAsync(
            CardView card,
            Vector2 fromPos,
            float fromZ,
            Vector3 fromScale,
            Vector2 toPos,
            float toZ,
            Vector3 toScale,
            float duration,
            Ease ease,
            CancellationToken token)
        {
            if (card == null)
            {
                return;
            }

            var rt = card.CachedRectTransform;
            var tr = card.CachedTransform;
            if (duration <= 0f)
            {
                rt.anchoredPosition = toPos;
                rt.localRotation = Quaternion.Euler(0f, 0f, toZ);
                tr.localScale = toScale;
                return;
            }

            rt.anchoredPosition = fromPos;
            rt.localRotation = Quaternion.Euler(0f, 0f, fromZ);
            tr.localScale = fromScale;

            var posHandle = LMotion.Create(fromPos, toPos, duration)
                .WithEase(ease)
                .Bind(v =>
                {
                    if (rt != null)
                    {
                        rt.anchoredPosition = v;
                    }
                });
            var rotHandle = LMotion.Create(fromZ, toZ, duration)
                .WithEase(ease)
                .Bind(z =>
                {
                    if (rt != null)
                    {
                        rt.localRotation = Quaternion.Euler(0f, 0f, z);
                    }
                });
            var scaleHandle = LMotion.Create(fromScale, toScale, duration)
                .WithEase(ease)
                .BindToLocalScale(tr);

            try
            {
                await UniTask.WhenAll(
                    posHandle.ToUniTask(token),
                    rotHandle.ToUniTask(token),
                    scaleHandle.ToUniTask(token));

                if (card != null)
                {
                    rt.anchoredPosition = toPos;
                    rt.localRotation = Quaternion.Euler(0f, 0f, toZ);
                    tr.localScale = toScale;
                }
            }
            catch (OperationCanceledException)
            {
                if (posHandle.IsActive())
                {
                    posHandle.Cancel();
                }

                if (rotHandle.IsActive())
                {
                    rotHandle.Cancel();
                }

                if (scaleHandle.IsActive())
                {
                    scaleHandle.Cancel();
                }

                throw;
            }
        }

        private Vector2 ResolveTravelAnchored(bool consumeArmed)
        {
            var visual = consumeArmed && _travelOrigin != null ? _travelOrigin : _drawOrigin;
            if (consumeArmed)
            {
                _travelOrigin = null;
            }

            return AnchoredFromVisual(visual);
        }

        private Vector2 AnchoredFromVisual(RectTransform visual)
        {
            if (visual == null)
            {
                return new Vector2(0f, 220f);
            }

            var worldCenter = visual.TransformPoint(visual.rect.center);
            var localInHand = (Vector2)CachedTransform.InverseTransformPoint(worldCenter);
            var rect = CachedRectTransform.rect;
            var anchorReference = new Vector2(
                Mathf.Lerp(rect.xMin, rect.xMax, 0.5f),
                Mathf.Lerp(rect.yMin, rect.yMax, 0f));
            return localInHand - anchorReference + new Vector2(0f, -CardHeight * 0.5f);
        }

        private static float SignedZ(float eulerZ)
        {
            if (eulerZ > 180f)
            {
                return eulerZ - 360f;
            }

            return eulerZ;
        }

        private static bool ContainsCard(List<CardView> cards, CardView card)
        {
            if (cards == null || card == null)
            {
                return false;
            }

            for (var i = 0; i < cards.Count; i++)
            {
                if (cards[i] == card)
                {
                    return true;
                }
            }

            return false;
        }

        private bool TryScreenToHand(Vector2 screen, out Vector2 local)
        {
            return RectTransformUtility.ScreenPointToLocalPointInRectangle(
                CachedRectTransform,
                screen,
                EventCamera(),
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

        private bool SameHand(IReadOnlyList<int> handIds)
        {
            if (_cards.Count != handIds.Count)
            {
                return false;
            }

            for (var i = 0; i < handIds.Count; i++)
            {
                if (_cards[i] == null || _cards[i].InstanceId != handIds[i])
                {
                    return false;
                }
            }

            return true;
        }

        private static bool ContainsId(IReadOnlyCollection<int> ids, int id)
        {
            if (ids == null)
            {
                return false;
            }

            foreach (var value in ids)
            {
                if (value == id)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
