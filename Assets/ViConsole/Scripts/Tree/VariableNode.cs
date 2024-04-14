namespace ViConsole.Tree
{
    public interface IVariableNode : ITreeNode
    {
        object Value { get; set; }
    }
    public class VariableNode : TreeNode, IVariableNode
    {
        public object Value { get; set; }

        public VariableNode(string name, object value) : base(name)
        {
            Value = value;
        }
    }
}