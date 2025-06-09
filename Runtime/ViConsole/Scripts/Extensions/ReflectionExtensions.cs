using System;
using System.Reflection;

namespace ViConsole.Extensions
{
    internal static class ReflectionExtensions
    {
        public static bool MatchesDelegate(this MethodInfo method, Type @delegate)
        {
            var delegateInvoke = @delegate.GetMethod("Invoke");
            if (delegateInvoke == null)
                return false;

            if (method.ReturnType != delegateInvoke.ReturnType)
                return false;

            ParameterInfo[] methodParams = method.GetParameters();
            ParameterInfo[] delegateParams = delegateInvoke.GetParameters();

            if (methodParams.Length != delegateParams.Length)
                return false;

            for (var i = 0; i < methodParams.Length; i++)
            {
                if (methodParams[i].ParameterType != delegateParams[i].ParameterType)
                    return false;
            }

            return true;
        }
    }
}