using System.IO;

namespace Unity.Services.Multiplay.Authoring.Editor
{
    static class MultiplayAuthoring
    {
        const string k_PackageName = "com.unity.services.multiplay";
        public static readonly string RootPath = Path.Combine(
            "Packages",
            k_PackageName,
            "Editor",
            "Authoring");
    }
}
