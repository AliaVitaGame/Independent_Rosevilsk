using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace SampleProjectAV.CustomUpmManager.Editor
{
    public sealed class UnityPackageImportTracker : AssetPostprocessor, IUnityPackageImportTracker
    {
        private static PendingImport pendingImport;

        public Task<UnityPackageImportResult> ImportPackageAsync(string packagePath, CustomUpmModule module, CustomUpmVersion version)
        {
            if (string.IsNullOrWhiteSpace(packagePath))
                throw new ArgumentException("Unitypackage path is required.", nameof(packagePath));

            if (module == null)
                throw new ArgumentNullException(nameof(module));

            if (version == null)
                throw new ArgumentNullException(nameof(version));

            if (pendingImport != null)
                throw new InvalidOperationException("Another unitypackage import is already in progress.");

            var existingAssets = new HashSet<string>(
                AssetDatabase.GetAllAssetPaths().Where(IsProjectAssetPath),
                StringComparer.OrdinalIgnoreCase);
            var record = new PendingImportRecord(module, version, existingAssets);
            SavePendingRecord(record);
            pendingImport = new PendingImport(record, GetOpenEditorWindowIds());
            AssetDatabase.importPackageCompleted += OnImportPackageCompleted;
            AssetDatabase.importPackageCancelled += OnImportPackageCancelled;
            AssetDatabase.importPackageFailed += OnImportPackageFailed;
            EditorApplication.update += CheckImportWindow;

            try
            {
                AssetDatabase.ImportPackage(packagePath, true);
                return pendingImport.Completion.Task;
            }
            catch
            {
                CompleteImport(import =>
                {
                    DeletePendingRecord();
                    import.Completion.TrySetException(new InvalidOperationException("Unitypackage import could not be started."));
                });
                throw;
            }
        }

        public void DeleteTrackedAssets(IEnumerable<string> assetPaths)
        {
            var paths = (assetPaths ?? Array.Empty<string>())
                .Where(IsProjectAssetPath)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderByDescending(path => path.Length)
                .ToArray();

            foreach (var path in paths)
            {
                if (!AssetDatabase.DeleteAsset(path))
                    Debug.LogWarning($"Custom UPM could not delete tracked imported asset: {path}");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        public static IReadOnlyList<string> FilterNewImportedPaths(IEnumerable<string> importedPaths, ISet<string> existingPaths)
        {
            return (importedPaths ?? Array.Empty<string>())
                .Where(IsProjectAssetPath)
                .Where(path => existingPaths == null || !existingPaths.Contains(path))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static void OnImportPackageCompleted(string packageName)
        {
            CompleteImportSuccessfully();
        }

        private static void OnImportPackageCancelled(string packageName)
        {
            CompleteImport(import =>
            {
                DeletePendingRecord();
                import.Completion.TrySetCanceled();
            });
        }

        private static void OnImportPackageFailed(string packageName, string errorMessage)
        {
            CompleteImport(import =>
            {
                DeletePendingRecord();
                import.Completion.TrySetException(new InvalidOperationException($"Unitypackage import failed: {errorMessage}"));
            });
        }

        private static void CheckImportWindow()
        {
            var import = pendingImport;
            if (import == null)
                return;

            if (IsImportPackageWindowOpen(import))
            {
                import.HasSeenImportWindow = true;
                return;
            }

            if (import.HasSeenImportWindow)
                CompleteImportSuccessfully();
        }

        private static void CompleteImportSuccessfully()
        {
            CompleteImport(import =>
            {
                AssetDatabase.Refresh();
                var record = LoadPendingRecord() ?? import.Record;
                var currentAssets = AssetDatabase.GetAllAssetPaths().Where(IsProjectAssetPath);
                var newPaths = record.ImportedAssetPaths.Count > 0
                    ? record.ImportedAssetPaths
                    : FilterNewImportedPaths(
                        currentAssets,
                        new HashSet<string>(record.ExistingAssetPaths, StringComparer.OrdinalIgnoreCase));
                newPaths = RelocateImportedAssets(record, newPaths);
                PersistInstallState(record, newPaths);
                DeletePendingRecord();
                import.Completion.TrySetResult(new UnityPackageImportResult(newPaths));
            });
        }

        private static void CompleteImport(Action<PendingImport> complete)
        {
            var import = pendingImport;
            if (import == null)
                return;

            pendingImport = null;
            AssetDatabase.importPackageCompleted -= OnImportPackageCompleted;
            AssetDatabase.importPackageCancelled -= OnImportPackageCancelled;
            AssetDatabase.importPackageFailed -= OnImportPackageFailed;
            EditorApplication.update -= CheckImportWindow;
            complete(import);
        }

        private static bool IsImportPackageWindowOpen(PendingImport import)
        {
            return Resources.FindObjectsOfTypeAll<EditorWindow>()
                .Any(window =>
                {
                    var typeName = window.GetType().FullName;
                    return typeName.IndexOf("ImportPackage", StringComparison.OrdinalIgnoreCase) >= 0
                           || typeName.IndexOf("PackageImport", StringComparison.OrdinalIgnoreCase) >= 0
                           || !import.InitialEditorWindowIds.Contains(window.GetEntityId());
                });
        }

        private static HashSet<EntityId> GetOpenEditorWindowIds()
        {
            return new HashSet<EntityId>(
                Resources.FindObjectsOfTypeAll<EditorWindow>().Select(window => window.GetEntityId()));
        }

        private static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            var record = LoadPendingRecord();
            if (record == null || importedAssets == null)
                return;

            var changed = false;
            foreach (var path in importedAssets)
            {
                if (!IsProjectAssetPath(path) || record.ExistingAssetPaths.Contains(path) || record.ImportedAssetPaths.Contains(path))
                    continue;

                record.ImportedAssetPaths.Add(path);
                changed = true;
            }

            if (changed)
                SavePendingRecord(record);
        }

        [InitializeOnLoadMethod]
        private static void RecoverImportAfterDomainReload()
        {
            EditorApplication.delayCall += FinalizeRecoveredImport;
        }

        private static void FinalizeRecoveredImport()
        {
            if (pendingImport != null || EditorApplication.isCompiling || EditorApplication.isUpdating)
                return;

            var record = LoadPendingRecord();
            if (record == null || record.ImportedAssetPaths.Count == 0)
                return;

            var relocatedPaths = RelocateImportedAssets(record, record.ImportedAssetPaths);
            PersistInstallState(record, relocatedPaths);
            DeletePendingRecord();
        }

        private static void PersistInstallState(PendingImportRecord record, IEnumerable<string> importedAssetPaths)
        {
            var stateStore = new JsonInstallStateStore(CustomUpmPaths.InstallStatePath);
            var database = stateStore.Load();
            var state = new CustomUpmInstallState
            {
                ModuleId = record.ModuleId,
                InstalledVersion = record.VersionName,
                InstalledRevision = record.VersionRevision,
                SourceKind = record.SourceKind
            };
            state.SetImportedAssetPaths(importedAssetPaths);
            database.Upsert(state);
            stateStore.Save(database);
        }

        private static PendingImportRecord LoadPendingRecord()
        {
            if (!File.Exists(CustomUpmPaths.PendingImportPath))
                return null;

            var json = File.ReadAllText(CustomUpmPaths.PendingImportPath);
            return string.IsNullOrWhiteSpace(json) ? null : JsonUtility.FromJson<PendingImportRecord>(json);
        }

        private static void SavePendingRecord(PendingImportRecord record)
        {
            Directory.CreateDirectory(CustomUpmPaths.CacheRoot);
            File.WriteAllText(CustomUpmPaths.PendingImportPath, JsonUtility.ToJson(record));
        }

        private static void DeletePendingRecord()
        {
            if (File.Exists(CustomUpmPaths.PendingImportPath))
                File.Delete(CustomUpmPaths.PendingImportPath);
        }

        private static bool IsProjectAssetPath(string path)
        {
            return !string.IsNullOrWhiteSpace(path)
                && path.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase);
        }

        private static IReadOnlyList<string> RelocateImportedAssets(PendingImportRecord record, IEnumerable<string> importedPaths)
        {
            var paths = (importedPaths ?? Array.Empty<string>())
                .Where(IsProjectAssetPath)
                .Select(CustomUpmCategoryImportPaths.NormalizeAssetPath)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (record == null ||
                !CustomUpmCategoryImportPaths.TryGetImportFolder(record.Category, out var destinationFolder))
                return paths;

            EnsureAssetFolder(destinationFolder);

            foreach (var move in CustomUpmCategoryImportPaths.BuildMoves(paths, destinationFolder))
            {
                EnsureAssetFolder(CustomUpmCategoryImportPaths.GetParentPath(move.To));
                var error = AssetDatabase.MoveAsset(move.From, move.To);
                if (!string.IsNullOrEmpty(error))
                    Debug.LogWarning($"Custom UPM could not move imported asset '{move.From}' to '{move.To}': {error}");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            return ResolveRelocatedPaths(paths, destinationFolder);
        }

        private static IReadOnlyList<string> ResolveRelocatedPaths(IEnumerable<string> originalPaths, string destinationFolder)
        {
            var resolved = new List<string>();
            foreach (var original in originalPaths ?? Array.Empty<string>())
            {
                var remapped = CustomUpmCategoryImportPaths.RemapAssetPath(original, destinationFolder);
                if (AssetExists(remapped))
                    resolved.Add(remapped);
                else if (AssetExists(original))
                    resolved.Add(original);
            }

            return resolved
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static void EnsureAssetFolder(string folderPath)
        {
            var normalized = CustomUpmCategoryImportPaths.NormalizeAssetPath(folderPath);
            if (string.IsNullOrEmpty(normalized) || AssetDatabase.IsValidFolder(normalized))
                return;

            var parts = normalized.Split('/');
            if (parts.Length == 0 || !string.Equals(parts[0], "Assets", StringComparison.OrdinalIgnoreCase))
                return;

            var current = parts[0];
            for (var i = 1; i < parts.Length; i++)
            {
                var next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);

                current = next;
            }
        }

        private static bool AssetExists(string path)
        {
            return !string.IsNullOrWhiteSpace(path)
                   && (AssetDatabase.IsValidFolder(path) || AssetDatabase.LoadMainAssetAtPath(path) != null);
        }

        private sealed class PendingImport
        {
            public PendingImport(PendingImportRecord record, ISet<EntityId> initialEditorWindowIds)
            {
                Record = record;
                InitialEditorWindowIds = initialEditorWindowIds;
                Completion = new TaskCompletionSource<UnityPackageImportResult>();
            }

            public PendingImportRecord Record { get; }
            public ISet<EntityId> InitialEditorWindowIds { get; }
            public TaskCompletionSource<UnityPackageImportResult> Completion { get; }
            public bool HasSeenImportWindow { get; set; }
        }

        [Serializable]
        private sealed class PendingImportRecord
        {
            public PendingImportRecord(CustomUpmModule module, CustomUpmVersion version, IEnumerable<string> existingAssetPaths)
            {
                ModuleId = module.Id;
                VersionName = version.Name;
                VersionRevision = version.Revision;
                SourceKind = module.SourceKind;
                Category = module.Category;
                ExistingAssetPaths = new List<string>(existingAssetPaths);
                ImportedAssetPaths = new List<string>();
            }

            public string ModuleId;
            public string VersionName;
            public string VersionRevision;
            public CustomUpmSourceKind SourceKind;
            public string Category;
            public List<string> ExistingAssetPaths;
            public List<string> ImportedAssetPaths;
        }
    }
}
