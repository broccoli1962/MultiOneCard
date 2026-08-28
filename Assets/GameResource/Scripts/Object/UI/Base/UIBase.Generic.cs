namespace Backend.Object.UI
{
    /// <summary>
    /// 강타입 Presenter 를 가지는 UI View 베이스.
    /// Awake 시점에 Presenter 를 생성하고 AttachView 로 결합한다.
    /// </summary>
    public abstract class UIBase<TPresenter> : UIBase
        where TPresenter : UIPresenter, new()
    {
        protected TPresenter Presenter { get; private set; }

        protected override void Awake()
        {
            base.Awake();
            Presenter = new TPresenter();
            Presenter.AttachView(this);
        }

        protected override void OnOpen()
        {
            base.OnOpen();
            Presenter?.OnOpen();
        }

        protected override void OnClose()
        {
            base.OnClose();
            Presenter?.OnClose();
        }
    }
}
