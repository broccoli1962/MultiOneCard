using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Backend.Object.UI
{
    /// <summary>
    /// 타이틀 화면 View. 표시와 시작 입력만 담당한다.
    /// </summary>
    public sealed class TitlePanel : UIPanel<TitlePresenter>
    {
        [SerializeField] private Font _font;
        [SerializeField] private Text _titleText;
        [SerializeField] private CommonButton _startButton;

        private bool _layoutReady;

        /// <summary>로비로 진입하는 시작 버튼.</summary>
        public event Action StartClicked;

        protected override void Awake()
        {
            if (Application.isPlaying)
            {
                EnsureLayout();
            }

            base.Awake();
        }

        /// <summary>
        /// 프리팹 미배선이어도 타이틀 레이아웃을 채운다.
        /// </summary>
        public void EnsureLayout()
        {
            if (_layoutReady && _startButton != null)
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

            bg.color = new Color(0.07f, 0.12f, 0.2f, 1f);
            bg.raycastTarget = true;

            _titleText = FindOrCreateText("Title", new Vector2(0.5f, 0.5f), new Vector2(0f, 180f), new Vector2(900f, 140f), 72f);
            _titleText.text = "원테이블";

            _startButton = FindOrCreateButton("Start", "시작", new Vector2(0.5f, 0.5f), new Vector2(0f, -40f), new Vector2(360f, 88f));
            BindButton(_startButton, () => StartClicked?.Invoke());

            _layoutReady = true;
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

            var textRt = go.GetComponent<RectTransform>();
            textRt.anchorMin = anchor;
            textRt.anchorMax = anchor;
            textRt.pivot = new Vector2(0.5f, 0.5f);
            textRt.anchoredPosition = pos;
            textRt.sizeDelta = size;

            if (!go.TryGetComponent(out Text text))
            {
                text = go.AddComponent<Text>();
            }

            ApplyTextStyle(text, fontSize);
            return text;
        }

        private CommonButton FindOrCreateButton(string name, string label, Vector2 anchor, Vector2 pos, Vector2 size)
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

            var buttonRt = go.GetComponent<RectTransform>();
            buttonRt.anchorMin = anchor;
            buttonRt.anchorMax = anchor;
            buttonRt.pivot = new Vector2(0.5f, 0.5f);
            buttonRt.anchoredPosition = pos;
            buttonRt.sizeDelta = size;
            if (go.TryGetComponent(out Image image))
            {
                image.color = new Color(0.16f, 0.16f, 0.18f, 0.92f);
            }

            if (!go.TryGetComponent(out CommonButton button))
            {
                button = go.AddComponent<CommonButton>();
            }

            button.useSound = false;
            EnsureButtonLabel(go.transform, label, 36f);
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

            var labelRt = go.GetComponent<RectTransform>();
            labelRt.anchorMin = Vector2.zero;
            labelRt.anchorMax = Vector2.one;
            labelRt.offsetMin = Vector2.zero;
            labelRt.offsetMax = Vector2.zero;
            if (!go.TryGetComponent(out Text text))
            {
                text = go.AddComponent<Text>();
            }

            text.text = label;
            ApplyTextStyle(text, fontSize);
        }

        private void ApplyTextStyle(Text text, float fontSize)
        {
            text.fontSize = (int)fontSize;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            var font = ResolveFont();
            if (font != null)
            {
                text.font = font;
            }
        }

        private Font ResolveFont()
        {
            return _font != null ? _font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
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
}
