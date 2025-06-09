using System;

namespace ViConsole.Attributes
{
    public class PresenterProviderForAttribute : Attribute
    {
        public Type ProviderForType { get; }


        public PresenterProviderForAttribute(Type providerForType)
        {
            ProviderForType = providerForType;
        }
    }
}