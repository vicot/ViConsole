using System;

namespace ViConsole.Extensions
{
    [Flags]
    public enum StringDecoration
    {
        None          = 0b0000,
        Italic        = 0b0001,
        Bold          = 0b0010,
        Underline     = 0b0100,
        Strikethrough = 0b1000,
    }
}