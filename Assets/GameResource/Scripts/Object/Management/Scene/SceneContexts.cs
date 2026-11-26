using System;

namespace Backend.Object.Management
{
    public enum SceneId
    {
        TitleScene,
        GameScene
    }

    internal static class SceneContexts
    {
        public static SceneContext Create(SceneId id) => id switch
        {
            SceneId.TitleScene => new TitleContext(),
            SceneId.GameScene => new GameSceneContext(),
            _ => throw new ArgumentOutOfRangeException(nameof(id))
        };
    }
}
