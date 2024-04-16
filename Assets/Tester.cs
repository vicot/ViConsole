using System.Text.RegularExpressions;
using UnityEngine;
using ViConsole.Attributes;

namespace ViConsole
{
    public class Tester : MonoBehaviour
    {
        [Command("cube")]
        public void Help(object hexString)
        {
            if(hexString is not string hex || !Regex.IsMatch(hex, @"^[0-9A-Fa-f]{6}$"))
            {
                Debug.LogError("Invalid hex string");
                return;
            }
            
            Color color = HexToColor(hexString.ToString());
            GetComponent<Renderer>().material.color = color;
            Debug.Log("Changed color");
        }
        
        public static Color HexToColor(string hex)
        {
            byte r = byte.Parse(hex.Substring(0,2), System.Globalization.NumberStyles.HexNumber);
            byte g = byte.Parse(hex.Substring(2,2), System.Globalization.NumberStyles.HexNumber);
            byte b = byte.Parse(hex.Substring(4,2), System.Globalization.NumberStyles.HexNumber);
            byte a = 255; // default alpha value, fully opaque
            
            return new Color32(r, g, b, a);
        }
    }
}