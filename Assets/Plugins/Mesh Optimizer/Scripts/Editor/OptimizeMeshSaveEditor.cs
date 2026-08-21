#if UNITY_EDITOR
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class OptimizeMeshSaveEditor
{
    public static bool IsValidFolderPath(string folderPath)
    {
        if (string.IsNullOrWhiteSpace(folderPath))
            return false;

        return !Regex.IsMatch(folderPath, "[:*?\"<>|]");
    }

    public static string SaveMeshForOptimizeMesh(OptimizeMesh optimizeMesh, Mesh mesh)
    {
        if (optimizeMesh == null)
            return null;

        var meshToSave = optimizeMesh.GetAssignedMesh() ?? mesh;
        if (meshToSave == null)
        {
            Debug.LogWarning("[OptimizeMesh] No mesh to save.", optimizeMesh);
            return null;
        }

        var folderPath = optimizeMesh.SaveMeshFolderPath;
        string assetPath;

        if (EditorUtility.IsPersistent(meshToSave) && AssetDatabase.Contains(meshToSave))
        {
            assetPath = AssetDatabase.GetAssetPath(meshToSave);
            folderPath = GetFolderPathFromAssetPath(assetPath);
        }
        else
        {
            var assetName = MakeSafeAssetName(meshToSave.name);
            if (string.IsNullOrWhiteSpace(assetName))
                assetName = MakeSafeAssetName(optimizeMesh.gameObject.name) + "_Optimized";

            var meshAsset = CreateMeshAssetCopy(meshToSave, assetName);
            MeshUtility.Optimize(meshAsset);

            folderPath = EnsureAssetFolder(folderPath);
            assetPath = GetUniqueAssetPath(folderPath, assetName, ".asset");

            AssetDatabase.CreateAsset(meshAsset, assetPath);
            AssetDatabase.SaveAssets();

            Debug.Log($"[OptimizeMesh] Mesh saved: {assetPath}", optimizeMesh);
        }

        var savedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(assetPath);
        if (savedMesh == null)
        {
            Debug.LogWarning("[OptimizeMesh] Failed to load saved mesh asset.", optimizeMesh);
            return folderPath;
        }

        AssignMeshToTarget(optimizeMesh, savedMesh);
        optimizeMesh.NotifyMeshSavedAsAsset(savedMesh);
        optimizeMesh.SaveMeshFolderPath = folderPath;

        EditorUtility.SetDirty(optimizeMesh);
        EditorGUIUtility.PingObject(savedMesh);

        return folderPath;
    }

    public static string EnsureAssetFolder(string folderPath)
    {
        folderPath = folderPath.Replace('\\', '/').Trim('/');
        if (string.IsNullOrWhiteSpace(folderPath))
            folderPath = "GeneratedLODs/Meshes";

        if (AssetDatabase.IsValidFolder("Assets/" + folderPath))
            return folderPath;

        var folderNames = folderPath
            .Split('/')
            .Where(part => !string.IsNullOrWhiteSpace(part))
            .Select(part => part.Trim())
            .ToArray();

        var currentPath = "Assets";
        foreach (var folderName in folderNames)
        {
            var nextPath = currentPath + "/" + folderName;
            if (!AssetDatabase.IsValidFolder(nextPath))
                AssetDatabase.CreateFolder(currentPath, folderName);

            currentPath = nextPath;
        }

        return folderPath;
    }

    static string GetFolderPathFromAssetPath(string assetPath)
    {
        if (string.IsNullOrWhiteSpace(assetPath))
            return "GeneratedLODs/Meshes";

        var directory = Path.GetDirectoryName(assetPath)?.Replace('\\', '/');
        if (string.IsNullOrWhiteSpace(directory))
            return "GeneratedLODs/Meshes";

        const string assetsPrefix = "Assets/";
        if (directory.StartsWith(assetsPrefix))
            directory = directory.Substring(assetsPrefix.Length);

        return directory;
    }

    static string GetUniqueAssetPath(string folderPath, string assetName, string extension)
    {
        var assetPath = "Assets/" + folderPath + "/" + assetName + extension;
        var assetNumber = 1;

        while (AssetDatabase.LoadAssetAtPath<Object>(assetPath) != null)
        {
            assetPath = "Assets/" + folderPath + "/" + assetName + " (" + assetNumber + ")" + extension;
            assetNumber++;
        }

        return assetPath;
    }

    static Mesh CreateMeshAssetCopy(Mesh source, string assetName)
    {
        var mesh = Object.Instantiate(source);
        mesh.name = MakeSafeAssetName(assetName);
        mesh.hideFlags = HideFlags.None;
        return mesh;
    }

    static void AssignMeshToTarget(OptimizeMesh optimizeMesh, Mesh savedMesh)
    {
        if (savedMesh == null || !optimizeMesh.TryGetMeshAssignmentTarget(out var meshFilter, out var skinnedMeshRenderer))
        {
            Debug.LogWarning("[OptimizeMesh] No MeshFilter or SkinnedMeshRenderer found to assign saved mesh.", optimizeMesh);
            return;
        }

        if (meshFilter != null)
        {
            Undo.RecordObject(meshFilter, "Assign Optimized Mesh");
            meshFilter.sharedMesh = savedMesh;
            EditorUtility.SetDirty(meshFilter);
        }
        else if (skinnedMeshRenderer != null)
        {
            Undo.RecordObject(skinnedMeshRenderer, "Assign Optimized Mesh");
            skinnedMeshRenderer.sharedMesh = savedMesh;
            EditorUtility.SetDirty(skinnedMeshRenderer);
        }

        EditorUtility.SetDirty(optimizeMesh);
        EditorUtility.SetDirty(optimizeMesh.gameObject);

        if (optimizeMesh.gameObject.scene.IsValid())
            EditorSceneManager.MarkSceneDirty(optimizeMesh.gameObject.scene);
    }

    static string MakeSafeAssetName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return "OptimizedMesh";

        foreach (var invalidChar in Path.GetInvalidFileNameChars())
            name = name.Replace(invalidChar, '_');

        return name.Replace(' ', '_')
            .Replace("_Optimized_Optimized", "_Optimized")
            .Replace("_Preview", string.Empty)
            .Replace("_Readable", string.Empty)
            .Replace("_Working", string.Empty)
            .Replace("_LOD", string.Empty);
    }
}
#endif
