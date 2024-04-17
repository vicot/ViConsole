using System;
using System.Linq;
using UnityEngine;
using ViConsole.Extensions;

namespace ViConsole.Parsing
{
    public class Token : IEquatable<Token>
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
                LexemeType.OpenInline => 4,
                LexemeType.CloseInline => 4,
                LexemeType.OpenIndex => 3,
                LexemeType.CloseIndex => 3,
                LexemeType.Identifier => 0,
                LexemeType.SpecialIdentifier => 0,
                LexemeType.Concatenation => 1,
                LexemeType.GetProperty => 1,
                _ => 0,
            };
        }

        public LexemeType Type { get; set; }

        public Lexeme Lexeme { get; }

        public int Priority { get; }

        public bool Equals(Token other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Type == other.Type && Equals(Lexeme, other.Lexeme);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((Token)obj);
        }

        public override int GetHashCode() => HashCode.Combine((int)Type, Lexeme);

        public static bool operator ==(Token left, Token right) => Equals(left, right);

        public static bool operator !=(Token left, Token right) => !Equals(left, right);

        public override string ToString() => $"[Token] {Type}:{Lexeme.Position}: {Lexeme.Value}";
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