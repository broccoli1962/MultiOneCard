using Backend.Object.UI;
using Cysharp.Threading.Tasks;

namespace Backend.Object.Management
{
    public sealed class TitleContext : SceneContext
    {
        public override UniTask PreloadAsync() => UniTask.WhenAll(
            UIManager.PreloadAsync<TitlePanel>(), UIManager.PreloadAsync<SettingsPopup>());

        public override async UniTask EnterAsync() => await UIManager.OpenAsync<TitlePanel>();
    }
}
