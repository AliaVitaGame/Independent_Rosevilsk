using System.Runtime.CompilerServices;

#if UNITY_INCLUDE_TESTS
[assembly: InternalsVisibleTo("Unity.Services.Multiplay.Authoring.Tests.Editor")]
[assembly: InternalsVisibleTo("Unity.Services.Multiplay.Authoring.IntegrationTests.EditMode")]
[assembly: InternalsVisibleTo("InternalsVisible.DynamicProxyGenAssembly2")]
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]
#endif

// Needed to enable record types
namespace System.Runtime.CompilerServices
{
    static class IsExternalInit {}
}
