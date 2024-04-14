using System;
using System.Reflection;
using ViConsole.Attributes;

namespace ViConsole.Tree
{
    public interface IPresenterNode : IMethodNode
    {
        PresenterProviderForAttribute Attribute { get; }
        Type Type { get; }
    }

    public class PresenterNode : MethodNode, IPresenterNode
    {
        public PresenterProviderForAttribute Attribute { get; }
        public Type Type { get; }

        public PresenterNode(MethodInfo method, PresenterProviderForAttribute attribute) : base(method)
        {
            Attribute = attribute;
            Type = attribute.ProviderForType;
        }
    }
}