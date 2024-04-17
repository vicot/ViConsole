using System;

namespace ViConsole.Tree
{
    public interface ITypeNode : ITreeNode
    {
        Type Type { get; }
        string ShortName { get; }
        string FullName { get; }
    }

    public class TypeNode : TreeNode, ITypeNode
    {
        public Type Type { get; }
        public string ShortName { get; }
        public string FullName { get; }

        public TypeNode(Type type) : base(type.FullName)
        {
            Type = type;
            ShortName = Type.Name;
            FullName = Type.FullName;
        }
    }
}