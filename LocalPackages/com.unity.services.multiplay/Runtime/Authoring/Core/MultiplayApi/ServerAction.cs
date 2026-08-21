namespace Unity.Services.Multiplay.Authoring.Core.MultiplayApi
{
    /// <summary>
    /// Action to trigger for the serve
    /// </summary>
    public enum ServerAction
    {
        /// <summary>
        /// Add server
        /// </summary>
        Add = 1,
        /// <summary>
        /// Delete Server
        /// </summary>
        Delete = 2,
        /// <summary>
        /// Restart Server
        /// </summary>
        Restart = 3,
        /// <summary>
        /// Shutdown server
        /// </summary>
        Shutdown = 4,
        /// <summary>
        /// Start server
        /// </summary>
        Start = 5,
        /// <summary>
        /// Stop server
        /// </summary>
        Stop = 6
    }
}
