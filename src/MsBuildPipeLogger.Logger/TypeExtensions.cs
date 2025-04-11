using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace MsBuildPipeLogger
{
    internal static class TypeExtensions
    {
        public static PropertyInfo GetProperty(this Type type, string name) => type.GetTypeInfo().GetDeclaredProperty(name);

        public static MethodInfo GetGetMethod(this PropertyInfo property) => property.GetMethod;

        public static FieldInfo GetField(this Type type, string name) => type.GetTypeInfo().GetDeclaredField(name);
    }
}
