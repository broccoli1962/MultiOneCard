using Backend.Object.Management;
using Cysharp.Threading.Tasks;

namespace Backend.Object.UI
{
    /// <summary>
    /// 타이틀에서 로비 패널을 연다. 매칭·판정은 하지 않는다.
    /// </summary>
    public sealed class TitlePresenter : UIPresenter<TitlePanel>
    {
        /// <summary>
        /// 시작 입력을 구독한다.
        /// </summary>
        public override void OnOpen()
        {
            View.EnsureLayout();
            View.SetWebHostWarning(WebBuild.IsPlayer);
            View.StartClicked += OnStartClicked;
            View.SettingsClicked += OnSettingsClicked;
        }

        /// <summary>
        /// 시작 입력을 해제한다.
        /// </summary>
        public override void OnClose()
        {
            if (View == null)
            {
                return;
            }

            View.StartClicked -= OnStartClicked;
            View.SettingsClicked -= OnSettingsClicked;
        }

        private void OnStartClicked()
        {
            UIManager.OpenAsync<LobbyPanel>().Forget();
        }

        private void OnSettingsClicked()
        {
            UIManager.OpenAsync<SettingsPopup>().Forget();
        }
    }
}
