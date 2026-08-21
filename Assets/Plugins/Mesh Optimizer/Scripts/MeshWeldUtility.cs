using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Welds coincident vertices so disconnected faces share topology before simplification.
/// </summary>
public static class MeshWeldUtility
{
    const float MinWeldDistance = 1e-7f;

    struct WeldGroup
    {
        public int count;
        public Vector3 positionSum;
        public Vector3 normalSum;
        public Vector4 tangentSum;
        public Color colorSum;
        public Vector4 uv0Sum;
        public bool hasNormal;
        public bool hasTangent;
        public bool hasColor;
        public bool hasUv0;
    }

    public static Mesh WeldVertices(Mesh source, float weldDistance)
    {
        if (source == null)
            return null;

        weldDistance = Mathf.Max(weldDistance, MinWeldDistance);
        var weldDistanceSqr = weldDistance * weldDistance;
        var uvToleranceSqr = weldDistanceSqr;

        var vertices = source.vertices;
        if (vertices == null || vertices.Length == 0)
            return Object.Instantiate(source);

        var normals = source.normals;
        var tangents = source.tangents;
        var colors = source.colors;
        var uv0 = new List<Vector4>(vertices.Length);
        source.GetUVs(0, uv0);

        var hasNormals = normals != null && normals.Length == vertices.Length;
        var hasTangents = tangents != null && tangents.Length == vertices.Length;
        var hasColors = colors != null && colors.Length == vertices.Length;
        var hasUv0 = uv0.Count == vertices.Length;

        var cellSize = weldDistance;
        var remap = new int[vertices.Length];
        var groups = new Dictionary<Vector3Int, List<int>>(vertices.Length);
        var neighborOffsets = new[]
        {
            new Vector3Int(0, 0, 0),
            new Vector3Int(-1, 0, 0), new Vector3Int(1, 0, 0),
            new Vector3Int(0, -1, 0), new Vector3Int(0, 1, 0),
            new Vector3Int(0, 0, -1), new Vector3Int(0, 0, 1),
            new Vector3Int(-1, -1, 0), new Vector3Int(-1, 1, 0), new Vector3Int(1, -1, 0), new Vector3Int(1, 1, 0),
            new Vector3Int(-1, 0, -1), new Vector3Int(-1, 0, 1), new Vector3Int(1, 0, -1), new Vector3Int(1, 0, 1),
            new Vector3Int(0, -1, -1), new Vector3Int(0, -1, 1), new Vector3Int(0, 1, -1), new Vector3Int(0, 1, 1),
            new Vector3Int(-1, -1, -1), new Vector3Int(-1, -1, 1), new Vector3Int(-1, 1, -1), new Vector3Int(-1, 1, 1),
            new Vector3Int(1, -1, -1), new Vector3Int(1, -1, 1), new Vector3Int(1, 1, -1), new Vector3Int(1, 1, 1)
        };

        Vector3Int CellKey(Vector3 p)
        {
            return new Vector3Int(
                Mathf.FloorToInt(p.x / cellSize),
                Mathf.FloorToInt(p.y / cellSize),
                Mathf.FloorToInt(p.z / cellSize));
        }

        bool CanMerge(int existingIndex, int candidateIndex)
        {
            var delta = vertices[candidateIndex] - vertices[existingIndex];
            if (delta.sqrMagnitude > weldDistanceSqr)
                return false;

            if (hasUv0)
            {
                var uvDelta = (Vector2)(uv0[candidateIndex] - uv0[existingIndex]);
                if (uvDelta.sqrMagnitude > uvToleranceSqr)
                    return false;
            }

            return true;
        }

        for (var i = 0; i < vertices.Length; i++)
        {
            var key = CellKey(vertices[i]);
            var merged = false;

            for (var n = 0; n < neighborOffsets.Length; n++)
            {
                var neighborKey = key + neighborOffsets[n];
                if (!groups.TryGetValue(neighborKey, out var bucket))
                    continue;

                for (var j = 0; j < bucket.Count; j++)
                {
                    var existing = bucket[j];
                    if (!CanMerge(existing, i))
                        continue;

                    remap[i] = remap[existing];
                    merged = true;
                    break;
                }

                if (merged)
                    break;
            }

            if (merged)
                continue;

            if (!groups.TryGetValue(key, out var ownBucket))
            {
                ownBucket = new List<int>(4);
                groups[key] = ownBucket;
            }

            ownBucket.Add(i);
            remap[i] = i;
        }

        var weldedGroups = new WeldGroup[vertices.Length];
        for (var i = 0; i < vertices.Length; i++)
        {
            var root = remap[i];
            ref var group = ref weldedGroups[root];
            group.count++;
            group.positionSum += vertices[i];

            if (hasNormals)
            {
                group.hasNormal = true;
                group.normalSum += normals[i];
            }

            if (hasTangents)
            {
                group.hasTangent = true;
                group.tangentSum += tangents[i];
            }

            if (hasColors)
            {
                group.hasColor = true;
                group.colorSum += colors[i];
            }

            if (hasUv0)
            {
                group.hasUv0 = true;
                group.uv0Sum += uv0[i];
            }
        }

        var compactMap = new Dictionary<int, int>(vertices.Length);
        var newVertices = new List<Vector3>(vertices.Length);
        var newNormals = hasNormals ? new List<Vector3>(vertices.Length) : null;
        var newTangents = hasTangents ? new List<Vector4>(vertices.Length) : null;
        var newColors = hasColors ? new List<Color>(vertices.Length) : null;
        var newUv0 = hasUv0 ? new List<Vector4>(vertices.Length) : null;

        for (var i = 0; i < vertices.Length; i++)
        {
            if (remap[i] != i)
                continue;

            ref var group = ref weldedGroups[i];
            var inv = 1f / group.count;
            compactMap[i] = newVertices.Count;
            newVertices.Add(group.positionSum * inv);

            if (group.hasNormal)
            {
                var n = group.normalSum * inv;
                if (n.sqrMagnitude > 1e-12f)
                    n.Normalize();
                newNormals.Add(n);
            }

            if (group.hasTangent)
            {
                var t = group.tangentSum * inv;
                if (Mathf.Abs(t.w) < 1e-6f)
                    t.w = 1f;
                if (t.sqrMagnitude > 1e-12f)
                {
                    var mag = Mathf.Sqrt(t.x * t.x + t.y * t.y + t.z * t.z);
                    if (mag > 1e-12f)
                        t = new Vector4(t.x / mag, t.y / mag, t.z / mag, t.w >= 0f ? 1f : -1f);
                }
                newTangents.Add(t);
            }

            if (group.hasColor)
                newColors.Add(group.colorSum * inv);

            if (group.hasUv0)
                newUv0.Add(group.uv0Sum * inv);
        }

        for (var i = 0; i < vertices.Length; i++)
        {
            if (remap[i] == i)
                continue;

            remap[i] = compactMap[remap[i]];
        }

        for (var i = 0; i < vertices.Length; i++)
        {
            if (remap[i] == i)
                remap[i] = compactMap[i];
        }

        var welded = Object.Instantiate(source);
        welded.name = source.name + "_Welded";
        welded.Clear(false);

        welded.vertices = newVertices.ToArray();
        if (newNormals != null)
            welded.normals = newNormals.ToArray();
        if (newTangents != null)
            welded.tangents = newTangents.ToArray();
        if (newColors != null)
            welded.colors = newColors.ToArray();
        if (newUv0 != null)
            welded.SetUVs(0, newUv0);

        welded.subMeshCount = source.subMeshCount;
        for (var subMesh = 0; subMesh < source.subMeshCount; subMesh++)
        {
            var triangles = source.GetTriangles(subMesh);
            var compactTriangles = new List<int>(triangles.Length);
            for (var t = 0; t < triangles.Length; t += 3)
            {
                var i0 = remap[triangles[t]];
                var i1 = remap[triangles[t + 1]];
                var i2 = remap[triangles[t + 2]];
                if (i0 == i1 || i1 == i2 || i0 == i2)
                    continue;

                compactTriangles.Add(i0);
                compactTriangles.Add(i1);
                compactTriangles.Add(i2);
            }

            welded.SetTriangles(compactTriangles, subMesh, false);
        }

        welded.bindposes = source.bindposes;
        welded.RecalculateBounds();
        if (!hasNormals)
            welded.RecalculateNormals();

        return welded;
    }

    public static float GetRecommendedWeldDistance(Mesh mesh, float relativeEpsilon = 1e-5f)
    {
        if (mesh == null)
            return MinWeldDistance;

        var size = mesh.bounds.size.magnitude;
        if (size <= 1e-8f)
            return MinWeldDistance;

        return Mathf.Max(size * relativeEpsilon, MinWeldDistance);
    }
}
