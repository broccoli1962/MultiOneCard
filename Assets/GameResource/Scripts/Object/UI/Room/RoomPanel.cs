using System;
using Backend.Net;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Backend.Object.UI
{
    /// <summary>
    /// 대기실 View. 슬롯·준비·시작·룸코드·규칙·채팅 표시와 입력만 담당한다.
    /// </summary>
    public sealed class RoomPanel : UIPanel<RoomPresenter>
    {
        private const int MaxSlots = 6;

        [SerializeField] private Font _font;
        [SerializeField] private Text _titleText;
        [SerializeField] private Text _roomCodeText;
        [SerializeField] private Text _statusText;
        [SerializeField] private Text _rulesText;
        [SerializeField] private GameObject _rulesRoot;
        [SerializeField] private ChatView _chatView;
        [SerializeField] private CommonButton _readyButton;
        [SerializeField] private CommonButton _startButton;
        [SerializeField] private CommonButton _rulesButton;
        [SerializeField] private CommonButton _backButton;

        private readonly Text[] _slotTexts = new Text[MaxSlots];
        private readonly CommonButton[] _slotButtons = new CommonButton[MaxSlots];
        private bool _layoutReady;

        /// <summary>내 좌석 준비.</summary>
        public event Action ReadyClicked;

        /// <summary>방장 시작.</summary>
        public event Action StartClicked;

        /// <summary>규칙 보기 토글.</summary>
        public event Action RulesClicked;

        /// <summary>로비로 돌아가기.</summary>
        public event Action BackClicked;

        /// <summary>슬롯 탭. 로컬 루프백에서 해당 좌석 Ready.</summary>
        public event Action<int> SlotClicked;

        /// <summary>채팅 서브뷰.</summary>
        public ChatView Chat => _chatView;

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
        /// 프리팹 미배선이어도 대기실 레이아웃을 채운다.
        /// </summary>
        public void EnsureLayout()
        {
            if (_layoutReady && _chatView != null && _readyButton != null)
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

            bg.color = new Color(0.08f, 0.13f, 0.2f, 1f);
            bg.raycastTarget = true;

            _titleText = FindOrCreateText("Title", new Vector2(0.5f, 1f), new Vector2(0f, -70f), new Vector2(640f, 64f), 42f);
            _titleText.text = "대기실";

            _roomCodeText = FindOrCreateText("RoomCode", new Vector2(0.5f, 1f), new Vector2(0f, -130f), new Vector2(640f, 48f), 30f);
            _roomCodeText.text = "코드 ------";

            for (var i = 0; i < MaxSlots; i++)
            {
                var y = -200f - i * 56f;
                _slotButtons[i] = FindOrCreateButton("Slot" + i, "빈 자리", new Vector2(0.5f, 1f), new Vector2(0f, y), new Vector2(640f, 50f));
                _slotTexts[i] = _slotButtons[i].GetComponentInChildren<Text>();
            }

            _readyButton = FindOrCreateButton("Ready", "준비", new Vector2(0.5f, 1f), new Vector2(-220f, -560f), new Vector2(200f, 64f));
            _startButton = FindOrCreateButton("Start", "시작", new Vector2(0.5f, 1f), new Vector2(0f, -560f), new Vector2(200f, 64f));
            _rulesButton = FindOrCreateButton("Rules", "규칙", new Vector2(0.5f, 1f), new Vector2(220f, -560f), new Vector2(200f, 64f));

            _statusText = FindOrCreateText("Status", new Vector2(0.5f, 1f), new Vector2(0f, -620f), new Vector2(900f, 40f), 24f);
            _statusText.text = string.Empty;

            _backButton = FindOrCreateButton("Back", "뒤로", new Vector2(0f, 1f), new Vector2(90f, -70f), new Vector2(140f, 64f));
            var backRt = _backButton.CachedRectTransform;
            backRt.anchorMin = new Vector2(0f, 1f);
            backRt.anchorMax = new Vector2(0f, 1f);
            backRt.pivot = new Vector2(0.5f, 0.5f);
            backRt.anchoredPosition = new Vector2(90f, -70f);

            _chatView = FindOrCreateChat();
            _chatView.EnsureLayout(_font);

            _rulesRoot = FindOrCreate("RulesRoot", typeof(RectTransform), typeof(Image));
            var rulesRt = _rulesRoot.GetComponent<RectTransform>();
            rulesRt.anchorMin = new Vector2(0.1f, 0.22f);
            rulesRt.anchorMax = new Vector2(0.9f, 0.82f);
            rulesRt.offsetMin = Vector2.zero;
            rulesRt.offsetMax = Vector2.zero;
            if (_rulesRoot.TryGetComponent(out Image rulesBg))
            {
                rulesBg.color = new Color(0.1f, 0.12f, 0.16f, 0.96f);
                rulesBg.raycastTarget = true;
            }

            _rulesText = FindOrCreateChildText(_rulesRoot.transform, "RulesBody", 24f);
            _rulesText.alignment = TextAnchor.UpperLeft;
            _rulesText.text =
                "공식 규칙\n" +
                "인원 2~6. 턴 15초.\n" +
                "같은 무늬 또는 같은 랭크.\n" +
                "조커·무색은 알약 락이 없으면 아무 위에도 가능.\n" +
                "한 턴 1장. 예외는 K.\n" +
                "손패 0장이면 1위.\n" +
                "퀵매치는 항상 공식.";
            _rulesRoot.SetActive(false);

            BindButton(_readyButton, () => ReadyClicked?.Invoke());
            BindButton(_startButton, () => StartClicked?.Invoke());
            BindButton(_rulesButton, () => RulesClicked?.Invoke());
            BindButton(_backButton, () => BackClicked?.Invoke());
            for (var i = 0; i < MaxSlots; i++)
            {
                var seat = i;
                BindButton(_slotButtons[i], () => SlotClicked?.Invoke(seat));
            }

            _layoutReady = true;
        }

        /// <summary>
        /// 룸코드·슬롯·방장 시작 버튼을 그린다.
        /// </summary>
        public void Render(RoomView room, int localSeat, bool isHost, string status)
        {
            EnsureLayout();
            var code = room != null && !string.IsNullOrEmpty(room.roomCode) ? room.roomCode : "------";
            _roomCodeText.text = "코드 " + code;
            _statusText.text = status ?? string.Empty;
            if (_startButton != null)
            {
                _startButton.CachedGameObject.SetActive(isHost);
            }

            var seatCount = room != null ? room.seatCount : 0;
            if (seatCount < 2)
            {
                seatCount = 2;
            }

            if (seatCount > MaxSlots)
            {
                seatCount = MaxSlots;
            }

            for (var i = 0; i < MaxSlots; i++)
            {
                var show = i < seatCount;
                if (_slotButtons[i] != null)
                {
                    _slotButtons[i].CachedGameObject.SetActive(show);
                }

                if (!show || _slotTexts[i] == null)
                {
                    continue;
                }

                var nick = room != null && room.nicks != null && i < room.nicks.Length ? room.nicks[i] : "빈 자리";
                var ready = room != null && room.ready != null && i < room.ready.Length && room.ready[i];
                var host = room != null && i == room.hostSeat;
                var mine = i == localSeat;
                _slotTexts[i].text = $"좌석{i + 1}  {nick}  {(ready ? "준비" : "대기")}{(host ? " 방장" : string.Empty)}{(mine ? " 나" : string.Empty)}";
            }

            var localReady = room != null && room.ready != null && localSeat >= 0 && localSeat < room.ready.Length
                && room.ready[localSeat];
            SetReadyLabel(localReady);
        }

        /// <summary>
        /// 규칙 패널을 보이거나 숨긴다.
        /// </summary>
        public void SetRulesVisible(bool visible)
        {
            EnsureLayout();
            if (_rulesRoot != null)
            {
                _rulesRoot.SetActive(visible);
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

        private void SetReadyLabel(bool ready)
        {
            if (_readyButton == null)
            {
                return;
            }

            var label = _readyButton.GetComponentInChildren<Text>();
            if (label != null)
            {
                label.text = ready ? "준비됨" : "준비";
            }
        }

        private ChatView FindOrCreateChat()
        {
            var existing = CachedTransform.Find("ChatView");
            GameObject go;
            if (existing != null)
            {
                go = existing.gameObject;
            }
            else
            {
                go = new GameObject("ChatView", typeof(RectTransform), typeof(Image), typeof(ChatView));
                go.transform.SetParent(CachedTransform, false);
            }

            if (!go.TryGetComponent(out ChatView chat))
            {
                chat = go.AddComponent<ChatView>();
            }

            return chat;
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

            ApplyTextStyle(text, fontSize, TextAnchor.MiddleCenter);
            return text;
        }

        private Text FindOrCreateChildText(Transform parent, string name, float fontSize)
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
            textRt.offsetMin = new Vector2(24f, 16f);
            textRt.offsetMax = new Vector2(-24f, -16f);
            if (!go.TryGetComponent(out Text text))
            {
                text = go.AddComponent<Text>();
            }

            ApplyTextStyle(text, fontSize, TextAnchor.UpperLeft);
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
            EnsureButtonLabel(go.transform, label, 26f);
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
            ApplyTextStyle(text, fontSize, TextAnchor.MiddleCenter);
        }

        private void ApplyTextStyle(Text text, float fontSize, TextAnchor alignment)
        {
            text.fontSize = (int)fontSize;
            text.alignment = alignment;
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

        private GameObject FindOrCreate(string name, params Type[] components)
        {
            var existing = CachedTransform.Find(name);
            if (existing != null)
            {
                return existing.gameObject;
            }

            var go = new GameObject(name, components);
            go.transform.SetParent(CachedTransform, false);
            return go;
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
