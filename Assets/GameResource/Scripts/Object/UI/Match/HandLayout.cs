using System;
using System.Collections.Generic;
using Backend.Object.Management;
using UnityEngine;
using UnityEngine.UI;

namespace Backend.Object.UI
{
    /// <summary>
    /// 내 손패 배치. 합법 100% / 불법 40%·선택 불가 / 선택 +16px.
    /// 상대 앞면은 붙이지 않는다.
    /// </summary>
    public sealed class HandLayout : UIView
    {
        public const float CardWidth = 130f;
        public const float CardHeight = 182f;
        public const float PreferredSpacing = 10f;
        public const float MinSpacing = -48f;
        public const float RestY = 8f;

        private readonly List<CardView> _cards = new List<CardView>();

        [SerializeField] private CardView _prefab;
        [SerializeField] private Font _font;

        /// <summary>손패 탭. 불법 장은 올리지 않는다.</summary>
        public event Action<int> CardClicked;

        /// <summary>현재 손패 카드.</summary>
        public IReadOnlyList<CardView> Cards => _cards;

        /// <summary>
        /// 풀 템플릿과 폰트를 받는다.
        /// </summary>
        public void Bind(CardView prefab, Font font)
        {
            if (prefab != null)
            {
                _prefab = prefab;
            }

            if (font != null)
            {
                _font = font;
            }

            if (TryGetComponent(out HorizontalLayoutGroup group))
            {
                group.enabled = false;
            }
        }

        /// <summary>
        /// 손패를 다시 깔고 합법/선택 시각을 적용한다.
        /// legalFlags 가 null 이면 모두 선택 가능(지급·미러·숨김).
        /// </summary>
        public void Render(
            IReadOnlyList<int> handIds,
            IReadOnlyList<string> handDefs,
            IReadOnlyCollection<int> selectedIds,
            IReadOnlyList<bool> legalFlags,
            bool interactable)
        {
            Release();

            var count = handIds != null ? handIds.Count : 0;
            if (count == 0 || _prefab == null)
            {
                return;
            }

            ObjectPoolManager.GetOrCreatePool(_prefab, CachedTransform);
            for (var i = 0; i < count; i++)
            {
                var card = ObjectPoolManager.Get<CardView>();
                if (card == null)
                {
                    continue;
                }

                var id = handIds[i];
                var def = handDefs != null && i < handDefs.Count ? handDefs[i] : "?";
                var selected = ContainsId(selectedIds, id);
                var legal = legalFlags == null || (i < legalFlags.Count && legalFlags[i]);

                card.CachedTransform.SetParent(CachedTransform, false);
                card.CachedRectTransform.sizeDelta = new Vector2(CardWidth, CardHeight);
                Place(card, i, count, selected);
                card.EnsureParts(_font);
                card.Clicked -= OnCardClicked;
                card.Clicked += OnCardClicked;
                card.BindFront(id, def, selected);
                card.SetLegal(legal);
                card.SetInteractable(interactable && legal);
                _cards.Add(card);
            }
        }

        /// <summary>
        /// 손패를 풀에 돌려준다.
        /// </summary>
        public void Release()
        {
            for (var i = 0; i < _cards.Count; i++)
            {
                var card = _cards[i];
                if (card == null)
                {
                    continue;
                }

                card.Clicked -= OnCardClicked;
                ObjectPoolManager.Release(card);
            }

            _cards.Clear();
        }

        private void Place(CardView card, int index, int count, bool selected)
        {
            var rt = card.CachedRectTransform;
            rt.anchorMin = new Vector2(0.5f, 0f);
            rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);

            var spacing = PreferredSpacing;
            var width = CachedRectTransform.rect.width;
            if (width < 1f)
            {
                width = 1080f;
            }

            var total = count * CardWidth + (count - 1) * spacing;
            if (total > width && count > 1)
            {
                spacing = (width - count * CardWidth) / (count - 1);
                if (spacing < MinSpacing)
                {
                    spacing = MinSpacing;
                }
            }

            var used = count * CardWidth + (count - 1) * spacing;
            var x = -used * 0.5f + CardWidth * 0.5f + index * (CardWidth + spacing);
            var y = RestY + (selected ? CardView.SelectedLift : 0f);
            rt.anchoredPosition = new Vector2(x, y);
            card.SetRest(new Vector2(x, RestY));
        }

        private void OnCardClicked(CardView card)
        {
            if (card != null)
            {
                CardClicked?.Invoke(card.InstanceId);
            }
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
