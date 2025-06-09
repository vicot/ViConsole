using UnityEngine.UIElements;

namespace ViConsole.Extensions
{
    public static class UiToolkitExtensions
    {
        public static T WithClassName<T>(this T element, string className) where T : VisualElement
        {
            element.AddToClassList(className);
            return element;
        }
    }
}