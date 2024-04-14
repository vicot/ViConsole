using UnityEngine;
using ViConsole.Attributes;

namespace ViConsole
{
    public class Tester : MonoBehaviour
    {
        [Command("help")]
        [ContextMenu("help")]
        public void Help()
        {
            Debug.Log("Help command executed");
        }
    }
}