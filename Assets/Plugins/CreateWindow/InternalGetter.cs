using System;
using System.Reflection;

namespace CreateWindow
{
    internal class InternalGetter<T> where T : Delegate
    {
        private readonly Type type;
        private readonly string funcName;

        public InternalGetter(Type type, string funcName)
        {
            this.type = type;
            this.funcName = funcName;
        }
        
        public T Func
        {
            get
            {
                return (T)type
                    .GetMethod(funcName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
                    .CreateDelegate(typeof(T), null);
            }
        }
    }
}