using System;

namespace ViConsole.Attributes
{
    public class CommandAttribute : Attribute
    {
        public string Name { get; }
        public string Description { get; }
        public bool IsBuiltIn { get; }
        public bool Hide { get; }

        public CommandAttribute(string name, string description = "", bool isBuiltIn = false, bool hide = false)
        {
            Name = name;
            Description = description;
            IsBuiltIn = isBuiltIn;
            Hide = hide;
        }
    }
}