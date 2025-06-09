using System;
using System.Collections.Generic;
using System.Text;
using ViConsole.Parsing;

namespace ViConsole.UIToolkit
{
    public interface ISyntaxColorizer
    {
        Dictionary<int, int> IndexReMap { get; }
        InputStyleSheet StyleSheet { get; set; }
        int NoParsesCount { get; }
        string ColorizeSyntax(string command, IEnumerable<Token> tokens);
    }

    public class SyntaxColorizer : ISyntaxColorizer
    {
        Dictionary<int, int> _indexReMap = new();

        public Dictionary<int, int> IndexReMap => _indexReMap;

        public InputStyleSheet StyleSheet { get; set; } = new();

        public int NoParsesCount { get; private set; }

        void Reset()
        {
            _indexReMap.Clear();
            NoParsesCount = 0;
        }

        public string ColorizeSyntax(string command, IEnumerable<Token> tokens)
        {
            Reset();
            var sb = new StringBuilder();

            var tokensEnumerator = tokens.GetEnumerator();
            Token token = null;
            void NextToken() => token = tokensEnumerator.MoveNext() ? tokensEnumerator.Current : null;
            NextToken();

            for (var i = 0; i < command.Length; i++)
            {
                if (token != null && token.Lexeme.Position == i)
                {
                    var added = ColorizeLexeme(ref sb, token);
                    if (added > 0)
                        i += added - 1;
                    NextToken();
                }
                else
                {
                    sb.Append(command[i]);
                }
            }

            return sb.ToString();
        }

        private int ColorizeLexeme(ref StringBuilder sb, Token token)
        {
            if (!StyleSheet.TryGetValue(token.Type, out var style)) style = InputStyle.Default;

            var lexeme = token.Lexeme.Text;
            var position = token.Lexeme.Position;

            //int count = 0;

            switch (token.Type)
            {
                case LexemeType.Invalid:
                case LexemeType.Command:
                case LexemeType.String:
                case LexemeType.Identifier:
                case LexemeType.SpecialIdentifier:
                    break;
                case LexemeType.OpenInline:
                    lexeme = Symbols.InlineStart.ToString();
                    break;
                case LexemeType.CloseInline:
                    lexeme = Symbols.InlineEnd.ToString();
                    break;
                case LexemeType.OpenIndex:
                    lexeme = Symbols.IndexStart.ToString();
                    break;
                case LexemeType.CloseIndex:
                    lexeme = Symbols.IndexEnd.ToString();
                    break;
                case LexemeType.Concatenation:
                    lexeme = Symbols.Concatenate.ToString();
                    break;
                case LexemeType.GetProperty:
                    lexeme = Symbols.Property.ToString();
                    break;
            }

            sb.Append(ApplyStyle(style, lexeme, position));
            return lexeme.Length;
        }

        string ApplyStyle(InputStyle style, string lexeme, int position)
        {
            string result = "";
            if (lexeme.Length > 0)
            {
                result = style.ApplyStyle(lexeme, out var prefix, out var suffix);
                AddReMap(position, prefix);
                AddReMap(position + lexeme.Length, suffix);
                NoParsesCount++;
            }

            return result;
        }

        void AddReMap(int position, int count)
        {
            if(!_indexReMap.TryAdd(position, count))
                _indexReMap[position] += count;
        }
    }
}