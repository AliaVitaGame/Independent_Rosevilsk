using System;
using System.Collections.Generic;
using System.Linq;

namespace SampleProjectAV.CustomUpmManager.Editor
{
    public static class VersionSorter
    {
        public static IReadOnlyList<CustomUpmVersion> SortDescending(IEnumerable<CustomUpmVersion> versions)
        {
            return (versions ?? Array.Empty<CustomUpmVersion>())
                .Where(version => version != null)
                .OrderByDescending(version => TryParseSemanticVersion(version.Name, out _))
                .ThenByDescending(version => ParseOrDefault(version.Name))
                .ThenByDescending(version => NamedRefPriority(version.Name))
                .ThenByDescending(version => version.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static SemanticVersion ParseOrDefault(string value)
        {
            return TryParseSemanticVersion(value, out var version) ? version : SemanticVersion.Zero;
        }

        private static int NamedRefPriority(string value)
        {
            if (string.Equals(value, "main", StringComparison.OrdinalIgnoreCase))
                return 3;

            if (string.Equals(value, "master", StringComparison.OrdinalIgnoreCase))
                return 2;

            if (string.Equals(value, "develop", StringComparison.OrdinalIgnoreCase))
                return 1;

            return 0;
        }

        private static bool TryParseSemanticVersion(string value, out SemanticVersion version)
        {
            version = SemanticVersion.Zero;

            if (string.IsNullOrWhiteSpace(value))
                return false;

            var trimmed = value.Trim();
            if (trimmed.StartsWith("v", StringComparison.OrdinalIgnoreCase))
                trimmed = trimmed.Substring(1);

            var suffixIndex = trimmed.IndexOfAny(new[] { '-', '+' });
            if (suffixIndex >= 0)
                trimmed = trimmed.Substring(0, suffixIndex);

            var parts = trimmed.Split('.');
            if (parts.Length == 0 || parts.Length > 4)
                return false;

            var numbers = new int[4];
            for (var i = 0; i < parts.Length; i++)
            {
                if (!int.TryParse(parts[i], out numbers[i]))
                    return false;
            }

            version = new SemanticVersion(numbers[0], numbers[1], numbers[2], numbers[3]);
            return true;
        }

        private readonly struct SemanticVersion : IComparable<SemanticVersion>
        {
            public static readonly SemanticVersion Zero = new SemanticVersion(0, 0, 0, 0);

            private readonly int major;
            private readonly int minor;
            private readonly int patch;
            private readonly int build;

            public SemanticVersion(int major, int minor, int patch, int build)
            {
                this.major = major;
                this.minor = minor;
                this.patch = patch;
                this.build = build;
            }

            public int CompareTo(SemanticVersion other)
            {
                var result = major.CompareTo(other.major);
                if (result != 0)
                    return result;

                result = minor.CompareTo(other.minor);
                if (result != 0)
                    return result;

                result = patch.CompareTo(other.patch);
                if (result != 0)
                    return result;

                return build.CompareTo(other.build);
            }
        }
    }
}
