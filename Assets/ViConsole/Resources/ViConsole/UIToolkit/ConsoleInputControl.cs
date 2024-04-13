using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UIElements;

namespace ViConsole.UIToolkit
{
    [UxmlElement]
    public partial class ConsoleInputControl : TextField
    {
        readonly TextElement _textElement;

        int _lastSelectIndex = 0;
        int _lastCursorIndex = 0;
        int _lastTaggedLength = 0;
        string _command = "";
        Dictionary<int, int> _indexReMap = new();

        public ConsoleInputControl()
        {
            _textElement = this.Q<TextElement>();
            _textElement.enableRichText = true;
            _textElement.generateVisualContent += OnGenerateVisualContent;

            this.RegisterValueChangedCallback(OnValueChanged);
        }

        void OnGenerateVisualContent(MeshGenerationContext obj)
        {
            _lastCursorIndex = cursorIndex;
            _lastSelectIndex = selectIndex;

            Debug.Log(_textElement.selection.cursorIndex);
            Debug.Log(_textElement.selection.selectIndex);
        }

        void OnValueChanged(ChangeEvent<string> args)
        {
            // Debug.Log(_textElement.selection.cursorIndex);
            // Debug.Log(_textElement.selection.selectIndex);
            var lastStart = _lastSelectIndex;
            var lastEnd = _lastCursorIndex;
            if (lastStart > lastEnd) (lastStart, lastEnd) = (lastEnd, lastStart);

            var newIndex = cursorIndex;
            var newValue = args.newValue;

            // special cases of </noparse> input
            if (lastStart == lastEnd && newIndex < _lastCursorIndex && _lastCursorIndex - newIndex > 1)
            {
                HandleInsert(ref _command, newValue, _lastCursorIndex);
            }
            else
            {
                // If anything is selected, it will be removed and replaced 
                if (lastStart != lastEnd)
                {
                    Debug.Log($"Removing start: {lastStart} - {lastEnd} ({lastEnd - lastStart}");
                    _command = _command.Remove(lastStart, lastEnd - lastStart);
                }

                if (newIndex < lastStart)
                {
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
                    HandleFrontDelete(ref _command, newIndex);
                }
            }

            int noparse = _command.IndexOf("</noparse>", StringComparison.OrdinalIgnoreCase);
            while (noparse >= 0)
            {
                _command = _command.Remove(noparse, "</noparse>".Length);
                cursorIndex = noparse;
                selectIndex = cursorIndex;
                noparse = _command.IndexOf("</noparse>", StringComparison.OrdinalIgnoreCase);
            }

            var taggedCommand = FormatText(_command);
            _lastTaggedLength = taggedCommand.Length;
            SetValueWithoutNotify(taggedCommand);
        }

        void HandlePaste(ref string command, string newValue, int index, int length)
        {
            var remappedIndex = RemapIndex(index);

            var inserted = newValue[remappedIndex..(remappedIndex + length)];
            Debug.Log(inserted);
            command = command.Insert(index, inserted);
        }

        void HandleFrontDelete(ref string command, int cursorIndex)
        {
            if (cursorIndex >= command.Length)
                return;

            command = command.Remove(cursorIndex, 1);
        }

        void HandleBackDelete(ref string command, int cursorIndex)
        {
            if (cursorIndex >= command.Length)
                return;

            command = command.Remove(cursorIndex, 1);
        }

        void HandleInsert(ref string command, string newValue, int index)
        {
            var remappedIndex = RemapIndex(index);

            var inserted = newValue[remappedIndex];
            Debug.Log(inserted);

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

        private string FormatText(string txt)
        {
            var sb = new StringBuilder();
            _indexReMap.Clear();

            var parts = txt.Split(" ", 3);
            if (parts.Length >= 1)
            {
                sb.AppendFormat("<color=red><noparse>{0}</noparse></color>", parts[0]);
                if (parts[0].Length > 0)
                {
                    _indexReMap[0] = "<color=red><noparse>".Length;
                    var idx = parts[0].Length;
                    _indexReMap[idx] = "</noparse></color>".Length;
                }
            }

            if (parts.Length >= 2)
            {
                var idx = parts[0].Length + 1;
                sb.AppendFormat(" <b><noparse>{0}</noparse></b>", parts[1]);
                if (parts[1].Length > 0)
                {
                    _indexReMap[idx] = "<b><noparse>".Length;
                    idx += parts[1].Length;
                    _indexReMap[idx] = "</noparse></b>".Length;
                }
            }

            if (parts.Length >= 3)
            {
                var idx = parts[0].Length + 1 + parts[1].Length + 1;
                sb.AppendFormat(" <i><noparse>{0}</noparse></i>", parts[2]);
                if (parts[2].Length > 0)
                {
                    _indexReMap[idx] = "<i><noparse>".Length;
                    idx += parts[2].Length;
                    _indexReMap[idx] = "</noparse></i>".Length;
                }
            }

            return sb.ToString();
        }
    }
}