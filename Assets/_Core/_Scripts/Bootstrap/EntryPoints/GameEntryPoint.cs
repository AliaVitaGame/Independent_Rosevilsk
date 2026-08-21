using System;
using System.Threading;
using GameTest;
using UnityEngine;
using VContainer.Unity;

namespace Game.Shared
{
    public class GameEntryPoint : IAsyncStartable, IDisposable
    {
        private readonly ICheatCodeRegistry _cheatCodeRegistry;
        private readonly ICheatCodesRuntimeUi _cheatCodesRuntimeUi;

        public GameEntryPoint(

            ICheatCodeRegistry cheatCodeRegistry,
            ICheatCodesRuntimeUi cheatCodesRuntimeUi
            )
        {
            _cheatCodeRegistry = cheatCodeRegistry;
            _cheatCodesRuntimeUi = cheatCodesRuntimeUi;
        }

        public async Awaitable StartAsync(CancellationToken cancellation = default)
        {
            if (Application.isEditor || Debug.isDebugBuild)
            {
                await _cheatCodeRegistry.WarmUp();
                _cheatCodesRuntimeUi.Initialize();
            }
        }

        public void Dispose()
        {

        }
    }
}
