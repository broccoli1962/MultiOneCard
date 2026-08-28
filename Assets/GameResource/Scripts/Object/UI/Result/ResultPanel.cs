using System;
using Backend.Net;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Backend.Object.UI
{
    /// <summary>
    /// 결과 화면 View. 순위·장수·점수와 재대결 투표 입력만 담당한다.
    /// </summary>
    public sealed class ResultPanel : UIPanel<ResultPresenter>
    {
        [SerializeField] private TMP_FontAsset _font;
        [SerializeField] private TextMeshProUGUI _titleText;
        [SerializeField] private TextMeshProUGUI _rankText;
        [SerializeField] private TextMeshProUGUI _timerText;
        [SerializeField] private TextMeshProUGUI _statusText;
        [SerializeField] private CommonButton _yesButton;
        [SerializeField] private CommonButton _noButton;

        private bool _layoutReady;

        /// <summary>재대결 찬성.</summary>
        public event Action YesClicked;

        /// <summary>재대결 반대. 미투표 만료는 Presenter Tick 이 반대로 보낸다.</summary>
        public event Action NoClicked;

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
            if (_layoutReady && _yesButton != null && _noButton != null)
            {
                return;
            }

            _titleText ??= FindOrCreateText("Title");
            _rankText ??= FindOrCreateText("Ranks");
            _timerText ??= FindOrCreateText("Timer");
            _statusText ??= FindOrCreateText("Status");
            _yesButton ??= FindOrCreateButton("Yes");
            _noButton ??= FindOrCreateButton("No");

            BindButton(_yesButton, () => YesClicked?.Invoke());
            BindButton(_noButton, () => NoClicked?.Invoke());

            _layoutReady = true;
        }

        /// <summary>
        /// 호스트가 보낸 재대결 투표 현황을 Presenter 에 넘긴다.
        /// </summary>
        public void ApplyRoom(RoomView room)
        {
            Presenter?.ApplyRoom(room);
        }

        /// <summary>
        /// 순위표와 재대결 남은 초·투표 현황을 그린다.
        /// </summary>
        public void Render(string ranks, int remainSeconds, bool voted, string status)
        {
            EnsureLayout();
            if (_rankText != null)
            {
                _rankText.text = ranks ?? string.Empty;
            }

            if (_timerText != null)
            {
                _timerText.text = remainSeconds > 0
                    ? $"재대결 {remainSeconds}초"
                    : "재대결 마감";
            }

            if (_statusText != null)
            {
                _statusText.text = status ?? string.Empty;
            }

            if (_yesButton != null)
            {
                _yesButton.interactable = !voted;
            }

            if (_noButton != null)
            {
                _noButton.interactable = !voted;
            }
        }

        /// <summary>
        /// 뒤로가기 시 반대로 처리한다.
        /// </summary>
        public override bool OnBackPressed()
        {
            NoClicked?.Invoke();
            return false;
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
