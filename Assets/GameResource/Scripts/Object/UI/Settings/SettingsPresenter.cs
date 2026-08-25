using System;
using Backend.App;
using Backend.Object.Management;
using UnityEngine;

namespace Backend.Object.UI
{
    /// <summary>
    /// 설정 입력. 해상도·화면 형태는 <see cref="DisplaySettings"/>, 사운드는 <see cref="AudioManager"/> 가 적용한다.
    /// </summary>
    public sealed class SettingsPresenter : UIPresenter<SettingsPopup>
    {
        private DisplaySize[] _sizes = Array.Empty<DisplaySize>();
        private int _sizeIndex;

        /// <summary>
        /// 현재 화면 값을 채우고 입력을 구독한다.
        /// </summary>
        public override void OnOpen()
        {
            View.EnsureLayout();
            BindView();
            RefreshFromScreen();
        }

        /// <summary>
        /// 입력 구독을 해제한다.
        /// </summary>
        public override void OnClose()
        {
            UnbindView();
        }

        private void BindView()
        {
            View.ModeClicked += OnModeClicked;
            View.ResolutionStepClicked += OnResolutionStepClicked;
            View.BgmStepClicked += OnBgmStepClicked;
            View.SfxStepClicked += OnSfxStepClicked;
            View.CloseClicked += OnCloseClicked;
        }

        private void UnbindView()
        {
            if (View == null)
            {
                return;
            }

            View.ModeClicked -= OnModeClicked;
            View.ResolutionStepClicked -= OnResolutionStepClicked;
            View.BgmStepClicked -= OnBgmStepClicked;
            View.SfxStepClicked -= OnSfxStepClicked;
            View.CloseClicked -= OnCloseClicked;
        }

        private void RefreshFromScreen()
        {
            if (!DisplaySettings.IsSupported)
            {
                View.SetDisplaySupported(false);
                PaintSound();
                return;
            }

            View.SetDisplaySupported(true);
            _sizes = DisplaySettings.ListSizes();
            _sizeIndex = DisplaySizeUtil.IndexOf(
                _sizes,
                _sizes.Length,
                DisplaySettings.CurrentWidth,
                DisplaySettings.CurrentHeight);
            if (_sizeIndex < 0)
            {
                _sizeIndex = 0;
            }

            Paint();
            PaintSound();
        }

        private void Paint()
        {
            View.SetMode(DisplaySettings.CurrentMode);
            if (_sizes.Length == 0)
            {
                View.SetResolutionLabel(DisplaySettings.CurrentWidth + " x " + DisplaySettings.CurrentHeight);
                return;
            }

            View.SetResolutionLabel(_sizes[_sizeIndex].Label);
        }

        private void PaintSound()
        {
            View.SetBgmVolumeLabel(VolumeLabel(AudioManager.GetBgmVolume()));
            View.SetSfxVolumeLabel(VolumeLabel(AudioManager.GetSfxVolume()));
        }

        private void OnBgmStepClicked(int delta)
        {
            AudioManager.SetBgmVolume(StepVolume(AudioManager.GetBgmVolume(), delta));
            PaintSound();
        }

        private void OnSfxStepClicked(int delta)
        {
            AudioManager.SetSfxVolume(StepVolume(AudioManager.GetSfxVolume(), delta));
            PaintSound();
            AudioManager.PlaySfx("Card_Flip");
        }

        private void OnModeClicked(DisplayWindowMode mode)
        {
            Apply(mode, CurrentSize());
        }

        private void OnResolutionStepClicked(int delta)
        {
            if (_sizes.Length == 0)
            {
                return;
            }

            _sizeIndex = DisplaySizeUtil.WrapStep(_sizeIndex, _sizes.Length, delta);
            Apply(DisplaySettings.CurrentMode, _sizes[_sizeIndex]);
        }

        private void Apply(DisplayWindowMode mode, DisplaySize size)
        {
            DisplaySettings.Apply(mode, size.Width, size.Height);
            Paint();
        }

        private DisplaySize CurrentSize()
        {
            if (_sizes.Length > 0 && _sizeIndex >= 0 && _sizeIndex < _sizes.Length)
            {
                return _sizes[_sizeIndex];
            }

            return new DisplaySize(DisplaySettings.CurrentWidth, DisplaySettings.CurrentHeight);
        }

        private static float StepVolume(float current, int delta)
        {
            return Mathf.Clamp01(Mathf.Round((current + delta * 0.1f) * 10f) / 10f);
        }

        private static string VolumeLabel(float linear)
        {
            return Mathf.RoundToInt(Mathf.Clamp01(linear) * 100f) + "%";
        }

        private void OnCloseClicked()
        {
            UIManager.Close(View);
        }
    }
}
