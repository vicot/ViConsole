using System.Reflection;
using ViConsole.Attributes;

namespace ViConsole.Tree
{
    public interface IDomainNode : ITreeNode
    {
        bool IsEnabled { get; set; }
        void RegisterCommand(MethodInfo methodInfo, CommandAttribute attribute);
        bool TryGetCommand(string name, out ICommandNode commandNode);
        void RegisterVariable(string name, object value);
        bool TryGetVariable(string name, out IVariableNode commandNode);
    }

    public class DomainNode : TreeNode, IDomainNode
    {
        public bool IsEnabled { get; set; }

        public DomainNode(Domains domain) : base(domain.ToString())
        {
        }

        public void RegisterCommand(MethodInfo methodInfo, CommandAttribute attribute)
        {
            var node = new CommandNode(methodInfo, attribute);
            AddNode(node);
        }

        public bool TryGetCommand(string name, out ICommandNode commandNode) => TryGet(name, out commandNode);
        
        public void RegisterVariable(string name, object value)
        {
            if (TryGetVariable(name, out var node))
            {
                //if (value == null) RemoveNode(node);
                node.Value = value;
            }
            else
            {
                //if (value != null)
                node = new VariableNode(name, value);
                AddNode(node);
            }
        }
        
        public bool TryGetVariable(string name, out IVariableNode commandNode) => TryGet(name, out commandNode);
        
        bool TryGet<T>(string name, out T commandNode) where T : class
        {
            var result = TryGetNode(name, out var node);
            commandNode = node as T;
            return result && commandNode != null;
        }
    }
}