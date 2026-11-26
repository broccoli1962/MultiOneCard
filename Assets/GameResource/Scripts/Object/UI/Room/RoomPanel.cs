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
        private const int MAX_SLOTS = 6;

        [SerializeField] private TMP_FontAsset _font;
        [SerializeField] private TextMeshProUGUI _titleText;
        [SerializeField] private TextMeshProUGUI _roomCodeText;
        [SerializeField] private CommonButton _copyCodeButton;
        [SerializeField] private TextMeshProUGUI _statusText;
        [SerializeField] private TextMeshProUGUI _rulesText;
        [SerializeField] private GameObject _rulesRoot;
        [SerializeField] private RulesView _rulesView;
        [SerializeField] private ChatView _chatView;
        [SerializeField] private CommonButton _readyButton;
        [SerializeField] private CommonButton _startButton;
        [SerializeField] private CommonButton _rulesButton;
        [SerializeField] private CommonButton _chatButton;
        [SerializeField] private CommonButton _backButton;
        [SerializeField] private CommonButton[] _slotButtons = new CommonButton[MAX_SLOTS];
        [SerializeField] private TextMeshProUGUI[] _slotTexts = new TextMeshProUGUI[MAX_SLOTS];
        [SerializeField] private TextMeshProUGUI[] _seatRoles = new TextMeshProUGUI[MAX_SLOTS];
        [SerializeField] private TextMeshProUGUI[] _readyStates = new TextMeshProUGUI[MAX_SLOTS];

        private bool _layoutReady;

        /// <summary>내 좌석 준비.</summary>
        public event Action ReadyClicked;

        /// <summary>방장 시작.</summary>
        public event Action StartClicked;

        /// <summary>규칙 보기 토글.</summary>
        public event Action RulesClicked;

        /// <summary>채팅 패널 토글.</summary>
        public event Action ChatClicked;

        /// <summary>방 코드 복사.</summary>
        public event Action CopyCodeClicked;

        /// <summary>로비로 돌아가기.</summary>
        public event Action BackClicked;

        /// <summary>슬롯 탭. 로컬 루프백에서 해당 좌석 Ready.</summary>
        public event Action<int> SlotClicked;

        /// <summary>채팅 서브뷰.</summary>
        public ChatView Chat => _chatView;

        /// <summary>규칙 오버레이.</summary>
        public RulesView Rules => _rulesView;

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

            _titleText ??= FindWidget<TextMeshProUGUI>("Title");
            _roomCodeText ??= FindWidget<TextMeshProUGUI>("RoomCode");
            _copyCodeButton ??= FindButton("CopyCode");
            EnsureSlots();
            _readyButton ??= FindButton("Ready");
            _startButton ??= FindButton("Start");
            _rulesButton ??= FindButton("Rules");
            _chatButton ??= FindButton("Chat");
            _statusText ??= FindWidget<TextMeshProUGUI>("Status");
            _backButton ??= FindButton("Back");
            _chatView ??= FindChat();
            _rulesRoot ??= FindChild("RulesRoot");
            if (_rulesRoot != null)
            {
                _rulesView ??= _rulesRoot.GetComponent<RulesView>();
            }

            if (_rulesText == null && _rulesRoot != null)
            {
                var body = _rulesRoot.transform.Find("RulesBody")
                    ?? _rulesRoot.transform.Find("RulesScroll/Viewport/Content/RulesBody");
                if (body != null)
                {
                    body.TryGetComponent(out _rulesText);
                }
            }

            if (_rulesView != null)
            {
                _rulesView.EnsureLayout();
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
            BindButton(_copyCodeButton, () => CopyCodeClicked?.Invoke());
            BindButton(_rulesButton, () => RulesClicked?.Invoke());
            BindButton(_chatButton, () => ChatClicked?.Invoke());
            BindButton(_backButton, () => BackClicked?.Invoke());
            for (var i = 0; i < MAX_SLOTS; i++)
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

            if (_copyCodeButton != null)
            {
                _copyCodeButton.CachedGameObject.SetActive(code != "------");
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

            if (seatCount > MAX_SLOTS)
            {
                seatCount = MAX_SLOTS;
            }

            for (var i = 0; i < MAX_SLOTS; i++)
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

                var nick = room?.nicks != null && i < room.nicks.Length ? room.nicks[i] : null;
                var occupied = !string.IsNullOrEmpty(nick);
                var ready = room != null && room.ready != null && i < room.ready.Length && room.ready[i];
                var host = occupied && i == room.hostSeat;
                slotText.text = occupied ? nick : "빈 자리";
                _seatRoles[i].text = $"좌석 {i + 1}{(host ? " · 방장" : string.Empty)}{(occupied && i == localSeat ? " · 나" : string.Empty)}";
                _readyStates[i].text = occupied ? (ready ? "준비 완료" : "준비 대기") : string.Empty;
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
            if (_rulesView != null)
            {
                if (visible)
                {
                    _rulesView.Show();
                }
                else
                {
                    _rulesView.Hide();
                }

                return;
            }

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
            if (_slotButtons == null || _slotButtons.Length != MAX_SLOTS)
            {
                _slotButtons = new CommonButton[MAX_SLOTS];
            }

            if (_slotTexts == null || _slotTexts.Length != MAX_SLOTS)
            {
                _slotTexts = new TextMeshProUGUI[MAX_SLOTS];
            }

            for (var i = 0; i < MAX_SLOTS; i++)
            {
                _slotButtons[i] ??= FindButton("Slot" + i);
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

        private ChatView FindChat()
        {
            var go = FindChild("ChatView");
            return go != null && go.TryGetComponent(out ChatView chat) ? chat : null;
        }

    }
}
