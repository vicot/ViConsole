using System.Reflection;
using ViConsole.Attributes;
using ViConsole.Extensions;

namespace ViConsole.Tree
{
    public interface ICommandNode : IMethodNode
    {
        CommandAttribute Attribute { get; }
        string Description { get; }
        string GetHelpText();
    }

    public class CommandNode : MethodNode, ICommandNode
    {
        public CommandAttribute Attribute { get; }
        public string Description { get; }

        public CommandNode(MethodInfo method, CommandAttribute attribute) : base(method, attribute.Name)
        {
            Attribute = attribute;
            Description = attribute.Description;
        }

        public string GetHelpText()
        {
            if (string.IsNullOrEmpty(Description))
                return Name.Decorate(StringDecoration.Bold);
            return $"{Name.Decorate(StringDecoration.Bold)} - {Description.Decorate(StringDecoration.Italic)}";
        }
    }
}