using Backend.Object.UI;
using Cysharp.Threading.Tasks;

namespace Backend.Object.Management
{
    /// <summary>
    /// 타이틀 씬 진입점. TitlePanel 을 연다.
    /// </summary>
    public sealed class TitleContext : SceneContext
    {
        /// <summary>
        /// 코어 준비 후 타이틀 패널을 연다.
        /// </summary>
        protected override UniTask OnEnterAsync()
        {
            UIManager.CloseAllUI();
            UIManager.OpenAsync<TitlePanel>().Forget();
            return UniTask.CompletedTask;
        }
    }
}
