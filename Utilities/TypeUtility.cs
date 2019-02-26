using System;
using System.Collections.Generic;

namespace NetworkScopes
{
	public static class TypeUtility
	{
		private static Dictionary<Type, string> cleanNames = new Dictionary<Type, string>()
		{
			{typeof(bool),"bool"},

			{typeof(byte),"byte"},
			{typeof(sbyte),"sbyte"},

			{typeof(char),"char"},
			{typeof(decimal),"decimal"},

			{typeof(string),"string"},

			{typeof(int),"int"},
			{typeof(uint),"uint"},

			{typeof(long),"long"},
			{typeof(ulong),"long"},

			{typeof(short),"short"},
			{typeof(ushort),"short"},

			{typeof(float),"float"},
			{typeof(double),"double"},


		};

		public static bool IsNativeType(Type type)
		{
			return cleanNames.ContainsKey(type);
		}

		public static string GetTypeName(this Type type, bool resolveGenerics = true)
		{
			if (resolveGenerics && type.IsGenericType)
			{
				Type[] genericArguments = type.GetGenericArguments();
				string[] paramTypes = Array.ConvertAll(genericArguments, t => t.Name);

				string typeFlatName = type.GetGenericTypeDefinition().Name;
				typeFlatName = typeFlatName.Substring(0, typeFlatName.Length - 2);

				return $"{typeFlatName}<{string.Join(",", paramTypes)}>";
			}
			// clean up native name
			else
			{
				string typeName;
				if (cleanNames.TryGetValue(type, out typeName))
					return typeName;
			}

			return type.Name;
		}
	}
}