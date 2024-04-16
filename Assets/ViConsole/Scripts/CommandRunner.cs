﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using ViConsole.Attributes;
using ViConsole.Extensions;
using ViConsole.Parsing;
using ViConsole.Tree;
using Object = UnityEngine.Object;

namespace ViConsole
{
    public interface ICommandRunner
    {
        Task Initialize(AddMessage addMessage);
        object ExecuteCommand(IEnumerable<Token> tokens);
        Tree.Tree CommandTree { get; }
    }

    public delegate void AddMessage(string message, LogType level);

    public class CommandRunner : ICommandRunner
    {
        AddMessage _addMessage;
        public Tree.Tree CommandTree { get; } = new();

        Assembly _rootAssembly;

        public async Task Initialize(AddMessage addMessage)
        {
            _addMessage = addMessage;
            _rootAssembly = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(a => a != null && a.FullName.StartsWith("Assembly-CSharp") && !a.FullName.StartsWith("Assembly-CSharp-Editor"));
            await Task.Run(Discover);
            _addMessage("Console initialized", LogType.Log);
        }

        public object ExecuteCommand(IEnumerable<Token> tokens)
        {
            var vars = CommandTree.GetDomain(Domains.Variables);
            var globals = CommandTree.GetDomain(Domains.Globals);
            var commands = CommandTree.GetDomain(Domains.Commands);

            var postfix = Parser.ConvertToPostfix(tokens);
            if (postfix == null) return null;
            var operandStack = new Stack<object>();

            foreach (var token in postfix)
            {
                switch (token.Type)
                {
                    case LexemeType.Identifier:
                    {
                        if (!vars.TryGetVariable(token.Lexeme.Value, out var variable))
                            throw new CommandException("Unknown variable", token);
                        operandStack.Push(variable.Value);
                        break;
                    }
                    case LexemeType.SpecialIdentifier:
                        if (!globals.TryGetVariable(token.Lexeme.Value, out var global))
                            throw new CommandException("Unknown global", token);
                        operandStack.Push(global.Value);
                        break;

                    case LexemeType.Command:
                        if (!commands.TryGetCommand(token.Lexeme.Value, out var command))
                            throw new CommandException("Unknown command", token);
                        var args = new object[command.Parameters.Length];
                        for (var i = command.Parameters.Length - 1; i >= 0; i--)
                        {
                            if (operandStack.Count == 0)
                                throw new CommandException($"Expected {command.Parameters.Length} parameters for command", token);
                            args[i] = operandStack.Pop();
                            if (command.Parameters[i].Type.IsEnum)
                                if (!Enum.TryParse(command.Parameters[i].Type, args[i].ToString(), ignoreCase: true, out args[i]))
                                    throw new CommandException($"Invalid value for parameter '{command.Parameters[i].Name}' in command", token);
                            if (!command.Parameters[i].Type.IsInstanceOfType(args[i]))
                                throw new CommandException($"Invalid value for parameter '{command.Parameters[i].Name}' in command", token);
                        }

                        object target = null;
                        if (!command.IsStatic)
                        {
                            if (command.Attribute.IsBuiltIn) target = this;
                            else
                            {
                                if (operandStack.Count == 0)
                                    throw new CommandException("Expected target for command", token);
                                target = operandStack.Pop();
                                if (target == null)
                                    throw new CommandException("Expected target for command", token);
                                if (!command.Method.DeclaringType?.IsInstanceOfType(target) ?? true)
                                    throw new CommandException("Invalid target for command", token);
                            }
                        }

                        var result = command.Execute(target, args);
                        operandStack.Push(result);
                        break;
                    case LexemeType.String:
                        operandStack.Push(token.Lexeme.Value);
                        break;

                    case LexemeType.Invalid:
                    default:
                        throw new CommandException("Unexpected token", token);
                }
            }

            if (operandStack.Count > 1)
                throw new CommandException("Invalid command", null);

            var operationResult = operandStack.Count > 0 ? operandStack.Pop() : null;
            CommandTree.GetDomain(Domains.Globals).RegisterVariable("$", operationResult);
            if (operationResult != null)
            {
                if (operationResult is not string && CommandTree.GetDomain(Domains.Presenters).TryGetPresenter(operationResult.GetType(), out var presenter))
                {
                    presenter.Execute(new[] { operationResult, _addMessage });
                }
            }

            return operationResult;
        }

        private void Discover()
        {
            var commands = CommandTree.GetDomain(Domains.Commands);
            var presenters = CommandTree.GetDomain(Domains.Presenters);

            var assemblies = GetAssemblies();
            foreach (var type in assemblies.SelectMany(a => a.GetTypes()))
            {
                var methods = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance);
                foreach (var method in methods)
                {
                    DiscoverCommand(commands, method);
                    DiscoverPresenter(presenters, method);
                }
            }

