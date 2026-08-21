using System;
using IndMeshSimplifier;
using UnityEngine;
using UnityEngine.Serialization;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

[DisallowMultipleComponent]
public class OptimizeMesh : MonoBehaviour
{
    public const float QualityOriginalThreshold = 0.9999f;
    public const int LodSlotCount = 3;
    const float LowQualityExponent = 3.25f;
    const int MinTrianglesAtZeroQuality = 8;

    [Range(0f, 1f)]
    [SerializeField] float _quality = 1f;

    [SerializeField]
    bool _livePreview;

    [SerializeField]
    OptimizeMeshLodSlot[] _lodSlots = OptimizeMeshLodSlot.CreateDefaults();

    [SerializeField]
    string _saveMeshFolderPath = "GeneratedLODs/Meshes";

    [SerializeField, HideInInspector]
    string _sourceAssetPath;

    [SerializeField, HideInInspector]
    string _sourceMeshName;

    Mesh _sourceMesh;
    Mesh _previewMesh;

    public float Quality => _quality;

    public bool LivePreview
    {
        get => _livePreview;
        set => _livePreview = value;
    }

    public string SaveMeshFolderPath
    {
        get => string.IsNullOrWhiteSpace(_saveMeshFolderPath) ? "GeneratedLODs/Meshes" : _saveMeshFolderPath;
        set => _saveMeshFolderPath = value;
    }

    public bool HasSourceMesh => _sourceMesh != null && _sourceMesh.vertexCount > 0;

    public OptimizeMeshLodSlot[] LodSlots => _lodSlots;

    public void EnsureLodSlots()
    {
        if (_lodSlots == null || _lodSlots.Length != LodSlotCount)
            _lodSlots = OptimizeMeshLodSlot.CreateDefaults();
    }

    public void ApplyDefaultLodSlots()
    {
        _lodSlots = OptimizeMeshLodSlot.CreateDefaults();
    }

    void Reset()
    {
        ApplyDefaultLodSlots();
    }

    public Mesh GetAssignedMesh()
    {
        if (!TryGetMeshAssignmentTarget(out var meshFilter, out var skinnedMeshRenderer))
            return null;

        if (meshFilter != null && meshFilter.sharedMesh != null)
            return meshFilter.sharedMesh;

        if (skinnedMeshRenderer != null && skinnedMeshRenderer.sharedMesh != null)
            return skinnedMeshRenderer.sharedMesh;

        return null;
    }

    public bool TryGetMeshAssignmentTarget(out MeshFilter meshFilter, out SkinnedMeshRenderer skinnedMeshRenderer)
    {
        meshFilter = GetComponent<MeshFilter>();
        skinnedMeshRenderer = GetComponent<SkinnedMeshRenderer>();

        if (meshFilter != null || skinnedMeshRenderer != null)
            return true;

        meshFilter = GetComponentInChildren<MeshFilter>(true);
        skinnedMeshRenderer = GetComponentInChildren<SkinnedMeshRenderer>(true);
        return meshFilter != null || skinnedMeshRenderer != null;
    }

    bool TryGetMeshFilter(out MeshFilter meshFilter)
    {
        if (TryGetMeshAssignmentTarget(out meshFilter, out _))
            return meshFilter != null;

        meshFilter = null;
        return false;
    }

    bool TryGetSkinnedMeshRenderer(out SkinnedMeshRenderer skinnedMeshRenderer)
    {
        if (TryGetMeshAssignmentTarget(out _, out skinnedMeshRenderer))
            return skinnedMeshRenderer != null;

        skinnedMeshRenderer = null;
        return false;
    }

    bool HasMeshSource()
    {
        return GetAssignedMesh() != null;
    }

    void OnDestroy()
    {
        ReleasePreviewMesh();
        ReleaseSourceMesh();
    }

