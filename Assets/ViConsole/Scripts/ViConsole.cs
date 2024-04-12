using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UIElements;

namespace ViConsole
{
    public class ViConsole : MonoBehaviour
    {
        [SerializeField] UIDocument doc;

        void Start()
        {
            OpenConsole();
        }

        [ContextMenu("Open Console")]
        public void OpenConsole()
        {
            var root = doc.rootVisualElement;
            var listView = root.Q<ListView>();
            var items = new List<string>()
            {
                "hello",
                "world",
                "hello <color=red>world</color>!",
                "hello <color=red>world</color>! hello <color=red>world</color>! hello <color=red>world</color>! hello <color=red>world</color>! hello <color=red>world</color>! hello <color=red>world</color>! hello <color=red>world</color>! hello <color=red>world</color>! hello <color=red>world</color>! hello <color=red>world</color>! hello <color=red>world</color>! hello <color=red>world</color>! hello <color=red>world</color>! hello <color=red>world</color>! hello <color=red>world</color>! hello <color=red>world</color>! hello <color=red>world</color>! hello <color=red>world</color>! hello <color=red>world</color>! hello <color=red>world</color>! hello <color=red>world</color>! hello <color=red>world</color>!",
                "world",
                "world",
                "world",
                "world",
                "world",
                "world",
                "world",
                "world",
                "world",
                "world",
                "world",
                "world",
                "world",
                "world",
                "world",
                "world",
            };

            Label makeLabel()
            {
                var label = new Label();
                label.AddToClassList("wrap");
                return label;
            }

            listView.makeItem = makeLabel;
            listView.bindItem = (e, i) => ((Label)e).text = items[i];
            listView.itemsSource = items;

            listView.Q<ScrollView>().mouseWheelScrollSize = 10000;

            // var inputBox = root.Q<TextField>();
            // inputBox.Q<TextElement>().enableRichText = true;
            // inputBox.Q<TextElement>().parseEscapeSequences = true;
            // inputBox.RegisterValueChangedCallback(OnValueChanged);
        }

        // void OnValueChanged(ChangeEvent<string> args)
        // {
        //     Debug.Log(args.newValue);
        //     // if (args.target is not TextField txt) return;
        //     //
        //     // // removed
        //     // if (args.newValue.Length < args.previousValue.Length)
        //     //     return;
        //     //
        //     // var diffPosition = 0;
        //     // for (int i = 0, j = 0; i < args.newValue.Length && j < args.previousValue.Length; ++i, ++j)
        //     // {
        //     //     if()
        //     // }
        //     //
        //     // txt.SetValueWithoutNotify("");
        // }
    }
}