using System;

namespace SampleProjectAV.CustomUpmManager.Editor
{
    public static class GitUrlUtility
    {
        public static string ComposePackageIdentifier(string sourceUrl, string revision)
        {
            if (string.IsNullOrWhiteSpace(revision))
                return sourceUrl;

            var baseUrl = RemoveFragment(sourceUrl);
            return $"{baseUrl}#{revision}";
        }

        public static string ExtractRepositoryUrl(string sourceUrl)
        {
            if (string.IsNullOrWhiteSpace(sourceUrl))
                return string.Empty;

            var withoutFragment = RemoveFragment(sourceUrl);
            var queryIndex = withoutFragment.IndexOf('?', StringComparison.Ordinal);
            return queryIndex >= 0 ? withoutFragment.Substring(0, queryIndex) : withoutFragment;
        }

        public static string ExtractPackagePath(string sourceUrl)
        {
            if (string.IsNullOrWhiteSpace(sourceUrl))
                return string.Empty;

            var withoutFragment = RemoveFragment(sourceUrl);
            var queryIndex = withoutFragment.IndexOf('?', StringComparison.Ordinal);
            if (queryIndex < 0 || queryIndex == withoutFragment.Length - 1)
                return string.Empty;

            var query = withoutFragment.Substring(queryIndex + 1);
            foreach (var parameter in query.Split('&'))
            {
                var separatorIndex = parameter.IndexOf('=', StringComparison.Ordinal);
                if (separatorIndex <= 0)
                    continue;

                var key = parameter.Substring(0, separatorIndex);
                if (!string.Equals(key, "path", StringComparison.OrdinalIgnoreCase))
                    continue;

                var value = parameter.Substring(separatorIndex + 1);
                var path = Uri.UnescapeDataString(value).Replace('\\', '/').Trim();
                return path.TrimStart('/');
            }

            return string.Empty;
        }

        private static string RemoveFragment(string sourceUrl)
        {
            if (string.IsNullOrWhiteSpace(sourceUrl))
                return string.Empty;

            var fragmentIndex = sourceUrl.IndexOf('#', StringComparison.Ordinal);
            return fragmentIndex >= 0 ? sourceUrl.Substring(0, fragmentIndex) : sourceUrl;
        }
    }
}
