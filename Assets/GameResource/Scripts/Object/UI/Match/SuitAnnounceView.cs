using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using LitMotion;
using LitMotion.Extensions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Backend.Object.UI
{
    /// <summary>
    /// 7 문양 지정 안내. 중앙에서 스케일 업으로 나타났다가 줄어들며 사라진다.
    /// </summary>
    public sealed class SuitAnnounceView : UIView
    {
        public const float TotalDuration = 3f;
        public const float PeakScale = 1.4f;
        public const float GrowDuration = 1.8f;

        [SerializeField] private Image _image;
        [SerializeField] private TextMeshProUGUI _label;
        [SerializeField] private CanvasGroup _group;
        [SerializeField] private TMP_FontAsset _font;

        private CancellationTokenSource _cts;
        private MotionHandle _scaleHandle;
        private MotionHandle _fadeHandle;

        /// <summary>
        /// 중앙 앵커·이미지·레이블을 준비한다. 레이캐스트는 막지 않는다.
        /// </summary>
        public void EnsureLayout(TMP_FontAsset font)
        {
            if (font != null)
            {
                _font = font;
            }

            var rt = CachedRectTransform;
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(280f, 280f);

            if (_image == null && !TryGetComponent(out _image))
            {
                _image = CachedGameObject.AddComponent<Image>();
            }

            _image.raycastTarget = false;
            _image.preserveAspect = true;

            if (_group == null && !TryGetComponent(out _group))
            {
                _group = CachedGameObject.AddComponent<CanvasGroup>();
            }

            _group.blocksRaycasts = false;
            _group.interactable = false;

            EnsureLabel();
            if (!CachedGameObject.activeSelf)
            {
                CachedTransform.localScale = Vector3.zero;
                _group.alpha = 0f;
            }
        }

        /// <summary>
        /// 지정된 문양을 3초 동안 키웠다가 줄이며 보여 준다.
        /// </summary>
        public void Play(string suit, Sprite sprite)
        {
            if (string.IsNullOrEmpty(suit) || GameStateUtil.IsQuitting)
            {
                return;
            }

            EnsureLayout(_font);
            Bind(suit, sprite);
            CachedTransform.SetAsLastSibling();
            CachedGameObject.SetActive(true);
            PlayAsync().Forget();
        }

        /// <summary>
        /// 진행 중인 안내를 즉시 끈다.
        /// </summary>
        public void Cancel()
        {
            StopTween();
            HideNow();
        }

        private void OnDisable()
        {
            StopTween();
        }

        private void EnsureLabel()
        {
            if (_label == null)
            {
                var labelTf = CachedTransform.Find("Label");
                if (labelTf != null)
                {
                    labelTf.TryGetComponent(out _label);
                }
            }

            if (_label == null)
            {
                var go = new GameObject("Label", typeof(RectTransform));
                go.transform.SetParent(CachedTransform, false);
                _label = go.AddComponent<TextMeshProUGUI>();
                var lrt = go.GetComponent<RectTransform>();
                lrt.anchorMin = Vector2.zero;
                lrt.anchorMax = Vector2.one;
                lrt.offsetMin = Vector2.zero;
                lrt.offsetMax = Vector2.zero;
            }

            _label.alignment = TextAlignmentOptions.Center;
            _label.raycastTarget = false;
            _label.fontSize = 160;
            _label.overflowMode = TextOverflowModes.Overflow;
            if (_font != null)
            {
                _label.font = _font;
            }
        }

        private void Bind(string suit, Sprite sprite)
        {
            var hasSprite = sprite != null;
            if (_image != null)
            {
                _image.sprite = sprite;
                _image.enabled = true;
                _image.color = hasSprite ? Color.white : ChoiceSheet.SuitBackground(suit);
            }

            if (_label != null)
            {
                _label.enabled = !hasSprite;
                _label.text = ChoiceSheet.SuitGlyph(suit);
                _label.color = ChoiceSheet.SuitForeground(suit);
            }
        }

        private async UniTaskVoid PlayAsync()
        {
            StopTween();
            _cts = new CancellationTokenSource();
            var token = _cts.Token;
            var tr = CachedTransform;
            tr.localScale = Vector3.zero;
            if (_group != null)
            {
                _group.alpha = 0f;
            }

            var shrink = TotalDuration - GrowDuration;
            if (shrink < 0.2f)
            {
                shrink = 0.2f;
            }

            try
            {
                _scaleHandle = LMotion.Create(Vector3.zero, Vector3.one * PeakScale, GrowDuration)
                    .WithEase(Ease.OutCubic)
                    .BindToLocalScale(tr);
                _fadeHandle = LMotion.Create(0f, 1f, Mathf.Min(0.4f, GrowDuration))
                    .WithEase(Ease.OutCubic)
                    .Bind(a =>
                    {
                        if (_group != null)
                        {
                            _group.alpha = a;
                        }
                    });
                await UniTask.WhenAll(_scaleHandle.ToUniTask(token), _fadeHandle.ToUniTask(token));
                token.ThrowIfCancellationRequested();

                _scaleHandle = LMotion.Create(Vector3.one * PeakScale, Vector3.zero, shrink)
                    .WithEase(Ease.InCubic)
                    .BindToLocalScale(tr);
                _fadeHandle = LMotion.Create(1f, 0f, shrink)
                    .WithEase(Ease.InCubic)
                    .Bind(a =>
                    {
                        if (_group != null)
                        {
                            _group.alpha = a;
                        }
                    });
                await UniTask.WhenAll(_scaleHandle.ToUniTask(token), _fadeHandle.ToUniTask(token));
                HideNow();
            }
            catch (OperationCanceledException)
            {
            }
        }

        private void StopTween()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
            if (_scaleHandle.IsActive())
            {
                _scaleHandle.Cancel();
            }

            if (_fadeHandle.IsActive())
            {
                _fadeHandle.Cancel();
            }
        }

        private void HideNow()
        {
            if (_group != null)
            {
                _group.alpha = 0f;
            }

            CachedTransform.localScale = Vector3.zero;
            CachedGameObject.SetActive(false);
        }
    }
}
