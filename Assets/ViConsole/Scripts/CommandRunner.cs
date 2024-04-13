using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using ViConsole.Attributes;

namespace ViConsole
{
    public class CommandRunner
    {
        public static CommandRunner Instance { get; } = new();
        static readonly Dictionary<string, MethodInfo> _commands = new();
        
        public async Task Initialize()
        {
            await DiscoverCommands();
        }

        public void Execute(string name)
        {
            Debug.Log($"Executing command {name}");
            if (_commands.TryGetValue(name, out var method))
            {
                method.Invoke(null, null);
            }
        }
        
        private async Task DiscoverCommands()
        {
            var assembly = Assembly.GetExecutingAssembly();
            foreach (var type in assembly.GetTypes())
            {
                var methods = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                foreach (var method in methods)
                {
                    var attribute = method.GetCustomAttribute<CommandAttribute>();
                    if (attribute != null)
                        _commands[attribute.Name] = method;
                }
            }
        }

        [Command("ls")]
        private static async Task ListAllCommands()
        {
            foreach (var commandName in _commands.Keys)
            {
                Debug.Log(commandName);
            }
        }  
    }
}