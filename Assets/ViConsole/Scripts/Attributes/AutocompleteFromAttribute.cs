using System;

namespace ViConsole.Attributes
{
    public class AutocompleteFromAttribute : Attribute
    {
        public string ProviderName { get; }

        public AutocompleteFromAttribute(string providerName)
        {
            ProviderName = providerName;
        }
    }
}