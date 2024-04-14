﻿using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UIElements;
using ViConsole.Extensions;
using ViConsole.Parsing;

namespace ViConsole.UIToolkit
{
    [UxmlElement]
    public partial class ConsoleInputControl : TextField
    {
        const string NoParseOpenTag = "<noparse>";
        const string NoParseCloseTag = "</noparse>";
        
        readonly TextElement _textElement;

        ISyntaxColorizer _colorizer;
        
        int _lastSelectIndex = 0;
        int _lastCursorIndex = 0;
        int _noParsesCount=> _colorizer.NoParsesCount;
        string _command = "";
        List<Token> _tokenizedCommand;
        Dictionary<int, int> _indexReMap => _colorizer.IndexReMap;

        public ViConsole Controller { get; set; }

        public ConsoleInputControl()
        {
            _textElement = this.Q<TextElement>();
            _textElement.enableRichText = true;
            _textElement.generateVisualContent += OnGenerateVisualContent;

            this.RegisterValueChangedCallback(OnValueChanged);
            RegisterCallback<KeyDownEvent>(OnKeyDown, TrickleDown.TrickleDown);
            
            _colorizer = new SyntaxColorizer();
            _colorizer.StyleSheet[LexemeType.Command] = new InputStyle(Color.green, StringDecoration.Bold);
            _colorizer.StyleSheet[LexemeType.String] = new InputStyle(Color.clear, StringDecoration.Italic);
            _colorizer.StyleSheet[LexemeType.Identifier] = new InputStyle(Color.red, StringDecoration.Underline);
        }

        void OnKeyDown(KeyDownEvent evt)
        {
            if (evt.keyCode is not (KeyCode.Return or KeyCode.KeypadEnter))
                return;

            var split = _command.Split(" ");
            if (split.Length == 0) return;
            var cmd = split[0];
            //CommandRunner.Instance.Execute(cmd)

            Controller.ExecuteCommand(_tokenizedCommand, _command);
            
            ClearCommand();

            focusController.IgnoreEvent(evt);
            evt.StopImmediatePropagation();
            evt.StopPropagation();
            textSelection.SelectAll();

            SetFocus();
        }

        void ClearCommand()
        {
            _command = "";
            cursorIndex = 0;
            selectIndex = 0;
            SetValueWithoutNotify("");
        }

        public void SetFocus()
        {
            schedule.Execute(() => { _textElement.Focus(); });
        }

        void OnGenerateVisualContent(MeshGenerationContext obj)
        {
            _lastCursorIndex = cursorIndex;
            _lastSelectIndex = selectIndex;
        }

        void OnValueChanged(ChangeEvent<string> args)
        {
            var lastStart = _lastSelectIndex;
            var lastEnd = _lastCursorIndex;
            if (lastStart > lastEnd) (lastStart, lastEnd) = (lastEnd, lastStart);

            var newIndex = cursorIndex;
            var newValue = args.newValue;

            var unexpectedNoParses = CheckForNoParses(newValue);

            bool hasSelection = lastStart != lastEnd;

            // If anything is selected, it will be removed and replaced 
            if (hasSelection) _command = _command.Remove(lastStart, lastEnd - lastStart);

            if (newIndex < lastStart)
            {
                if (!hasSelection)
                    HandleBackDelete(ref _command, newIndex);
            }
            else if (newIndex > lastStart)
            {
                if (newIndex - lastStart > 1)
                    HandlePaste(ref _command, newValue, lastStart, newIndex - lastStart);
                else
                    HandleInsert(ref _command, newValue, lastStart);
            }
            else if (newIndex == lastStart)
            {
                if (!hasSelection)
                    HandleFrontDelete(ref _command, newIndex);
            }

            unexpectedNoParses |= _command.Contains(NoParseCloseTag);
            if (unexpectedNoParses && _command.Length > 0)
            {
                Debug.LogWarning("Found rogue noparse closing tag. Nuking the entire command");
                ClearCommand();
                return;
            }

            //var taggedCommand = FormatText(_command);
            var lexems = Parser.Parse(_command);
            _tokenizedCommand = Parser.Tokenize(lexems);
            var taggedCommand = _colorizer.ColorizeSyntax(_command, _tokenizedCommand);
            SetValueWithoutNotify(taggedCommand);
        }

