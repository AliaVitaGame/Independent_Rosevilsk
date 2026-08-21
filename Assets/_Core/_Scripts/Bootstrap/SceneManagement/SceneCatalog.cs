using System;

namespace Game.Bootstrap.SceneManagement
{
    public sealed class SceneCatalog : ISceneCatalog
    {
        public string GetSceneName(SceneId sceneId)
        {
            return sceneId switch
            {
                SceneId.Bootstrap => "Bootstrap",
                SceneId.Menu => "MainMenu",
                SceneId.Game => "Game",
                _ => throw new ArgumentOutOfRangeException(nameof(sceneId), sceneId, null)
            };
        }
    }
}
