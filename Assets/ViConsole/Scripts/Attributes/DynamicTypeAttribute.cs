using System;

namespace ViConsole.Attributes
{
    public class DynamicTypeAttribute : Attribute
    {
        public string ProviderName { get; }
        public string DependendOn { get; }

        public DynamicTypeAttribute(string providerName, string dependendOn="")
        {
            ProviderName = providerName;
            DependendOn = dependendOn;
        }
    }
}