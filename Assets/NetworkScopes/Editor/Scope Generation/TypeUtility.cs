
using System;

namespace NetworkScopes.CodeGeneration
{
    public static class TypeUtility
    {
        public static string GetReadableName(this Type type)
        {
            return GetReadableTypeName(type);
        }
    
        public static string GetReadableTypeName(Type type)
        {
            string typeName = GetReadableTypeName(type.Name);

            if (typeName == null)
            {
                
                Type declaringType = type.DeclaringType;

                if (type.IsNested && declaringType != null && !type.IsGenericParameter)
                {
                    return $"{declaringType.Name}.{type.Name}";
                }
            
                return type.Name;
            }

            return typeName;
        }

        public static string GetReadableTypeName(string typeName)
        {
            switch (typeName)
            {
                case "Object": return "object";
                case "String": return "string";
                case "Boolean": return "bool";
                case "Byte": return "byte";
                case "Char": return "char";
                case "Decimal": return "decimal";
                case "Double": return "double";
                case "Int16": return "short";
                case "Int32": return "int";
                case "Int64": return "long";
                case "SByte": return "sbyte";
                case "Single": return "float";
                case "UInt16": return "ushort";
                case "UInt32": return "uint";
                case "UInt64": return "ulong";
                case "Void": return "void";
                default: return typeName;
            }
        }
    }
}