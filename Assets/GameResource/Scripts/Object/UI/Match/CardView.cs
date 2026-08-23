using System;
using System.Collections.Generic;
using Backend.App;
using Backend.Object.Management;
using UnityEngine;
using UnityEngine.UI;

namespace Backend.Object.UI
{
    /// <summary>
    /// 내 손패·공개 discardTop 은 Addressable 앞면 스프라이트.
    /// 상대 손패는 뒷면+장수만 보여 앞면을 붙이지 않는다.
    /// </summary>
    public sealed class CardView : UIView
    {
        /// <summary>기획서 §8 선택 시 위로 올리는 픽셀.</summary>
        public const float SelectedLift = 16f;

        private static readonly Dictionary<string, Sprite> SpriteCache = new Dictionary<string, Sprite>();
        private static readonly Color FrontTint = new Color(0.93f, 0.9f, 0.82f, 1f);
        private static readonly Color BackTint = new Color(0.22f, 0.28f, 0.4f, 1f);
        private static readonly Color SuitBlack = new Color(0.18f, 0.18f, 0.2f, 1f);
        private static readonly Color SuitRed = new Color(0.72f, 0.18f, 0.2f, 1f);
        private static readonly Color SuitBlue = new Color(0.16f, 0.32f, 0.72f, 1f);
        private static readonly Color SpecTint = new Color(0.45f, 0.38f, 0.22f, 1f);

        [SerializeField] private Image _fill;
        [SerializeField] private Text _label;
        [SerializeField] private CommonButton _button;
        [SerializeField] private CanvasGroup _group;

        private Vector2 _restAnchored;

        /// <summary>앞면이면 인스턴스 id. 뒷면·버림은 -1.</summary>
        public int InstanceId { get; private set; } = -1;

        /// <summary>앞면 defId. 뒷면이면 null.</summary>
        public string DefId { get; private set; }

        /// <summary>카드 탭. 풀 반환 후에도 구독은 유지한다.</summary>
        public event Action<CardView> Clicked;

        /// <summary>
        /// 인스펙터 미배선 시 플레이스홀더 자식과 버튼을 만든다.
        /// </summary>
        public void EnsureParts(Font font)
        {
            if (_fill == null && !TryGetComponent(out _fill))
            {
                _fill = CachedGameObject.AddComponent<Image>();
            }

            _fill.raycastTarget = true;

            if (_label == null)
            {
                var labelTf = CachedTransform.Find("Label");
                GameObject labelGo;
                if (labelTf == null)
                {
                    labelGo = new GameObject("Label", typeof(RectTransform));
                    labelGo.transform.SetParent(CachedTransform, false);
                }
                else
                {
                    labelGo = labelTf.gameObject;
                }

                if (!labelGo.TryGetComponent(out _label))
                {
                    _label = labelGo.AddComponent<Text>();
                }

                var labelRt = _label.rectTransform;
                labelRt.anchorMin = Vector2.zero;
                labelRt.anchorMax = Vector2.one;
                labelRt.offsetMin = new Vector2(6f, 6f);
                labelRt.offsetMax = new Vector2(-6f, -6f);
            }

            _label.raycastTarget = false;
            _label.alignment = TextAnchor.MiddleCenter;
            _label.horizontalOverflow = HorizontalWrapMode.Wrap;
            _label.verticalOverflow = VerticalWrapMode.Truncate;
            _label.fontSize = 26;
            _label.color = Color.white;
            if (font != null)
            {
                _label.font = font;
            }

            if (_button == null)
            {
                TryGetComponent(out _button);
            }

            if (_button == null)
            {
                _button = CachedGameObject.AddComponent<CommonButton>();
            }

            _button.useSound = false;
            if (_button.OnClick == null)
            {
                _button.OnClick = new UnityEngine.Events.UnityEvent();
            }

            _button.OnClick.RemoveListener(HandleClick);
            _button.OnClick.AddListener(HandleClick);

            if (_group == null && !TryGetComponent(out _group))
            {
                _group = CachedGameObject.AddComponent<CanvasGroup>();
            }
        }

