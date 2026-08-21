namespace Unity.Multiplayer.Playmode.VirtualProjects.Editor
{
    static class VirtualProjectsEditor
    {
        public static bool IsClone
            => CommandLineParameters.ReadIsClone();

        public static VirtualProjectIdentifier CloneIdentifier
            => CommandLineParameters.ReadVirtualProjectIdentifier();

        public static bool IsScenarioClone
            => CommandLineParameters.ReadIsScenarioClone();

        public static string MainEditorProcessId
            => CommandLineParameters.ReadIsClone()
                ? CommandLineParameters.ReadMainProcessId()
                : System.Diagnostics.Process.GetCurrentProcess().Id.ToString();
    }
}
