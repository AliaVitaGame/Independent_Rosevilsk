using System;
using System.Collections.Generic;
using System.Linq;

namespace SampleProjectAV.CustomUpmManager.Editor
{
    public static class GoogleSheetModuleRegistryParser
    {
        public static CustomUpmRegistry Parse(string csv)
        {
            var registry = new CustomUpmRegistry();
            var rows = ParseCsv(csv);

            if (rows.Count == 0 || IsEmptyRow(rows[0]))
                return registry;

            var headers = BuildHeaderMap(rows[0]);
            if (!HasRequiredHeaders(headers))
                throw new InvalidOperationException("Google Sheet CSV is missing the Source URL column.");

            for (var i = 1; i < rows.Count; i++)
            {
                var row = rows[i];
                if (IsEmptyRow(row) || !IsEnabled(Get(row, headers, "enabled")))
                    continue;

                var sourceUrl = Get(row, headers, "sourceurl");
                if (string.IsNullOrWhiteSpace(sourceUrl))
                    continue;

                var sourceKind = ParseSourceKind(Get(row, headers, "sourcekind"), sourceUrl);
                var module = CustomUpmModuleFactory.Create(
                    Get(row, headers, "displayname"),
                    sourceUrl,
                    sourceKind,
                    Get(row, headers, "packagename"),
                    Get(row, headers, "docsurl"),
                    Get(row, headers, "defaultversion"),
                    Get(row, headers, "unitypackagepath"),
                    Get(row, headers, "category"));

                var id = Get(row, headers, "id");
                if (!string.IsNullOrWhiteSpace(id))
                    module.Id = id.Trim();

                registry.Modules.Add(module);
            }

            return registry;
        }

        public static bool HasRequiredHeaders(string csv)
        {
            var rows = ParseCsv(csv);
            return rows.Count > 0 && HasRequiredHeaders(BuildHeaderMap(rows[0]));
        }

        private static bool HasRequiredHeaders(IReadOnlyDictionary<string, int> headers)
        {
            return headers != null && headers.ContainsKey("sourceurl");
        }

        private static Dictionary<string, int> BuildHeaderMap(IReadOnlyList<string> headers)
        {
            var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < headers.Count; i++)
            {
                var key = NormalizeHeader(headers[i]);
                if (!string.IsNullOrEmpty(key) && !map.ContainsKey(key))
                    map.Add(key, i);
            }

            return map;
        }

        private static string NormalizeHeader(string header)
        {
            return new string((header ?? string.Empty)
                .Where(char.IsLetterOrDigit)
                .Select(char.ToLowerInvariant)
                .ToArray());
        }

        private static string Get(IReadOnlyList<string> row, IReadOnlyDictionary<string, int> headers, string key)
        {
            if (!headers.TryGetValue(key, out var index) || index < 0 || index >= row.Count)
                return string.Empty;

            return row[index]?.Trim() ?? string.Empty;
        }

        private static bool IsEnabled(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return true;

            return !string.Equals(value, "false", StringComparison.OrdinalIgnoreCase)
                   && !string.Equals(value, "no", StringComparison.OrdinalIgnoreCase)
                   && !string.Equals(value, "0", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsEmptyRow(IEnumerable<string> row)
        {
            return row == null || row.All(string.IsNullOrWhiteSpace);
        }

        private static CustomUpmSourceKind ParseSourceKind(string value, string sourceUrl)
        {
            if (int.TryParse(value, out var numeric) && Enum.IsDefined(typeof(CustomUpmSourceKind), numeric))
                return (CustomUpmSourceKind)numeric;

            if (Enum.TryParse(value, true, out CustomUpmSourceKind sourceKind))
                return sourceKind;

            return CustomUpmSourceKindDetector.Detect(sourceUrl);
        }

        private static List<List<string>> ParseCsv(string csv)
        {
            var rows = new List<List<string>>();
            var row = new List<string>();
            var cell = string.Empty;
            var inQuotes = false;

            for (var i = 0; i < (csv ?? string.Empty).Length; i++)
            {
                var current = csv[i];
                if (current == '"')
                {
                    if (inQuotes && i + 1 < csv.Length && csv[i + 1] == '"')
                    {
                        cell += '"';
                        i++;
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }
                }
                else if (current == ',' && !inQuotes)
                {
                    row.Add(cell);
                    cell = string.Empty;
                }
                else if ((current == '\n' || current == '\r') && !inQuotes)
                {
                    if (current == '\r' && i + 1 < csv.Length && csv[i + 1] == '\n')
                        i++;

                    row.Add(cell);
                    rows.Add(row);
                    row = new List<string>();
                    cell = string.Empty;
                }
                else
                {
                    cell += current;
                }
            }

            row.Add(cell);
            if (!IsEmptyRow(row))
                rows.Add(row);

            return rows;
        }
    }
}
