namespace SampleProjectAV.CustomUpmManager.Editor
{
    public sealed class GitRemoteRef
    {
        public GitRemoteRef(string name, string revision, bool isTag)
        {
            Name = name;
            Revision = revision;
            IsTag = isTag;
        }

        public string Name { get; }
        public string Revision { get; }
        public bool IsTag { get; }
    }
}
