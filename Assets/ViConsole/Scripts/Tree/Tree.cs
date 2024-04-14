using System.Linq;
using UnityEngine;
using ViConsole.Attributes;

namespace ViConsole.Tree
{
    public interface ITree
    {
    }

    public class Tree : ITree
    {
        IRootNode _root = new RootNode();

        public IDomainNode GetDomain(Domains domain)
        {
            if (!_root.TryGetNode(domain, out var node))
                node = _root.AddNode(new DomainNode(domain));

            return node;
        }
    }
}