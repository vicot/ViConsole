using System;

namespace ViConsole.Tree
{
    public struct Parameter : IEquatable<Parameter>
    {
        public string Name;
        public Type Type;

        public bool Equals(Parameter other)
        {
            return Name == other.Name && Type == other.Type;
        }

        public override bool Equals(object obj)
        {
            return obj is Parameter other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Name, Type);
        }
    }
}