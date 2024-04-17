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

        List<string> _commandHistory;
        int _commandHistoryIndex = 0;

        readonly TextElement _textElement;
        Label _syntaxHint;
        ListView _autocompleteListView;

        ISyntaxColorizer _colorizer;

        int _lastSelectIndex = 0;
        int _lastCursorIndex = 0;
        int _noParsesCount => _colorizer.NoParsesCount;
        string _command = "";
        string _taggedCommand = "";
        List<Token> _tokenizedCommand;
        Dictionary<int, int> _indexReMap => _colorizer.IndexReMap;
        List<string> _autocompleteHints = new();

        public ViConsole Controller { get; set; }
        public ICommandRunner CommandRunner { get; set; }

        public ConsoleInputControl()
        {
            _commandHistory = new List<string>();

            _textElement = this.Q<TextElement>();
            _textElement.enableRichText = true;
            _textElement.generateVisualContent += OnGenerateVisualContent;

            this.RegisterValueChangedCallback(OnValueChanged);
            RegisterCallback<KeyDownEvent>(OnKeyDown, TrickleDown.TrickleDown);

            _colorizer = new SyntaxColorizer();

            _autocompleteHints.Add("test1");
            _autocompleteHints.Add("test2");
            _autocompleteHints.Add("test3");
            RefreshHints();
        }

        public ConsoleInputControl(Label syntaxHint, ListView autocompleteListView)
        {
            _syntaxHint = syntaxHint;
            _autocompleteListView = autocompleteListView;
        }

        public void SetupHints()
        {
            _syntaxHint = panel.visualTree.Q<Label>("syntax-hint");
            _autocompleteListView = panel.visualTree.Q<ListView>("autocomplete-hints");
            _autocompleteListView.makeItem = () => new Label().WithClassName("autocomplete-hint");
            _autocompleteListView.bindItem = (element, i) => (element as Label).text = _autocompleteHints[i];
            _autocompleteListView.itemsSource = _autocompleteHints;
        }

        void RefreshHints()
        {
            _autocompleteListView?.schedule.Execute(() =>
            {
                _autocompleteListView.RefreshItems();
                _autocompleteListView.ScrollToItem(_autocompleteHints.Count);
            });
        }

        public void SetStyle(InputStyleSheet styleSheet)
        {
            _colorizer.StyleSheet = styleSheet;
        }

        void OnKeyDown(KeyDownEvent evt)
        {
            if (evt.keyCode is not (KeyCode.Return or KeyCode.KeypadEnter))
                return;

            _commandHistory.Add(_command);
            _commandHistoryIndex = _commandHistory.Count - 1;

            Controller.ExecuteCommand(_tokenizedCommand, _command, _taggedCommand);

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
            _tokenizedCommand.Clear();
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

            var (cmd, pos) = CommandRunner?.SimulateExecution(_tokenizedCommand, cursorIndex) ?? (null, 0);
            if (cmd != null)
            {
                _syntaxHint?.schedule.Execute(() =>
                {
                    _syntaxHint.style.visibility = Visibility.Visible;
                    _syntaxHint.text = cmd.GetSyntaxHint(pos);
                });
            }
            else
            {
                _syntaxHint?.schedule.Execute(() =>
                {
                    _syntaxHint.style.visibility = Visibility.Hidden;
                    _syntaxHint.text = "";
                });
            }
        }

        void OnValueChanged(ChangeEvent<string> args)
        {
            _commandHistoryIndex = _commandHistory.Count - 1;
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

            ShowCommand();
        }

        void ShowCommand()
        {
            var lexems = Parser.Parse(_command);
            _tokenizedCommand = Parser.Tokenize(lexems);
            _taggedCommand = _colorizer.ColorizeSyntax(_command, _tokenizedCommand);
            SetValueWithoutNotify(_taggedCommand);
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

        public void NextCommand()
        {
            if (_commandHistory.Count <= 0)
                return;

            ClearCommand();
            _commandHistoryIndex = Mathf.Clamp(_commandHistoryIndex, 0, _commandHistory.Count - 1);
            _command = _commandHistory[_commandHistoryIndex++];
            ShowCommand();
        }

        public void PreviousCommand()
        {
            if (_commandHistory.Count <= 0)
                return;

            ClearCommand();
            _commandHistoryIndex = Mathf.Clamp(_commandHistoryIndex, 0, _commandHistory.Count - 1);
            _command = _commandHistory[_commandHistoryIndex--];
            ShowCommand();
        }
    }
}