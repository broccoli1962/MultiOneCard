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
        private static readonly Color _idleButton = Color.white;
        private static readonly Color _selectedButton = new Color(1f, 0.9f, 0.65f, 1f);

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

            _titleText ??= FindWidget<TextMeshProUGUI>("Title");
            _modeLabel ??= FindWidget<TextMeshProUGUI>("ModeLabel");
            _windowedButton ??= FindButton("Windowed");
            _fullscreenButton ??= FindButton("Fullscreen");
            _borderlessButton ??= FindButton("Borderless");
            _resolutionLabel ??= FindWidget<TextMeshProUGUI>("ResolutionLabel");
            _resolutionPrevButton ??= FindButton("ResolutionPrev");
            _resolutionValue ??= FindWidget<TextMeshProUGUI>("ResolutionValue");
            _resolutionNextButton ??= FindButton("ResolutionNext");
            _unsupportedText ??= FindWidget<TextMeshProUGUI>("Unsupported");
            _soundLabel ??= FindWidget<TextMeshProUGUI>("SoundLabel");
            _bgmLabel ??= FindWidget<TextMeshProUGUI>("BgmLabel");
            _bgmPrevButton ??= FindButton("BgmPrev");
            _bgmValue ??= FindWidget<TextMeshProUGUI>("BgmValue");
            _bgmNextButton ??= FindButton("BgmNext");
            _sfxLabel ??= FindWidget<TextMeshProUGUI>("SfxLabel");
            _sfxPrevButton ??= FindButton("SfxPrev");
            _sfxValue ??= FindWidget<TextMeshProUGUI>("SfxValue");
            _sfxNextButton ??= FindButton("SfxNext");
            _closeButton ??= FindButton("Close");

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

        private static void Highlight(CommonButton button, bool selected)
        {
            if (button == null || !button.TryGetComponent(out Image image))
            {
                return;
            }

            image.color = selected ? _selectedButton : _idleButton;
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
