using System;

namespace Framework.Utils
{
    [AttributeUsage(AttributeTargets.Method)]
    public class ButtonAttribute : Attribute
    {
        public string Label { get; }
        public string Group { get; }

        public ButtonAttribute(string label = null, string group = null)
        {
            Label = label;
            Group = group;
        }
    }
}
