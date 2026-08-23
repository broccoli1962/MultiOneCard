using System;
using UnityEngine;
using UnityEngine.UI;

namespace Backend.Object.UI
{
    /// <summary>
    /// 로비 View. 닉·퀵매치·방 만들기·룸코드 입장 입력만 담당한다.
    /// </summary>
    public sealed class LobbyPanel : UIPanel<LobbyPresenter>
    {
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
            _quickMatch2Button ??= FindOrCreateButton("Quick2");
            _quickMatch4Button ??= FindOrCreateButton("Quick4");
            _quickMatch6Button ??= FindOrCreateButton("Quick6");
            _createRoomButton ??= FindOrCreateButton("CreateRoom");
            _roomCodeInput ??= FindOrCreateInput("RoomCodeInput");
            _joinRoomButton ??= FindOrCreateButton("JoinRoom");
            _statusText ??= FindOrCreateText("Status");
            _backButton ??= FindOrCreateButton("Back");

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

        private Text FindOrCreateText(string name)
        {
            var go = FindOrCreate(name);
            return go != null && go.TryGetComponent(out Text text) ? text : null;
        }

        private InputField FindOrCreateInput(string name)
        {
            var go = FindOrCreate(name);
            return go != null && go.TryGetComponent(out InputField input) ? input : null;
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
