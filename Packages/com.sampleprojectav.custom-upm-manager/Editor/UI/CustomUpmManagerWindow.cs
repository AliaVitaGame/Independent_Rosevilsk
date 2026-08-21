using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace SampleProjectAV.CustomUpmManager.Editor
{
    public sealed class CustomUpmManagerWindow : EditorWindow
    {
        private const float InstalledColumnWidth = 150f;
        private const float AvailableColumnWidth = 220f;
        private const float ControlColumnWidth = 110f;
        private const float DocColumnWidth = 70f;
        private const float HeaderHeight = 32f;
        private const float CategoryHeaderHeight = 26f;
        private const float CategorySpacing = 12f;
        private const float ModuleRowHeight = 44f;
        private const float CellPadding = 8f;
        private const float ControlHeight = 28f;
        private const double DocumentationPreviewDelay = 1.5d;
        private const float DocumentationPreviewWidth = 380f;

        private readonly Dictionary<string, int> selectedVersionIndexes = new Dictionary<string, int>();

        private IModuleRegistryStore registryStore;
        private IInstallStateStore installStateStore;
        private IModuleVersionProvider versionProvider;
        private IModuleInstaller installer;
        private IGitClient gitClient;

        private CustomUpmRegistry registry;
        private CustomUpmInstallStateDatabase installStateDatabase;
        private Vector2 listScroll;
        private bool deleteBeforeUpdate = true;
        private bool busy;
        private string statusMessage;
        private float operationProgress;
        private CustomUpmModule hoveredModule;
        private double hoverStartTime;
        private bool pointerOverDocumentationPreview;
        private bool loadingDocumentationPreview;
        private DocumentationPreview documentationPreview;
        private Texture2D documentationPreviewTexture;

        private readonly struct TableColumns
        {
            public TableColumns(Rect name, Rect installed, Rect available, Rect control, Rect documentation)
            {
                Name = name;
                Installed = installed;
                Available = available;
                Control = control;
                Documentation = documentation;
            }

            public Rect Name { get; }
            public Rect Installed { get; }
            public Rect Available { get; }
            public Rect Control { get; }
            public Rect Documentation { get; }
        }

        [MenuItem("Tools/SampleProjectAV/Custom UPM")]
        public static void Open()
        {
            var window = GetWindow<CustomUpmManagerWindow>("CPM Installer");
            window.minSize = new Vector2(900f, 520f);
            window.Show();
        }

        private void OnEnable()
        {
            wantsMouseMove = true;
            CustomUpmPaths.EnsureSettingsDirectory();
            CustomUpmPaths.EnsureCacheDirectories();

            gitClient = new ProcessGitClient();
            registryStore = new GoogleSheetModuleRegistryStore(
                new JsonModuleRegistryStore(CustomUpmPaths.RegistryPath),
                GoogleSheetModuleRegistryStore.DefaultSpreadsheetUrl);
            installStateStore = new JsonInstallStateStore(CustomUpmPaths.InstallStatePath);
            versionProvider = new ModuleVersionProvider(gitClient);
            installer = new ModuleInstaller(
                new UnityPackageManagerClient(),
                new DownloadClient(),
                gitClient,
                new UnityPackageImportTracker(),
                installStateStore,
                new AssetStoreClient());

            Reload();
            PrefetchDocumentationPreviews();
            EditorApplication.update += RepaintWhileHovering;
        }

        private void OnDisable()
        {
            EditorApplication.update -= RepaintWhileHovering;
        }

        private void OnGUI()
        {
            if (Event.current.type == EventType.MouseMove)
            {
                pointerOverDocumentationPreview = false;
                Repaint();
            }

            DrawToolbar();
            DrawStatus();

            using (new EditorGUI.DisabledScope(busy))
                DrawModuleList();

            DrawDocumentationPreview();
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            GUILayout.Label("CPM Installer", EditorStyles.boldLabel, GUILayout.Width(105f));

            deleteBeforeUpdate = GUILayout.Toggle(
                deleteBeforeUpdate,
                "Delete before update",
                EditorStyles.toolbarButton,
                GUILayout.Width(160f));

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(130f)))
                RunOperation("Refreshing modules from Google Sheet...", RefreshAllAsync);

            EditorGUILayout.EndHorizontal();
        }

        private void DrawStatus()
        {
            if (string.IsNullOrWhiteSpace(statusMessage))
                return;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();
            var statusContent = new GUIContent(statusMessage, "Click to copy");
            var statusRect = GUILayoutUtility.GetRect(statusContent, EditorStyles.wordWrappedLabel, GUILayout.ExpandWidth(true));
            GUI.Label(statusRect, statusContent, EditorStyles.wordWrappedLabel);
            EditorGUIUtility.AddCursorRect(statusRect, MouseCursor.Link);

            if (Event.current.type == EventType.MouseDown
                && Event.current.button == 0
                && statusRect.Contains(Event.current.mousePosition))
            {
                GUIUtility.systemCopyBuffer = statusMessage;
                ShowNotification(new GUIContent("Message copied"));
                Event.current.Use();
            }

            if (!busy && GUILayout.Button("X", GUILayout.Width(24f), GUILayout.Height(22f)))
            {
                statusMessage = string.Empty;
                operationProgress = 0f;
            }

            EditorGUILayout.EndHorizontal();

            if (busy)
            {
                var progressRect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);
                EditorGUI.ProgressBar(progressRect, operationProgress, $"{operationProgress * 100f:0}%");
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawModuleList()
        {
            EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));

            DrawHeader();

            listScroll = EditorGUILayout.BeginScrollView(listScroll);

            if (registry.Modules.Count == 0)
            {
                EditorGUILayout.HelpBox("No modules found. Add enabled rows in the Google Sheet, then refresh this list.", MessageType.Info);
            }

            var rowIndex = 0;
            var isFirstCategory = true;
            foreach (var category in GetCategories())
            {
                var modules = registry.Modules
                    .Where(module => string.Equals(module.Category, category, StringComparison.OrdinalIgnoreCase))
                    .OrderBy(module => module.DisplayName, StringComparer.OrdinalIgnoreCase)
                    .ToArray();

                if (modules.Length == 0)
                    continue;

                if (!isFirstCategory)
                    GUILayout.Space(CategorySpacing);

                DrawCategoryHeader(category);
                foreach (var module in modules)
                    DrawModuleRow(module, rowIndex++);

                isFirstCategory = false;
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private static void DrawHeader()
        {
            var headerRect = EditorGUILayout.GetControlRect(false, HeaderHeight);
            var columns = GetColumns(headerRect);

            EditorGUI.DrawRect(headerRect, GetHeaderColor());
            DrawGrid(headerRect, columns);
            DrawCellLabel(columns.Name, "Name", EditorStyles.boldLabel);
            DrawCellLabel(columns.Installed, "Installed", EditorStyles.boldLabel);
            DrawCellLabel(columns.Available, "Available", EditorStyles.boldLabel);
            DrawCellLabel(columns.Control, "Control", EditorStyles.boldLabel);
            DrawCellLabel(columns.Documentation, "Doc", EditorStyles.boldLabel);
        }

        private static void DrawCategoryHeader(string category)
        {
            var rect = EditorGUILayout.GetControlRect(false, CategoryHeaderHeight);
            EditorGUI.DrawRect(rect, GetCategoryHeaderColor());
            var title = CustomUpmCategoryImportPaths.TryGetImportFolder(category, out var folder)
                ? $"{category}  →  {folder}"
                : category;
            GUI.Label(
                new Rect(rect.x + CellPadding, rect.y, rect.width - CellPadding * 2f, rect.height),
                title,
                EditorStyles.boldLabel);
        }

        private List<string> GetCategories()
        {
            var categories = new List<string>();
            if (registry == null)
                return categories;

            categories.AddRange(registry.Modules
                .Select(module => module.Category)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(category => category, StringComparer.OrdinalIgnoreCase));
            return categories;
        }

        private void DrawModuleRow(CustomUpmModule module, int rowIndex)
        {
            var rowRect = EditorGUILayout.GetControlRect(false, ModuleRowHeight);
            var columns = GetColumns(rowRect);
            var hovered = rowRect.Contains(Event.current.mousePosition);

            EditorGUI.DrawRect(rowRect, GetRowColor(rowIndex, hovered));
            DrawGrid(rowRect, columns);

            TrackDocumentationPreviewHover(module, rowRect);

            var installed = installStateDatabase.TryGet(module.Id, out var state);
            var versions = GetVersionsForUi(module);
            DrawModuleName(module, columns.Name);
            DrawCellLabel(columns.Installed, installed ? state.InstalledVersion : "-", EditorStyles.label);
            DrawVersionPopup(module, versions, GetPopupRect(columns.Available));
            var selectedVersion = GetSelectedVersion(module, versions);

            var buttonLabel = GetControlLabel(module, installed, selectedVersion);
            if (DrawLeftClickButton(GetControlRect(columns.Control), buttonLabel, GetControlButtonColor(installed, buttonLabel)))
            {
                if (installed)
                    DeleteModule(module, state);
                else if (installer.SupportsCachedPackage(module) && installer.HasCachedPackage(module, selectedVersion))
                    RunOperation($"Importing {module.DisplayName}...", token => installer.ImportAsync(module, selectedVersion, token));
                else if (installer.SupportsCachedPackage(module))
                    RunOperation($"Downloading {module.DisplayName}...", (progress, token) => installer.DownloadAsync(module, selectedVersion, progress, token));
                else
                    RunOperation($"Installing {module.DisplayName}...", token => installer.InstallAsync(module, selectedVersion, deleteBeforeUpdate, token));
            }

            if (DrawLeftClickButton(GetControlRect(columns.Documentation), "Doc"))
                OpenDocumentation(module);

            if (Event.current.type == EventType.ContextClick && rowRect.Contains(Event.current.mousePosition))
            {
                var menu = new GenericMenu();
                if (installer.SupportsCachedPackage(module) && installer.HasCachedPackage(module, selectedVersion))
                {
                    menu.AddItem(new GUIContent("Delete completely"), false, () =>
                        RunOperation($"Deleting downloaded file for {module.DisplayName}...", token => installer.DeleteCachedPackageAsync(module, selectedVersion, token)));
                }
                else
                {
                    menu.AddDisabledItem(new GUIContent("Delete completely"));
                }

                menu.ShowAsContext();
                Event.current.Use();
            }
        }

        private void DrawModuleName(CustomUpmModule module, Rect cellRect)
        {
            var nameRect = GetCellLabelRect(cellRect);
            GUI.Label(nameRect, module.DisplayName, EditorStyles.label);
            EditorGUIUtility.AddCursorRect(nameRect, MouseCursor.Link);

            if (!nameRect.Contains(Event.current.mousePosition))
                return;

            if (Event.current.type == EventType.MouseDown && Event.current.button == 0)
            {
                GUIUtility.systemCopyBuffer = module.DisplayName;
                ShowNotification(new GUIContent("Package name copied"));
                Event.current.Use();
            }
        }

        private void TrackDocumentationPreviewHover(CustomUpmModule module, Rect rowRect)
        {
            if (!rowRect.Contains(Event.current.mousePosition))
                return;

            pointerOverDocumentationPreview = true;
            if (hoveredModule != null && string.Equals(hoveredModule.Id, module.Id, StringComparison.Ordinal))
                return;

            hoveredModule = module;
            hoverStartTime = EditorApplication.timeSinceStartup;
            documentationPreview = null;
            documentationPreviewTexture = null;
            loadingDocumentationPreview = false;
        }

        private void DeleteModule(CustomUpmModule module, CustomUpmInstallState state)
        {
            if (state.SourceKind == CustomUpmSourceKind.UnityPackageUrl ||
                state.SourceKind == CustomUpmSourceKind.GitRepositoryUnityPackages)
            {
                var assetCount = state.ImportedAssetPaths.Count;
                if (assetCount == 0)
                {
                    var forgetInstallation = EditorUtility.DisplayDialog(
                        "Delete unavailable",
                        "This import has no tracked file list. If you already removed its assets manually, choose Mark as uninstalled to clear only the CPM status. It will not delete any project files.",
                        "Mark as uninstalled",
                        "Cancel");
                    if (forgetInstallation)
                    {
                        RunOperation(
                            $"Marking {module.DisplayName} as uninstalled...",
                            token => installer.ForgetInstallationAsync(module, token));
                    }

                    return;
                }

                var confirmed = EditorUtility.DisplayDialog(
                    "Delete imported assets?",
                    $"Delete {assetCount} tracked asset(s) imported by {module.DisplayName}? Existing project files were not tracked and will not be removed.",
                    "Delete",
                    "Cancel");
                if (!confirmed)
                    return;
            }

            RunOperation($"Deleting {module.DisplayName}...", token => installer.DeleteAsync(module, token));
        }

        private void DrawVersionPopup(CustomUpmModule module, IReadOnlyList<CustomUpmVersion> versions, Rect popupRect)
        {
            if (versions.Count == 0)
            {
                GUI.Label(popupRect, "-", EditorStyles.label);
                return;
            }

            var index = GetSelectedVersionIndex(module, versions);
            var names = versions.Select(version => version.Name).ToArray();
            var newIndex = EditorGUI.Popup(popupRect, index, names);

            if (newIndex != index)
                selectedVersionIndexes[module.Id] = newIndex;
        }

        private static TableColumns GetColumns(Rect rowRect)
        {
            var nameWidth = rowRect.width - InstalledColumnWidth - AvailableColumnWidth - ControlColumnWidth - DocColumnWidth;
            var name = new Rect(rowRect.x, rowRect.y, Mathf.Max(0f, nameWidth), rowRect.height);
            var installed = new Rect(name.xMax, rowRect.y, InstalledColumnWidth, rowRect.height);
            var available = new Rect(installed.xMax, rowRect.y, AvailableColumnWidth, rowRect.height);
            var control = new Rect(available.xMax, rowRect.y, ControlColumnWidth, rowRect.height);
            var documentation = new Rect(control.xMax, rowRect.y, DocColumnWidth, rowRect.height);
            return new TableColumns(name, installed, available, control, documentation);
        }

        private static Rect GetControlRect(Rect cellRect)
        {
            return new Rect(
                cellRect.x + CellPadding,
                cellRect.y + (cellRect.height - ControlHeight) * 0.5f,
                cellRect.width - CellPadding * 2f,
                ControlHeight);
        }

        private static Rect GetPopupRect(Rect cellRect)
        {
            var popupHeight = EditorGUIUtility.singleLineHeight;
            return new Rect(
                cellRect.x + CellPadding,
                cellRect.y + (cellRect.height - popupHeight) * 0.5f,
                cellRect.width - CellPadding * 2f,
                popupHeight);
        }

        private static bool DrawLeftClickButton(Rect rect, string label, Color? backgroundColor = null)
        {
            var currentEvent = Event.current;
            if (currentEvent.isMouse && currentEvent.button != 0 && rect.Contains(currentEvent.mousePosition))
                return false;

            var previousBackgroundColor = GUI.backgroundColor;
            if (backgroundColor.HasValue)
                GUI.backgroundColor = backgroundColor.Value;

            try
            {
                return GUI.Button(rect, label);
            }
            finally
            {
                GUI.backgroundColor = previousBackgroundColor;
            }
        }

        private static void DrawCellLabel(Rect cellRect, string text, GUIStyle style)
        {
            var labelRect = GetCellLabelRect(cellRect);
            GUI.Label(labelRect, text, style);
        }

        private static Rect GetCellLabelRect(Rect cellRect)
        {
            return new Rect(cellRect.x + CellPadding, cellRect.y, cellRect.width - CellPadding * 2f, cellRect.height);
        }

        private static void DrawGrid(Rect rowRect, TableColumns columns)
        {
            var separatorColor = GetSeparatorColor();
            EditorGUI.DrawRect(new Rect(rowRect.x, rowRect.y, rowRect.width, 1f), separatorColor);
            EditorGUI.DrawRect(new Rect(rowRect.x, rowRect.yMax - 1f, rowRect.width, 1f), separatorColor);
            DrawVerticalSeparator(columns.Installed.x, rowRect, separatorColor);
            DrawVerticalSeparator(columns.Available.x, rowRect, separatorColor);
            DrawVerticalSeparator(columns.Control.x, rowRect, separatorColor);
            DrawVerticalSeparator(columns.Documentation.x, rowRect, separatorColor);
        }

        private static void DrawVerticalSeparator(float x, Rect rowRect, Color color)
        {
            EditorGUI.DrawRect(new Rect(x, rowRect.y, 1f, rowRect.height), color);
        }

        private static Color GetHeaderColor()
        {
            return EditorGUIUtility.isProSkin
                ? new Color(0.235f, 0.235f, 0.235f)
                : new Color(0.78f, 0.78f, 0.78f);
        }

        private static Color GetRowColor(int rowIndex, bool hovered)
        {
            if (hovered)
            {
                return EditorGUIUtility.isProSkin
                    ? new Color(0.25f, 0.3f, 0.35f)
                    : new Color(0.72f, 0.82f, 0.92f);
            }

            var alternate = rowIndex % 2 != 0;
            if (EditorGUIUtility.isProSkin)
                return alternate ? new Color(0.18f, 0.18f, 0.18f) : new Color(0.155f, 0.155f, 0.155f);

            return alternate ? new Color(0.91f, 0.91f, 0.91f) : new Color(0.96f, 0.96f, 0.96f);
        }

        private static Color GetCategoryHeaderColor()
        {
            return EditorGUIUtility.isProSkin
                ? new Color(0.12f, 0.12f, 0.12f)
                : new Color(0.84f, 0.84f, 0.84f);
        }

        private static Color GetSeparatorColor()
        {
            return EditorGUIUtility.isProSkin
                ? new Color(0.08f, 0.08f, 0.08f)
                : new Color(0.62f, 0.62f, 0.62f);
        }

        private IReadOnlyList<CustomUpmVersion> GetVersionsForUi(CustomUpmModule module)
        {
            module.EnsureLists();

            if (module.Versions.Count > 0)
                return VersionSorter.SortDescending(module.Versions);

            if (module.SourceKind == CustomUpmSourceKind.GitUpmPackage)
            {
                return new List<CustomUpmVersion>
                {
                    new CustomUpmVersion("default branch", string.Empty)
                    {
                        SourceUrl = module.SourceUrl,
                        UnityPackagePath = module.UnityPackagePath
                    }
                };
            }

            var name = string.IsNullOrWhiteSpace(module.DefaultVersion) ? "1.0.0" : module.DefaultVersion;
            return new List<CustomUpmVersion>
            {
                new CustomUpmVersion(
                    name,
                    module.SourceKind == CustomUpmSourceKind.GitRepositoryUnityPackages ? string.Empty : name)
                {
                    SourceUrl = module.SourceUrl,
                    UnityPackagePath = module.UnityPackagePath
                }
            };
        }

        private CustomUpmVersion GetSelectedVersion(CustomUpmModule module, IReadOnlyList<CustomUpmVersion> versions)
        {
            if (versions.Count == 0)
                return new CustomUpmVersion("1.0.0", "1.0.0");

            return versions[GetSelectedVersionIndex(module, versions)];
        }

        private int GetSelectedVersionIndex(CustomUpmModule module, IReadOnlyList<CustomUpmVersion> versions)
        {
            if (!selectedVersionIndexes.TryGetValue(module.Id, out var index))
                index = 0;

            if (index < 0 || index >= versions.Count)
                index = 0;

            selectedVersionIndexes[module.Id] = index;
            return index;
        }

        private string GetControlLabel(CustomUpmModule module, bool installed, CustomUpmVersion selectedVersion)
        {
            if (installed)
                return "Delete";

            if (installer.SupportsCachedPackage(module))
                return installer.HasCachedPackage(module, selectedVersion) ? "Import" : "Install";

            return module.SourceKind == CustomUpmSourceKind.AssetStoreUrl ? "Install/Open" : "Install";
        }

        private static Color? GetControlButtonColor(bool installed, string buttonLabel)
        {
            if (installed)
                return new Color(0.82f, 0.25f, 0.25f);

            return buttonLabel.StartsWith("Install", StringComparison.Ordinal)
                ? new Color(0.2f, 0.82f, 0.86f)
                : null;
        }

        private async Task RefreshAllAsync(CancellationToken cancellationToken)
        {
            registry = registryStore.Load();
            selectedVersionIndexes.Clear();

            foreach (var module in registry.Modules)
                await RefreshModuleAsync(module, cancellationToken);

            PrefetchDocumentationPreviews();
        }

        private async Task RefreshModuleAsync(CustomUpmModule module, CancellationToken cancellationToken)
        {
            var versions = await versionProvider.GetVersionsAsync(module, cancellationToken);
            module.SetVersions(versions);
            registryStore.Save(registry);
        }

        private void RunOperation(string message, Func<CancellationToken, Task> operation)
        {
            RunOperation(message, (_, cancellationToken) => operation(cancellationToken));
        }

        private void RunOperation(string message, Func<IProgress<float>, CancellationToken, Task> operation)
        {
            RunOperationAsync(message, operation).Forget();
        }

        private async Task RunOperationAsync(string message, Func<IProgress<float>, CancellationToken, Task> operation)
        {
            if (busy)
                return;

            busy = true;
            statusMessage = message;
            operationProgress = 0f;
            Repaint();

            try
            {
                var progress = new Progress<float>(value =>
                {
                    operationProgress = Mathf.Clamp01(value);
                    Repaint();
                });

                await operation(progress, CancellationToken.None);
                installStateDatabase = installStateStore.Load();
                operationProgress = 1f;
                statusMessage = message.Replace("...", " complete.");
            }
            catch (OperationCanceledException)
            {
                statusMessage = "Operation canceled.";
                operationProgress = 0f;
            }
            catch (Exception exception)
            {
                statusMessage = $"Error: {exception.Message}";
                operationProgress = 0f;
                Debug.LogException(exception);
            }
            finally
            {
                busy = false;
                operationProgress = 0f;
                Repaint();
            }
        }

        private void Reload()
        {
            registry = registryStore.Load();
            installStateDatabase = installStateStore.Load();
        }

        private void PrefetchDocumentationPreviews()
        {
            if (registry == null)
                return;

            var urls = registry.Modules.Select(module =>
                string.IsNullOrWhiteSpace(module.DocumentationUrl) ? module.SourceUrl : module.DocumentationUrl);
            DocumentationPreviewClient.PrefetchAsync(urls).Forget();
        }

        private void RepaintWhileHovering()
        {
            if (hoveredModule != null || loadingDocumentationPreview)
                Repaint();
        }

        private void DrawDocumentationPreview()
        {
            if (hoveredModule == null)
                return;

            var previewRect = GetDocumentationPreviewRect();
            if (!pointerOverDocumentationPreview && !previewRect.Contains(Event.current.mousePosition))
            {
                hoveredModule = null;
                documentationPreview = null;
                documentationPreviewTexture = null;
                loadingDocumentationPreview = false;
                return;
            }

            if (EditorApplication.timeSinceStartup - hoverStartTime < DocumentationPreviewDelay)
                return;

            if (!loadingDocumentationPreview && documentationPreview == null)
                LoadDocumentationPreviewAsync(hoveredModule).Forget();

            if (previewRect.Contains(Event.current.mousePosition))
                pointerOverDocumentationPreview = true;

            GUI.Box(previewRect, GUIContent.none, EditorStyles.helpBox);
            var contentRect = new Rect(previewRect.x + 10f, previewRect.y + 8f, previewRect.width - 20f, previewRect.height - 16f);
            if (loadingDocumentationPreview)
            {
                GUI.Label(contentRect, "Loading documentation preview...", EditorStyles.wordWrappedLabel);
                return;
            }

            if (documentationPreview == null)
                return;

            var y = contentRect.y;
            if (documentationPreviewTexture != null)
            {
                const float imageHeight = 120f;
                GUI.DrawTexture(new Rect(contentRect.x, y, contentRect.width, imageHeight), documentationPreviewTexture, ScaleMode.ScaleToFit);
                y += imageHeight + 6f;
            }

            GUI.Label(new Rect(contentRect.x, y, contentRect.width, 36f), documentationPreview.Title, EditorStyles.boldLabel);
            y += 38f;
            GUI.Label(new Rect(contentRect.x, y, contentRect.width, 46f), documentationPreview.Description, EditorStyles.wordWrappedMiniLabel);
            y += 48f;

            if (GUI.Button(new Rect(contentRect.x, y, 90f, 22f), "Open docs"))
                OpenDocumentation(hoveredModule);
        }

        private async Task LoadDocumentationPreviewAsync(CustomUpmModule module)
        {
            loadingDocumentationPreview = true;
            try
            {
                var url = string.IsNullOrWhiteSpace(module.DocumentationUrl) ? module.SourceUrl : module.DocumentationUrl;
                var preview = await DocumentationPreviewClient.GetPreviewAsync(url);
                if (hoveredModule == null || !string.Equals(hoveredModule.Id, module.Id, StringComparison.Ordinal))
                    return;

                documentationPreview = preview;
                if (preview.ImageData != null && preview.ImageData.Length > 0)
                {
                    documentationPreviewTexture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                    ImageConversion.LoadImage(documentationPreviewTexture, preview.ImageData);
                }
            }
            catch
            {
                documentationPreview = new DocumentationPreview("Documentation", "Preview is unavailable. You can still open the documentation.", string.Empty, null);
            }
            finally
            {
                loadingDocumentationPreview = false;
                Repaint();
            }
        }

        private Rect GetDocumentationPreviewRect()
        {
            var height = documentationPreviewTexture == null ? 150f : 270f;
            var x = Mathf.Clamp(Event.current.mousePosition.x + 16f, 8f, position.width - DocumentationPreviewWidth - 8f);
            var y = Mathf.Clamp(Event.current.mousePosition.y + 16f, 8f, position.height - height - 8f);
            return new Rect(x, y, DocumentationPreviewWidth, height);
        }

        private static void OpenDocumentation(CustomUpmModule module)
        {
            var url = string.IsNullOrWhiteSpace(module.DocumentationUrl)
                ? module.SourceUrl
                : module.DocumentationUrl;

            if (!string.IsNullOrWhiteSpace(url))
                Application.OpenURL(url);
        }
    }

    internal static class TaskExtensions
    {
        public static async void Forget(this Task task)
        {
            await task;
        }
    }
}
