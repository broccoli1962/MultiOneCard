using System;
using Backend.Net;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Backend.Object.UI
{
    /// <summary>
    /// 대기실 View. 슬롯·준비·시작·룸코드·규칙·채팅 표시와 입력만 담당한다.
    /// </summary>
    public sealed class RoomPanel : UIPanel<RoomPresenter>
    {
        private const int MaxSlots = 6;

        [SerializeField] private TMP_FontAsset _font;
        [SerializeField] private TextMeshProUGUI _titleText;
        [SerializeField] private TextMeshProUGUI _roomCodeText;
        [SerializeField] private TextMeshProUGUI _statusText;
        [SerializeField] private TextMeshProUGUI _rulesText;
        [SerializeField] private GameObject _rulesRoot;
        [SerializeField] private ChatView _chatView;
        [SerializeField] private CommonButton _readyButton;
        [SerializeField] private CommonButton _startButton;
        [SerializeField] private CommonButton _rulesButton;
        [SerializeField] private CommonButton _chatButton;
        [SerializeField] private CommonButton _backButton;
        [SerializeField] private CommonButton[] _slotButtons = new CommonButton[MaxSlots];
        [SerializeField] private TextMeshProUGUI[] _slotTexts = new TextMeshProUGUI[MaxSlots];

        private bool _layoutReady;

        /// <summary>내 좌석 준비.</summary>
        public event Action ReadyClicked;

        /// <summary>방장 시작.</summary>
        public event Action StartClicked;

        /// <summary>규칙 보기 토글.</summary>
        public event Action RulesClicked;

        /// <summary>채팅 패널 토글.</summary>
        public event Action ChatClicked;

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
        /// 프리팹 자식에 묶인 고정 위젯을 찾아 이벤트를 묶는다.
        /// </summary>
        public void EnsureLayout()
        {
            if (_layoutReady && _chatView != null && _readyButton != null)
            {
                return;
            }

            _titleText ??= FindOrCreateText("Title");
            _roomCodeText ??= FindOrCreateText("RoomCode");
            EnsureSlots();
            _readyButton ??= FindOrCreateButton("Ready");
            _startButton ??= FindOrCreateButton("Start");
            _rulesButton ??= FindOrCreateButton("Rules");
            _chatButton ??= FindOrCreateButton("Chat");
            _statusText ??= FindOrCreateText("Status");
            _backButton ??= FindOrCreateButton("Back");
            _chatView ??= FindOrCreateChat();
            _rulesRoot ??= FindOrCreate("RulesRoot");
            if (_rulesText == null && _rulesRoot != null)
            {
                var body = _rulesRoot.transform.Find("RulesBody");
                if (body != null)
                {
                    body.TryGetComponent(out _rulesText);
                }
            }

            if (_chatView != null)
            {
                _chatView.EnsureLayout(_font);
            }

            if (_rulesRoot != null)
            {
                _rulesRoot.SetActive(false);
            }

            BindButton(_readyButton, () => ReadyClicked?.Invoke());
            BindButton(_startButton, () => StartClicked?.Invoke());
            BindButton(_rulesButton, () => RulesClicked?.Invoke());
            BindButton(_chatButton, () => ChatClicked?.Invoke());
            BindButton(_backButton, () => BackClicked?.Invoke());
            for (var i = 0; i < MaxSlots; i++)
            {
                var seat = i;
                var button = SlotButton(i);
                BindButton(button, () => SlotClicked?.Invoke(seat));
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
            if (_roomCodeText != null)
            {
                _roomCodeText.text = "코드 " + code;
            }

            if (_statusText != null)
            {
                _statusText.text = status ?? string.Empty;
            }

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
                var button = SlotButton(i);
                if (button != null)
                {
                    button.CachedGameObject.SetActive(show);
                }

                var slotText = SlotText(i);
                if (!show || slotText == null)
                {
                    continue;
                }

                var nick = room != null && room.nicks != null && i < room.nicks.Length && !string.IsNullOrEmpty(room.nicks[i])
                    ? room.nicks[i]
                    : "빈 자리";
                var ready = room != null && room.ready != null && i < room.ready.Length && room.ready[i];
                var host = room != null && i == room.hostSeat;
                var mine = i == localSeat;
                slotText.text = $"좌석{i + 1}  {nick}  {(ready ? "준비" : "대기")}{(host ? " 방장" : string.Empty)}{(mine ? " 나" : string.Empty)}";
                var hostIcon = button != null ? button.CachedTransform.Find("HostIcon") : null;
                if (hostIcon != null)
                {
                    hostIcon.gameObject.SetActive(host);
                }
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
        /// 채팅 패널을 보이거나 숨긴다. 토글 버튼은 항상 켠다.
        /// </summary>
        public void SetChatVisible(bool visible)
        {
            EnsureLayout();
            if (_chatView != null)
            {
                _chatView.CachedGameObject.SetActive(visible);
            }

            SetChatLabel(visible);
            ChatView.SetUnreadDot(_chatButton, false);
        }

        /// <summary>
        /// 채팅 패널이 닫혀 있을 때 새 메시지 레드닷을 켠다.
        /// </summary>
        public void NotifyChatArrived()
        {
            EnsureLayout();
            if (_chatView != null && _chatView.CachedGameObject.activeSelf)
            {
                return;
            }

            ChatView.SetUnreadDot(_chatButton, true);
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

        private void SetChatLabel(bool visible)
        {
            if (_chatButton == null)
            {
                return;
            }

            var label = _chatButton.GetComponentInChildren<TextMeshProUGUI>(true);
            if (label != null)
            {
                label.text = visible ? "채팅 닫기" : "채팅";
            }
        }

        private void SetReadyLabel(bool ready)
        {
            if (_readyButton == null)
            {
                return;
            }

            var label = _readyButton.GetComponentInChildren<TextMeshProUGUI>(true);
            if (label != null)
            {
                label.text = ready ? "준비됨" : "준비";
            }
        }

        private void EnsureSlots()
        {
            if (_slotButtons == null || _slotButtons.Length != MaxSlots)
            {
                _slotButtons = new CommonButton[MaxSlots];
            }

            if (_slotTexts == null || _slotTexts.Length != MaxSlots)
            {
                _slotTexts = new TextMeshProUGUI[MaxSlots];
            }

            for (var i = 0; i < MaxSlots; i++)
            {
                _slotButtons[i] ??= FindOrCreateButton("Slot" + i);
                if (_slotTexts[i] == null && _slotButtons[i] != null)
                {
                    _slotTexts[i] = _slotButtons[i].GetComponentInChildren<TextMeshProUGUI>(true);
                }
            }
        }

        private CommonButton SlotButton(int index)
        {
            return _slotButtons != null && index >= 0 && index < _slotButtons.Length
                ? _slotButtons[index]
                : null;
        }

        private TextMeshProUGUI SlotText(int index)
        {
            return _slotTexts != null && index >= 0 && index < _slotTexts.Length
                ? _slotTexts[index]
                : null;
        }

        private ChatView FindOrCreateChat()
        {
            var go = FindOrCreate("ChatView");
            return go != null && go.TryGetComponent(out ChatView chat) ? chat : null;
        }

        private TextMeshProUGUI FindOrCreateText(string name)
        {
            var go = FindOrCreate(name);
            return go != null && go.TryGetComponent(out TextMeshProUGUI text) ? text : null;
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

        private GameObject FindOrCreate(string name)
        {
            var existing = CachedTransform.Find(name);
            return existing != null ? existing.gameObject : null;
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
