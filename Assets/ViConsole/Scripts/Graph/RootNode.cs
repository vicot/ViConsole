namespace ViConsole.Graph
{
    public interface IRootNode : IDomainNode
    {
        IDomainNode AddNode(IDomainNode node);
        bool TryGetNode(GraphDomains domain, out IDomainNode node);
    }

    public class RootNode : DomainNode, IRootNode
    {
        public RootNode() : base(GraphDomains.Root)
        {
        }

        public IDomainNode AddNode(IDomainNode node) => base.AddNode(node) as IDomainNode;

        public bool TryGetNode(GraphDomains domain, out IDomainNode domainNode)
        {
            var result = base.TryGetNode(domain.ToString(), out var node);
            domainNode = node as IDomainNode;
            return result && node != null;
        }
    }
}