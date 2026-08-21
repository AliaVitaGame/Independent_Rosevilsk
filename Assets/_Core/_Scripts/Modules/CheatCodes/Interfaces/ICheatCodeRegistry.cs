using System.Collections.Generic;
using Game.Meta.Lifecycle;

namespace GameTest
{
    public interface ICheatCodeRegistry : IServicePreloader
    {
        IReadOnlyList<string> Categories { get; }
        IReadOnlyList<CheatCodeMeta> GetByCategory(string category);
    }
}