        bool CheckForNoParses(string txt)
        {
            var openIndices = txt.IndicesOf(NoParseOpenTag).ToList();
            var closeIndices = txt.IndicesOf(NoParseCloseTag).ToList();

            //case 1 - more noparses than expected
            if (closeIndices.Count > _noParsesCount)
                return true;

            //case 2 - multiple closes before next open
            for (int i = 0; i < openIndices.Count - 1; i++)
            {
                var open = openIndices[i];
                var nextOpen = openIndices[i + 1];
                var closes = closeIndices.Count(x => x > open & x < nextOpen);
                if (closes > 1) return true;
            }

            return false;
        }

        void HandleFrontDelete(ref string command, int index)
        {
            if (index >= command.Length)
                return;

            command = command.Remove(index, 1);
        }

        void HandleBackDelete(ref string command, int index)
        {
            if (index >= command.Length)
                return;

            command = command.Remove(index, 1);
        }

        void HandlePaste(ref string command, string newValue, int index, int length)
        {
            var remappedIndex = RemapIndex(index);
            var inserted = newValue[remappedIndex..(remappedIndex + length)];
            command = command.Insert(index, inserted);
        }

        void HandleInsert(ref string command, string newValue, int index)
        {
            var remappedIndex = RemapIndex(index);
            var inserted = newValue[remappedIndex];
            command = command.Insert(index, inserted.ToString());
        }

        int RemapIndex(int index)
        {
            var remappedIndex = index;
            foreach (var remapKey in _indexReMap.Keys.OrderBy(x => x))
            {
                if (index > remapKey)
                    remappedIndex += _indexReMap[remapKey];
                else break;
            }

            return remappedIndex;
        }

        private string ReplaceAt(string str, int position, string c)
        {
            return str[..position] + c + str[(position + 1)..];
        }

        // private string FormatText(string txt)
        // {
        //     var sb = new StringBuilder();
        //     _indexReMap.Clear();
        //
        //     _noParsesCount = 0;
        //
        //     var parts = txt.Split(" ", 3);
        //     if (parts.Length >= 1)
        //     {
        //         if (parts[0].Length > 0)
        //         {
        //             int prefixLength = 0, suffixLength = 0;
        //             sb.Append(parts[0].NoParse(ref prefixLength, ref suffixLength).Colorize(Color.red, ref prefixLength, ref suffixLength));
        //             _indexReMap[0] = prefixLength;
        //             var idx = parts[0].Length;
        //             _indexReMap[idx] = suffixLength;
        //             _noParsesCount++;
        //         }
        //     }
        //
        //     if (parts.Length >= 2)
        //     {
        //         sb.Append(" ");
        //         var idx = parts[0].Length + 1;
        //         if (parts[1].Length > 0)
        //         {
        //             int prefixLength = 0, suffixLength = 0;
        //             sb.Append(parts[1].NoParse(ref prefixLength, ref suffixLength).Decorate(StringDecoration.Bold, ref prefixLength, ref suffixLength));
        //             _indexReMap[idx] = prefixLength;
        //             idx += parts[1].Length;
        //             _indexReMap[idx] = suffixLength;
        //             _noParsesCount++;
        //         }
        //     }
        //
        //     if (parts.Length >= 3)
        //     {
        //         sb.Append(" ");
        //         var idx = parts[0].Length + 1 + parts[1].Length + 1;
        //         if (parts[2].Length > 0)
        //         {
        //             int prefixLength = 0, suffixLength = 0;
        //             sb.Append(parts[2].NoParse(ref prefixLength, ref suffixLength).Decorate(StringDecoration.Italic, ref prefixLength, ref suffixLength));
        //             _indexReMap[idx] = prefixLength;
        //             idx += parts[2].Length;
        //             _indexReMap[idx] = suffixLength;
        //             _noParsesCount++;
        //         }
        //     }
        //
        //     return sb.ToString();
        // }
    }
}