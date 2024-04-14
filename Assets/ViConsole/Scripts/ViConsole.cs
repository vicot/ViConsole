using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.TextCore.Text;
using UnityEngine.UIElements;
using ViConsole.Extensions;
using ViConsole.Parsing;
using ViConsole.Tree;
using ViConsole.UIToolkit;

namespace ViConsole
{
    public class ViConsole : MonoBehaviour
    {
        CustomStyleProperty<Color> _propertyPrimaryColor = new CustomStyleProperty<Color>("--primary-color");
        CustomStyleProperty<Color> _propertySecondaryColor = new CustomStyleProperty<Color>("--secondary-color");
        CustomStyleProperty<Color> _propertyColorWarning = new CustomStyleProperty<Color>("--color-warning");
        CustomStyleProperty<Color> _propertyColorError = new CustomStyleProperty<Color>("--color-error");
        CustomStyleProperty<Color> _propertyColorException = new CustomStyleProperty<Color>("--color-exception");
        CustomStyleProperty<Color> _propertyColorVariable = new CustomStyleProperty<Color>("--color-variable");

        Dictionary<LogType, InputStyle> logStyles = new();
        InputStyleSheet styleSheet;

        [SerializeField] UIDocument doc;

        [Header("Config")] [SerializeField] int scrollback = 100;

        internal static ViConsole Instance { get; private set; }

        internal Tree.Tree CommandTree => _commandRunner.CommandTree;

        RingList<MessageEntry> messages = new();

        VisualElement _root;
        ListView _listView;
        ConsoleInputControl _inputBox;
        ICommandRunner _commandRunner = new CommandRunner();

        bool styleSet = false;

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
            await _commandRunner.Initialize(AddMessage);
            OpenConsole();
            messages.MaxLength = scrollback;
            Application.logMessageReceivedThreaded += OnLogReceived;
        }

        void OnLogReceived(string message, string stacktrace, LogType type) => AddMessage($"[{type.ToString()}] {message}", type);

        void AddMessage(string message, LogType level = LogType.Log)
        {
            if (logStyles.TryGetValue(level, out var style))
            {
                message = style.ApplyStyle(message);
            }

            messages.Add(new MessageEntry { Message = message, Level = level });
            _listView?.schedule.Execute(() =>
            {
                _listView.RefreshItems();
                _listView.ScrollToItem(messages.Count());
            });
        }

        [ContextMenu("Open Console")]
        public async void OpenConsole()
        {
            _root = doc.rootVisualElement;
            _root.RegisterCallback<CustomStyleResolvedEvent>(CustomStylesResolved);
            //var c = root.customStyle.TryGetValue(new CustomStyleProperty<Color>("--background-color"), out var color);
            //Debug.Log(c);
            //Debug.Log(color);

            _listView = _root.Q<ListView>();

            _listView.makeItem = MakeItem;
            _listView.bindItem = BindItem;
            //_listView.unbindItem = UnbindItem;
            _listView.itemsSource = messages;

            _listView.Q<ScrollView>().mouseWheelScrollSize = 10000;

            _inputBox = _root.Q<ConsoleInputControl>();
            _inputBox.Controller = this;
            if (!styleSet && styleSheet != null)
            {
                _inputBox.SetStyle(styleSheet);
                styleSet = true;
            }
        }

        void CustomStylesResolved(CustomStyleResolvedEvent evt)
        {
            ApplyCustomStyle(evt.customStyle);
        }

        void ApplyCustomStyle(ICustomStyle style)
        {
            styleSheet = new InputStyleSheet();
            Color color;
            if (style.TryGetValue(_propertyPrimaryColor, out color))
            {
                styleSheet[LexemeType.String] = new InputStyle(color, StringDecoration.Italic);
                logStyles[LogType.Log] = new InputStyle(color, addNoParse: false);
            }

            if (style.TryGetValue(_propertySecondaryColor, out color))
            {
                styleSheet[LexemeType.Command] = new InputStyle(color, StringDecoration.Bold);
            }

            if (style.TryGetValue(_propertyColorVariable, out color))
            {
                styleSheet[LexemeType.Identifier] = new InputStyle(color);
                styleSheet[LexemeType.SpecialIdentifier] = new InputStyle(color);
            }

            if (style.TryGetValue(_propertyColorError, out color))
            {
                styleSheet[LexemeType.Invalid] = new InputStyle(color, StringDecoration.Italic);
                logStyles[LogType.Error] = new InputStyle(color, addNoParse: false);
            }

            if (style.TryGetValue(_propertyColorWarning, out color))
            {
                logStyles[LogType.Warning] = new InputStyle(color, addNoParse: false);
            }

            if (style.TryGetValue(_propertyColorException, out color))
            {
                logStyles[LogType.Exception] = new InputStyle(color, addNoParse: false);
            }

            if (_inputBox != null)
            {
                _inputBox.SetStyle(styleSheet);
                styleSet = true;
            }
            else
            {
                var inputBox = _root.Q<ConsoleInputControl>();
                inputBox?.SetStyle(styleSheet);
                styleSet = true;
            }
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
            label.RegisterCallback<MouseEnterEvent>(evt => { label.style.backgroundColor = label.resolvedStyle.backgroundColor; });
            label.RegisterCallback<MouseLeaveEvent>(evt => { label.style.backgroundColor = StyleKeyword.Null; });
            return label;
        }

        public void ExecuteCommand(IEnumerable<Token> tokens, string rawCommand)
        {
            try
            {
                var result = _commandRunner.ExecuteCommand(tokens);
                if (result != null)
                    AddMessage($"'{result}'", LogType.Log);
            }
            catch (CommandException e)
            {
                AddMessage(rawCommand, LogType.Exception);
                AddMessage($"{e.Message}", LogType.Exception);
            }
        }
    }
}