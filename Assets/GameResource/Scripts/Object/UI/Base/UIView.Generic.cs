namespace Backend.Object.UI
{
    /// <summary>
    /// 강타입 Presenter 를 가지는 UIView 베이스.
    /// Awake 시점에 Presenter 를 생성하고 AttachView 로 결합하며,
    /// Show/Hide 라이프사이클을 Presenter 의 OnOpen/OnClose 에 매핑한다.
    /// </summary>
    public abstract class UIView<TPresenter> : UIView
        where TPresenter : UIPresenter, new()
    {
        protected TPresenter Presenter { get; private set; }

        protected override void Awake()
        {
            base.Awake();
            Presenter = new TPresenter();
            Presenter.AttachView(this);
        }

        protected override void OnShow()
        {
            base.OnShow();
            Presenter?.OnOpen();
        }

        protected override void OnHide()
        {
            base.OnHide();
            Presenter?.OnClose();
        }
    }
}
