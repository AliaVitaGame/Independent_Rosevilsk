namespace Unity.Services.Multiplay.Authoring.Core.Assets
{
    /// <summary>
    /// Struct representing a type-safe Build Identifier
    /// </summary>
    public readonly struct BuildName : IResourceName
    {
        /// <summary>
        /// The name of the Build
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
            return obj is BuildName name && name.Name == Name;
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return Name.GetHashCode();
        }

        /// <summary>
        /// Verifies whether both BuildNames represent the same identity
        /// </summary>
        /// <param name="left">The left operand</param>
        /// <param name="right">The right operand</param>
        /// <returns>Returns true if equal</returns>
        public static bool operator==(BuildName left, BuildName right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Verifies whether both BuildNames represent the different identities
        /// </summary>
        /// <param name="left">The left operand</param>
        /// <param name="right">The right operand</param>
        /// <returns>Returns true if not equal</returns>
        public static bool operator!=(BuildName left, BuildName right)
        {
            return !(left == right);
        }
    }
}
