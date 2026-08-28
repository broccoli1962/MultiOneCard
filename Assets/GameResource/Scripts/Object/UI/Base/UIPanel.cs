namespace Backend.Object.UI
{
    /// <summary>
    /// 메인 화면/메뉴 등 패널 UI 베이스 (비MVP).
    /// 기본 레이어는 Panel. HUD 처럼 사용하려면 Layer 만 override.
    /// </summary>
    public abstract class UIPanel : UIBase
    {
        public override UILayer Layer => UILayer.Panel;
    }

    /// <summary>
    /// MVP 결합형 UIPanel. 강타입 Presenter 를 자동 생성한다.
    /// </summary>
    public abstract class UIPanel<TPresenter> : UIBase<TPresenter>
        where TPresenter : UIPresenter, new()
    {
        public override UILayer Layer => UILayer.Panel;
    }
}
