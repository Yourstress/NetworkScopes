
using System;
using System.Collections.Generic;

namespace NetworkScopes
{
    public static class TypeExtensions
    {
        public static bool IsList(this Type type)
        {
            return type.IsGenericType && 
                   type.GetGenericTypeDefinition() == typeof(List<>);
        }
    }
}