        /// <summary>
        /// HandLayout 이 정한 선택 전 위치.
        /// </summary>
        public void SetRest(Vector2 restAnchored)
        {
            _restAnchored = restAnchored;
        }

        /// <summary>
        /// 내 손패 앞면. selected 면 16px 올린다.
        /// </summary>
        public void BindFront(int instanceId, string defId, bool selected)
        {
            InstanceId = instanceId;
            DefId = defId;
            var sprite = LoadCardSprite(CardArtKeys.FrontAddress(defId));
            var fallback = string.IsNullOrEmpty(defId) ? "?" : defId;
            ApplySprite(sprite, FrontColor(defId), fallback, FrontLabelColor(defId), sprite == null);
            SetInteractable(true);
            SetLegal(true);
            SetSelected(selected);
        }

        /// <summary>
        /// 상대·덱 뒷면. 앞면 스프라이트는 붙이지 않고 장수만 표시한다.
        /// </summary>
        public void BindBack(int count)
        {
            InstanceId = -1;
            DefId = null;
            var sprite = LoadCardSprite(CardArtKeys.BackAddress());
            ApplySprite(sprite, BackTint, count.ToString(), Color.white, true);
            SetLegal(true);
            SetInteractable(false);
            SetSelected(false);
        }

        /// <summary>
        /// 공개 버림 top. 탭하지 않는다.
        /// </summary>
        public void BindDiscard(string defId)
        {
            BindFront(-1, defId, false);
            SetInteractable(false);
        }

        /// <summary>
        /// 선택 여부에 따라 카드를 16px 올린다.
        /// </summary>
        public void SetSelected(bool selected)
        {
            var pos = _restAnchored;
            if (selected)
            {
                pos.y += SelectedLift;
            }

            CachedRectTransform.anchoredPosition = pos;
            CachedTransform.localScale = selected ? new Vector3(1.06f, 1.06f, 1f) : Vector3.one;
        }

        /// <summary>
        /// 합법=불투명, 불법=투명 40%·레이캐스트 차단.
        /// </summary>
        public void SetLegal(bool legal)
        {
            if (_group == null && !TryGetComponent(out _group))
            {
                _group = CachedGameObject.AddComponent<CanvasGroup>();
            }

            if (_group != null)
            {
                _group.alpha = legal ? 1f : GamePointer.IllegalAlpha;
                _group.blocksRaycasts = legal;
                _group.interactable = legal;
            }
        }

        /// <summary>
        /// 입력 가능 여부를 버튼에 반영한다.
        /// </summary>
        public void SetInteractable(bool interactable)
        {
            if (_button != null)
            {
                _button.interactable = interactable;
            }
        }

        private void HandleClick()
        {
            Clicked?.Invoke(this);
        }

        private void ApplySprite(Sprite sprite, Color fallbackTint, string label, Color labelColor, bool showLabel)
        {
            if (_fill != null)
            {
                _fill.sprite = sprite;
                _fill.color = sprite != null ? Color.white : fallbackTint;
                _fill.preserveAspect = sprite != null;
            }

            if (_label != null)
            {
                _label.enabled = showLabel;
                _label.text = label;
                _label.color = labelColor;
            }
        }

        private static Sprite LoadCardSprite(string address)
        {
            if (string.IsNullOrEmpty(address) || GameStateUtil.IsQuitting)
            {
                return null;
            }

            if (SpriteCache.TryGetValue(address, out var cached) && cached != null)
            {
                return cached;
            }

            var sprite = ResourceManager.LoadResource<Sprite>(address);
            if (sprite != null)
            {
                SpriteCache[address] = sprite;
            }

            return sprite;
        }

        private static Color FrontColor(string defId)
        {
            if (string.IsNullOrEmpty(defId))
            {
                return FrontTint;
            }

            if (defId.StartsWith("SPEC", StringComparison.Ordinal) || defId.StartsWith("JOKER", StringComparison.Ordinal))
            {
                return SpecTint;
            }

            return FrontTint;
        }

        private static Color FrontLabelColor(string defId)
        {
            if (string.IsNullOrEmpty(defId) || defId.Length < 1)
            {
                return SuitBlack;
            }

            switch (defId[0])
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
