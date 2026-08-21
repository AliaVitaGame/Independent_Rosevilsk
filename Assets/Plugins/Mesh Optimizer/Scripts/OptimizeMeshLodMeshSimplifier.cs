using System;
using IndMeshSimplifier;
using UnityEngine;

public static class OptimizeMeshLodMeshSimplifier
{
    public static Mesh Simplify(Mesh sourceMesh, float meshQuality, int sourceVertexCount, int sourceTriangleCount)
    {
        if (sourceMesh == null)
            return null;

        var readableMesh = MeshReadableUtility.EnsureReadable(sourceMesh);
        if (readableMesh == null || readableMesh.vertexCount == 0)
        {
            Debug.LogWarning("[OptimizeMesh] LOD simplify failed: source mesh is not readable.");
            return null;
        }

        if (meshQuality >= OptimizeMesh.QualityOriginalThreshold)
        {
            var originalCopy = UnityEngine.Object.Instantiate(readableMesh);
            originalCopy.name = readableMesh.name + "_LOD";
            return originalCopy;
        }

        var vertexCount = sourceVertexCount > 0 ? sourceVertexCount : readableMesh.vertexCount;
        var triangleCount = sourceTriangleCount > 0 ? sourceTriangleCount : CountTriangles(readableMesh);
        var simplifyRatio = OptimizeMesh.MapQualityToTriangleRatio(meshQuality, vertexCount, triangleCount);

        Mesh workingMesh = null;
        Mesh weldedMesh = null;

        try
        {
            workingMesh = UnityEngine.Object.Instantiate(readableMesh);
            workingMesh.name = readableMesh.name + "_LOD_Working";

            var weldDistance = MeshWeldUtility.GetRecommendedWeldDistance(workingMesh);
            weldedMesh = MeshWeldUtility.WeldVertices(workingMesh, weldDistance);
            DestroyMesh(workingMesh);
            workingMesh = weldedMesh;
            weldedMesh = null;

            var meshSimplifier = CreateSimplifier(readableMesh, meshQuality);
            meshSimplifier.Initialize(workingMesh);
            meshSimplifier.SimplifyMesh(simplifyRatio);

            var resultMesh = meshSimplifier.ToMesh();
            resultMesh.name = readableMesh.name + $"_LOD_Q{meshQuality:F2}";
            resultMesh.bindposes = readableMesh.bindposes;
            return resultMesh;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[OptimizeMesh] LOD simplify failed: {ex.Message}");
            Debug.LogException(ex);
            return null;
        }
        finally
        {
            DestroyMesh(workingMesh);
            DestroyMesh(weldedMesh);
        }
    }

    static MeshSimplifier CreateSimplifier(Mesh sourceForScale, float meshQuality)
    {
        var linkDistance = MeshWeldUtility.GetRecommendedWeldDistance(sourceForScale);
        var qualityT = Mathf.Clamp01(meshQuality);
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

    static int CountTriangles(Mesh mesh)
    {
        if (mesh == null)
            return 0;

        var count = 0;
        for (var i = 0; i < mesh.subMeshCount; i++)
            count += (int)mesh.GetIndexCount(i);

        return count / 3;
    }

    static void DestroyMesh(Mesh mesh)
    {
        if (mesh == null)
            return;

#if UNITY_EDITOR
        if (!Application.isPlaying)
            UnityEngine.Object.DestroyImmediate(mesh);
        else
#endif
            UnityEngine.Object.Destroy(mesh);
    }
}
