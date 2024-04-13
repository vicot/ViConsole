namespace ViConsole.Graph
{
    public interface IGraph
    {
    }

    public class Graph : IGraph
    {
        IRootNode _root = new RootNode();

        public IDomainNode GetDomain(GraphDomains domain)
        {
            if (!_root.TryGetNode(domain, out var node))
                node = _root.AddNode(new DomainNode(domain));

            return node;
        }
    }
}