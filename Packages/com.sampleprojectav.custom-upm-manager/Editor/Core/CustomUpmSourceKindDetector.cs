using System;

namespace SampleProjectAV.CustomUpmManager.Editor
{
    public static class CustomUpmSourceKindDetector
    {
        public static CustomUpmSourceKind Detect(string sourceUrl)
        {
            var normalized = StripQueryAndFragment(sourceUrl).Trim();
            if (normalized.IndexOf("assetstore.unity.com", StringComparison.OrdinalIgnoreCase) >= 0)
                return CustomUpmSourceKind.AssetStoreUrl;

            if (normalized.EndsWith(".unitypackage", StringComparison.OrdinalIgnoreCase))
                return CustomUpmSourceKind.UnityPackageUrl;

            return CustomUpmSourceKind.GitUpmPackage;
        }

        private static string StripQueryAndFragment(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            var queryIndex = value.IndexOf('?');
            var fragmentIndex = value.IndexOf('#');
            var cutIndex = -1;

            if (queryIndex >= 0 && fragmentIndex >= 0)
                cutIndex = Math.Min(queryIndex, fragmentIndex);
            else if (queryIndex >= 0)
                cutIndex = queryIndex;
            else if (fragmentIndex >= 0)
                cutIndex = fragmentIndex;

            return cutIndex >= 0 ? value.Substring(0, cutIndex) : value;
        }
    }
}
