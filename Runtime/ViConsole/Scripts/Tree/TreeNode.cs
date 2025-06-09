using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace ViConsole.Tree
{
    public interface ITreeNode : IEnumerable<ITreeNode>
    {
        ITreeNode Parent { get; set; }
        public string Name { get; }
        IReadOnlyList<ITreeNode> Nodes { get; }
        ITreeNode AddNode(ITreeNode node);
        ITreeNode GetNode(string name);
        bool TryGetNode(string name, out ITreeNode node);
        bool RemoveNode(ITreeNode node);
    }

    public abstract class TreeNode : ITreeNode
    {
        public ITreeNode Parent { get; set; }

        protected TreeNode()
        {
            Name = "[unset]";
        }

        public TreeNode(string name)
        {
            Name = name;
        }

        public string Name { get; }

        public IReadOnlyList<ITreeNode> Nodes => _nodes.AsReadOnly();

        protected IRootNode Root
        {
            get
            {
                ITreeNode node = this;
                while (node.Parent != null) node = node.Parent;
                return node as IRootNode;
            }
        }

        public ITreeNode GetNode(string name) => _nodes.FirstOrDefault(node => node.Name == name);

        public bool TryGetNode(string name, out ITreeNode node)
        {
            node = _nodes.FirstOrDefault(node => node.Name == name);
            return node != null;
        }

        public ITreeNode AddNode(ITreeNode node)
        {
            node.Parent = this;
            _nodes.Add(node);
            return node;
        }

        public bool RemoveNode(ITreeNode node) => _nodes.Remove(node);

        List<ITreeNode> _nodes = new();

        public IEnumerator<ITreeNode> GetEnumerator() => _nodes.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}