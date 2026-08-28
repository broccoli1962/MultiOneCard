using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Backend.Object.UI
{
    /// <summary>
    /// 로비 View. 닉·방 만들기·방 목록·공개/비공개·룸코드 입장 입력을 담당한다.
    /// </summary>
    public sealed class LobbyPanel : UIPanel<LobbyPresenter>
    {
        [SerializeField] private TMP_FontAsset _font;
        [SerializeField] private TextMeshProUGUI _titleText;
        [SerializeField] private TextMeshProUGUI _statusText;
        [SerializeField] private TMP_InputField _nickInput;
        [SerializeField] private TMP_InputField _roomCodeInput;
        [SerializeField] private CommonButton _createRoomButton;
        [SerializeField] private CommonButton _roomListButton;
        [SerializeField] private CommonButton _joinRoomButton;
        [SerializeField] private CommonButton _backButton;
        [SerializeField] private CommonButton _settingsButton;
        [SerializeField] private CommonButton _publicButton;
        [SerializeField] private CommonButton _privateButton;

        private bool _layoutReady;

        /// <summary>닉 입력 변경.</summary>
        public event Action<string> NickChanged;

        /// <summary>방 만들기.</summary>
        public event Action CreateRoomClicked;

        /// <summary>공개 방 목록.</summary>
        public event Action RoomListClicked;

        /// <summary>룸코드 입장.</summary>
        public event Action JoinRoomClicked;

        /// <summary>타이틀로 돌아가기.</summary>
        public event Action BackClicked;

        /// <summary>설정 팝업.</summary>
        public event Action SettingsClicked;

        /// <summary>공개/비공개. true 면 비공개.</summary>
        public event Action<bool> VisibilityClicked;

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
            _createRoomButton ??= FindOrCreateButton("CreateRoom");
            _roomListButton ??= FindOrCreateButton("RoomList");
            _roomCodeInput ??= FindOrCreateInput("RoomCodeInput");
            _joinRoomButton ??= FindOrCreateButton("JoinRoom");
            _statusText ??= FindOrCreateText("Status");
            _backButton ??= FindOrCreateButton("Back");
            _settingsButton ??= FindOrCreateButton("Settings");
            _publicButton ??= FindOrCreateButton("Public");
            _privateButton ??= FindOrCreateButton("Private");

            BindInput(_nickInput, value => NickChanged?.Invoke(value));
            BindButton(_createRoomButton, () => CreateRoomClicked?.Invoke());
            BindButton(_roomListButton, () => RoomListClicked?.Invoke());
            BindButton(_joinRoomButton, () => JoinRoomClicked?.Invoke());
            BindButton(_backButton, () => BackClicked?.Invoke());
            BindButton(_settingsButton, () => SettingsClicked?.Invoke());
            BindButton(_publicButton, () => VisibilityClicked?.Invoke(false));
            BindButton(_privateButton, () => VisibilityClicked?.Invoke(true));

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
        /// 공개/비공개 버튼을 강조한다.
        /// </summary>
        public void SetVisibility(bool isPrivate)
        {
            EnsureLayout();
            Highlight(_publicButton, !isPrivate);
            Highlight(_privateButton, isPrivate);
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
    }
}
