using System.Linq;
using System.Reflection;
using Shared.Providers;
using VContainer;

namespace Shared.Utils
{
    public static class InjectUtilities
    {
        public static void RegisterSettingsProviders(IContainerBuilder builder)
        {
            var providerTypes = Assembly.GetExecutingAssembly()
                .GetTypes()
                .Where(t => t.GetCustomAttribute<SettingsProviderAttribute>() != null &&
                            t.IsClass &&
                            !t.IsAbstract);

            foreach (var providerType in providerTypes)
            {
                var interfaces = providerType.GetInterfaces();

                foreach (var interfaceType in interfaces)
                {
                    builder.Register(providerType, Lifetime.Singleton).AsImplementedInterfaces();
                }
            }
        }
    }
}