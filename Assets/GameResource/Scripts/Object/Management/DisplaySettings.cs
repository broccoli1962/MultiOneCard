using Backend.App;
using UnityEngine;

namespace Backend.Object.Management
{
    /// <summary>
    /// 해상도·화면 형태를 PlayerPrefs에 저장하고 <see cref="Screen"/> 에 적용한다.
    /// 모바일 등 미지원 플랫폼에서는 적용하지 않는다.
    /// </summary>
    public static class DisplaySettings
    {
        private const string PREF_MODE = "DisplaySettings_Mode";
        private const string PREF_WIDTH = "DisplaySettings_Width";
        private const string PREF_HEIGHT = "DisplaySettings_Height";

        /// <summary>창 모드·해상도를 바꿀 수 있는 플랫폼인지.</summary>
        public static bool IsSupported
        {
            get
            {
                switch (Application.platform)
                {
                    case RuntimePlatform.WindowsPlayer:
                    case RuntimePlatform.WindowsEditor:
                    case RuntimePlatform.OSXPlayer:
                    case RuntimePlatform.OSXEditor:
                    case RuntimePlatform.LinuxPlayer:
                    case RuntimePlatform.LinuxEditor:
                        return true;
                    default:
                        return false;
                }
            }
        }

        /// <summary>현재 Unity 화면 형태를 설정 enum으로 읽는다.</summary>
        public static DisplayWindowMode CurrentMode => FromUnity(Screen.fullScreenMode);

        /// <summary>현재 가로(px).</summary>
        public static int CurrentWidth => Screen.width;

        /// <summary>현재 세로(px).</summary>
        public static int CurrentHeight => Screen.height;

        /// <summary>
        /// 모니터가 제공하는 해상도 + 현재 창 크기를 유일 목록으로 반환한다.
        /// </summary>
        public static DisplaySize[] ListSizes()
        {
            var resolutions = Screen.resolutions;
            var sourceCount = resolutions != null ? resolutions.Length : 0;
            var widths = new int[sourceCount + 1];
            var heights = new int[sourceCount + 1];

            for (var i = 0; i < sourceCount; i++)
            {
                widths[i] = resolutions[i].width;
                heights[i] = resolutions[i].height;
            }

            widths[sourceCount] = Screen.width;
            heights[sourceCount] = Screen.height;

            var dest = new DisplaySize[widths.Length];
            var count = DisplaySizeUtil.CollectUnique(widths, heights, dest);
            if (count == dest.Length)
            {
                return dest;
            }

            var trimmed = new DisplaySize[count];
            for (var i = 0; i < count; i++)
            {
                trimmed[i] = dest[i];
            }

            return trimmed;
        }

        /// <summary>
        /// 화면 형태와 해상도를 적용하고 저장한다. 미지원 플랫폼은 무시한다.
        /// </summary>
        public static void Apply(DisplayWindowMode mode, int width, int height)
        {
            if (!IsSupported)
            {
                return;
            }

            if (width < 1 || height < 1)
            {
                Debug.LogWarning($"[DisplaySettings] Invalid size {width}x{height}");
                return;
            }

            ApplyInternal(mode, width, height);
            Save(mode, width, height);
        }

        /// <summary>
        /// 저장된 값이 있으면 부트 시 적용한다. 첫 실행은 Unity 기본값을 유지한다.
        /// </summary>
        public static void ApplySaved()
        {
            if (!IsSupported)
            {
                return;
            }

            if (!PlayerPrefs.HasKey(PREF_WIDTH) || !PlayerPrefs.HasKey(PREF_HEIGHT))
            {
                return;
            }

            var mode = (DisplayWindowMode)PlayerPrefs.GetInt(PREF_MODE, (int)DisplayWindowMode.Windowed);
            if (mode != DisplayWindowMode.Windowed
                && mode != DisplayWindowMode.Fullscreen
                && mode != DisplayWindowMode.Borderless)
            {
                mode = DisplayWindowMode.Windowed;
            }

            var width = PlayerPrefs.GetInt(PREF_WIDTH);
            var height = PlayerPrefs.GetInt(PREF_HEIGHT);
            if (width < 1 || height < 1)
            {
                return;
            }

            ApplyInternal(mode, width, height);
        }

        private static void ApplyInternal(DisplayWindowMode mode, int width, int height)
        {
            var unityMode = ToUnity(mode);
            var refresh = Screen.currentResolution.refreshRateRatio;
            if (refresh.numerator == 0 || refresh.denominator == 0)
            {
                Screen.SetResolution(width, height, unityMode);
                return;
            }

            Screen.SetResolution(width, height, unityMode, refresh);
        }

        private static void Save(DisplayWindowMode mode, int width, int height)
        {
            PlayerPrefs.SetInt(PREF_MODE, (int)mode);
            PlayerPrefs.SetInt(PREF_WIDTH, width);
            PlayerPrefs.SetInt(PREF_HEIGHT, height);
            PlayerPrefs.Save();
        }

        private static FullScreenMode ToUnity(DisplayWindowMode mode)
        {
            switch (mode)
            {
                case DisplayWindowMode.Fullscreen:
                    return FullScreenMode.ExclusiveFullScreen;
                case DisplayWindowMode.Borderless:
                    return FullScreenMode.FullScreenWindow;
                default:
                    return FullScreenMode.Windowed;
            }
        }

        private static DisplayWindowMode FromUnity(FullScreenMode mode)
        {
            switch (mode)
            {
                case FullScreenMode.ExclusiveFullScreen:
                    return DisplayWindowMode.Fullscreen;
                case FullScreenMode.FullScreenWindow:
                    return DisplayWindowMode.Borderless;
                default:
                    return DisplayWindowMode.Windowed;
            }
        }
    }
}
