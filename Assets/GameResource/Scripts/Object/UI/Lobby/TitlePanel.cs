using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Backend.Object.UI
{
    /// <summary>
    /// 타이틀 화면 View. 표시와 시작 입력만 담당한다.
    /// </summary>
    public sealed class TitlePanel : UIPanel<TitlePresenter>
    {
        [SerializeField] private TMP_FontAsset _font;
        [SerializeField] private TextMeshProUGUI _titleText;
        [SerializeField] private TextMeshProUGUI _webWarningText;
        [SerializeField] private CommonButton _startButton;
        [SerializeField] private CommonButton _settingsButton;

        private bool _layoutReady;

        /// <summary>로비로 진입하는 시작 버튼.</summary>
        public event Action StartClicked;

        /// <summary>설정 팝업.</summary>
        public event Action SettingsClicked;

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
            if (_layoutReady && _startButton != null)
            {
                return;
            }

            _titleText ??= FindOrCreateText("Title");
            _webWarningText ??= FindOrCreateText("WebWarning");
            _startButton ??= FindOrCreateButton("Start");
            _settingsButton ??= FindOrCreateButton("Settings");
            BindButton(_startButton, () => StartClicked?.Invoke());
            BindButton(_settingsButton, () => SettingsClicked?.Invoke());
            _layoutReady = true;
        }

        /// <summary>
        /// WebGL 방장 탭 경고. 네이티브에서는 숨긴다.
        /// </summary>
        public void SetWebHostWarning(bool visible)
        {
            EnsureLayout();
            if (_webWarningText == null)
            {
                return;
            }

            _webWarningText.gameObject.SetActive(visible);
            if (!visible)
            {
                return;
            }

            _webWarningText.raycastTarget = false;
            _webWarningText.fontSize = 28;
            _webWarningText.color = new Color(1f, 0.85f, 0.4f, 1f);
            _webWarningText.alignment = TextAlignmentOptions.Center;
            _webWarningText.textWrappingMode = TextWrappingModes.Normal;
            _webWarningText.text =
                "방을 만든 탭이 서버입니다. 다른 탭으로 바꾸거나 창을 최소화하면 게임이 끊길 수 있습니다.";
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
