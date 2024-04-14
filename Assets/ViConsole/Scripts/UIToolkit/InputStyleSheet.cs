using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using ViConsole.Extensions;
using ViConsole.Parsing;

namespace ViConsole.UIToolkit
{
    public struct InputStyle
    {
        public Color Color;
        public StringDecoration Decoration;
        [FormerlySerializedAs("NoParse")] public bool AddNoParse;

        public InputStyle(Color color, StringDecoration decoration = StringDecoration.None, bool addNoParse = true)
        {
            Color = color;
            Decoration = decoration;
            AddNoParse = addNoParse;
        }

        public string ApplyStyle(string text) => ApplyStyle(text, out _, out _);

        public string ApplyStyle(string text, out int prefixLength, out int suffixLength)
        {
            prefixLength = 0;
            suffixLength = 0;
            if (AddNoParse) text = text.NoParse(ref prefixLength, ref suffixLength);
            if (Decoration != StringDecoration.None) text = text.Decorate(Decoration, ref prefixLength, ref suffixLength);
            if (Color != Color.clear) text = text.Colorize(Color, ref prefixLength, ref suffixLength);
            return text;
        }
        
        public static InputStyle Default = new InputStyle(Color.clear, StringDecoration.None);
    }

    public class InputStyleSheet : Dictionary<LexemeType, InputStyle>
    {
    }
}