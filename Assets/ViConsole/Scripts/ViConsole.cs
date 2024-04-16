using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;
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
        CustomStyleProperty<Color> _propertyColorOperator = new CustomStyleProperty<Color>("--color-operator");
        CustomStyleProperty<Color> _propertyColorLiterals = new CustomStyleProperty<Color>("--color-literals");

        Dictionary<LogType, InputStyle> logStyles = new();
        InputStyleSheet styleSheet;

        [SerializeField] UIDocument doc;

        [Header("Config")] [SerializeField] int scrollback = 100;
        [SerializeField] InputAction toggleConsoleAction;

        InputAction _previousCommandAction;
        InputAction _nextCommandAction;


        //internal static ViConsole Instance { get; private set; }

        //internal Tree.Tree CommandTree => _commandRunner.CommandTree;

        RingList<MessageEntry> messages = new();

        VisualElement _root;
        ListView _listView;
        ConsoleInputControl _inputBox;
        ICommandRunner _commandRunner = new CommandRunner();
        bool _isOpen = false;

        bool _styleSet = false;

        void Awake()
        {
            // if (Instance != null)
            // {
            //     Debug.LogWarning("Multiple ViConsole instances detected, destroying the new one.");
            //     Destroy(gameObject);
            //     return;
            // }
            //
            // Instance = this;
            DontDestroyOnLoad(gameObject);

            _nextCommandAction = new InputAction("NextCommand", InputActionType.Button, "<Keyboard>/downArrow");
            _previousCommandAction = new InputAction("PreviousCommand", InputActionType.Button, "<Keyboard>/upArrow");
        }

        async void Start()
        {
            await _commandRunner.Initialize(AddMessage);
            messages.MaxLength = scrollback;
            Application.logMessageReceivedThreaded += OnLogReceived;

            _root = doc.rootVisualElement;
            CloseConsole();
            _root.RegisterCallback<CustomStyleResolvedEvent>(CustomStylesResolved);

            _listView = _root.Q<ListView>();

            _listView.makeItem = MakeItem;
            _listView.bindItem = BindItem;
            _listView.itemsSource = messages;

            _listView.Q<ScrollView>().mouseWheelScrollSize = 10000;

            _inputBox = _root.Q<ConsoleInputControl>();
            _inputBox.Controller = this;
            if (!_styleSet)
            {
                ApplyCustomStyle(_root.customStyle);
                _inputBox.SetStyle(styleSheet);
                _styleSet = true;
            }

        }

        void OnLogReceived(string message, string stacktrace, LogType type)
        {
            var timestamp = $"[{DateTime.Now:HH:mm:ss}]".Colorize(Color.gray);
            AddMessage($" [{type.ToString()}] {message}", type);
        }

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

        void OnEnable()
        {
            toggleConsoleAction.Enable();
            toggleConsoleAction.performed += OnToggleConsole;
        }

        void OnDisable()
        {
            CloseConsole();
        }

        public void OpenConsole()
        {
            _nextCommandAction.Enable();
            _nextCommandAction.performed += OnHistoryNext;
            _previousCommandAction.Enable();
            _previousCommandAction.performed += OnHistoryPrev;
            _root.style.visibility = new StyleEnum<Visibility>(Visibility.Visible);
            _isOpen = true;
            _inputBox?.SetFocus();
        }

        public void CloseConsole()
        {
            _nextCommandAction.Disable();
            _nextCommandAction.performed -= OnHistoryNext;
            _previousCommandAction.Disable();
            _previousCommandAction.performed -= OnHistoryPrev;
            _root.style.visibility = new StyleEnum<Visibility>(Visibility.Hidden);
            _isOpen = false;
        }

        void OnToggleConsole(InputAction.CallbackContext obj)
        {
            if (_isOpen)
                CloseConsole();
            else OpenConsole();
        }

        void OnHistoryNext(InputAction.CallbackContext obj) => _inputBox?.NextCommand(); 
        void OnHistoryPrev(InputAction.CallbackContext obj) => _inputBox?.PreviousCommand(); 

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
                logStyles[LogType.Log] = new InputStyle(color, addNoParse: false);
            }

            if (style.TryGetValue(_propertyColorLiterals, out color))
            {
                styleSheet[LexemeType.String] = new InputStyle(color, StringDecoration.Italic);
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

            if (style.TryGetValue(_propertyColorOperator, out color))
            {
                styleSheet[LexemeType.Concatenation] = new InputStyle(color);
                styleSheet[LexemeType.OpenIndex] = new InputStyle(color);
                styleSheet[LexemeType.CloseIndex] = new InputStyle(color);
                styleSheet[LexemeType.OpenInline] = new InputStyle(color);
                styleSheet[LexemeType.CloseInline] = new InputStyle(color);
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
                _styleSet = true;
            }
            else
            {
                var inputBox = _root.Q<ConsoleInputControl>();
                inputBox?.SetStyle(styleSheet);
                _styleSet = true;
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