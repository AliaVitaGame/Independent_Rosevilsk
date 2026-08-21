using System;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

#if UNITY_EDITOR
using UnityEditor;
#endif

public static class MeshReadableUtility
{
    public static Mesh EnsureReadable(Mesh mesh)
    {
        if (mesh == null)
            return null;

        if (mesh.isReadable && mesh.vertexCount > 0)
            return mesh;

#if UNITY_EDITOR
        if (MeshImportSettingsUtility.TryEnableReadWriteAndReimport(mesh, out var reimported) &&
            reimported != null &&
            reimported.isReadable &&
            reimported.vertexCount > 0)
        {
            return reimported;
        }
#endif

        return CreateReadableCopy(mesh);
    }

    public static Mesh CreateReadableCopy(Mesh source)
    {
        if (source == null)
            return null;

        if (source.isReadable && source.vertexCount > 0)
        {
            var clone = UnityEngine.Object.Instantiate(source);
            clone.name = source.name + "_Readable";
            return clone;
        }

        try
        {
            using var meshDataArray = AcquireReadOnlyMeshData(source);
            if (meshDataArray.Length == 0)
            {
                Debug.LogWarning("[OptimizeMesh] Failed to read mesh data.");
                return null;
            }

            var readable = CopyMeshDataToMesh(meshDataArray[0], source.name + "_Readable", source.indexFormat);
            if (readable == null || readable.vertexCount == 0)
            {
                if (readable != null)
                    DestroyMesh(readable);

                Debug.LogWarning("[OptimizeMesh] Mesh copy is empty.");
                return null;
            }

            return readable;
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[OptimizeMesh] Failed to create readable mesh copy: {ex.Message}");
            return null;
        }
    }

    static Mesh.MeshDataArray AcquireReadOnlyMeshData(Mesh mesh)
    {
#if UNITY_EDITOR
        return MeshUtility.AcquireReadOnlyMeshData(mesh);
#else
        return Mesh.AcquireReadOnlyMeshData(mesh);
#endif
    }

    static Mesh CopyMeshDataToMesh(Mesh.MeshData src, string meshName, IndexFormat indexFormat)
    {
        var vertexCount = src.vertexCount;
        var mesh = new Mesh
        {
            name = meshName,
            indexFormat = indexFormat
        };

        using (var vertices = new NativeArray<Vector3>(vertexCount, Allocator.Temp))
        {
            src.GetVertices(vertices);
            mesh.SetVertices(vertices);
        }

        if (src.HasVertexAttribute(VertexAttribute.Normal))
        {
            using var normals = new NativeArray<Vector3>(vertexCount, Allocator.Temp);
            src.GetNormals(normals);
            mesh.SetNormals(normals);
        }

        if (src.HasVertexAttribute(VertexAttribute.Tangent))
        {
            using var tangents = new NativeArray<Vector4>(vertexCount, Allocator.Temp);
            src.GetTangents(tangents);
            mesh.SetTangents(tangents);
        }

        if (src.HasVertexAttribute(VertexAttribute.Color))
        {
            using var colors = new NativeArray<Color>(vertexCount, Allocator.Temp);
            src.GetColors(colors);
            mesh.SetColors(colors);
        }

        for (var channel = 0; channel < 8; channel++)
        {
            var texCoord = VertexAttribute.TexCoord0 + channel;
            if (!src.HasVertexAttribute(texCoord))
                break;

            using var uvs = new NativeArray<Vector2>(vertexCount, Allocator.Temp);
            src.GetUVs(channel, uvs);
            mesh.SetUVs(channel, uvs);
        }

        mesh.subMeshCount = src.subMeshCount;
        for (var subMeshIndex = 0; subMeshIndex < src.subMeshCount; subMeshIndex++)
        {
            var subMesh = src.GetSubMesh(subMeshIndex);
            var indexCount = (int)subMesh.indexCount;

            using var indices = new NativeArray<int>(indexCount, Allocator.Temp);
            src.GetIndices(indices, subMeshIndex);

            if (indexFormat == IndexFormat.UInt32)
            {
                mesh.SetIndices(indices, MeshTopology.Triangles, subMeshIndex);
            }
            else
            {
                var indices16 = new ushort[indexCount];
                for (var i = 0; i < indexCount; i++)
                    indices16[i] = (ushort)indices[i];

                mesh.SetIndices(indices16, MeshTopology.Triangles, subMeshIndex);
            }
        }

        mesh.RecalculateBounds();
        return mesh;
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
