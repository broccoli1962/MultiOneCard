using System;
using Backend.Object.Management;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Backend.Object.UI
{
    /// <summary>
    /// 로비 View. 닉·퀵매치·방 만들기·룸코드 입장 입력만 담당한다.
    /// </summary>
    public sealed class LobbyPanel : UIPanel<LobbyPresenter>
    {
        [SerializeField] private TMP_FontAsset _font;
        [SerializeField] private TextMeshProUGUI _titleText;
        [SerializeField] private TextMeshProUGUI _statusText;
        [SerializeField] private TMP_InputField _nickInput;
        [SerializeField] private TMP_InputField _lanHostInput;
        [SerializeField] private TMP_InputField _roomCodeInput;
        [SerializeField] private CommonButton _quickMatch2Button;
        [SerializeField] private CommonButton _quickMatch4Button;
        [SerializeField] private CommonButton _quickMatch6Button;
        [SerializeField] private CommonButton _createRoomButton;
        [SerializeField] private CommonButton _joinRoomButton;
        [SerializeField] private CommonButton _backButton;
        [SerializeField] private CommonButton _settingsButton;
        [SerializeField] private CommonButton _modeRelayButton;
        [SerializeField] private CommonButton _modeLanButton;

        private TextMeshProUGUI _lanHostLabel;
        private bool _layoutReady;

        /// <summary>닉 입력 변경.</summary>
        public event Action<string> NickChanged;

        /// <summary>LAN 호스트 IP 입력 변경.</summary>
        public event Action<string> LanHostChanged;

        /// <summary>퀵매치 인원. 2/4/6.</summary>
        public event Action<int> QuickMatchClicked;

        /// <summary>방 만들기.</summary>
        public event Action CreateRoomClicked;

        /// <summary>룸코드 입장.</summary>
        public event Action JoinRoomClicked;

        /// <summary>타이틀로 돌아가기.</summary>
        public event Action BackClicked;

        /// <summary>설정 팝업.</summary>
        public event Action SettingsClicked;

        /// <summary>접속 경로.</summary>
        public event Action<ConnectionMode> ModeClicked;

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
        /// 프리팹 자식에 묶인 고정 위젯을 찾아 이벤트를 묶는다.
        /// </summary>
        public void EnsureLayout()
        {
            if (_layoutReady && _nickInput != null && _roomCodeInput != null)
            {
                return;
            }

            _titleText ??= FindOrCreateText("Title");
            _nickInput ??= FindOrCreateInput("NickInput");
            _lanHostInput ??= FindOrCreateInput("ServerUrlInput");
            _lanHostLabel ??= FindOrCreateText("ServerUrlLabel");
            _quickMatch2Button ??= FindOrCreateButton("Quick2");
            _quickMatch4Button ??= FindOrCreateButton("Quick4");
            _quickMatch6Button ??= FindOrCreateButton("Quick6");
            _createRoomButton ??= FindOrCreateButton("CreateRoom");
            _roomCodeInput ??= FindOrCreateInput("RoomCodeInput");
            _joinRoomButton ??= FindOrCreateButton("JoinRoom");
            _statusText ??= FindOrCreateText("Status");
            _backButton ??= FindOrCreateButton("Back");
            _settingsButton ??= FindOrCreateButton("Settings");
            _modeRelayButton ??= FindOrCreateButton("ModeRelay");
            _modeLanButton ??= FindOrCreateButton("ModeLan");

            BindInput(_nickInput, value => NickChanged?.Invoke(value));
            BindInputEnd(_lanHostInput, value => LanHostChanged?.Invoke(value));
            BindButton(_quickMatch2Button, () => QuickMatchClicked?.Invoke(2));
            BindButton(_quickMatch4Button, () => QuickMatchClicked?.Invoke(4));
            BindButton(_quickMatch6Button, () => QuickMatchClicked?.Invoke(6));
            BindButton(_createRoomButton, () => CreateRoomClicked?.Invoke());
            BindButton(_joinRoomButton, () => JoinRoomClicked?.Invoke());
            BindButton(_backButton, () => BackClicked?.Invoke());
            BindButton(_settingsButton, () => SettingsClicked?.Invoke());
            BindButton(_modeRelayButton, () => ModeClicked?.Invoke(ConnectionMode.Relay));
            BindButton(_modeLanButton, () => ModeClicked?.Invoke(ConnectionMode.Lan));

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
        /// 저장된 LAN 호스트 IP를 입력칸에 채운다.
        /// </summary>
        public void SetLanHost(string host)
        {
            EnsureLayout();
            if (_lanHostInput != null)
            {
                _lanHostInput.SetTextWithoutNotify(host ?? string.Empty);
            }
        }

        /// <summary>
        /// 선택된 접속 경로 버튼을 강조하고 LAN IP 입력 표시를 맞춘다.
        /// </summary>
        public void SetMode(ConnectionMode mode)
        {
            EnsureLayout();
            Highlight(_modeRelayButton, mode == ConnectionMode.Relay);
            Highlight(_modeLanButton, mode == ConnectionMode.Lan);
            SetLanHostVisible(mode == ConnectionMode.Lan);
        }

        /// <summary>
        /// 릴레이/LAN 선택 UI. WebGL 은 릴레이만 쓰므로 숨긴다.
        /// </summary>
        public void SetConnectionModeVisible(bool visible)
        {
            EnsureLayout();
            if (_modeRelayButton != null)
            {
                _modeRelayButton.gameObject.SetActive(visible);
            }

            if (_modeLanButton != null)
            {
                _modeLanButton.gameObject.SetActive(visible);
            }

            if (!visible)
            {
                SetLanHostVisible(false);
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

        private void SetLanHostVisible(bool visible)
        {
            if (_lanHostInput != null)
            {
                _lanHostInput.gameObject.SetActive(visible);
            }

            if (_lanHostLabel != null)
            {
                _lanHostLabel.gameObject.SetActive(visible);
            }
        }

        private TextMeshProUGUI FindOrCreateText(string name)
        {
            var go = FindOrCreate(name);
            return go != null && go.TryGetComponent(out TextMeshProUGUI text) ? text : null;
        }

        private TMP_InputField FindOrCreateInput(string name)
        {
            var go = FindOrCreate(name);
            return go != null && go.TryGetComponent(out TMP_InputField input) ? input : null;
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

        private static void BindInput(TMP_InputField input, UnityEngine.Events.UnityAction<string> action)
        {
            if (input == null)
            {
                return;
            }

            input.onValueChanged.RemoveAllListeners();
            input.onValueChanged.AddListener(action);
        }

        private static void Highlight(CommonButton button, bool selected)
        {
            if (button == null || !button.TryGetComponent(out Image image))
            {
                return;
            }

            image.color = selected ? new Color(1f, 0.9f, 0.65f, 1f) : Color.white;
        }

        private static void BindInputEnd(TMP_InputField input, UnityEngine.Events.UnityAction<string> action)
        {
            if (input == null)
            {
                return;
            }

            input.onEndEdit.RemoveAllListeners();
            input.onEndEdit.AddListener(action);
        }
    }
}
