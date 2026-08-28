using Backend.Object.Management;

namespace Backend.Object.UI
{
    /// <summary>
    /// 특수 카드 목록. 닫기만 구독한다.
    /// </summary>
    public sealed class CardListPresenter : UIPresenter<CardListPanel>
    {
        /// <summary>
        /// 닫기를 구독한다.
        /// </summary>
        public override void OnOpen()
        {
            View.EnsureLayout();
            View.CloseClicked += OnCloseClicked;
        }

        /// <summary>
        /// 닫기 구독을 해제한다.
        /// </summary>
        public override void OnClose()
        {
            if (View == null)
            {
                return;
            }

            View.CloseClicked -= OnCloseClicked;
        }

        private void OnCloseClicked()
        {
            UIManager.Close(View);
        }
    }
}
