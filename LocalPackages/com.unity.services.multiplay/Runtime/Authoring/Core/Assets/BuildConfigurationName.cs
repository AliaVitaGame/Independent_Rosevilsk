namespace Unity.Services.Multiplay.Authoring.Core.Assets
{
    /// <summary>
    /// Struct representing a type-safe Build Configuration Identifier
    /// </summary>
    public readonly struct BuildConfigurationName : IResourceName
    {
        /// <summary>
        /// The name of the Build Configuration
        /// </summary>
        public string Name { get; init; }

        /// <inheritdoc />
        public override string ToString()
        {
            return Name;
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            return obj is BuildConfigurationName name && name.Name == Name;
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return Name.GetHashCode();
        }
    }
}