            commands.IsEnabled = true;
        }

        void DiscoverCommand(IDomainNode domain, MethodInfo method)
        {
            var attribute = method.GetCustomAttribute<CommandAttribute>();
            if (attribute != null)
                domain.RegisterCommand(method, attribute);
        }

        void DiscoverPresenter(IDomainNode domain, MethodInfo method)
        {
            var attribute = method.GetCustomAttribute<PresenterProviderForAttribute>();
            if (attribute != null)
                domain.RegisterPresenter(method, attribute);
        }

        #region builtin presenters

        [PresenterProviderFor(typeof(IEnumerable))]
        static void EnumerablePresenter(IEnumerable obj, AddMessage addMessage)
        {
            int i = 0;
            foreach (var o in obj)
            {
                ++i;
                
                if (o == null)
                {
                    addMessage($"[{i}] null", LogType.Log);
                }

                else
                {
                    addMessage($"[{i}] {o}", LogType.Log);
                }
            }
        }        
        
        [PresenterProviderFor(typeof(GameObject))]
        static void GameObjectPresenter(GameObject obj, AddMessage addMessage)
        {
            addMessage($"Name: {obj.name}", LogType.Log);
            addMessage($"Tag: {obj.tag}", LogType.Log);
            addMessage($"Layer: {LayerMask.LayerToName(obj.layer)}", LogType.Log);
            addMessage($"Active: {obj.activeSelf}", LogType.Log);
            addMessage($"Position: {obj.transform.position}", LogType.Log);
            addMessage($"Rotation: {obj.transform.rotation}", LogType.Log);
            addMessage($"Scale: {obj.transform.localScale}", LogType.Log);
        }

        [PresenterProviderFor(typeof(Component))]
        static void ComponentPresenter(Component obj, AddMessage addMessage)
        {
            addMessage($"Name: {obj.name}", LogType.Log);
            addMessage($"Tag: {obj.tag}", LogType.Log);
            addMessage($"Position: {obj.transform.position}", LogType.Log);
            addMessage($"Rotation: {obj.transform.rotation}", LogType.Log);
            addMessage($"Scale: {obj.transform.localScale}", LogType.Log);
        }

        [PresenterProviderFor(typeof(Object))]
        static void UnityObjectPresenter(Object obj, AddMessage addMessage)
        {
            if (obj is GameObject go)
            {
                GameObjectPresenter(go, addMessage);
                return;
            }

            if (obj is Component comp)
            {
                ComponentPresenter(comp, addMessage);
                return;
            }


            addMessage($"Name: {obj.name}", LogType.Log);
        }

        #endregion

        #region builtin commands

        [Command("true", isBuiltIn: true, hide: true)]
        static bool True() => true;

        [Command("false", isBuiltIn: true, hide: true)]
        static bool False() => false;

        [Command("null", isBuiltIn: true, hide: true)]
        static object Null() => null;

        [Command("ls", "List all commands", isBuiltIn: true)]
        void PrintAllCommands()
        {
            var commands = CommandTree.GetDomain(Domains.Commands);
            _addMessage("Available commands:".Decorate(StringDecoration.Italic), LogType.Log);
            if (commands.IsEnabled)
            {
                foreach (var command in commands.OfType<ICommandNode>())
                {
                    if (command.Attribute.Hide) continue;
                    _addMessage(command.GetHelpText(), LogType.Log);
                }
            }
        }

        [Command("__builtin_index", isBuiltIn: true, hide: true)]
        static object Index(object obj, object index)
        {
            if (!Int32.TryParse(index.ToString(), out var i))
                throw new CommandException("Index must be an integer");

            if (obj is IList list)
                return list[i];
            throw new CommandException("Index can only be used on lists");
        }


        [Command("__builtin_getproperty", isBuiltIn: true, hide: true)]
        static object GetProperty(object obj, object name)
        {
            if (name is not string propertyName)
                throw new CommandException("Property name must be a string");

            var type = obj.GetType();

            foreach (var property in type.GetProperties())
            {
                if (string.Equals(property.Name, propertyName))
                    return property.GetValue(obj);
            }

            foreach (var field in type.GetFields())
            {
                if (string.Equals(field.Name, propertyName))
                    return field.GetValue(obj);
            }

            throw new CommandException($"Property '{name}' not found");
        }


        [Command("__builtin_concat", isBuiltIn: true, hide: true)]
        static object Concatenate(object a, object b) => a.ToString() + b.ToString();

        [Command("echo", "Print result", isBuiltIn: true)]
        void Print(object obj) => _addMessage($"'{obj.ToString()}'", LogType.Log);

