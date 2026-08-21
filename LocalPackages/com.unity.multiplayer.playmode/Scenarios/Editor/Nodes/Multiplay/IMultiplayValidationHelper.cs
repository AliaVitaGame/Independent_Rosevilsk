// TODO: MTT-8667 This should be moved to a proper assembly and remove these scripting defines.
#if MULTIPLAY_API_AVAILABLE && UNITY_EDITOR
using System.Threading;
using System.Threading.Tasks;
using Unity.Multiplayer.PlayMode.Scenarios.Editor.GraphsFoundation;

namespace Unity.Multiplayer.PlayMode.Scenarios.Editor.Nodes.Multiplay
{
    internal interface IMultiplayValidationHelper
    {
        Task<MultiplayValidationHelper.ValidateRemoteResult> ValidateRemoteAsync(CancellationToken cancellationToken);

        bool IsProjectLinkedToUnityCloud();

        Task<Scenario.ValidationResult> ValidateMultiplayProviderStatusAsync(
            MultiplayValidationHelper.ValidateRemoteResult validateRemoteResult, CancellationToken cancellationToken);
    }
}
#endif
