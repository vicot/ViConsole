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
            _lastCursorIndex = _textElement.selection.cursorIndex;
            _lastSelectIndex = _textElement.selection.selectIndex;

            Debug.Log(_textElement.selection.cursorIndex);
            Debug.Log(_textElement.selection.selectIndex);
        }

        private string FormatText(string txt)
        {
            var sb = new StringBuilder();
            _indexReMap.Clear();

            var parts = txt.Split(" ", 3);
            if (parts.Length >= 1)
            {
                sb.AppendFormat("<color=red><noparse>{0}</noparse></color>", parts[0]);
                _indexReMap[0] = "<color=red><noparse>".Length;
                var idx = parts[0].Length;
                _indexReMap[idx] = "</noparse></color>".Length;
            }

            if (parts.Length >= 2)
            {
                var idx = parts[0].Length + 1;
                sb.AppendFormat(" <b><noparse>{0}</noparse></b>", parts[1]);
                _indexReMap[idx] = "<b><noparse>".Length;
                idx += parts[1].Length;
                _indexReMap[idx] = "</noparse></b>".Length;
            }

            if (parts.Length >= 3)
            {
                var idx = parts[0].Length + 1 + parts[1].Length + 1;
                sb.AppendFormat(" <i><noparse>{0}</noparse></i>", parts[2]);
                _indexReMap[idx] = "<i><noparse>".Length;
                idx += parts[2].Length;
                _indexReMap[idx] = "</noparse></i>".Length;
            }

            return sb.ToString();
        }

        void OnValueChanged(ChangeEvent<string> args)
        {
            Debug.Log(_textElement.selection.cursorIndex);
            Debug.Log(_textElement.selection.selectIndex);

            // var cursorIndex = _textElement.selection.cursorIndex;
            // var selectIndex = _textElement.selection.selectIndex;
            if (_lastCursorIndex == _lastSelectIndex)
            {
                if (cursorIndex < _lastCursorIndex)
                {
                    HandleBackDelete(ref _command, cursorIndex);
                }
                else if (cursorIndex > _lastCursorIndex)
                {
                    if (cursorIndex - _lastCursorIndex > 1)
                        HandlePaste(ref _command, args.newValue, cursorIndex);
                    else
                        HandleInsert(ref _command, args.newValue);
                }
                else if (cursorIndex == _lastCursorIndex)
                {
                    HandleFrontDelete();
                }
            }

            SetValueWithoutNotify(FormatText(_command));
        }

        void HandlePaste(ref string command, string newValue, int i)
        {
        }

        void HandleFrontDelete()
        {
        }

        void HandleInsert(ref string command, string newValue)
        {
            var remappedIndex = RemapIndex(_lastCursorIndex);

            var inserted = newValue[remappedIndex];
            Debug.Log(inserted);

            command = command.Insert(_lastCursorIndex, inserted.ToString());
        }

        void HandleBackDelete(ref string command, int cursorIndex)
        {
            command = command.Remove(cursorIndex, 1);
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
    }
}