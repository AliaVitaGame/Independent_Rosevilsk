namespace Unity.Multiplayer.PlayMode.Scenarios.Editor.Api
{
    /// <summary>
    /// This enum defines the sequential stages involved in the execution workflow.
    /// Instances use these stages to process groups of nodes, while Scenarios leverage
    /// them to orchestrate and coordinate stage execution across multiple Instances.
    /// </summary>
    internal enum ExecutionStage
    {
        None,
        Prepare,
        Deploy,
        Run
    }
}
