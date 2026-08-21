using System;
using System.Reflection;

namespace GameTest
{
    public sealed class CheatCodeMeta
    {
        public CheatCodeMeta(Type declaringType, MethodInfo method, string readableName, string category)
        {
            DeclaringType = declaringType;
            Method = method;
            ReadableName = readableName;
            Category = category;
        }

        public Type DeclaringType { get; }
        public MethodInfo Method { get; }
        public string ReadableName { get; }
        public string Category { get; }
    }
}
