namespace Unity.Services.Multiplay.Authoring.Core.Assets
{
    /// <summary>
    /// Struct representing a generic named resource
    /// </summary>
    public interface IResourceName
    {
        /// <summary>
        /// The resource name
        /// </summary>
        string Name { get; init; }
    }
}
