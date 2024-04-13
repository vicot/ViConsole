namespace ViConsole.Graph
{
    public interface IDomainNode : IGraphNode
    {
    }

    public class DomainNode : GraphNode, IDomainNode
    {
        public DomainNode(GraphDomains domain) : base(domain.ToString())
        {
        }
    }
}