using System;

namespace ViConsole.Attributes
{
    [AttributeUsage(AttributeTargets.Method)]
    public class ValueConverterAttribute : Attribute
    {
        public Type TargetType { get; }
        public Type SourceType { get; }
        public bool Builtin { get; }

        public ValueConverterAttribute(Type targetType) : this(targetType, typeof(string), false)
        {
        }

        public ValueConverterAttribute(Type targetType, Type sourceType) : this(targetType, sourceType, false)
        {
        }

        internal ValueConverterAttribute(Type targetType, Type sourceType, bool builtin = false)
        {
            TargetType = targetType;
            SourceType = sourceType;
            Builtin = builtin;
        }
    }
}