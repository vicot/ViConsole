using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using ViConsole.Attributes;
using ViConsole.Extensions;

namespace ViConsole.Tree
{
    public interface IMethodNode : ITreeNode
    {
        bool IsStatic { get; }
        Parameter[] Parameters { get; }
        Type ReturnType { get; }
        MethodInfo Method { get; }
        object Execute(object[] args);
        object Execute(object target, object[] args);
    }

    public delegate IEnumerable<string> AutocompleteProvider(object target);

    public class MethodNode : TreeNode, IMethodNode
    {

        Dictionary<Parameter,AutocompleteProvider> ParameterAutocomplete = new();

        public MethodInfo Method { get; }
        public Type ReturnType { get; }
        public bool IsStatic { get; }
        public Parameter[] Parameters { get; }

        public MethodNode(MethodInfo method, string name) : base(name)
        {
            Method = method;
            IsStatic = Method.IsStatic;
            ReturnType = Method.ReturnType;
            Parameters = Method.GetParameters().Select(p => new Parameter { Name = p.Name, Type = p.ParameterType }).ToArray();
            FindAutocompleteProviders();
        }
        
        public MethodNode(MethodInfo method) : this(method, method.Name)
        {
        }

        void FindAutocompleteProviders()
        {
            var parameterInfos = Method.GetParameters();
            for (var i = 0; i < parameterInfos.Length; i++)
            {
                var parameterInfo = parameterInfos[i];
                var attribute = parameterInfo.GetCustomAttribute<AutocompleteFromAttribute>();
                if (attribute != null)
                {
                    var provider = Method.DeclaringType?.GetMethod(attribute.ProviderName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                    if (provider != null && provider.MatchesDelegate(typeof(AutocompleteProvider)))
                    {
                        ParameterAutocomplete[Parameters[i]] = target => provider.Invoke(target, null) as IEnumerable<string>;
                    }

                    continue;
                }

                if (parameterInfo.ParameterType.IsEnum)
                {
                    ParameterAutocomplete[Parameters[i]] = _ => Enum.GetNames(parameterInfo.ParameterType);
                }
            }
        }

        public IEnumerable<string> GetSuggestionsFor(Parameter parameter, object target)
        {
            if (ParameterAutocomplete.TryGetValue(parameter, out var provider)) return provider.Invoke(target);
            return Enumerable.Empty<string>();
        }

        public object Execute(object[] args) => Execute(null, args);

        public object Execute(object target, object[] args)
        {
            try
            {
                return Method.Invoke(target, args);
            }
            catch (Exception e)
            {
                Debug.LogErrorFormat("Command failed: {0}", e.Message);
                Debug.LogException(e);
            }

            return null;
        }
    }
}