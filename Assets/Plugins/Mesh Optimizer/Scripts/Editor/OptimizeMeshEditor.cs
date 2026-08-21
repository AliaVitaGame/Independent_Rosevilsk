#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(OptimizeMesh))]
public class OptimizeMeshEditor : Editor
{
    SerializedProperty _quality;
    SerializedProperty _livePreview;
    SerializedProperty _lodSlots;
    SerializedProperty _saveMeshFolderPath;

    GUIStyle _invalidPathStyle;

    void OnEnable()
    {
        _quality = serializedObject.FindProperty("_quality");
        _livePreview = serializedObject.FindProperty("_livePreview");
        _lodSlots = serializedObject.FindProperty("_lodSlots");
        _saveMeshFolderPath = serializedObject.FindProperty("_saveMeshFolderPath");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        var optimizeMesh = (OptimizeMesh)target;
        optimizeMesh.EnsureLodSlots();

        EditorGUILayout.PropertyField(_quality, new GUIContent("Quality"));
        EditorGUILayout.PropertyField(_livePreview, new GUIContent("Live Preview"));

        var mesh = optimizeMesh.GetAssignedMesh();

        if (mesh != null)
        {
            var triangleCount = CountTriangles(mesh);
            var triangleRatio = OptimizeMesh.MapQualityToTriangleRatio(
                optimizeMesh.Quality, mesh.vertexCount, triangleCount);
            var targetTris = Mathf.RoundToInt(triangleCount * triangleRatio);
            EditorGUILayout.HelpBox(
                $"Current: {triangleCount} tris | {mesh.vertexCount} verts\n" +
                $"After Optimize (~Quality): ~{targetTris} tris ({triangleRatio * 100f:0.##}% of source)",
                MessageType.Info);
        }
        else
        {
            EditorGUILayout.HelpBox(
                "Add a MeshFilter or SkinnedMeshRenderer with a mesh assigned, lower Quality, then click Optimize.",
                MessageType.Warning);
        }

        EditorGUILayout.Space();

        using (new EditorGUI.DisabledScope(mesh == null))
        {
            if (GUILayout.Button("Optimize", GUILayout.Height(28)))
                optimizeMesh.DecimateMesh();
        }

        DrawSaveMeshSection(optimizeMesh, mesh);

        EditorGUILayout.Space();
        DrawLodSettings();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("LOD", EditorStyles.boldLabel);

        var hasLodGroup = optimizeMesh.GetComponent<LODGroup>() != null;
        using (new EditorGUI.DisabledScope(mesh == null))
        {
            if (GUILayout.Button("Create LODs", GUILayout.Height(28)))
            {
                serializedObject.ApplyModifiedProperties();
                var lodSlots = OptimizeMeshLodSetup.ReadLodSlots(_lodSlots);
                OptimizeMeshLodSetup.Create(optimizeMesh.gameObject, optimizeMesh, lodSlots);
            }
        }

        using (new EditorGUI.DisabledScope(!hasLodGroup))
        {
            if (GUILayout.Button("Remove LODs"))
                OptimizeMeshLodSetup.Destroy(optimizeMesh.gameObject);
        }

        if (hasLodGroup)
        {
            DrawActiveLodTransitions(optimizeMesh);

            using (new EditorGUI.DisabledScope(mesh == null))
            {
                if (GUILayout.Button("Apply LOD Transitions"))
                {
                    serializedObject.ApplyModifiedProperties();
                    var lodSlots = OptimizeMeshLodSetup.ReadLodSlots(_lodSlots);
                    if (OptimizeMeshLodSetup.ApplyLodTransitions(optimizeMesh.gameObject, optimizeMesh, lodSlots))
                        Debug.Log("[OptimizeMesh] LOD transition values applied.", optimizeMesh);
                    else
                        Debug.LogWarning("[OptimizeMesh] Failed to apply LOD transition values.", optimizeMesh);
                }
            }

            EditorGUILayout.HelpBox(
                "Transition values map 1:1 to LOD Group. Edit them here, then click Apply LOD Transitions.",
                MessageType.Info);
        }

        serializedObject.ApplyModifiedProperties();
    }

