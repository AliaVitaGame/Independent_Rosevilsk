using System.Threading;
using System.Threading.Tasks;
using Unity.Multiplayer.PlayMode.Scenarios.Editor.GraphsFoundation;

namespace Unity.Multiplayer.PlayMode.Scenarios.Editor
{
    abstract class PlayModeController
    {
        protected internal virtual void SetupExecutionGraph(ExecutionGraph graph) { }

        protected internal virtual Task<Scenario.ValidationResult> ValidateForRunningAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(new Scenario.ValidationResult(true, string.Empty));
        }
    }
}