    Mesh GetMeshForSourceCapture()
    {
        var assigned = GetAssignedMesh();
        if (assigned == null)
            return null;

        if (_previewMesh != null && assigned == _previewMesh)
        {
#if UNITY_EDITOR
            if (!string.IsNullOrEmpty(_sourceAssetPath))
            {
                var assetMesh = MeshImportSettingsUtility.LoadMeshFromAsset(_sourceAssetPath, _sourceMeshName);
                if (assetMesh != null)
                    return assetMesh;
            }
#endif
            return _sourceMesh;
        }

        return assigned;
    }

    void EnsureSourceMesh()
    {
        if (_sourceMesh != null && _sourceMesh.vertexCount > 0)
            return;

        var mesh = GetMeshForSourceCapture();
        if (mesh == null)
            return;

        CaptureSourceFromMesh(mesh);
    }

    void CaptureSourceFromMesh(Mesh mesh)
    {
        if (mesh == null)
            return;

        var readable = MeshReadableUtility.EnsureReadable(mesh);
        if (readable == null || readable.vertexCount == 0)
        {
            Debug.LogError("[OptimizeMesh] Failed to read mesh.", this);
            return;
        }

#if UNITY_EDITOR
        var assetPath = AssetDatabase.GetAssetPath(mesh);
        if (!string.IsNullOrEmpty(assetPath))
        {
            _sourceAssetPath = assetPath;
            _sourceMeshName = mesh.name;

            var assetMesh = MeshImportSettingsUtility.LoadMeshFromAsset(assetPath, mesh.name);
            if (assetMesh != null)
                readable = MeshReadableUtility.EnsureReadable(assetMesh);
        }
#endif

        ReleaseSourceMesh();
        _sourceMesh = Instantiate(readable);
        _sourceMesh.name = mesh.name + "_OptimizeSource";
        _sourceMesh.hideFlags = HideFlags.HideAndDontSave;
    }

    public void InvalidateSourceMesh()
    {
        ReleaseSourceMesh();
    }

    public void ApplyOptimization()
    {
        if (!HasMeshSource())
        {
            Debug.LogWarning("[OptimizeMesh] Object needs a MeshFilter or SkinnedMeshRenderer with a mesh assigned.", this);
            return;
        }

        InvalidateSourceMesh();
        EnsureSourceMesh();

        if (_sourceMesh == null || _sourceMesh.vertexCount == 0)
            return;

        ReleasePreviewMesh();

        if (_quality >= QualityOriginalThreshold)
        {
            _previewMesh = Instantiate(_sourceMesh);
            _previewMesh.name = GetPreviewMeshBaseName() + "_Preview";
            _previewMesh.hideFlags = HideFlags.HideAndDontSave;
            AssignMesh(_previewMesh);
            MarkSceneDirty();
            return;
        }

        var sourceTriangleCount = CountTriangles(_sourceMesh);
        var sourceVertexCount = _sourceMesh.vertexCount;
        var simplifyQuality = MapQualityToTriangleRatio(_quality, sourceVertexCount, sourceTriangleCount);
        var targetTriangleCount = Mathf.Max(
            MinTrianglesAtZeroQuality,
            Mathf.RoundToInt(sourceTriangleCount * simplifyQuality));
        Mesh workingMesh = null;
        Mesh weldedMesh = null;

        try
        {
            workingMesh = Instantiate(_sourceMesh);
            workingMesh.name = _sourceMesh.name + "_Working";
            workingMesh.hideFlags = HideFlags.HideAndDontSave;

            var weldDistance = MeshWeldUtility.GetRecommendedWeldDistance(workingMesh);
            weldedMesh = MeshWeldUtility.WeldVertices(workingMesh, weldDistance);
            DestroyWorkingMesh(workingMesh);
            workingMesh = weldedMesh;
            weldedMesh = null;

            var meshSimplifier = CreateConfiguredSimplifier(_sourceMesh, _quality);
            meshSimplifier.Initialize(workingMesh);
            meshSimplifier.SimplifyMesh(simplifyQuality);

            _previewMesh = meshSimplifier.ToMesh();
            _previewMesh.name = GetPreviewMeshBaseName() + "_Optimized";
            _previewMesh.hideFlags = HideFlags.HideAndDontSave;
            _previewMesh.bindposes = _sourceMesh.bindposes;
            AssignMesh(_previewMesh);

            var resultTriangleCount = CountTriangles(_previewMesh);
            Debug.Log(
                $"[OptimizeMesh] Quality {_quality:F2}: {sourceVertexCount} verts | " +
                $"{sourceTriangleCount} → {resultTriangleCount} tris (target ~{targetTriangleCount}).",
                this);
            MarkSceneDirty();
        }
        catch (Exception ex)
        {
            Debug.LogError($"[OptimizeMesh] Error: {ex.Message}", this);
            Debug.LogException(ex);
        }
        finally
        {
            DestroyWorkingMesh(workingMesh);
            DestroyWorkingMesh(weldedMesh);
        }
    }

