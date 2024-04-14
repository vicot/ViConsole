using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.TextCore.Text;
using UnityEngine.UIElements;
using ViConsole.Parsing;
using ViConsole.Tree;
using ViConsole.UIToolkit;

namespace ViConsole
{
    public class ViConsole : MonoBehaviour
    {
        [SerializeField] UIDocument doc;

        [Header("Config")] [SerializeField] int scrollback = 100;

        internal static ViConsole Instance { get; private set; }
        

        internal Tree.Tree CommandTree => _commandRunner.CommandTree;

        RingList<MessageEntry> messages = new();

        ListView _listView;
        ICommandRunner _commandRunner = new CommandRunner();

        void Awake()
        {
            if (Instance != null)
            {
                Debug.LogWarning("Multiple ViConsole instances detected, destroying the new one.");
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        async void Start()
        {
            await _commandRunner.Initialize();
            OpenConsole();
            messages.MaxLength = scrollback;
            Application.logMessageReceivedThreaded += OnLogReceived;
        }

        void OnLogReceived(string message, string stacktrace, LogType type) => AddMessage($"[{type.ToString()}] {message}", type);

        void AddMessage(string message, LogType level = LogType.Log)
        {
            messages.Add(new MessageEntry { Message = message, Level = level });
            _listView.schedule.Execute(() =>
            {
                _listView.RefreshItems();
                _listView.ScrollToItem(messages.Count());
            });
        }

        [ContextMenu("Open Console")]
        public async void OpenConsole()
        {
            var root = doc.rootVisualElement;
            _listView = root.Q<ListView>();

            _listView.makeItem = MakeItem;
            _listView.bindItem = BindItem;
            //_listView.unbindItem = UnbindItem;
            _listView.itemsSource = messages;

            _listView.Q<ScrollView>().mouseWheelScrollSize = 10000;

            var _inputBox = root.Q<ConsoleInputControl>();
            _inputBox.Controller = this;
        }

        void BindItem(VisualElement element, int index)
        {
            Label label = ((Label)element);
            var message = messages[index];
            label.text = message.Message;
            foreach (var cls in label.GetClasses().Where(c => c.StartsWith("log-")).ToList())
                label.RemoveFromClassList(cls);

            if (message.Level != LogType.Log) label.AddToClassList($"log-{message.Level.ToString().ToLower()}");
        }

        // void UnbindItem(VisualElement element, int index)
        // {
        //     Label label = ((Label)element);
        //     label.text = "<--->";
        //     foreach (var cls in label.GetClasses().Where(c => c.StartsWith("log-")))
        //     {
        //         label.RemoveFromClassList(cls);
        //     }
        // }

        Label MakeItem()
        {
            var label = new Label();
            label.AddToClassList("wrap");
            label.AddToClassList("log");
            return label;
        }

        public object ExecuteCommand(IEnumerable<Token> tokens, string rawCommand)
        {
            try
            {
                return _commandRunner.ExecuteCommand(tokens);
            }
            catch (CommandException e)
            {
                AddMessage(rawCommand, LogType.Exception);
                AddMessage($"{e.Message}", LogType.Exception);
            }

            return null;
        }

        
    }
}