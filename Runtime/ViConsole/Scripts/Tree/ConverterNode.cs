using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using ViConsole.Attributes;

namespace ViConsole.Tree
{
    public interface IConverterNode : IMethodNode
    {
        ValueConverterAttribute Attribute { get; }
        bool TryConvert(object value, out object convertedValue, Type parameterType, Tree commandTree);
    }

    public class ConverterNode : MethodNode, IConverterNode
    {
        public ValueConverterAttribute Attribute { get; }

        public ConverterNode(MethodInfo method, ValueConverterAttribute attribute) : base(method)
        {
            Attribute = attribute;
        }

        public bool TryConvert(object value, out object convertedValue, Type parameterType, Tree commandTree)
        {
            if (value.GetType().IsAssignableFrom(Attribute.SourceType))
            {
                var argsList = new List<object>();
                argsList.Add(value);
                argsList.Add(null);

                foreach (var parameter in Method.GetParameters().Skip(2))
                {
                    if (parameter.ParameterType == typeof(Tree) && Attribute.Builtin)
                        argsList.Add(commandTree);
                    else if (parameter.ParameterType == typeof(Type))
                        argsList.Add(parameterType);
                }

                if (argsList.Count == Method.GetParameters().Length)
                {
                    var args = argsList.ToArray();

                    var result = Method.Invoke(null, args);
                    if (result is true)
                    {
                        convertedValue = args[1];
                        return true;
                    }
                }
                else
                {
                    Debug.LogError("Invalid signature for converter method {Method.Name}");
                }
            }

            convertedValue = null;
            return false;
        }
    }
}