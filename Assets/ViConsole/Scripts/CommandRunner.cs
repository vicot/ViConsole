using System;
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

namespace ViConsole
{
    public interface ICommandRunner
    {
        Task Initialize(Action<string, LogType> addMessage);
        object ExecuteCommand(IEnumerable<Token> tokens);
        Tree.Tree CommandTree { get; }
    }

    public class CommandRunner : ICommandRunner
    {
        Action<string, LogType> _addMessage;
        public Tree.Tree CommandTree { get; } = new();

        public async Task Initialize(Action<string, LogType> addMessage)
        {
            _addMessage = addMessage;
            await Task.Run(DiscoverCommands);
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

            return operandStack.Count > 0 ? operandStack.Pop() : null;
        }

        private void DiscoverCommands()
        {
            var commands = CommandTree.GetDomain(Domains.Commands);
            var assembly = Assembly.GetExecutingAssembly();
            foreach (var type in assembly.GetTypes())
            {
                var methods = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance);
                foreach (var method in methods)
                {
                    var attribute = method.GetCustomAttribute<CommandAttribute>();
                    if (attribute != null)
                        commands.RegisterCommand(method, attribute);
                }
            }

            commands.IsEnabled = true;
        }

        #region builtin commands

        [Command("true", isBuiltIn: true, hide: true)]
        public static bool True() => true;

        [Command("false", isBuiltIn: true, hide: true)]
        public static bool False() => false;

        [Command("null", isBuiltIn: true, hide: true)]
        public static object Null() => null;

        [Command("ls", "List all commands", isBuiltIn: true)]
        private void PrintAllCommands()
        {
            var commands = ViConsole.Instance.CommandTree.GetDomain(Domains.Commands);
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
        private static object Index(object obj, int index)
        {
            if (obj is IList list)
                return list[index];
            else throw new CommandException("Index can only be used on lists");
        }


        [Command("__builtin_concat", isBuiltIn: true, hide: true)]
        private static object Concatenate(object a, object b) => a.ToString() + b.ToString();

        [Command("echo", "Print result", isBuiltIn: true)]
        private void Print(object obj) => _addMessage($"'{obj.ToString()}'", LogType.Log);

        [Command("var", "Save named variable", isBuiltIn: true)]
        private void SetVar(string name, object value)
        {
            var vars = CommandTree.GetDomain(Domains.Variables);
            vars.RegisterVariable(name, value);
            _addMessage($"${name} = '{value}'", LogType.Log);
        }


        [Command("lsvar", "List saved variables", isBuiltIn: true)]
        private void GetVars()
        {
            var vars = CommandTree.GetDomain(Domains.Variables);
            foreach (var variable in vars.OfType<IVariableNode>())
            {
                _addMessage($"${variable.Name} = ${variable.Value.ToString()}", LogType.Log);
            }
        }

        [Command("types", "Find types by name", isBuiltIn: true)]
        private List<Type> FindType(string name)
        {
            return AppDomain.CurrentDomain.GetAssemblies()
                .Select(a => a?.GetTypes().FirstOrDefault(t => string.Equals(t.Name, "Camera", StringComparison.OrdinalIgnoreCase)))
                .Where(x => x != null)
                .ToList();
        }

        [Command("find", "Find object in hierarchy by Name, Tag or component Type", isBuiltIn: true)]
        private object GetObject(FindByTypes type, object value)
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
                    var monoType = value as Type;
                    if (monoType == null)
                        throw new CommandException("Invalid type");
                    return GameObject.FindFirstObjectByType(monoType);
                }
            }

            _addMessage("No object found", LogType.Warning);
            return null;
        }

        #endregion
    }
}