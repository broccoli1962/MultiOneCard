using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Backend.Object.UI
{
    /// <summary>
    /// 결과 화면 View. 순위·장수·점수와 재대결 투표 입력만 담당한다.
    /// </summary>
    public sealed class ResultPanel : UIPanel<ResultPresenter>
    {
        [SerializeField] private Font _font;
        [SerializeField] private Text _titleText;
        [SerializeField] private Text _rankText;
        [SerializeField] private Text _timerText;
        [SerializeField] private Text _statusText;
        [SerializeField] private CommonButton _yesButton;
        [SerializeField] private CommonButton _noButton;

        private bool _layoutReady;

        /// <summary>재대결 찬성.</summary>
        public event Action YesClicked;

        /// <summary>재대결 반대. 미투표 만료도 Presenter 가 이 쪽으로 처리한다.</summary>
        public event Action NoClicked;

        protected override bool DefaultHandleBackButton => true;

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
        }

        /// <summary>
        /// 프리팹 미배선이어도 결과 레이아웃을 채운다.
        /// </summary>
        public void EnsureLayout()
        {
            if (_layoutReady && _yesButton != null && _noButton != null)
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

            bg.color = new Color(0.06f, 0.08f, 0.14f, 0.96f);
            bg.raycastTarget = true;

            _titleText = FindOrCreateText("Title", new Vector2(0.5f, 1f), new Vector2(0f, -90f), new Vector2(720f, 72f), 48f);
            _titleText.text = "결과";

            _rankText = FindOrCreateText("Ranks", new Vector2(0.5f, 0.5f), new Vector2(0f, 80f), new Vector2(860f, 320f), 32f);
            _timerText = FindOrCreateText("Timer", new Vector2(0.5f, 0.5f), new Vector2(0f, -160f), new Vector2(720f, 48f), 28f);
            _statusText = FindOrCreateText("Status", new Vector2(0.5f, 0.5f), new Vector2(0f, -210f), new Vector2(720f, 40f), 24f);

            _yesButton = FindOrCreateButton("Yes", "재대결", new Vector2(0.5f, 0.5f), new Vector2(-160f, -290f), new Vector2(240f, 80f));
            _noButton = FindOrCreateButton("No", "반대", new Vector2(0.5f, 0.5f), new Vector2(160f, -290f), new Vector2(240f, 80f));

            BindButton(_yesButton, () => YesClicked?.Invoke());
            BindButton(_noButton, () => NoClicked?.Invoke());

            _layoutReady = true;
        }

        /// <summary>
        /// 순위표와 재대결 남은 초를 그린다.
        /// </summary>
        public void Render(string ranks, int remainSeconds, bool voted, bool voteYes)
        {
            EnsureLayout();
            _rankText.text = ranks ?? string.Empty;
            _timerText.text = remainSeconds > 0
                ? $"재대결 {remainSeconds}초"
                : "재대결 마감";
            if (!voted)
            {
                _statusText.text = "미투표는 반대";
            }
            else
            {
                _statusText.text = voteYes ? "재대결 찬성" : "재대결 반대";
            }

            if (_yesButton != null)
            {
                _yesButton.interactable = !voted;
            }

            if (_noButton != null)
            {
                _noButton.interactable = !voted;
            }
        }

        /// <summary>
        /// 뒤로가기 시 반대로 처리한다.
        /// </summary>
        public override bool OnBackPressed()
        {
            NoClicked?.Invoke();
            return false;
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
            text.fontSize = (int)fontSize;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.raycastTarget = false;
            if (_font != null)
            {
                text.font = _font;
            }
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
