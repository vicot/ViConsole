using System;
using ViConsole.Parsing;

namespace ViConsole
{
    public class CommandException : Exception
    {
        public Token Token { get; }

        public CommandException(string message) : this(message, null) { }
        public CommandException(string message, Token token) : base($"{message} '{token?.Lexeme?.Value}'")
        { 
            Token = token;
        }
    }
}