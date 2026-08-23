using System;
using System.Threading;
using GameTest;
using Modules.CharacterCreator;
using Modules.SaveSystem;
using Modules.SaveSystem.Runtime;
using UnityEngine;
using VContainer.Unity;

namespace Game.Shared
{
    public class GameEntryPoint : IAsyncStartable, IDisposable
    {
        private readonly ICheatCodeRegistry _cheatCodeRegistry;
        private readonly ICheatCodesRuntimeUi _cheatCodesRuntimeUi;
        private readonly IGameSaveService _saveService;

        private GameSaveRuntime _saveRuntime;

        public GameEntryPoint(
            ICheatCodeRegistry cheatCodeRegistry,
            ICheatCodesRuntimeUi cheatCodesRuntimeUi,
            IGameSaveService saveService)
        {
            _cheatCodeRegistry = cheatCodeRegistry;
            _cheatCodesRuntimeUi = cheatCodesRuntimeUi;
            _saveService = saveService;
        }

        public async Awaitable StartAsync(CancellationToken cancellation = default)
        {
            if (Application.isEditor || Debug.isDebugBuild)
            {
                await _cheatCodeRegistry.WarmUp();
                _cheatCodesRuntimeUi.Initialize();
            }

            EnsureSaveRuntime();
            await RestoreSaveWhenReady(cancellation);
        }

        public void Dispose()
        {
            if (_saveRuntime != null)
            {
                UnityEngine.Object.Destroy(_saveRuntime.gameObject);
                _saveRuntime = null;
            }
        }

        private void EnsureSaveRuntime()
        {
            if (_saveService == null || _saveService.ActiveSlotIndex < 0)
                return;

            var existing = UnityEngine.Object.FindFirstObjectByType<GameSaveRuntime>();
            if (existing != null)
            {
                _saveRuntime = existing;
                _saveRuntime.Initialize(_saveService);
                return;
            }

            var go = new GameObject("GameSaveRuntime");
            UnityEngine.Object.DontDestroyOnLoad(go);
            _saveRuntime = go.AddComponent<GameSaveRuntime>();
            _saveRuntime.Initialize(_saveService);
        }

        private async Awaitable RestoreSaveWhenReady(CancellationToken cancellation)
        {
            if (_saveService == null)
                return;

            const int maxFrames = 300;
            for (var i = 0; i < maxFrames; i++)
            {
                cancellation.ThrowIfCancellationRequested();

                if (HasLocalPlayer())
                {
                    _saveService.ApplyPendingLoadToWorld();
                    // One more frame so Model/Animator are fully ready, then re-apply appearance.
                    await Awaitable.NextFrameAsync(cancellation);
                    ReapplyAppearance();
                    return;
                }

                await Awaitable.NextFrameAsync(cancellation);
            }

            _saveService.ApplyPendingLoadToWorld();
            ReapplyAppearance();
        }

        private void ReapplyAppearance()
        {
            if (_saveService == null)
                return;

            var saveAppearance = _saveService.GetActiveAppearance();
            if (saveAppearance == null || !saveAppearance.hasValue)
                return;

            var appearance = CharacterAppearanceCodec.FromSaveData(saveAppearance);
            if (appearance == null)
            {
                Debug.LogWarning("[Appearance] Saved appearance could not be decoded.");
                return;
            }

            PlayerAppearanceApplier.ApplyToLocalPlayer(appearance);
        }

        private static bool HasLocalPlayer()
        {
            var players = UnityEngine.Object.FindObjectsByType<HEAVYART.TopDownShooter.Netcode.PlayerBehaviour>(
                FindObjectsInactive.Exclude);
            foreach (var player in players)
            {
                if (player != null && player.IsOwner)
                    return true;
            }

            return players != null && players.Length > 0;
        }
    }
}
