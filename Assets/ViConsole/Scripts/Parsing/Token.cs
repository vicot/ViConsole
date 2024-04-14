using System;
using System.Linq;
using UnityEngine;
using ViConsole.Extensions;

namespace ViConsole.Parsing
{
    public class Token
    {
        public Token(Lexeme lexeme)
        {
            Lexeme = lexeme;
            Type = lexeme.Type;
            if (Type == LexemeType.Command && char.IsDigit(lexeme.Value[0]))
                Type = LexemeType.String;

            Priority = Type switch
            {
                LexemeType.Invalid => 0,
                LexemeType.Command => 1,
                LexemeType.String => 0,
                LexemeType.OpenInline => 3,
                LexemeType.CloseInline => 3,
                LexemeType.OpenIndex => 2,
                LexemeType.CloseIndex => 2,
                LexemeType.Identifier => 0,
                LexemeType.SpecialIdentifier => 0,
                LexemeType.Concatenation => 1,
                _ => 0,
            };
        }

        public LexemeType Type { get; set; }

        public Lexeme Lexeme { get; }

        public int Priority { get; }
    }

    public enum TokenType
    {
        Invalid,
        Command,
        Parameter,
        Target,
        Var,
    }
}