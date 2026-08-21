#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class MeshImportSettingsUtility
{
    public static bool TryEnableReadWriteAndReimport(Mesh mesh, out Mesh refreshedMesh)
    {
        refreshedMesh = mesh;
        if (mesh == null)
            return false;

        var path = AssetDatabase.GetAssetPath(mesh);
        if (string.IsNullOrEmpty(path))
            return false;

        var changed = false;

        var modelImporter = AssetImporter.GetAtPath(path) as ModelImporter;
        if (modelImporter != null)
        {
            if (!modelImporter.isReadable)
            {
                modelImporter.isReadable = true;
                changed = true;
            }

            if (changed)
            {
                modelImporter.SaveAndReimport();
                AssetDatabase.Refresh();
                Debug.Log($"[OptimizeMesh] Read/Write Enabled turned on and asset reimported: {path}");
            }

            refreshedMesh = FindMeshSubAsset(path, mesh.name) ?? mesh;
            return true;
        }

        if (TrySetMeshAssetReadable(mesh))
        {
            refreshedMesh = mesh;
            return true;
        }

        return false;
    }

    static bool TrySetMeshAssetReadable(Mesh mesh)
    {
        var path = AssetDatabase.GetAssetPath(mesh);
        if (string.IsNullOrEmpty(path) || !path.EndsWith(".asset"))
            return false;

        var serialized = new SerializedObject(mesh);
        var readable = serialized.FindProperty("m_IsReadable");
        if (readable == null || readable.boolValue)
            return false;

        readable.boolValue = true;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(mesh);
        AssetDatabase.SaveAssets();
        return true;
    }

    public static Mesh LoadMeshFromAsset(string assetPath, string meshName)
    {
        if (string.IsNullOrEmpty(assetPath))
            return null;

        if (!string.IsNullOrEmpty(meshName))
        {
            var subAssets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
            foreach (var subAsset in subAssets)
            {
                if (subAsset is Mesh meshAsset && meshAsset.name == meshName)
                    return meshAsset;
            }
        }

        return AssetDatabase.LoadAssetAtPath<Mesh>(assetPath);
    }

    static Mesh FindMeshSubAsset(string assetPath, string meshName)
    {
        return LoadMeshFromAsset(assetPath, meshName);
    }
}
#endif