    void AssignMesh(Mesh mesh)
    {
        if (TryGetMeshFilter(out var meshFilter))
            meshFilter.sharedMesh = mesh;
        else if (TryGetSkinnedMeshRenderer(out var skinned))
            skinned.sharedMesh = mesh;
    }

    string GetPreviewMeshBaseName()
    {
        return _sourceMesh != null
            ? _sourceMesh.name.Replace("_OptimizeSource", string.Empty)
            : gameObject.name;
    }

    static int CountTriangles(Mesh mesh)
    {
        if (mesh == null)
            return 0;

        var count = 0;
        for (var i = 0; i < mesh.subMeshCount; i++)
            count += (int)mesh.GetIndexCount(i);

        return count / 3;
    }

    void MarkSceneDirty()
    {
#if UNITY_EDITOR
        if (Application.isPlaying)
            return;

        EditorUtility.SetDirty(this);
        if (TryGetMeshFilter(out var meshFilter))
            EditorUtility.SetDirty(meshFilter);
        else if (TryGetSkinnedMeshRenderer(out var skinned))
            EditorUtility.SetDirty(skinned);

        if (gameObject.scene.IsValid())
            EditorSceneManager.MarkSceneDirty(gameObject.scene);
#endif
    }

    public static float MapQualityToTriangleRatio(float quality)
    {
        return MapQualityToTriangleRatio(quality, 0, 0);
    }

    public static float MapQualityToTriangleRatio(float quality, int vertexCount, int triangleCount)
    {
        if (quality >= QualityOriginalThreshold)
            return 1f;

        var t = Mathf.Clamp01(quality);
        var minRatio = GetMinTriangleRatioForMesh(vertexCount, triangleCount);

        if (t <= 0f)
            return minRatio;

        var curved = Mathf.Pow(t, LowQualityExponent);
        var ratio = Mathf.Max(minRatio, curved);

        if (triangleCount > 0)
        {
            var targetByQuality = Mathf.RoundToInt(triangleCount * curved);
            var targetByVertices = GetTargetTriangleCountAtZeroQuality(vertexCount);
            var blendedTarget = Mathf.RoundToInt(Mathf.Lerp(targetByVertices, triangleCount, t));
            var absoluteTarget = Mathf.Min(targetByQuality, blendedTarget);
            absoluteTarget = Mathf.Max(MinTrianglesAtZeroQuality, absoluteTarget);
            ratio = Mathf.Min(ratio, (float)absoluteTarget / triangleCount);
            ratio = Mathf.Max(minRatio, ratio);
        }

        return ratio;
    }

    public static int GetTargetTriangleCountAtZeroQuality(int vertexCount)
    {
        if (vertexCount <= 0)
            return MinTrianglesAtZeroQuality;

        if (vertexCount < 500)
            return Mathf.Max(MinTrianglesAtZeroQuality, vertexCount / 6);

        if (vertexCount < 5_000)
            return 16 + vertexCount / 80;

        if (vertexCount < 50_000)
            return 80 + vertexCount / 400;

        if (vertexCount < 200_000)
            return 200 + vertexCount / 1000;

        return 400 + vertexCount / 2500;
    }

