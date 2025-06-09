using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine.UIElements;
using ViConsole.Attributes;
using ViConsole.Extensions;

namespace ViConsole.Tree
{
    public interface ICommandNode : IMethodNode
    {
        CommandAttribute Attribute { get; }
        string Description { get; }
        string GetHelpText();
        string GetSyntaxHint(int highlightPosition);
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

        public string GetSyntaxHint(int highlightPosition)
        {
            var parts = new List<string>();
            var name = Name;
            if (highlightPosition == 0) name = name.Decorate(StringDecoration.Bold);
            parts.Add(name);
            var parameters = Parameters.Select(p => $"{p.Name}:{p.Type.Name}").ToList();
            for (var i = 0; i < parameters.Count; i++)
            {
                var parameter = parameters[i];
                if (highlightPosition == i + 1) parameter = parameter.Decorate(StringDecoration.Bold);
                parts.Add(parameter);
            }

            return string.Join(" ", parts);
        }
    }
}