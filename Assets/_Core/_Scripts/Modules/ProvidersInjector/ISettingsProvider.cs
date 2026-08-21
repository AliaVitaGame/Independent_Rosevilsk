namespace Shared.Providers
{
    public interface ISettingsProvider
    {
        T GetSettings<T>() where T : class;
    }
}