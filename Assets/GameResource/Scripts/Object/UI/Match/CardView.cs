using System;
using System.Collections.Generic;
using Backend.App;
using Backend.Object.Management;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Backend.Object.UI
{
    /// <summary>
    /// 내 손패·공개 discardTop 은 Addressable 앞면 스프라이트.
    /// 상대 손패는 뒷면+장수만 보여 앞면을 붙이지 않는다.
    /// </summary>
    public sealed class CardView : UIView, IInitializePotentialDragHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerEnterHandler, IPointerExitHandler
    {
        /// <summary>기획서 §8 선택 시 위로 올리는 픽셀.</summary>
        public const float SelectedLift = 16f;

        /// <summary>마우스 오버 시 살짝 올리는 픽셀.</summary>
        public const float HoverLift = 12f;

        private static readonly Dictionary<string, Sprite> SpriteCache = new Dictionary<string, Sprite>();
        private static readonly List<RaycastResult> RaycastHits = new List<RaycastResult>();
        private static readonly Color FrontTint = new Color(0.93f, 0.9f, 0.82f, 1f);
        private static readonly Color BackTint = new Color(0.22f, 0.28f, 0.4f, 1f);
        private static readonly Color SuitBlack = new Color(0.18f, 0.18f, 0.2f, 1f);
        private static readonly Color SuitRed = new Color(0.72f, 0.18f, 0.2f, 1f);
        private static readonly Color SuitBlue = new Color(0.16f, 0.32f, 0.72f, 1f);
        private static readonly Color SpecTint = new Color(0.45f, 0.38f, 0.22f, 1f);

        [SerializeField] private Image _fill;
        [SerializeField] private TextMeshProUGUI _label;
        [SerializeField] private CommonButton _button;
        [SerializeField] private CanvasGroup _group;

        private Vector2 _restAnchored;
        private float _restZ;
        private bool _hasRest;
        private bool _dragEnabled;
        private bool _hoverEnabled;
        private bool _hovered;
        private bool _selected;
        private bool _traveling;
        private bool _legal = true;
        private Canvas _canvas;
        private int _bindSerial;

        /// <summary>앞면이면 인스턴스 id. 뒷면·버림은 -1.</summary>
        public int InstanceId { get; private set; } = -1;

        /// <summary>합법 힌트. 이동 연출이 끝나도 SetLegal 값을 유지한다.</summary>
        public bool IsLegal => _legal;

        /// <summary>앞면 defId. 뒷면이면 null.</summary>
        public string DefId { get; private set; }

        /// <summary>카드 탭. 풀 반환 후에도 구독은 유지한다.</summary>
        public event Action<CardView> Clicked;

        /// <summary>손패 위에 마우스를 올렸다.</summary>
        public event Action<CardView> Hovered;

        /// <summary>손패에서 마우스가 나갔다.</summary>
        public event Action<CardView> Unhovered;

        /// <summary>손패 드래그가 시작됐다.</summary>
        public event Action<CardView, PointerEventData> DragBegan;

        /// <summary>손패 드래그 중.</summary>
        public event Action<CardView, PointerEventData> DragMoved;

        /// <summary>손패 드래그가 끝났다.</summary>
        public event Action<CardView, PointerEventData> DragEnded;

        /// <summary>손패에서 포인터를 따라가는 중이면 true.</summary>
        public bool IsDragging { get; private set; }

        /// <summary>덱에서 자리로 이동 중이면 true. 그동안 휴식 포즈를 덮지 않는다.</summary>
        public bool IsTraveling => _traveling;

        /// <summary>HandLayout 이 정한 선택 전 위치.</summary>
        public Vector2 RestAnchored => _restAnchored;

        /// <summary>HandLayout 이 정한 부채 회전(Z).</summary>
        public float RestZ => _restZ;

        /// <summary>
        /// 프리팹에 묶인 Fill·Label·Button을 찾는다. Label 자식은 만들지 않는다.
        /// </summary>
        public void EnsureParts(TMP_FontAsset font)
        {
            if (_fill == null && !TryGetComponent(out _fill))
            {
                _fill = CachedGameObject.AddComponent<Image>();
            }

            _fill.raycastTarget = true;

            if (_label == null)
            {
                var labelTf = CachedTransform.Find("Label");
                if (labelTf != null)
                {
                    labelTf.TryGetComponent(out _label);
                }
            }

            if (_label != null)
            {
                _label.raycastTarget = false;
                _label.alignment = TextAlignmentOptions.Center;
                _label.textWrappingMode = TextWrappingModes.Normal;
                _label.overflowMode = TextOverflowModes.Truncate;
                _label.fontSize = 26;
                _label.color = Color.white;
                if (font != null)
                {
                    _label.font = font;
                }
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
        /// 상대 손패 캡션을 카드 바깥 하단에 두 줄 중앙정렬로 둔다.
        /// </summary>
        public void LayoutCaptionBelow()
        {
            if (_label == null)
            {
                return;
            }

            var rt = _label.rectTransform;
            rt.anchorMin = new Vector2(0.5f, 0f);
            rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -6f);
            rt.sizeDelta = new Vector2(240f, 52f);
            _label.alignment = TextAlignmentOptions.Center;
            _label.textWrappingMode = TextWrappingModes.NoWrap;
            _label.overflowMode = TextOverflowModes.Overflow;
            _label.fontSize = 18;
            _label.raycastTarget = false;
        }

        /// <summary>
        /// HandLayout 이 정한 선택 전 위치·회전.
        /// </summary>
        public void SetRest(Vector2 restAnchored, float restZ = 0f)
        {
            _restAnchored = restAnchored;
            _restZ = restZ;
            _hasRest = true;
        }

        /// <summary>
        /// 드로우 이동 중에는 휴식 포즈·입력을 잠시 끈다.
        /// </summary>
        public void SetTraveling(bool traveling)
        {
            _traveling = traveling;
            if (!traveling)
            {
                ApplyPose();
            }
        }

        /// <summary>
        /// 내 손패 앞면. selected 면 16px 올린다.
        /// </summary>
        public void BindFront(int instanceId, string defId, bool selected)
        {
            InstanceId = instanceId;
            DefId = defId;
            var fallback = string.IsNullOrEmpty(defId) ? "?" : defId;
            BindCardArt(
                CardArtKeys.FrontAddress(defId),
                FrontColor(defId),
                fallback,
                FrontLabelColor(defId),
                alwaysShowLabel: false);
            SetDragEnabled(false);
            SetInteractable(true);
            SetLegal(true);
            SetSelected(selected);
        }

        /// <summary>
        /// 상대·덱 뒷면. 앞면 스프라이트는 붙이지 않고 장수만 표시한다.
        /// </summary>
        public void BindBack(int count)
        {
            BindBack(count, count.ToString(), false);
        }

        /// <summary>
        /// 상대 뒷면. caption 은 닉·장수, justActed 면 강조한다.
        /// </summary>
        public void BindBack(int count, string caption, bool justActed)
        {
            InstanceId = -1;
            DefId = null;
            var label = string.IsNullOrEmpty(caption) ? count.ToString() : caption;
            BindCardArt(
                CardArtKeys.BackAddress(),
                justActed ? new Color(0.42f, 0.36f, 0.18f, 1f) : BackTint,
                label,
                Color.white,
                alwaysShowLabel: true);
            SetDragEnabled(false);
            SetLegal(true);
            SetInteractable(false);
            SetSelected(false);
            CachedTransform.localScale = justActed ? new Vector3(1.12f, 1.12f, 1f) : Vector3.one;
        }

        /// <summary>
        /// 공개 버림 top. 탭하지 않는다. justPlayed 면 방금 낸 장으로 강조한다.
        /// </summary>
        public void BindDiscard(string defId, bool justPlayed = false)
        {
            BindFront(-1, defId, false);
            SetInteractable(false);
            CachedTransform.localScale = justPlayed ? new Vector3(1.14f, 1.14f, 1f) : Vector3.one;
        }

        /// <summary>
        /// 선택 여부에 따라 카드를 부채 위쪽(로컬 up)으로 올린다.
        /// </summary>
        public void SetSelected(bool selected)
        {
            _selected = selected;
            ApplyPose();
        }

        /// <summary>
        /// 호버 연출을 켤지. 끄면 올린 호버를 내린다.
        /// </summary>
        public void SetHoverEnabled(bool enabled)
        {
            _hoverEnabled = enabled;
            if (!enabled && _hovered)
            {
                ClearHover();
            }
        }

        /// <summary>
        /// 호버 올림을 알림 없이 되돌린다. 다른 장으로 호버가 옮겨갈 때 쓴다.
        /// </summary>
        public void ClearHover()
        {
            if (!_hovered)
            {
                return;
            }

            _hovered = false;
            if (!IsDragging)
            {
                ApplyPose();
            }
        }

        /// <summary>
        /// 손패 위에 포인터가 들어오면 살짝 올린다.
        /// </summary>
        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!_hoverEnabled || IsDragging || InstanceId < 0 || _hovered)
            {
                return;
            }

            _hovered = true;
            ApplyPose();
            Hovered?.Invoke(this);
        }

        /// <summary>
        /// 손패에서 포인터가 나가면 호버 올림을 되돌린다.
        /// </summary>
        public void OnPointerExit(PointerEventData eventData)
        {
            if (eventData != null
                && RectTransformUtility.RectangleContainsScreenPoint(
                    CachedRectTransform,
                    eventData.position,
                    EventCamera()))
            {
                return;
            }

            EndHover();
        }

        private void LateUpdate()
        {
            if (!_hovered || IsDragging || GameStateUtil.IsQuitting)
            {
                return;
            }

            var pointer = Pointer.current;
            if (pointer == null)
            {
                EndHover();
                return;
            }

            var pos = pointer.position.ReadValue();
            if (RectTransformUtility.RectangleContainsScreenPoint(CachedRectTransform, pos, EventCamera())
                || IsTopRaycastTarget())
            {
                return;
            }

            EndHover();
        }

        private void EndHover()
        {
            if (!_hovered)
            {
                return;
            }

            _hovered = false;
            if (!IsDragging)
            {
                ApplyPose();
            }

            Unhovered?.Invoke(this);
        }

        private bool IsTopRaycastTarget()
        {
            var es = EventSystem.current;
            var pointer = Pointer.current;
            if (es == null || pointer == null)
            {
                return false;
            }

            var data = new PointerEventData(es)
            {
                position = pointer.position.ReadValue()
            };
            RaycastHits.Clear();
            es.RaycastAll(data, RaycastHits);
            if (RaycastHits.Count == 0)
            {
                return false;
            }

            var top = RaycastHits[0].gameObject;
            return top == CachedGameObject || top.transform.IsChildOf(CachedTransform);
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

        /// <summary>
        /// 합법=불투명, 불법=더 투명·레이캐스트 차단.
        /// </summary>
        public void SetLegal(bool legal)
        {
            _legal = legal;
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
        /// 입력 가능 여부를 버튼·레이캐스트에 반영한다. 끄면 호버·클릭도 막는다.
        /// </summary>
        public void SetInteractable(bool interactable)
        {
            if (_button != null)
            {
                _button.interactable = interactable;
            }

            if (_group != null)
            {
                _group.interactable = interactable;
                _group.blocksRaycasts = interactable;
            }
        }

        /// <summary>
        /// 손패 드래그 내기. 덱·미리보기·지급 탭은 끈다.
        /// </summary>
        public void SetDragEnabled(bool enabled)
        {
            _dragEnabled = enabled;
            if (!enabled && IsDragging)
            {
                IsDragging = false;
            }
        }

        /// <summary>
        /// 포인터를 따라 카드를 옮긴다. 회전은 세운다.
        /// </summary>
        public void FollowAnchored(Vector2 anchored)
        {
            CachedRectTransform.anchoredPosition = anchored;
            CachedRectTransform.localRotation = Quaternion.identity;
            CachedTransform.localScale = new Vector3(1.12f, 1.12f, 1f);
        }

        /// <summary>
        /// 부채 자리로 되돌린다.
        /// </summary>
        public void RestoreRest(bool selected)
        {
            IsDragging = false;
            _traveling = false;
            SetSelected(selected);
        }

        private void ApplyPose()
        {
            if (_traveling || !_hasRest)
            {
                return;
            }

            var rot = Quaternion.Euler(0f, 0f, _restZ);
            var lift = _selected ? SelectedLift : (_hovered && _hoverEnabled ? HoverLift : 0f);
            var up = (Vector2)(rot * Vector3.up);
            CachedRectTransform.anchoredPosition = _restAnchored + up * lift;
            CachedRectTransform.localRotation = rot;
            var scale = _selected ? 1.08f : (_hovered && _hoverEnabled ? 1.04f : 1f);
            CachedTransform.localScale = new Vector3(scale, scale, 1f);
        }

        /// <summary>
        /// 드래그가 꺼져 있으면 EventSystem 이 클릭을 삼키지 않게 한다.
        /// </summary>
        public void OnInitializePotentialDrag(PointerEventData eventData)
        {
            if (eventData == null)
            {
                return;
            }

            if (!_dragEnabled)
            {
                eventData.pointerDrag = null;
            }
        }

        /// <summary>
        /// 손패 드래그를 시작한다.
        /// </summary>
        public void OnBeginDrag(PointerEventData eventData)
        {
            if (!_dragEnabled || eventData == null)
            {
                return;
            }

            _traveling = false;
            IsDragging = true;
            DragBegan?.Invoke(this, eventData);
        }

        /// <summary>
        /// 손패 드래그 위치를 올린다.
        /// </summary>
        public void OnDrag(PointerEventData eventData)
        {
            if (!IsDragging || eventData == null)
            {
                return;
            }

            DragMoved?.Invoke(this, eventData);
        }

        /// <summary>
        /// 손패 드래그를 끝낸다.
        /// </summary>
        public void OnEndDrag(PointerEventData eventData)
        {
            if (!IsDragging)
            {
                return;
            }

            IsDragging = false;
            DragEnded?.Invoke(this, eventData);
        }

        private void HandleClick()
        {
            Clicked?.Invoke(this);
        }

        private void BindCardArt(
            string address,
            Color fallbackTint,
            string label,
            Color labelColor,
            bool alwaysShowLabel)
        {
            var serial = ++_bindSerial;
            if (TryGetCachedSprite(address, out var cached))
            {
                ApplySprite(cached, fallbackTint, label, labelColor, alwaysShowLabel || cached == null);
                return;
            }

            if (WebBuild.IsPlayer)
            {
                ApplySprite(null, fallbackTint, label, labelColor, true);
                ApplySpriteAsync(serial, address, fallbackTint, label, labelColor, alwaysShowLabel).Forget();
                return;
            }

            var sprite = LoadCardSprite(address);
            ApplySprite(sprite, fallbackTint, label, labelColor, alwaysShowLabel || sprite == null);
        }

        private async UniTaskVoid ApplySpriteAsync(
            int serial,
            string address,
            Color fallbackTint,
            string label,
            Color labelColor,
            bool alwaysShowLabel)
        {
            var sprite = await LoadCardSpriteAsync(address);
            if (this == null || serial != _bindSerial || GameStateUtil.IsQuitting)
            {
                return;
            }

            ApplySprite(sprite, fallbackTint, label, labelColor, alwaysShowLabel || sprite == null);
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

        private static bool TryGetCachedSprite(string address, out Sprite sprite)
        {
            sprite = null;
            if (string.IsNullOrEmpty(address))
            {
                return true;
            }

            return SpriteCache.TryGetValue(address, out sprite) && sprite != null;
        }

        private static Sprite LoadCardSprite(string address)
        {
            if (string.IsNullOrEmpty(address) || GameStateUtil.IsQuitting)
            {
                return null;
            }

            if (TryGetCachedSprite(address, out var cached))
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

        private static async UniTask<Sprite> LoadCardSpriteAsync(string address)
        {
            if (string.IsNullOrEmpty(address) || GameStateUtil.IsQuitting)
            {
                return null;
            }

            if (TryGetCachedSprite(address, out var cached))
            {
                return cached;
            }

            var sprite = await ResourceManager.LoadResourceAsync<Sprite>(address);
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
