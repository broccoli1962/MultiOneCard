namespace Backend.Object.UI
{
    /// <summary>
    /// 모달 팝업 UI 베이스 (비MVP). 기본 레이어는 Popup, 뒤로가기 키 처리는 기본 ON.
    /// </summary>
    public abstract class UIPopup : UIBase
    {
        public override UILayer Layer => UILayer.Popup;
        protected override bool DefaultHandleBackButton => true;
    }

    /// <summary>
    /// MVP 결합형 UIPopup. 강타입 Presenter 를 자동 생성한다.
    /// </summary>
    public abstract class UIPopup<TPresenter> : UIBase<TPresenter>
        where TPresenter : UIPresenter, new()
    {
        public override UILayer Layer => UILayer.Popup;
        protected override bool DefaultHandleBackButton => true;
    }
}