    static float GetMinTriangleRatioForMesh(int vertexCount, int triangleCount)
    {
        if (triangleCount <= 0)
            return 0.000001f;

        var minTriangles = GetTargetTriangleCountAtZeroQuality(vertexCount);
        return Mathf.Clamp((float)minTriangles / triangleCount, 0.000001f, 1f);
    }

    MeshSimplifier CreateConfiguredSimplifier(Mesh sourceForScale, float quality)
    {
        var linkDistance = MeshWeldUtility.GetRecommendedWeldDistance(sourceForScale);
        var qualityT = Mathf.Clamp01(quality);
        var iterationBoost = Mathf.RoundToInt(Mathf.Lerp(350f, 100f, qualityT));
        var agressivenessBoost = Mathf.Lerp(15f, 7f, qualityT);
        var preserveBorders = qualityT > 0.15f;

        return new MeshSimplifier
        {
            PreserveBorderEdges = preserveBorders,
            PreserveUVSeamEdges = preserveBorders,
            PreserveUVFoldoverEdges = preserveBorders,
            EnableSmartLink = true,
            VertexLinkDistance = linkDistance,
            MaxIterationCount = iterationBoost,
            Agressiveness = agressivenessBoost
        };
    }

    static void DestroyWorkingMesh(Mesh mesh)
    {
        if (mesh == null)
            return;

#if UNITY_EDITOR
        if (!Application.isPlaying)
            DestroyImmediate(mesh);
        else
#endif
            Destroy(mesh);
    }

    void ReleasePreviewMesh()
    {
        if (_previewMesh == null)
            return;

#if UNITY_EDITOR
        if (!Application.isPlaying)
            DestroyImmediate(_previewMesh);
        else
#endif
            Destroy(_previewMesh);

        _previewMesh = null;
    }

    void ReleaseSourceMesh()
    {
        if (_sourceMesh == null)
            return;

#if UNITY_EDITOR
        if (!Application.isPlaying)
            DestroyImmediate(_sourceMesh);
        else
#endif
            Destroy(_sourceMesh);

        _sourceMesh = null;
    }

#if UNITY_EDITOR
    public void DecimateMesh()
    {
        ApplyOptimization();
    }

    public void NotifyMeshSavedAsAsset(Mesh savedMesh)
    {
        if (savedMesh == null)
            return;

        ReleasePreviewMesh();
        ReleaseSourceMesh();

        _sourceAssetPath = AssetDatabase.GetAssetPath(savedMesh);
        _sourceMeshName = savedMesh.name;
        CaptureSourceFromMesh(savedMesh);
    }

    void OnValidate()
    {
        if (Application.isPlaying || !_livePreview)
            return;

        EditorApplication.delayCall += OnValidateDelayed;
    }

    void OnValidateDelayed()
    {
        if (this == null || !_livePreview)
            return;

        ApplyOptimization();
    }
#endif
}

[Serializable]
public class OptimizeMeshLodSlot
{
    [FormerlySerializedAs("screenHeight")]
    [Range(0.001f, 1f)]
    public float transition = 0.5f;

    [Range(0f, 1f)]
    public float meshQuality = 1f;

    public float GetMeshTriangleRatio(int vertexCount, int triangleCount)
    {
        if (meshQuality >= OptimizeMesh.QualityOriginalThreshold)
            return 1f;

        return OptimizeMesh.MapQualityToTriangleRatio(meshQuality, vertexCount, triangleCount);
    }

    public static OptimizeMeshLodSlot[] CreateDefaults()
    {
        return new[]
        {
            new OptimizeMeshLodSlot { transition = 0.5f, meshQuality = 1f },
            new OptimizeMeshLodSlot { transition = 0.2f, meshQuality = 0.5f },
            new OptimizeMeshLodSlot { transition = 0.08f, meshQuality = 0.25f }
        };
    }
}
