using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace SampleProjectAV.CustomUpmManager.Editor
{
    public sealed class AddModuleWindow : EditorWindow
    {
        private IGitClient gitClient;
        private IModuleMetadataProvider metadataProvider;
        private Action<IReadOnlyList<CustomUpmModule>> onModulesAdded;

        private string displayName;
        private string sourceUrl;
        private string packageName;
        private string documentationUrl;
        private string defaultVersion = "1.0.0";
        private string unityPackagePath;
        private string scanRevision;
        private CustomUpmSourceKind sourceKind = CustomUpmSourceKind.GitUpmPackage;
        private Vector2 scanScroll;
        private bool scanning;
        private bool metadataLoading;
        private bool displayNameEditedManually;
        private bool documentationUrlEditedManually;
        private string statusMessage;
        private string lastMetadataKey;
        private readonly List<string> scannedUnityPackages = new List<string>();
        private readonly HashSet<string> selectedUnityPackages = new HashSet<string>();

        public static void Open(
            IGitClient gitClient,
            IModuleMetadataProvider metadataProvider,
            Action<IReadOnlyList<CustomUpmModule>> onModulesAdded)
        {
            var window = CreateInstance<AddModuleWindow>();
            window.gitClient = gitClient;
            window.metadataProvider = metadataProvider;
            window.onModulesAdded = onModulesAdded;
            window.titleContent = new GUIContent("Add Custom Module");
            window.minSize = new Vector2(520f, 440f);
            window.ShowUtility();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Add Module", EditorStyles.boldLabel);
            EditorGUILayout.Space(4f);

            DrawNameField();
            DrawSourceUrlField();
            DrawDocumentationUrlField();

            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginChangeCheck();
            sourceKind = (CustomUpmSourceKind)EditorGUILayout.EnumPopup("Source Kind", sourceKind);
            if (EditorGUI.EndChangeCheck())
                FetchMetadataIfPossible(false);

            if (GUILayout.Button("Auto", GUILayout.Width(60f)))
            {
                sourceKind = CustomUpmSourceKindDetector.Detect(sourceUrl);
                FetchMetadataIfPossible(false);
            }

            EditorGUILayout.EndHorizontal();

            if (sourceKind == CustomUpmSourceKind.GitRepositoryUnityPackages)
                DrawGitUnityPackagesControls();

            if (!string.IsNullOrWhiteSpace(statusMessage))
                EditorGUILayout.HelpBox(statusMessage, GetStatusMessageType());

            GUILayout.FlexibleSpace();

            using (new EditorGUI.DisabledScope(scanning || metadataLoading || string.IsNullOrWhiteSpace(sourceUrl)))
            {
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Cancel"))
                    Close();

                if (GUILayout.Button("Add"))
                    AddModulesAndCloseAsync().Forget();

                EditorGUILayout.EndHorizontal();
            }
        }

        private void DrawNameField()
        {
            EditorGUILayout.BeginHorizontal();

            EditorGUI.BeginChangeCheck();
            displayName = EditorGUILayout.TextField("Name", displayName);
            if (EditorGUI.EndChangeCheck())
                displayNameEditedManually = true;

            using (new EditorGUI.DisabledScope(metadataLoading || string.IsNullOrWhiteSpace(sourceUrl)))
            {
                if (GUILayout.Button(metadataLoading ? "..." : "Fetch", GUILayout.Width(60f)))
                    FetchMetadataIfPossible(true);
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawSourceUrlField()
        {
            var previousSourceUrl = sourceUrl;

            EditorGUI.BeginChangeCheck();
            sourceUrl = EditorGUILayout.DelayedTextField("Source URL", sourceUrl);
            if (!EditorGUI.EndChangeCheck())
                return;

            SyncDocumentationUrlWithSource(previousSourceUrl);
            sourceKind = CustomUpmSourceKindDetector.Detect(sourceUrl);
            FetchMetadataIfPossible(false);
        }

        private void DrawDocumentationUrlField()
        {
            if (string.IsNullOrWhiteSpace(documentationUrl) && !string.IsNullOrWhiteSpace(sourceUrl))
                documentationUrl = sourceUrl;

            EditorGUI.BeginChangeCheck();
            documentationUrl = EditorGUILayout.TextField("Docs URL", documentationUrl);
            if (!EditorGUI.EndChangeCheck())
                return;

            documentationUrlEditedManually = !string.IsNullOrWhiteSpace(documentationUrl)
                && !string.Equals(documentationUrl, sourceUrl, StringComparison.Ordinal);
        }

        private void SyncDocumentationUrlWithSource(string previousSourceUrl)
        {
            if (documentationUrlEditedManually && !string.IsNullOrWhiteSpace(documentationUrl))
                return;

            if (string.IsNullOrWhiteSpace(documentationUrl)
                || string.Equals(documentationUrl, previousSourceUrl, StringComparison.Ordinal))
            {
                documentationUrl = sourceUrl;
                documentationUrlEditedManually = false;
            }
        }

        private MessageType GetStatusMessageType()
        {
            if (scanning || metadataLoading)
                return MessageType.Info;

            return statusMessage.IndexOf("failed", StringComparison.OrdinalIgnoreCase) >= 0
                || statusMessage.IndexOf("not found", StringComparison.OrdinalIgnoreCase) >= 0
                    ? MessageType.Warning
                    : MessageType.Info;
        }

        private void DrawGitUnityPackagesControls()
        {
            scanRevision = EditorGUILayout.TextField("Scan Revision", scanRevision);
            unityPackagePath = EditorGUILayout.TextField("Package Path", unityPackagePath);

            if (GUILayout.Button("Scan Repository For .unitypackage Files"))
                ScanRepository().Forget();

            if (scannedUnityPackages.Count == 0)
                return;

            EditorGUILayout.LabelField("Found Packages", EditorStyles.boldLabel);
            scanScroll = EditorGUILayout.BeginScrollView(scanScroll, GUILayout.MinHeight(140f));

            foreach (var packageFile in scannedUnityPackages)
            {
                var selected = selectedUnityPackages.Contains(packageFile);
                var newSelected = EditorGUILayout.ToggleLeft(packageFile, selected);

                if (newSelected)
                    selectedUnityPackages.Add(packageFile);
                else
                    selectedUnityPackages.Remove(packageFile);
            }

            EditorGUILayout.EndScrollView();
        }

        private async System.Threading.Tasks.Task ScanRepository()
        {
            if (scanning)
                return;

            scanning = true;
            statusMessage = "Scanning repository...";
            Repaint();

            try
            {
                CustomUpmPaths.EnsureCacheDirectories();
                var repositoryPath = await gitClient.PrepareRepositoryAsync(sourceUrl, scanRevision, CustomUpmPaths.GitCacheRoot, CancellationToken.None);
                var files = await gitClient.FindUnityPackageFilesAsync(repositoryPath, CancellationToken.None);

                scannedUnityPackages.Clear();
                scannedUnityPackages.AddRange(files);
                selectedUnityPackages.Clear();

                foreach (var file in scannedUnityPackages)
                    selectedUnityPackages.Add(file);

                statusMessage = scannedUnityPackages.Count == 0
                    ? "No .unitypackage files were found."
                    : $"Found {scannedUnityPackages.Count} .unitypackage file(s).";
            }
            catch (Exception exception)
            {
                statusMessage = $"Scan failed: {exception.Message}";
                Debug.LogException(exception);
            }
            finally
            {
                scanning = false;
                Repaint();
            }
        }

        private void FetchMetadataIfPossible(bool forceApplyName)
        {
            FetchMetadataAsync(forceApplyName).Forget();
        }

        private async Task FetchMetadataAsync(bool forceApplyName)
        {
            if (metadataProvider == null || metadataLoading || string.IsNullOrWhiteSpace(sourceUrl))
                return;

            var requestKey = BuildMetadataKey();
            if (!forceApplyName && string.Equals(requestKey, lastMetadataKey, StringComparison.Ordinal))
                return;

            metadataLoading = true;
            statusMessage = "Loading package metadata...";
            Repaint();

            try
            {
                var requestSourceKind = sourceKind;
                var requestSourceUrl = sourceUrl;
                var requestUnityPackagePath = unityPackagePath;

                var metadata = await metadataProvider.GetMetadataAsync(
                    requestSourceKind,
                    requestSourceUrl,
                    requestUnityPackagePath,
                    CancellationToken.None);
                metadata = metadata ?? PackageJsonMetadata.Empty;

                if (!IsCurrentMetadataRequest(requestSourceKind, requestSourceUrl, requestUnityPackagePath))
                    return;

                ApplyMetadata(metadata, forceApplyName);
                lastMetadataKey = requestKey;
                statusMessage = metadata.HasAnyValue
                    ? "Metadata loaded."
                    : "Metadata not found; using fallback name.";
            }
            catch (Exception exception)
            {
                statusMessage = $"Metadata failed: {exception.Message}";
                Debug.LogException(exception);
            }
            finally
            {
                metadataLoading = false;
                Repaint();
            }
        }

        private void ApplyMetadata(PackageJsonMetadata metadata, bool forceApplyName)
        {
            if (metadata == null)
                return;

            if (!string.IsNullOrWhiteSpace(metadata.DisplayName)
                && (forceApplyName || !displayNameEditedManually || string.IsNullOrWhiteSpace(displayName)))
            {
                displayName = metadata.DisplayName;
                displayNameEditedManually = false;
            }

            if (!string.IsNullOrWhiteSpace(metadata.PackageName))
                packageName = metadata.PackageName;

            if (!string.IsNullOrWhiteSpace(metadata.Version))
                defaultVersion = metadata.Version;
        }

        private bool IsCurrentMetadataRequest(
            CustomUpmSourceKind requestSourceKind,
            string requestSourceUrl,
            string requestUnityPackagePath)
        {
            return sourceKind == requestSourceKind
                   && string.Equals(sourceUrl, requestSourceUrl, StringComparison.Ordinal)
                   && string.Equals(unityPackagePath, requestUnityPackagePath, StringComparison.Ordinal);
        }

        private string BuildMetadataKey()
        {
            return $"{(int)sourceKind}|{sourceUrl}|{unityPackagePath}";
        }

        private async Task AddModulesAndCloseAsync()
        {
            if (string.IsNullOrWhiteSpace(displayName) || string.IsNullOrWhiteSpace(packageName))
                await FetchMetadataAsync(false);

            var modules = BuildModules();
            if (modules.Count == 0)
            {
                statusMessage = "No module can be added from the current data.";
                Repaint();
                return;
            }

            onModulesAdded?.Invoke(modules);
            Close();
        }

        private List<CustomUpmModule> BuildModules()
        {
            if (sourceKind != CustomUpmSourceKind.GitRepositoryUnityPackages)
            {
                return new List<CustomUpmModule>
                {
                    CustomUpmModuleFactory.Create(
                        displayName,
                        sourceUrl,
                        sourceKind,
                        packageName,
                        documentationUrl,
                        defaultVersion)
                };
            }

            var selectedPackages = selectedUnityPackages.Count > 0
                ? selectedUnityPackages.OrderBy(path => path, StringComparer.OrdinalIgnoreCase).ToArray()
                : string.IsNullOrWhiteSpace(unityPackagePath)
                    ? Array.Empty<string>()
                    : new[] { unityPackagePath };

            return selectedPackages
                .Select(packagePath => CustomUpmModuleFactory.Create(
                    string.IsNullOrWhiteSpace(displayName) ? null : $"{displayName} - {System.IO.Path.GetFileNameWithoutExtension(packagePath)}",
                    sourceUrl,
                    sourceKind,
                    packageName,
                    documentationUrl,
                    defaultVersion,
                    packagePath))
                .ToList();
        }
    }
}
