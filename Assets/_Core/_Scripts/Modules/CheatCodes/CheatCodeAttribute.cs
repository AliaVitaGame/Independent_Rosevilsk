using System;

namespace GameTest
{
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class CheatCodeAttribute : Attribute
    {
        public CheatCodeAttribute(string readableName = null, string category = null, int order = 0, int categoryOrder = 0)
        {
            ReadableName = readableName;
            Category = category;
            Order = order;
            CategoryOrder = categoryOrder;
        }

        public string ReadableName { get; }
        public string Category { get; }
        public int Order { get; }
        public int CategoryOrder { get; }
    }
}
