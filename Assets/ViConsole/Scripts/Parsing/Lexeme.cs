namespace ViConsole.Parsing
{
    public class Lexeme
    {
        public LexemeType Type { get; set; }
        public string Value { get; set; }
        public int Position { get; set; }

        public bool Empty => Value.Length == 0;
        
        public Lexeme(int position) : this(position, LexemeType.Invalid)
        {
        }
        
        public Lexeme(int position, LexemeType type, string value = "")
        {
            Position = position;
            Type = type;
            Value = value;
        }
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
        Concatenation
    }
}