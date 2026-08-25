using System;
using Backend.App;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Backend.Object.UI
{
    /// <summary>
    /// 설정 팝업 View. 화면 형태·해상도·사운드 입력과 표시만 담당한다.
    /// </summary>
    public sealed class SettingsPopup : UIPopup<SettingsPresenter>
    {
        private static readonly Color IdleButton = Color.white;
        private static readonly Color SelectedButton = new Color(1f, 0.9f, 0.65f, 1f);

        [SerializeField] private TMP_FontAsset _font;
        [SerializeField] private TextMeshProUGUI _titleText;
        [SerializeField] private TextMeshProUGUI _modeLabel;
        [SerializeField] private CommonButton _windowedButton;
        [SerializeField] private CommonButton _fullscreenButton;
        [SerializeField] private CommonButton _borderlessButton;
        [SerializeField] private TextMeshProUGUI _resolutionLabel;
        [SerializeField] private CommonButton _resolutionPrevButton;
        [SerializeField] private TextMeshProUGUI _resolutionValue;
        [SerializeField] private CommonButton _resolutionNextButton;
        [SerializeField] private TextMeshProUGUI _unsupportedText;
        [SerializeField] private TextMeshProUGUI _soundLabel;
        [SerializeField] private TextMeshProUGUI _bgmLabel;
        [SerializeField] private CommonButton _bgmPrevButton;
        [SerializeField] private TextMeshProUGUI _bgmValue;
        [SerializeField] private CommonButton _bgmNextButton;
        [SerializeField] private TextMeshProUGUI _sfxLabel;
        [SerializeField] private CommonButton _sfxPrevButton;
        [SerializeField] private TextMeshProUGUI _sfxValue;
        [SerializeField] private CommonButton _sfxNextButton;
        [SerializeField] private CommonButton _closeButton;

        private bool _layoutReady;

        /// <summary>화면 형태 선택.</summary>
        public event Action<DisplayWindowMode> ModeClicked;

        /// <summary>해상도 목록 한 칸. -1 이전, +1 다음.</summary>
        public event Action<int> ResolutionStepClicked;

        /// <summary>BGM 볼륨 한 칸. -1 줄임, +1 올림.</summary>
        public event Action<int> BgmStepClicked;

        /// <summary>SFX 볼륨 한 칸. -1 줄임, +1 올림.</summary>
        public event Action<int> SfxStepClicked;

        /// <summary>닫기.</summary>
        public event Action CloseClicked;

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
            if (_layoutReady && _closeButton != null)
            {
                return;
            }

            _titleText ??= FindOrCreateText("Title");
            _modeLabel ??= FindOrCreateText("ModeLabel");
            _windowedButton ??= FindOrCreateButton("Windowed");
            _fullscreenButton ??= FindOrCreateButton("Fullscreen");
            _borderlessButton ??= FindOrCreateButton("Borderless");
            _resolutionLabel ??= FindOrCreateText("ResolutionLabel");
            _resolutionPrevButton ??= FindOrCreateButton("ResolutionPrev");
            _resolutionValue ??= FindOrCreateText("ResolutionValue");
            _resolutionNextButton ??= FindOrCreateButton("ResolutionNext");
            _unsupportedText ??= FindOrCreateText("Unsupported");
            _soundLabel ??= FindOrCreateText("SoundLabel");
            _bgmLabel ??= FindOrCreateText("BgmLabel");
            _bgmPrevButton ??= FindOrCreateButton("BgmPrev");
            _bgmValue ??= FindOrCreateText("BgmValue");
            _bgmNextButton ??= FindOrCreateButton("BgmNext");
            _sfxLabel ??= FindOrCreateText("SfxLabel");
            _sfxPrevButton ??= FindOrCreateButton("SfxPrev");
            _sfxValue ??= FindOrCreateText("SfxValue");
            _sfxNextButton ??= FindOrCreateButton("SfxNext");
            _closeButton ??= FindOrCreateButton("Close");

            BindButton(_windowedButton, () => ModeClicked?.Invoke(DisplayWindowMode.Windowed));
            BindButton(_fullscreenButton, () => ModeClicked?.Invoke(DisplayWindowMode.Fullscreen));
            BindButton(_borderlessButton, () => ModeClicked?.Invoke(DisplayWindowMode.Borderless));
            BindButton(_resolutionPrevButton, () => ResolutionStepClicked?.Invoke(-1));
            BindButton(_resolutionNextButton, () => ResolutionStepClicked?.Invoke(1));
            BindButton(_bgmPrevButton, () => BgmStepClicked?.Invoke(-1));
            BindButton(_bgmNextButton, () => BgmStepClicked?.Invoke(1));
            BindButton(_sfxPrevButton, () => SfxStepClicked?.Invoke(-1));
            BindButton(_sfxNextButton, () => SfxStepClicked?.Invoke(1));
            BindButton(_closeButton, () => CloseClicked?.Invoke());

            _layoutReady = true;
        }

        /// <summary>
        /// 지원 여부에 따라 화면 설정 위젯을 켜고 끈다.
        /// </summary>
        public void SetDisplaySupported(bool supported)
        {
            EnsureLayout();
            SetActive(_modeLabel, supported);
            SetActive(_windowedButton, supported);
            SetActive(_fullscreenButton, supported);
            SetActive(_borderlessButton, supported);
            SetActive(_resolutionLabel, supported);
            SetActive(_resolutionPrevButton, supported);
            SetActive(_resolutionValue, supported);
            SetActive(_resolutionNextButton, supported);
            SetActive(_unsupportedText, !supported);
        }

        /// <summary>
        /// 선택된 화면 형태 버튼을 강조한다.
        /// </summary>
        public void SetMode(DisplayWindowMode mode)
        {
            EnsureLayout();
            Highlight(_windowedButton, mode == DisplayWindowMode.Windowed);
            Highlight(_fullscreenButton, mode == DisplayWindowMode.Fullscreen);
            Highlight(_borderlessButton, mode == DisplayWindowMode.Borderless);
        }

        /// <summary>
        /// 현재 해상도 문구를 표시한다.
        /// </summary>
        public void SetResolutionLabel(string label)
        {
            EnsureLayout();
            if (_resolutionValue != null)
            {
                _resolutionValue.text = label ?? string.Empty;
            }
        }

        /// <summary>
        /// BGM 볼륨 문구를 표시한다.
        /// </summary>
        public void SetBgmVolumeLabel(string label)
        {
            EnsureLayout();
            if (_bgmValue != null)
            {
                _bgmValue.text = label ?? string.Empty;
            }
        }

        /// <summary>
        /// SFX 볼륨 문구를 표시한다.
        /// </summary>
        public void SetSfxVolumeLabel(string label)
        {
            EnsureLayout();
            if (_sfxValue != null)
            {
                _sfxValue.text = label ?? string.Empty;
            }
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

        private static void Highlight(CommonButton button, bool selected)
        {
            if (button == null || !button.TryGetComponent(out Image image))
            {
                return;
            }

            image.color = selected ? SelectedButton : IdleButton;
        }

        private static void SetActive(Component component, bool active)
        {
            if (component != null)
            {
                component.gameObject.SetActive(active);
            }
        }
    }
}
