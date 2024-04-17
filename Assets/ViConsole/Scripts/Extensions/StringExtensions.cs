using System;
using System.Collections.Generic;
using UnityEngine;

namespace ViConsole.Extensions
{
    public static class StringExtensions
    {
        static int _tmp;

        public static IEnumerable<int> IndicesOf(this string source, string subString, StringComparison comparisonType = StringComparison.OrdinalIgnoreCase)
        {
            int index = -1;
            do
            {
                index = source.IndexOf(subString, index + 1, comparisonType);
                if (index == -1) continue;
                yield return index;
            } while (index != -1);
        }

        public static bool FuzzyContains(this string text, string pattern)
        {
            var i = -1;
            foreach (var letter in pattern)
            {
                i = text.IndexOf(letter, i + 1);
                if (i < 0) return false;
            }

            return true;
        }

        public static string NoParse(this string message, ref int totalPrefixLength, ref int totalSuffixLength)
            => message.DecorateTag("noparse", ref totalPrefixLength, ref totalSuffixLength);

        public static string Colorize(this string message, Color color) => Colorize(message, color, ref _tmp, ref _tmp);

        public static string Colorize(this string message, Color color, ref int totalPrefixLength, ref int totalSuffixLength)
        {
            const string suffix = "</color>";
            var prefix = $"<color=#{ColorUtility.ToHtmlStringRGB(color)}>";
            totalPrefixLength += prefix.Length;
            totalSuffixLength += suffix.Length;
            return $"{prefix}{message}{suffix}";
        }

        public static string Decorate(this string message, StringDecoration decoration) => Decorate(message, decoration, ref _tmp, ref _tmp);

        public static string Decorate(this string message, StringDecoration decoration, ref int totalPrefixLength, ref int totalSuffixLength)
        {
            if (decoration.HasFlag(StringDecoration.Italic))
                message = message.DecorateItalic(ref totalPrefixLength, ref totalSuffixLength);

            if (decoration.HasFlag(StringDecoration.Bold))
                message = message.DecorateBold(ref totalPrefixLength, ref totalSuffixLength);

            if (decoration.HasFlag(StringDecoration.Underline))
                message = message.DecorateUnderline(ref totalPrefixLength, ref totalSuffixLength);

            if (decoration.HasFlag(StringDecoration.Strikethrough))
                message = message.DecorateStrikethrough(ref totalPrefixLength, ref totalSuffixLength);

            return message;
        }

        static string DecorateTag(this string message, string tag, ref int totalPrefixLength, ref int totalSuffixLength)
        {
            string prefix = $"<{tag}>";
            string suffix = $"</{tag}>";
            totalPrefixLength += prefix.Length;
            totalSuffixLength += suffix.Length;
            return $"{prefix}{message}{suffix}";
        }

        static string DecorateItalic(this string message, ref int totalPrefixLength, ref int totalSuffixLength)
            => message.DecorateTag("i", ref totalPrefixLength, ref totalSuffixLength);

        static string DecorateBold(this string message, ref int totalPrefixLength, ref int totalSuffixLength)
            => message.DecorateTag("b", ref totalPrefixLength, ref totalSuffixLength);

        static string DecorateUnderline(this string message, ref int totalPrefixLength, ref int totalSuffixLength)
            => message.DecorateTag("u", ref totalPrefixLength, ref totalSuffixLength);

        static string DecorateStrikethrough(this string message, ref int totalPrefixLength, ref int totalSuffixLength)
            => message.DecorateTag("s", ref totalPrefixLength, ref totalSuffixLength);
    }
}