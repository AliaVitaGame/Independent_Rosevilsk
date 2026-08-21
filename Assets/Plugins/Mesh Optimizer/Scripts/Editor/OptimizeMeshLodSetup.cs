#if UNITY_EDITOR
using System;
using System.IO;
using IndMeshSimplifier;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public static class OptimizeMeshLodSetup
{
    const string LodRootFolder = "Assets/GeneratedLODs";

    public static void Create(GameObject target, OptimizeMesh optimizeMesh)
    {
        var lodSlots = optimizeMesh != null ? optimizeMesh.LodSlots : OptimizeMeshLodSlot.CreateDefaults();
        Create(target, optimizeMesh, lodSlots);
    }

    public static void Create(GameObject target, OptimizeMesh optimizeMesh, OptimizeMeshLodSlot[] lodSlots)
    {
        if (target == null)
            return;

        optimizeMesh?.EnsureLodSlots();

        if (lodSlots == null || lodSlots.Length < OptimizeMesh.LodSlotCount)
            lodSlots = optimizeMesh != null ? optimizeMesh.LodSlots : OptimizeMeshLodSlot.CreateDefaults();

        try
        {
            EditorUtility.DisplayProgressBar("Creating LODs", "Preparing object…", 0.05f);

            if (!HasAnyRenderableMesh(target))
            {
                Debug.LogWarning("[OptimizeMesh] No mesh for LOD. MeshFilter or SkinnedMeshRenderer required.", target);
                return;
            }

            EnsureRootRenderers(target);
            EnsureMeshesReadable(target);
            GetSourceMeshStats(target, out var sourceVertexCount, out var sourceTriangleCount);

            EditorUtility.DisplayProgressBar("Creating LODs", "Removing old LODs…", 0.15f);
            LODGenerator.DestroyLODs(target);

            var existingGroup = target.GetComponent<LODGroup>();
            if (existingGroup != null)
                Undo.DestroyObjectImmediate(existingGroup);

            var helper = GetOrCreateHelper(target);
            var saveAssetsPath = GetSaveAssetsPath(target.name);
            var levels = BuildLodLevels(target, lodSlots, sourceVertexCount, sourceTriangleCount);
            ConfigureHelper(helper, levels, saveAssetsPath);

            EditorUtility.DisplayProgressBar("Creating LODs", "Generating LOD levels…", 0.35f);
            var lodGroup = LODGenerator.GenerateLODs(target, levels, true, helper.SimplificationOptions, saveAssetsPath);
            if (lodGroup == null)
            {
                Debug.LogError("[OptimizeMesh] Failed to create LOD Group.", target);
                return;
            }

            lodGroup.fadeMode = helper.FadeMode;
            lodGroup.animateCrossFading = helper.AnimateCrossFading;

            EditorUtility.DisplayProgressBar("Creating LODs", "Applying mesh quality…", 0.7f);
            ApplyMeshQualityToLodLevels(target, lodSlots, saveAssetsPath, sourceVertexCount, sourceTriangleCount);

            SetGeneratedFlag(helper, true);
            ApplyLodTransitions(target, optimizeMesh, lodSlots);
            EditorUtility.SetDirty(target);
            AssetDatabase.SaveAssets();

            Debug.Log(
                $"[OptimizeMesh] LODs ready for \"{target.name}\": {OptimizeMesh.LodSlotCount} levels, meshes in {LodRootFolder}/{SanitizeName(target.name)}.",
                target);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[OptimizeMesh] LOD creation failed: {ex.Message}", target);
            Debug.LogException(ex);
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }
    }

    public static void Destroy(GameObject target)
    {
        if (target == null)
            return;

        try
        {
            EditorUtility.DisplayProgressBar("LOD", "Removing LODs…", 0.5f);
            LODGenerator.DestroyLODs(target);

            var helper = target.GetComponent<LODGeneratorHelper>();
            if (helper != null)
                SetGeneratedFlag(helper, false);

            var lodGroup = target.GetComponent<LODGroup>();
            if (lodGroup != null)
                Undo.DestroyObjectImmediate(lodGroup);

            EditorUtility.SetDirty(target);
            Debug.Log($"[OptimizeMesh] LODs removed from \"{target.name}\".", target);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[OptimizeMesh] LOD removal failed: {ex.Message}", target);
            Debug.LogException(ex);
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }
    }

    static void ApplyMeshQualityToLodLevels(
        GameObject target,
        OptimizeMeshLodSlot[] lodSlots,
        string saveAssetsPath,
        int sourceVertexCount,
        int sourceTriangleCount)
    {
        var lodParent = target.transform.Find(LODGenerator.LODParentGameObjectName);
        if (lodParent == null)
        {
            Debug.LogWarning("[OptimizeMesh] LOD parent was not found after generation.", target);
            return;
        }

        for (var levelIndex = 0; levelIndex < OptimizeMesh.LodSlotCount; levelIndex++)
        {
            var levelTransform = lodParent.Find($"Level{levelIndex:00}");
            if (levelTransform == null)
                continue;

            ApplyMeshQualityToRenderers(
                levelTransform,
                lodSlots[levelIndex].meshQuality,
                sourceVertexCount,
                sourceTriangleCount,
                target.name,
                levelIndex,
                saveAssetsPath);
        }
    }

    static void ApplyMeshQualityToRenderers(
        Transform levelTransform,
        float meshQuality,
        int sourceVertexCount,
        int sourceTriangleCount,
        string objectName,
        int levelIndex,
        string saveAssetsPath)
    {
        var meshFilters = levelTransform.GetComponentsInChildren<MeshFilter>(true);
        foreach (var meshFilter in meshFilters)
        {
            if (meshFilter == null || meshFilter.sharedMesh == null)
                continue;

            AssignSimplifiedMesh(
                meshFilter,
                meshFilter.sharedMesh,
                meshQuality,
                sourceVertexCount,
                sourceTriangleCount,
                objectName,
                meshFilter.name,
                levelIndex,
                saveAssetsPath);
        }

        var skinnedRenderers = levelTransform.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        foreach (var skinnedRenderer in skinnedRenderers)
        {
            if (skinnedRenderer == null || skinnedRenderer.sharedMesh == null)
                continue;

            var sourceMesh = skinnedRenderer.sharedMesh;
            var simplifiedMesh = OptimizeMeshLodMeshSimplifier.Simplify(
                sourceMesh,
                meshQuality,
                sourceVertexCount,
                sourceTriangleCount);
            if (simplifiedMesh == null)
                continue;

            var savedMesh = SaveLodMeshAsset(
                simplifiedMesh,
                objectName,
                skinnedRenderer.name,
                levelIndex,
                sourceMesh.name,
                saveAssetsPath);

            Undo.RecordObject(skinnedRenderer, "Apply LOD Mesh Quality");
            skinnedRenderer.sharedMesh = savedMesh;
            EditorUtility.SetDirty(skinnedRenderer);
            LogLodMeshResult(levelIndex, meshQuality, sourceMesh, savedMesh);
        }
    }

    static void AssignSimplifiedMesh(
        MeshFilter meshFilter,
        Mesh sourceMesh,
        float meshQuality,
        int sourceVertexCount,
        int sourceTriangleCount,
        string objectName,
        string rendererName,
        int levelIndex,
        string saveAssetsPath)
    {
        var simplifiedMesh = OptimizeMeshLodMeshSimplifier.Simplify(
            sourceMesh,
            meshQuality,
            sourceVertexCount,
            sourceTriangleCount);
        if (simplifiedMesh == null)
            return;

        var savedMesh = SaveLodMeshAsset(
            simplifiedMesh,
            objectName,
            rendererName,
            levelIndex,
            sourceMesh.name,
            saveAssetsPath);

        Undo.RecordObject(meshFilter, "Apply LOD Mesh Quality");
        meshFilter.sharedMesh = savedMesh;
        EditorUtility.SetDirty(meshFilter);
        LogLodMeshResult(levelIndex, meshQuality, sourceMesh, savedMesh);
    }

    static void LogLodMeshResult(int levelIndex, float meshQuality, Mesh sourceMesh, Mesh resultMesh)
    {
        Debug.Log(
            $"[OptimizeMesh] LOD {levelIndex} Mesh Quality {meshQuality:F2}: {CountTriangles(sourceMesh)} -> {CountTriangles(resultMesh)} tris.");
    }

    static bool HasAnyRenderableMesh(GameObject target)
    {
        var meshFilter = target.GetComponent<MeshFilter>();
        if (meshFilter != null && meshFilter.sharedMesh != null)
            return true;

        var skinned = target.GetComponent<SkinnedMeshRenderer>();
        if (skinned != null && skinned.sharedMesh != null)
            return true;

        return target.GetComponentInChildren<MeshFilter>(true) != null ||
               target.GetComponentInChildren<SkinnedMeshRenderer>(true) != null;
    }

    static void EnsureRootRenderers(GameObject target)
    {
        var meshFilter = target.GetComponent<MeshFilter>();
        if (meshFilter == null || meshFilter.sharedMesh == null)
            return;

        if (target.GetComponent<MeshRenderer>() != null)
            return;

        var meshRenderer = Undo.AddComponent<MeshRenderer>(target);
        var reference = target.GetComponentInChildren<MeshRenderer>(true);
        if (reference != null && reference != meshRenderer)
            meshRenderer.sharedMaterials = reference.sharedMaterials;
    }

    static void EnsureMeshesReadable(GameObject target)
    {
        var meshFilters = target.GetComponentsInChildren<MeshFilter>(true);
        foreach (var meshFilter in meshFilters)
        {
            if (meshFilter == null || meshFilter.sharedMesh == null)
                continue;

            if (IsUnderGeneratedLodParent(meshFilter.transform, target.transform))
                continue;

            AssignReadableMesh(meshFilter, meshFilter.sharedMesh);
        }

        var skinnedRenderers = target.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        foreach (var skinnedRenderer in skinnedRenderers)
        {
            if (skinnedRenderer == null || skinnedRenderer.sharedMesh == null)
                continue;

            if (IsUnderGeneratedLodParent(skinnedRenderer.transform, target.transform))
                continue;

            AssignReadableSkinnedMesh(skinnedRenderer, skinnedRenderer.sharedMesh);
        }
    }

    static void AssignReadableMesh(MeshFilter meshFilter, Mesh sourceMesh)
    {
        var readableMesh = MeshReadableUtility.EnsureReadable(sourceMesh);
        if (readableMesh == null || readableMesh == sourceMesh)
            return;

        Undo.RecordObject(meshFilter, "Assign Readable Mesh");
        meshFilter.sharedMesh = readableMesh;
        EditorUtility.SetDirty(meshFilter);
    }

    static void AssignReadableSkinnedMesh(SkinnedMeshRenderer skinnedRenderer, Mesh sourceMesh)
    {
        var readableMesh = MeshReadableUtility.EnsureReadable(sourceMesh);
        if (readableMesh == null || readableMesh == sourceMesh)
            return;

        Undo.RecordObject(skinnedRenderer, "Assign Readable Mesh");
        skinnedRenderer.sharedMesh = readableMesh;
        EditorUtility.SetDirty(skinnedRenderer);
    }

    static LODGeneratorHelper GetOrCreateHelper(GameObject target)
    {
        var helper = target.GetComponent<LODGeneratorHelper>();
        if (helper != null)
            return helper;

        return Undo.AddComponent<LODGeneratorHelper>(target);
    }

    static void ConfigureHelper(LODGeneratorHelper helper, LODLevel[] levels, string saveAssetsPath)
    {
        helper.FadeMode = LODFadeMode.None;
        helper.AnimateCrossFading = false;
        helper.AutoCollectRenderers = true;
        helper.Levels = levels;
        helper.SaveAssetsPath = saveAssetsPath;

        var options = SimplificationOptions.Default;
        options.EnableSmartLink = true;
        options.PreserveBorderEdges = true;
        options.PreserveUVSeamEdges = true;
        options.PreserveUVFoldoverEdges = true;
        options.MaxIterationCount = 100;
        options.Agressiveness = 7.0;
        helper.SimplificationOptions = options;

        EditorUtility.SetDirty(helper);
    }

    static LODLevel[] BuildLodLevels(
        GameObject target,
        OptimizeMeshLodSlot[] lodSlots,
        int vertexCount,
        int triangleCount)
    {
        if (lodSlots == null || lodSlots.Length < OptimizeMesh.LodSlotCount)
            lodSlots = OptimizeMeshLodSlot.CreateDefaults();

        var levels = new LODLevel[OptimizeMesh.LodSlotCount];
        for (var i = 0; i < OptimizeMesh.LodSlotCount; i++)
        {
            var slot = lodSlots[i];
            var triangleRatio = slot.GetMeshTriangleRatio(vertexCount, triangleCount);
            var targetTris = triangleCount > 0
                ? Mathf.RoundToInt(triangleCount * triangleRatio)
                : OptimizeMesh.GetTargetTriangleCountAtZeroQuality(vertexCount);
            Debug.Log(
                $"[OptimizeMesh] LOD {i}: Mesh Quality {slot.meshQuality:F2} -> target ~{targetTris} tris ({triangleRatio * 100f:0.##}% of source).",
                target);

            levels[i] = CreateLevel(
                slot.transition,
                1f,
                i >= 1,
                i >= 2,
                i < 2 ? ShadowCastingMode.On : ShadowCastingMode.Off,
                i < 2);
        }

        return levels;
    }

    public static bool ApplyLodTransitions(GameObject target, OptimizeMesh optimizeMesh)
    {
        var lodSlots = optimizeMesh != null ? optimizeMesh.LodSlots : OptimizeMeshLodSlot.CreateDefaults();
        return ApplyLodTransitions(target, optimizeMesh, lodSlots);
    }

    public static bool ApplyLodTransitions(GameObject target, OptimizeMesh optimizeMesh, OptimizeMeshLodSlot[] lodSlots)
    {
        if (target == null || optimizeMesh == null)
            return false;

        optimizeMesh.EnsureLodSlots();

        if (lodSlots == null || lodSlots.Length < OptimizeMesh.LodSlotCount)
            lodSlots = optimizeMesh.LodSlots;

        var lodGroup = target.GetComponent<LODGroup>();
        if (lodGroup == null)
            return false;

        var lods = lodGroup.GetLODs();
        if (lods == null || lods.Length == 0)
            return false;

        var applyCount = Mathf.Min(lods.Length, lodSlots.Length, OptimizeMesh.LodSlotCount);
        var changed = false;

        for (var i = 0; i < applyCount; i++)
        {
            var transition = Mathf.Clamp01(lodSlots[i].transition);
            if (!Mathf.Approximately(lods[i].screenRelativeTransitionHeight, transition))
            {
                lods[i].screenRelativeTransitionHeight = transition;
                changed = true;
            }
        }

        if (!changed)
            return true;

        Undo.RecordObject(lodGroup, "Apply LOD Transitions");
        lodGroup.SetLODs(lods);
        EditorUtility.SetDirty(lodGroup);

        var helper = target.GetComponent<LODGeneratorHelper>();
        if (helper != null && helper.Levels != null)
        {
            var helperLevels = helper.Levels;
            var helperCount = Mathf.Min(helperLevels.Length, lodSlots.Length);
            for (var i = 0; i < helperCount; i++)
                helperLevels[i].ScreenRelativeTransitionHeight = Mathf.Clamp01(lodSlots[i].transition);

            helper.Levels = helperLevels;
            EditorUtility.SetDirty(helper);
        }

        return true;
    }

    static LODLevel CreateLevel(
        float transition,
        float quality,
        bool combineMeshes,
        bool combineSubMeshes,
        ShadowCastingMode shadows,
        bool receiveShadows)
    {
        return new LODLevel(transition, quality)
        {
            CombineMeshes = combineMeshes,
            CombineSubMeshes = combineSubMeshes,
            SkinQuality = SkinQuality.Auto,
            ShadowCastingMode = shadows,
            ReceiveShadows = receiveShadows,
            SkinnedMotionVectors = receiveShadows,
            LightProbeUsage = receiveShadows ? LightProbeUsage.BlendProbes : LightProbeUsage.Off,
            ReflectionProbeUsage = receiveShadows ? ReflectionProbeUsage.BlendProbes : ReflectionProbeUsage.Off
        };
    }

    static Mesh SaveLodMeshAsset(
        Mesh mesh,
        string objectName,
        string rendererName,
        int levelIndex,
        string meshName,
        string saveAssetsPath)
    {
        EnsureAssetFolder($"{LodRootFolder}/{saveAssetsPath}");

        var safeMeshName = SanitizeName($"{levelIndex:00}_{meshName}");
        var assetPath = AssetDatabase.GenerateUniqueAssetPath($"{LodRootFolder}/{saveAssetsPath}/{safeMeshName}.asset");
        AssetDatabase.CreateAsset(mesh, assetPath);

        var savedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(assetPath);
        return savedMesh != null ? savedMesh : mesh;
    }

    static string GetSaveAssetsPath(string objectName)
    {
        EnsureAssetFolder(LodRootFolder);
        var objectFolder = $"{LodRootFolder}/{SanitizeName(objectName)}";
        EnsureAssetFolder(objectFolder);
        return SanitizeName(objectName);
    }

    static void EnsureAssetFolder(string folderPath)
    {
        if (AssetDatabase.IsValidFolder(folderPath))
            return;

        var parent = Path.GetDirectoryName(folderPath)?.Replace('\\', '/');
        var folderName = Path.GetFileName(folderPath);
        if (string.IsNullOrEmpty(parent) || string.IsNullOrEmpty(folderName))
            return;

        if (!AssetDatabase.IsValidFolder(parent))
            EnsureAssetFolder(parent);

        AssetDatabase.CreateFolder(parent, folderName);
    }

    public static void GetSourceMeshStats(GameObject target, out int vertexCount, out int triangleCount)
    {
        vertexCount = 0;
        triangleCount = 0;

        var meshRenderers = target.GetComponentsInChildren<MeshRenderer>(true);
        foreach (var meshRenderer in meshRenderers)
        {
            if (meshRenderer == null || !meshRenderer.enabled)
                continue;

            if (IsUnderGeneratedLodParent(meshRenderer.transform, target.transform))
                continue;

            var meshFilter = meshRenderer.GetComponent<MeshFilter>();
            if (meshFilter == null || meshFilter.sharedMesh == null)
                continue;

            vertexCount += meshFilter.sharedMesh.vertexCount;
            triangleCount += CountTriangles(meshFilter.sharedMesh);
        }

        var skinnedRenderers = target.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        foreach (var skinnedRenderer in skinnedRenderers)
        {
            if (skinnedRenderer == null || !skinnedRenderer.enabled)
                continue;

            if (IsUnderGeneratedLodParent(skinnedRenderer.transform, target.transform))
                continue;

            if (skinnedRenderer.sharedMesh == null)
                continue;

            vertexCount += skinnedRenderer.sharedMesh.vertexCount;
            triangleCount += CountTriangles(skinnedRenderer.sharedMesh);
        }
    }

    static bool IsUnderGeneratedLodParent(Transform rendererTransform, Transform rootTransform)
    {
        var lodParent = rootTransform.Find(LODGenerator.LODParentGameObjectName);
        return lodParent != null && rendererTransform.IsChildOf(lodParent);
    }

    static int CountTriangles(Mesh mesh)
    {
        var count = 0;
        for (var i = 0; i < mesh.subMeshCount; i++)
            count += (int)mesh.GetIndexCount(i);

        return count / 3;
    }

    static string SanitizeName(string name)
    {
        if (string.IsNullOrEmpty(name))
            return "Model";

        foreach (var invalid in Path.GetInvalidFileNameChars())
            name = name.Replace(invalid, '_');

        return name.Replace(' ', '_');
    }

    static void SetGeneratedFlag(LODGeneratorHelper helper, bool isGenerated)
    {
        var serialized = new SerializedObject(helper);
        var property = serialized.FindProperty("isGenerated");
        if (property == null)
            return;

        property.boolValue = isGenerated;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    public static OptimizeMeshLodSlot[] ReadLodSlots(SerializedProperty lodSlotsProperty)
    {
        var slots = new OptimizeMeshLodSlot[OptimizeMesh.LodSlotCount];
        if (lodSlotsProperty == null || !lodSlotsProperty.isArray || lodSlotsProperty.arraySize < OptimizeMesh.LodSlotCount)
            return OptimizeMeshLodSlot.CreateDefaults();

        for (var i = 0; i < OptimizeMesh.LodSlotCount; i++)
        {
            var slotProperty = lodSlotsProperty.GetArrayElementAtIndex(i);
            slots[i] = new OptimizeMeshLodSlot
            {
                transition = slotProperty.FindPropertyRelative("transition").floatValue,
                meshQuality = slotProperty.FindPropertyRelative("meshQuality").floatValue
            };
        }

        return slots;
    }
}
#endif