    void DrawLodSettings()
    {
        EditorGUILayout.LabelField("LOD Settings", EditorStyles.boldLabel);

        if (GUILayout.Button("Reset LOD Auto Quality"))
        {
            var optimizeMesh = (OptimizeMesh)target;
            optimizeMesh.ApplyDefaultLodSlots();
            serializedObject.Update();
            EditorUtility.SetDirty(optimizeMesh);
        }

        if (_lodSlots == null || !_lodSlots.isArray || _lodSlots.arraySize != OptimizeMesh.LodSlotCount)
            return;

        for (var i = 0; i < OptimizeMesh.LodSlotCount; i++)
        {
            var slot = _lodSlots.GetArrayElementAtIndex(i);

            EditorGUILayout.Space(4);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField($"LOD {i}", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(
                    slot.FindPropertyRelative("transition"),
                    new GUIContent("Transition", "Same value as LOD Group Screen Relative Transition Height (0-1)."));

                EditorGUILayout.PropertyField(
                    slot.FindPropertyRelative("meshQuality"),
                    new GUIContent("Mesh Quality"));

                var meshQuality = slot.FindPropertyRelative("meshQuality").floatValue;
                var optimizeMeshRef = (OptimizeMesh)target;
                OptimizeMeshLodSetup.GetSourceMeshStats(optimizeMeshRef.gameObject, out var vertCount, out var triCount);
                var ratio = OptimizeMesh.MapQualityToTriangleRatio(meshQuality, vertCount, triCount);
                var targetTris = triCount > 0
                    ? Mathf.RoundToInt(triCount * ratio)
                    : OptimizeMesh.GetTargetTriangleCountAtZeroQuality(vertCount);
                EditorGUILayout.LabelField(
                    $"≈ {targetTris} tris ({ratio * 100f:0.##}% of original)",
                    EditorStyles.miniLabel);
            }
        }
    }

    void DrawSaveMeshSection(OptimizeMesh optimizeMesh, Mesh mesh)
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Save Mesh", EditorStyles.boldLabel);

        var folderPath = _saveMeshFolderPath.stringValue;
        var pathIsValid = OptimizeMeshSaveEditor.IsValidFolderPath(folderPath);

        if (_invalidPathStyle == null)
        {
            _invalidPathStyle = new GUIStyle(EditorStyles.textField);
            _invalidPathStyle.normal.textColor = Color.red;
            _invalidPathStyle.focused.textColor = Color.red;
        }

        EditorGUILayout.LabelField("Folder under Assets (without \"Assets/\")");
        _saveMeshFolderPath.stringValue = EditorGUILayout.TextField(
            folderPath,
            pathIsValid ? EditorStyles.textField : _invalidPathStyle);

        var assignedMesh = optimizeMesh.GetAssignedMesh();
        var meshIsSaved = assignedMesh != null && EditorUtility.IsPersistent(assignedMesh);
        var canSave = mesh != null && (pathIsValid || meshIsSaved);

        using (new EditorGUI.DisabledScope(!canSave))
        {
            var buttonLabel = meshIsSaved ? "Show Saved Mesh" : "Save Mesh";
            if (GUILayout.Button(buttonLabel))
            {
                serializedObject.ApplyModifiedProperties();
                var savedFolderPath = OptimizeMeshSaveEditor.SaveMeshForOptimizeMesh(optimizeMesh, assignedMesh);
                if (!string.IsNullOrWhiteSpace(savedFolderPath))
                    _saveMeshFolderPath.stringValue = savedFolderPath;

                serializedObject.Update();
            }
        }

        if (!pathIsValid)
        {
            EditorGUILayout.HelpBox(
                "Enter a valid folder path, e.g. GeneratedLODs/Meshes",
                MessageType.Warning);
        }
    }

    void DrawActiveLodTransitions(OptimizeMesh optimizeMesh)
    {
        var lodGroup = optimizeMesh.GetComponent<LODGroup>();
        if (lodGroup == null)
            return;

        var lods = lodGroup.GetLODs();
        if (lods == null || lods.Length == 0)
            return;

        EditorGUILayout.LabelField("LOD Group (active)", EditorStyles.miniBoldLabel);
        var displayCount = Mathf.Min(lods.Length, OptimizeMesh.LodSlotCount);
        for (var i = 0; i < displayCount; i++)
        {
            EditorGUILayout.LabelField(
                $"LOD {i}: {lods[i].screenRelativeTransitionHeight:F3}",
                EditorStyles.miniLabel);
        }
    }

    static int CountTriangles(Mesh mesh)
    {
        var count = 0;
        for (var i = 0; i < mesh.subMeshCount; i++)
            count += (int)mesh.GetIndexCount(i);

        return count / 3;
    }
}
#endif
