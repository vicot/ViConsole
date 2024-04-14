namespace ViConsole.Tree
{
    public interface IRootNode : ITreeNode
    {
        IDomainNode AddNode(IDomainNode node);
        bool TryGetNode(Domains domain, out IDomainNode node);
    }

    public class RootNode : TreeNode, IRootNode
    {
        public RootNode() : base("[root]")
        {
        }

        public IDomainNode AddNode(IDomainNode node) => base.AddNode(node) as IDomainNode;

        public bool TryGetNode(Domains domain, out IDomainNode domainNode)
        {
            var result = base.TryGetNode(domain.ToString(), out var node);
            domainNode = node as IDomainNode;
            return result && domainNode != null;
        }
    }
}