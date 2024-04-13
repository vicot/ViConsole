using System.Collections.Generic;
using System.Linq;

namespace ViConsole.Graph
{
    public interface IGraphNode
    {
        string Name { get; }
        IGraphNode AddNode(IGraphNode node);
        IGraphNode GetNode(string name);
        bool TryGetNode(string name, out IGraphNode node);
    }

    public abstract class GraphNode : IGraphNode
    {
        protected GraphNode()
        {
            Name = "[unset]";
        } 
        
        public GraphNode(string name)
        {
            Name = name;
        }

        public string Name { get; }

        public IReadOnlyList<IGraphNode> Nodes => _nodes.AsReadOnly();

        public IGraphNode GetNode(string name) => _nodes.FirstOrDefault(node => node.Name == name);

        public bool TryGetNode(string name, out IGraphNode node)
        {
            node = _nodes.FirstOrDefault(node => node.Name == name);
            return node == null;
        }

        public IGraphNode AddNode(IGraphNode node)
        {
            _nodes.Add(node);
            return node;
        }

        List<IGraphNode> _nodes = new();
    }
}