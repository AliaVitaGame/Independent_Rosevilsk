using System.IO;
using UnityEditor;
using UnityEngine;

namespace Unity.Services.Multiplay.Authoring.Editor.Assets
{
    static class MultiplayConfigResource
    {
        public const string FileExtension = ".gsh";
        const string k_TexturePath = "DefaultAsset Icon";
        public static readonly Texture2D Icon = (Texture2D)EditorGUIUtility.IconContent(k_TexturePath).image;
        public static readonly string MonoDefinitionPath = Path.Combine(
            MultiplayAuthoring.RootPath,
            "Assets",
            "MultiplayConfigAsset.cs");
    }
}
