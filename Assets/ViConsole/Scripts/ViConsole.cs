using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UIElements;

namespace ViConsole
{
    public class ViConsole : MonoBehaviour
    {
        [SerializeField] UIDocument doc;

        [Header("Config")] [SerializeField] int scrollback = 100;

        //ConcurrentQueue<string> messages = new();
        RingList<string> messages = new();

        //List<string> messages = new();
        ListView _listView;

        void Start()
        {
            OpenConsole();
            messages.MaxLength = scrollback;
            Application.logMessageReceivedThreaded += OnLogReceived;
        }

        void OnLogReceived(string message, string stacktrace, LogType type)
        {
            messages.Add(message);
            _listView.schedule.Execute(() => _listView.RefreshItems());
        }

        [ContextMenu("Open Console")]
        public async void OpenConsole()
        {
            CommandRunner.Instance.Initialize();
            var root = doc.rootVisualElement;
            _listView = root.Q<ListView>();

            Label makeLabel()
            {
                var label = new Label();
                label.AddToClassList("wrap");
                return label;
            }

            _listView.makeItem = makeLabel;
            _listView.bindItem = (e, i) => ((Label)e).text = messages[i];
            _listView.unbindItem = (e, i) => ((Label)e).text = "<--->";
            //_listView.destroyItem = element => element.RemoveFromHierarchy();
            _listView.itemsSource = messages;

            _listView.Q<ScrollView>().mouseWheelScrollSize = 10000;
        }

    }
}