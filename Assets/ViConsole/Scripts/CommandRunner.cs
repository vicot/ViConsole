using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using ViConsole.Attributes;
using ViConsole.Parsing;
using ViConsole.Tree;

namespace ViConsole
{
    public interface ICommandRunner
    {
        Task Initialize();
        object ExecuteCommand(IEnumerable<Token> tokens);
        Tree.Tree CommandTree { get; }
    }

    public class CommandRunner : ICommandRunner
    {
        public Tree.Tree CommandTree { get; } = new();

        public async Task Initialize()
        {
            await Task.Run(DiscoverCommands);
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
                                if (!Enum.TryParse(command.Parameters[i].Type, args[i].ToString(), out args[i]))
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

        [Command("ls", "List all commands", isBuiltIn: true)]
        private int PrintAllCommands(bool test)
        {
            int count = 0;
            var commands = ViConsole.Instance.CommandTree.GetDomain(Domains.Commands);
            if (commands.IsEnabled)
            {
                foreach (var command in commands.OfType<ICommandNode>())
                {
                    if (test && command.Name == "ls") continue;
                    count++;
                    Debug.Log(command.GetHelpText());
                }
            }

            return count;
        }

        [Command("__builtin_index", isBuiltIn:true, hide:true)]
        private static object Index(object obj, int index)
        {
            if (obj is IList list)
                return list[index];
            else throw new CommandException("Index can only be used on lists");
        }

        
        [Command("__builtin_concat", isBuiltIn:true, hide:true)]
        private static object Concatenate(object a, object b) => a.ToString() + b.ToString();

        #endregion
    }
}