namespace Game.Shared.Bootstrap
{
    public static class BootstrapPlayModeSettings
    {
        private const string Key = "BootstrapPlayMode_Enabled";

        public static bool IsPlayFromBootstrapEnabled()
        {
#if UNITY_EDITOR
            return UnityEditor.EditorPrefs.GetBool(Key, true);
#else
            return true;
#endif
        }

#if UNITY_EDITOR
        public static void SetEnabled(bool value) => UnityEditor.EditorPrefs.SetBool(Key, value);
#endif
    }
}
