using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace ViConsole.Parsing
{
    public class Parser
    {
        public static List<Lexeme> Parse(string command)
        {
            var lexemes = new List<Lexeme>();
            Lexeme currentLexeme = null;
            bool inString = false;
            for (int i = 0; i < command.Length; i++)
            {
                var c = command[i];
                if (currentLexeme == null)
                    currentLexeme = new Lexeme(i);

                if (!inString && c == Symbols.Separator)
                {
                    if (!currentLexeme.Empty)
                    {
                        lexemes.Add(currentLexeme);
                    }

                    currentLexeme = null;

                    continue;
                }

                if (currentLexeme.Empty && currentLexeme.Type == LexemeType.Invalid)
                {
                    inString = false;
                    if (c == Symbols.Identifier)
                    {
                        currentLexeme.Type = LexemeType.Identifier;
                        currentLexeme.Prefix = c.ToString();
                        // Skip the identifier symbol
                    }
                    else if (c == Symbols.SpecialIdentifier)
                    {
                        currentLexeme.Type = LexemeType.SpecialIdentifier;
                        currentLexeme.Prefix = c.ToString();
                        // Skip the special identifier symbol
                    }
                    else if (c == Symbols.InlineStart)
                    {
                        currentLexeme.Type = LexemeType.OpenInline;
                        lexemes.Add(currentLexeme);
                        currentLexeme = null;
                    }
                    else if (c == Symbols.InlineEnd)
                    {
                        currentLexeme.Type = LexemeType.CloseInline;
                        lexemes.Add(currentLexeme);
                        currentLexeme = null;
                    }
                    else if (c == Symbols.IndexStart)
                    {
                        currentLexeme.Type = LexemeType.OpenIndex;
                        lexemes.Add(currentLexeme);
                        currentLexeme = null;
                    }
                    else if (c == Symbols.IndexEnd)
                    {
                        currentLexeme.Type = LexemeType.CloseIndex;
                        lexemes.Add(currentLexeme);
                        currentLexeme = null;
                    }
                    else if (c == Symbols.Concatenate)
                    {
                        currentLexeme.Type = LexemeType.Concatenation;
                        lexemes.Add(currentLexeme);
                        currentLexeme = null;
                    }
                    else if (c == Symbols.Property)
                    {
                        currentLexeme.Type = LexemeType.GetProperty;
                        lexemes.Add(currentLexeme);
                        currentLexeme = null;
                    }
                    else if (char.IsLetterOrDigit(c) || c == '_')
                    {
                        currentLexeme.Type = LexemeType.Command;
                        currentLexeme.Value += c;
                    }
                    else if (c == Symbols.String)
                    {
                        currentLexeme.Type = LexemeType.String;
                        currentLexeme.Prefix = c.ToString();
                        inString = true;
                    }
                    else
                    {
                        currentLexeme.Type = LexemeType.Invalid;
                        currentLexeme.Value += c;
                        lexemes.Add(currentLexeme);
                        currentLexeme = null;
                    }

                    continue;
                }

                if (currentLexeme.Type is LexemeType.Identifier or LexemeType.Command)
                {
                    if (char.IsLetterOrDigit(c) || c == '_')
                    {
                        currentLexeme.Value += c;
                    }
                    else
                    {
                        lexemes.Add(currentLexeme);
                        currentLexeme = null;
                        i--;
                    }
                }
                else if (currentLexeme.Type is LexemeType.SpecialIdentifier)
                {
                    if (char.IsLetterOrDigit(c) || c == '_')
                    {
                        currentLexeme.Value += c;
                    }
                    else if (c == '$' && currentLexeme.Value.Length == 0)
                    {
                        currentLexeme.Value += c;
                    }
                    else
                    {
                        lexemes.Add(currentLexeme);
                        currentLexeme = null;
                        i--;
                    }
                }
                else if (currentLexeme.Type == LexemeType.String)
                {
                    if (char.IsLetterOrDigit(c) || c == '_')
                    {
                        currentLexeme.Value += c;
                    }
                    else if (inString && c != Symbols.String)
                    {
                        currentLexeme.Value += c;
                    }
                    else
                    {
                        if (inString && c == Symbols.String)
                            currentLexeme.Suffix = c.ToString();
                        lexemes.Add(currentLexeme);
                        currentLexeme = null;
                        if (!inString || c != Symbols.String)
                            i--;
                        inString = false;
                    }
                }
            }

            if (currentLexeme != null)
            {
                if (!currentLexeme.Empty || currentLexeme.Type is not LexemeType.Invalid)
                    lexemes.Add(currentLexeme);
            }

            //Print(lexemes);
            return lexemes;
        }

        public static List<Token> Tokenize(List<Lexeme> lexemes)
        {
            var tokens = new List<Token>();
            bool firstToken = true;

            foreach (var lexeme in lexemes)
            {
                if (!firstToken && lexeme.Type == LexemeType.Command)
                {
                    lexeme.Type = LexemeType.String;
                }

                tokens.Add(new Token(lexeme));
                firstToken = lexeme.Type == LexemeType.OpenInline;
            }

            return tokens;
        }

        public static List<Token> ConvertToPostfix(IEnumerable<Token> tokens)
        {
            Stack<Token> operatorStack = new();
            List<Token> output = new();
            if (tokens == null || !tokens.Any()) return null;

            foreach (var token in tokens)
            {
                if (token.Type is LexemeType.String or LexemeType.Identifier or LexemeType.SpecialIdentifier)
                {
                    output.Add(token);
                }

                if (token.Type is LexemeType.Command)
                {
                    while (operatorStack.Count > 0 && operatorStack.Peek().Priority >= token.Priority
                                                   && operatorStack.Peek().Type != LexemeType.OpenInline
                                                   && operatorStack.Peek().Type != LexemeType.OpenIndex)
                        output.Add(operatorStack.Pop());

                    operatorStack.Push(token);
                }

                if (token.Type is LexemeType.Concatenation)
                {
                    while (operatorStack.Count > 0 && operatorStack.Peek().Priority >= token.Priority
                                                   && operatorStack.Peek().Type != LexemeType.OpenInline
                                                   && operatorStack.Peek().Type != LexemeType.OpenIndex)
                        output.Add(operatorStack.Pop());

                    var concatCommand = new Token(new Lexeme(token.Lexeme.Position, LexemeType.Command, "__builtin_concat"));
                    operatorStack.Push(concatCommand);
                }

                if (token.Type is LexemeType.GetProperty)
                {
                    while (operatorStack.Count > 0 && operatorStack.Peek().Priority >= token.Priority
                                                   && operatorStack.Peek().Type != LexemeType.OpenInline
                                                   && operatorStack.Peek().Type != LexemeType.OpenIndex)
                        output.Add(operatorStack.Pop());

                    var concatCommand = new Token(new Lexeme(token.Lexeme.Position, LexemeType.Command, "__builtin_getproperty"));
                    operatorStack.Push(concatCommand);
                }

                if (token.Type is LexemeType.OpenInline or LexemeType.OpenIndex)
                    operatorStack.Push(token);

                if (token.Type is LexemeType.CloseInline)
                {
                    while (operatorStack.Count > 0 && operatorStack.Peek().Type != LexemeType.OpenInline)
                        output.Add(operatorStack.Pop());

                    if (operatorStack.Count > 0)
                        operatorStack.Pop();
                    else
                        return null;
                }

                if (token.Type is LexemeType.CloseIndex)
                {
                    while (operatorStack.Count > 0 && operatorStack.Peek().Lexeme.Type != LexemeType.OpenIndex)
                        output.Add(operatorStack.Pop());

                    Token indexCommand;
                    if (operatorStack.Count > 0)
                        indexCommand = operatorStack.Pop();
                    else
                        return null;


                    if (indexCommand.Type == LexemeType.OpenIndex)
                    {
                        indexCommand.Type = LexemeType.Command;
                        indexCommand.Lexeme.Value = "__builtin_index";
                    }

                    output.Add(indexCommand);
                }
            }

            while (operatorStack.Count > 0)
            {
                output.Add(operatorStack.Pop());
            }

            //PrintOutput(output);
            return output;
        }

        private static void PrintOutput(List<Token> tokens)
        {
            //var sb = new StringBuilder();
            var sb = string.Join(" ", tokens.Select(t => $"{t.Type} ({t.Lexeme.Value})"));

            Debug.Log(sb.ToString());
        }

        private static void Print(List<Lexeme> lexems)
        {
            var sb = new StringBuilder();
            foreach (var lexem in lexems)
            {
                sb.AppendFormat(" {0} ", lexem.Type);
            }

            Debug.Log(sb.ToString());

            sb.Clear();
            foreach (var lexem in lexems)
            {
                sb.AppendFormat("{0}|", lexem.Value);
            }

            Debug.Log(sb.ToString());
        }
    }
}