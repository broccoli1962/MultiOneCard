using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Backend.Object.UI
{
    /// <summary>
    /// 로비 View. 닉·퀵매치·방 만들기·룸코드 입장 입력만 담당한다.
    /// </summary>
    public sealed class LobbyPanel : UIPanel<LobbyPresenter>
    {
        private const int NickMaxLength = 12;
        private const int RoomCodeLength = 6;

        [SerializeField] private Font _font;
        [SerializeField] private Text _titleText;
        [SerializeField] private Text _statusText;
        [SerializeField] private InputField _nickInput;
        [SerializeField] private InputField _roomCodeInput;
        [SerializeField] private CommonButton _quickMatch2Button;
        [SerializeField] private CommonButton _quickMatch4Button;
        [SerializeField] private CommonButton _quickMatch6Button;
        [SerializeField] private CommonButton _createRoomButton;
        [SerializeField] private CommonButton _joinRoomButton;
        [SerializeField] private CommonButton _backButton;

        private bool _layoutReady;

        /// <summary>닉 입력 변경.</summary>
        public event Action<string> NickChanged;

        /// <summary>퀵매치 인원. 2/4/6.</summary>
        public event Action<int> QuickMatchClicked;

        /// <summary>방 만들기.</summary>
        public event Action CreateRoomClicked;

        /// <summary>룸코드 입장.</summary>
        public event Action JoinRoomClicked;

        /// <summary>타이틀로 돌아가기.</summary>
        public event Action BackClicked;

        /// <summary>현재 닉 입력.</summary>
        public string NickText => _nickInput != null ? _nickInput.text : string.Empty;

        /// <summary>현재 룸코드 입력.</summary>
        public string RoomCodeText => _roomCodeInput != null ? _roomCodeInput.text : string.Empty;

        protected override bool DefaultHandleBackButton => true;

        protected override void Awake()
        {
            if (Application.isPlaying)
            {
                EnsureLayout();
            }

            base.Awake();
        }

        /// <summary>
        /// 프리팹 미배선이어도 로비 레이아웃을 채운다.
        /// </summary>
        public void EnsureLayout()
        {
            if (_layoutReady && _nickInput != null && _roomCodeInput != null)
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

            bg.color = new Color(0.08f, 0.14f, 0.22f, 1f);
            bg.raycastTarget = true;

            _titleText = FindOrCreateText("Title", new Vector2(0.5f, 1f), new Vector2(0f, -90f), new Vector2(900f, 80f), 48f);
            _titleText.text = "로비";

            FindOrCreateText("NickLabel", new Vector2(0.5f, 0.5f), new Vector2(0f, 260f), new Vector2(640f, 40f), 26f).text = "닉네임 (2~12자)";
            _nickInput = FindOrCreateInput("NickInput", new Vector2(0f, 200f), new Vector2(640f, 72f), NickMaxLength, InputField.ContentType.Standard, "닉 입력");

            _quickMatch2Button = FindOrCreateButton("Quick2", "퀵매치 2인", new Vector2(0.5f, 0.5f), new Vector2(-220f, 80f), new Vector2(200f, 80f));
            _quickMatch4Button = FindOrCreateButton("Quick4", "퀵매치 4인", new Vector2(0.5f, 0.5f), new Vector2(0f, 80f), new Vector2(200f, 80f));
            _quickMatch6Button = FindOrCreateButton("Quick6", "퀵매치 6인", new Vector2(0.5f, 0.5f), new Vector2(220f, 80f), new Vector2(200f, 80f));

            _createRoomButton = FindOrCreateButton("CreateRoom", "방 만들기", new Vector2(0.5f, 0.5f), new Vector2(0f, -20f), new Vector2(420f, 80f));

            FindOrCreateText("RoomLabel", new Vector2(0.5f, 0.5f), new Vector2(0f, -110f), new Vector2(640f, 40f), 26f).text = "룸코드 6자리";
            _roomCodeInput = FindOrCreateInput("RoomCodeInput", new Vector2(-110f, -170f), new Vector2(300f, 72f), RoomCodeLength, InputField.ContentType.IntegerNumber, "000000");
            _joinRoomButton = FindOrCreateButton("JoinRoom", "입장", new Vector2(0.5f, 0.5f), new Vector2(170f, -170f), new Vector2(200f, 72f));

            _statusText = FindOrCreateText("Status", new Vector2(0.5f, 0.5f), new Vector2(0f, -270f), new Vector2(900f, 80f), 28f);
            _statusText.text = string.Empty;

            _backButton = FindOrCreateButton("Back", "뒤로", new Vector2(0f, 1f), new Vector2(90f, -70f), new Vector2(140f, 64f));
            var backRt = _backButton.CachedRectTransform;
            backRt.anchorMin = new Vector2(0f, 1f);
            backRt.anchorMax = new Vector2(0f, 1f);
            backRt.pivot = new Vector2(0.5f, 0.5f);
            backRt.anchoredPosition = new Vector2(90f, -70f);

            BindInput(_nickInput, value => NickChanged?.Invoke(value));
            BindButton(_quickMatch2Button, () => QuickMatchClicked?.Invoke(2));
            BindButton(_quickMatch4Button, () => QuickMatchClicked?.Invoke(4));
            BindButton(_quickMatch6Button, () => QuickMatchClicked?.Invoke(6));
            BindButton(_createRoomButton, () => CreateRoomClicked?.Invoke());
            BindButton(_joinRoomButton, () => JoinRoomClicked?.Invoke());
            BindButton(_backButton, () => BackClicked?.Invoke());

            _layoutReady = true;
        }

        /// <summary>
        /// 저장된 닉을 입력칸에 채운다.
        /// </summary>
        public void SetNick(string nick)
        {
            EnsureLayout();
            if (_nickInput != null)
            {
                _nickInput.SetTextWithoutNotify(nick ?? string.Empty);
            }
        }

        /// <summary>
        /// 상태 문구를 표시한다.
        /// </summary>
        public void SetStatus(string status)
        {
            EnsureLayout();
            if (_statusText != null)
            {
                _statusText.text = status ?? string.Empty;
            }
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

            ApplyTextStyle(text, fontSize, raycast: false);
            return text;
        }

        private InputField FindOrCreateInput(
            string name,
            Vector2 pos,
            Vector2 size,
            int characterLimit,
            InputField.ContentType contentType,
            string placeholder)
        {
            var existing = CachedTransform.Find(name);
            GameObject go;
            if (existing != null)
            {
                go = existing.gameObject;
            }
            else
            {
                go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(InputField));
                go.transform.SetParent(CachedTransform, false);
            }

            var inputRt = go.GetComponent<RectTransform>();
            inputRt.anchorMin = new Vector2(0.5f, 0.5f);
            inputRt.anchorMax = new Vector2(0.5f, 0.5f);
            inputRt.pivot = new Vector2(0.5f, 0.5f);
            inputRt.anchoredPosition = pos;
            inputRt.sizeDelta = size;
            if (go.TryGetComponent(out Image image))
            {
                image.color = new Color(0.12f, 0.12f, 0.14f, 0.95f);
            }

            if (!go.TryGetComponent(out InputField input))
            {
                input = go.AddComponent<InputField>();
            }

            var text = FindOrCreateChildText(go.transform, "Text", Color.white);
            var place = FindOrCreateChildText(go.transform, "Placeholder", new Color(1f, 1f, 1f, 0.35f));
            place.text = placeholder;
            input.textComponent = text;
            input.placeholder = place;
            input.characterLimit = characterLimit;
            input.contentType = contentType;
            input.lineType = InputField.LineType.SingleLine;
            return input;
        }

        private Text FindOrCreateChildText(Transform parent, string name, Color color)
        {
            var existing = parent.Find(name);
            GameObject go;
            if (existing != null)
            {
                go = existing.gameObject;
            }
            else
            {
                go = new GameObject(name, typeof(RectTransform), typeof(Text));
                go.transform.SetParent(parent, false);
            }

            var textRt = go.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = new Vector2(12f, 4f);
            textRt.offsetMax = new Vector2(-12f, -4f);
            if (!go.TryGetComponent(out Text text))
            {
                text = go.AddComponent<Text>();
            }

            text.color = color;
            text.raycastTarget = false;
            text.alignment = TextAnchor.MiddleLeft;
            text.fontSize = 28;
            text.supportRichText = false;
            var font = ResolveFont();
            if (font != null)
            {
                text.font = font;
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
            EnsureButtonLabel(go.transform, label, 28f);
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
            ApplyTextStyle(text, fontSize, raycast: false);
        }

        private void ApplyTextStyle(Text text, float fontSize, bool raycast)
        {
            text.fontSize = (int)fontSize;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.raycastTarget = raycast;
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

        private static void BindInput(InputField input, UnityEngine.Events.UnityAction<string> action)
        {
            if (input == null)
            {
                return;
            }

            input.onValueChanged.RemoveAllListeners();
            input.onValueChanged.AddListener(action);
        }
    }
}
