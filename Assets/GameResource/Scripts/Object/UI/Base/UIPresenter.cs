using Backend.Util;

namespace Backend.Object.UI
{
    /// <summary>
    /// MVP 패턴의 Presenter 비제네릭 베이스. View 와의 결합을 담당.
    /// 정적 데이터는 TableManager 를 통해 조회한다.
    /// View 타입은 UIBase / UIView 공통 조상인 CachedMonobehaviour 로 일반화한다.
    /// </summary>
    public abstract class UIPresenter
    {
        protected CachedMonobehaviour BaseView { get; private set; }

        internal void AttachView(CachedMonobehaviour view)
        {
            BaseView = view;
            OnAttached();
        }

        protected virtual void OnAttached() { }

        public virtual void OnOpen() { }
        public virtual void OnClose() { }
    }

    /// <summary>
    /// 강타입 View 참조를 가지는 Presenter 베이스.
    /// </summary>
    public abstract class UIPresenter<TView> : UIPresenter where TView : CachedMonobehaviour
    {
        protected TView View => BaseView as TView;
    }
}