        [Command("var", "Save named variable", isBuiltIn: true)]
        void SetVar(string name, object value)
        {
            var vars = CommandTree.GetDomain(Domains.Variables);
            vars.RegisterVariable(name, value);
            _addMessage($"${name} = '{value}'", LogType.Log);
        }


        [Command("lsvar", "List saved variables", isBuiltIn: true)]
        void GetVars()
        {
            var vars = CommandTree.GetDomain(Domains.Variables);
            foreach (var variable in vars.OfType<IVariableNode>())
            {
                _addMessage($"${variable.Name} = ${variable.Value.ToString()}", LogType.Log);
            }
        }

        [Command("types", "Find types by name", isBuiltIn: true)]
        List<Type> FindType(string name)
        {
            return GetAssemblies()
                .Select(a => a.GetTypes().FirstOrDefault(t => string.Equals(t.Name, name, StringComparison.OrdinalIgnoreCase)))
                .Where(x => x != null)
                .ToList();
        }

        IEnumerable<Assembly> GetAssemblies()
        {
            if (_rootAssembly == null) return Enumerable.Empty<Assembly>();
            List<AssemblyName> assemblyNames = _rootAssembly.GetReferencedAssemblies().ToList();
            List<Assembly> assemblies = new();
            assemblies.Add(_rootAssembly);
            assemblies.AddRange(assemblyNames.Select(Assembly.Load));
            return assemblies;
        }

        [Command("find", "Find object in hierarchy by Name, Tag or component Type", isBuiltIn: true)]
        object GetObject(FindByTypes type, object value)
        {
            switch (type)
            {
                case FindByTypes.Name:
                {
                    var name = value as string;
                    if (string.IsNullOrEmpty(name))
                        throw new CommandException("Invalid name");
                    return GameObject.Find(name);
                }
                case FindByTypes.Tag:
                {
                    var name = value as string;
                    if (string.IsNullOrEmpty(name))
                        throw new CommandException("Invalid name");
                    return GameObject.FindWithTag(name);
                }
                case FindByTypes.Type:
                {
                    Type objType;
                    if (value is string typeName)
                    {
                        var matches = FindType(typeName);
                        if (matches.Count > 1)
                            throw new CommandException($"Multiple matching types for '{typeName}' found");
                        if (matches.Count == 0)
                            throw new CommandException($"No matching types for '{typeName}' found");
                        objType = matches[0];
                    }
                    else objType = value as Type;

                    if (objType == null)
                        throw new CommandException("Invalid type");
                    return GameObject.FindFirstObjectByType(objType);
                }
            }

            _addMessage("No object found", LogType.Warning);
            return null;
        }

        [Command("findall", "Find objects in hierarchy by Name (starting with), Tag or component Type", isBuiltIn: true)]
        object GetObjectAll(FindByTypes type, object value)
        {
            switch (type)
            {
                case FindByTypes.Name:
                {
                    var name = value as string;
                    if (string.IsNullOrEmpty(name))
                        throw new CommandException("Invalid name");
                    return GameObject.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None).Where(go => go.name.StartsWith(name)).ToList();
                }
                case FindByTypes.Tag:
                {
                    var name = value as string;
                    if (string.IsNullOrEmpty(name))
                        throw new CommandException("Invalid name");
                    return GameObject.FindGameObjectsWithTag(name).ToList();
                }
                case FindByTypes.Type:
                {
                    Type objType;
                    if (value is string typeName)
                    {
                        var matches = FindType(typeName);
                        if (matches.Count > 1)
                            throw new CommandException($"Multiple matching types for '{typeName}' found");
                        if (matches.Count == 0)
                            throw new CommandException($"No matching types for '{typeName}' found");
                        objType = matches[0];
                    }
                    else objType = value as Type;

                    if (objType == null)
                        throw new CommandException("Invalid type");
                    return GameObject.FindObjectsByType(objType, FindObjectsInactive.Include, FindObjectsSortMode.None).ToList();
                }
            }

            _addMessage("No object found", LogType.Warning);
            return null;
        }

        [Command("describe", "Describe object", isBuiltIn: true)]
        void Describe(object obj)
        {
            if (obj == null)
            {
                _addMessage("Object is null", LogType.Warning);
                return;
            }

            if (!CommandTree.GetDomain(Domains.Presenters).TryGetPresenter(obj.GetType(), out var presenter))
            {
                _addMessage(obj.ToString(), LogType.Log);
                _addMessage("No presenter found for object", LogType.Warning);
                return;
            }

            presenter.Execute(new[] { obj, _addMessage });
        }
        
        [Command("getcomponent", "Get component from object", isBuiltIn: true)]
        object GetComponent(GameObject obj, Type type)
        {
            return obj.GetComponent(type);
        }

        #endregion
    }
}