namespace ViConsole.Parsing
{
    public class Lexeme
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