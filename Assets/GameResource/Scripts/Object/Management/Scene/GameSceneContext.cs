using Backend.Object.UI;
using Cysharp.Threading.Tasks;

namespace Backend.Object.Management
{
    internal sealed class GameSceneContext : SceneContext
    {
        public override UniTask PreloadAsync() => UIManager.PreloadAsync<LobbyPanel>();
        public override async UniTask EnterAsync() => await UIManager.OpenAsync<LobbyPanel>();
    }
}
