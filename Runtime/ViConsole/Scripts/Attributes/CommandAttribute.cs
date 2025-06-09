using System;

namespace ViConsole.Attributes
{
    public class CommandAttribute : Attribute
    {
        public string Name { get; }
        public string Description { get; }
        internal bool IsBuiltIn { get; }
        public bool Hide { get; }
        public Type[] ConvertersFor { get; }

        public CommandAttribute(string name, string description = "", bool hide = false, params Type[] convertersFor) : this(name, description, false, hide, convertersFor) { }
        
        internal CommandAttribute(string name, string description = "", bool isBuiltIn = false, bool hide = false, params Type[] convertersFor) 
        {
            Name = name;
            Description = description;
            IsBuiltIn = isBuiltIn;
            Hide = hide;
            ConvertersFor = convertersFor;
        }
    }
}