using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace ViConsole.Tree
{
    public interface ITreeNode : IEnumerable<ITreeNode>
    {
        public string Name { get; }
        ITreeNode AddNode(ITreeNode node);
        ITreeNode GetNode(string name);
        bool TryGetNode(string name, out ITreeNode node);
        bool RemoveNode(ITreeNode node);
    }

    public abstract class TreeNode : ITreeNode
    {
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

        public ITreeNode GetNode(string name) => _nodes.FirstOrDefault(node => node.Name == name);

        public bool TryGetNode(string name, out ITreeNode node)
        {
            node = _nodes.FirstOrDefault(node => node.Name == name);
            return node != null;
        }

        public ITreeNode AddNode(ITreeNode node)
        {
            _nodes.Add(node);
            return node;
        }
        
        public bool RemoveNode(ITreeNode node) => _nodes.Remove(node);

        List<ITreeNode> _nodes = new();
        
        public IEnumerator<ITreeNode> GetEnumerator() => _nodes.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}