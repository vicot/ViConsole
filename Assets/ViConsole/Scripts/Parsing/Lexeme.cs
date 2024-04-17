using System;

namespace ViConsole.Parsing
{
    public class Lexeme : IEquatable<Lexeme>
    {
        public LexemeType Type { get; set; }
        public string Value { get; set; }
        public int Position { get; set; }

        public string Prefix { get; set; }
        
        public string Suffix { get; set; }

        public string Text => Prefix + Value + Suffix;

        public bool Empty => Text.Length == 0;
        
        public Lexeme(int position) : this(position, LexemeType.Invalid)
        {
        }
        
        public Lexeme(int position, LexemeType type, string value = "")
        {
            Position = position;
            Type = type;
            Value = value;
        }

        public bool Equals(Lexeme other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Type == other.Type && Position == other.Position;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((Lexeme)obj);
        }

        public override int GetHashCode() => HashCode.Combine((int)Type, Position);

        public static bool operator ==(Lexeme left, Lexeme right) => Equals(left, right);

        public static bool operator !=(Lexeme left, Lexeme right) => !Equals(left, right);

        public override string ToString() => $"[Lexeme] {Type}:{Position}: {Value}";
    }

    public enum LexemeType
    {
        Invalid,
        Command,
        String,
        OpenInline,
        CloseInline,
        OpenIndex,
        CloseIndex,
        Identifier,
        SpecialIdentifier,
        Concatenation,
        GetProperty
    }
}