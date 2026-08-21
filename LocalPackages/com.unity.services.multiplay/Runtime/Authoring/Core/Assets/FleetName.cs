namespace Unity.Services.Multiplay.Authoring.Core.Assets
{
    /// <summary>
    /// Struct representing a type-safe Fleet Identifier
    /// </summary>
    public readonly struct FleetName : IResourceName
    {
        /// <summary>
        /// Creates a new instance of FleetName
        /// </summary>
        /// <param name="name">The name of the Fleet</param>
        public FleetName(string name) { Name = name; }

        /// <summary>
        /// The name of the Fleet
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
            return obj is FleetName name && name.Name == Name;
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return Name.GetHashCode();
        }
    }
}
