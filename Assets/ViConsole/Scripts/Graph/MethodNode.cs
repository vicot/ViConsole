using System;
using System.Linq;
using System.Reflection;

namespace ViConsole.Graph
{
    public interface IMethodNode : IGraphNode
    {
    }

    public class MethodNode : GraphNode, IMethodNode
    {
        readonly MethodInfo _method;

        public bool IsStatic { get; }
        public Parameter[] Parameters { get; }

        public MethodNode(MethodInfo method) : base(method.Name)
        {
            _method = method;
            IsStatic = _method.IsStatic;
            Parameters = _method.GetParameters().Select(p => new Parameter { Name = p.Name, Type = p.ParameterType }).ToArray();
        }

        public object Execute(object[] args) => Execute(null, args);
        
        public object Execute(object target, object[] args)
        {
            _method.Invoke(null, args);
        }
    }
}