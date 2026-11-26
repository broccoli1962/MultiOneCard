using Cysharp.Threading.Tasks;

namespace Backend.Object.Management
{
    /// <summary>AppFlow가 씬 진입마다 생성하는 컨텍스트.</summary>
    public abstract class SceneContext
    {
        /// <summary>씬에 진입하기 전에 필요한 리소스를 준비한다.</summary>
        public abstract UniTask PreloadAsync();
        /// <summary>씬 활성화 후 첫 화면과 구독을 구성한다.</summary>
        public abstract UniTask EnterAsync();
        /// <summary>이전 씬이 살아 있는 동안 컨텍스트가 소유한 상태를 정리한다.</summary>
        public virtual UniTask ExitAsync() => UniTask.CompletedTask;
    }
}
