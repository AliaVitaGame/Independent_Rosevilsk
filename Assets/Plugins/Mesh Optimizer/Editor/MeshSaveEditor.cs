using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

public static class MeshSaverEditor
{
    [MenuItem("CONTEXT/MeshFilter/Save Mesh...")]
    public static void SaveMeshInPlace(MenuCommand menuCommand)
    {
        var meshFilter = menuCommand.context as MeshFilter;
        if (meshFilter == null || meshFilter.sharedMesh == null)
            return;

        var mesh = meshFilter.sharedMesh;
        SaveMeshWithDialog(mesh, mesh.name, true);
    }

    [MenuItem("CONTEXT/MeshFilter/Save Mesh As New Instance...")]
    public static void SaveMeshNewInstanceItem(MenuCommand menuCommand)
    {
        var meshFilter = menuCommand.context as MeshFilter;
        if (meshFilter == null || meshFilter.sharedMesh == null)
            return;

        var mesh = meshFilter.sharedMesh;
        SaveMeshWithDialog(mesh, mesh.name, true);
    }

    public static bool IsValidFolderPath(string folderPath)
    {
        if (string.IsNullOrWhiteSpace(folderPath))
            return false;

        return !Regex.IsMatch(folderPath, "[:*?\"<>|]");
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

    public static string GetUniqueAssetPath(string folderPath, string assetName, string extension)
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

    public static Mesh CreateMeshAssetCopy(Mesh source, string assetName)
    {
        var mesh = Object.Instantiate(source);
        mesh.name = MakeSafeAssetName(assetName);
        mesh.hideFlags = HideFlags.None;
        return mesh;
    }

    static void SaveMeshWithDialog(Mesh mesh, string name, bool optimizeMesh)
    {
        var path = EditorUtility.SaveFilePanel("Save Separate Mesh Asset", "Assets/", name, "asset");
        if (string.IsNullOrEmpty(path))
            return;

        path = FileUtil.GetProjectRelativePath(path);
        var meshToSave = CreateMeshAssetCopy(mesh, name);

        if (optimizeMesh)
            MeshUtility.Optimize(meshToSave);

        AssetDatabase.CreateAsset(meshToSave, path);
        AssetDatabase.SaveAssets();
        EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<Mesh>(path));
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
            .Replace("_Readable", string.Empty);
    }
}
