using System;

namespace NetworkScopes
{
	public static class TypeUtility
	{
		public static string GetTypeName(Type type, bool resolveGenerics = true)
		{
			if (resolveGenerics && type.IsGenericType)
			{
				Type[] genericArguments = type.GetGenericArguments();
				string[] paramTypes = Array.ConvertAll(genericArguments, t => t.Name);

				string typeFlatName = type.GetGenericTypeDefinition().Name;
				typeFlatName = typeFlatName.Substring(0, typeFlatName.Length - 2);

				return $"{typeFlatName}<{string.Join(",", paramTypes)}>";
			}

			return type.Name;
		}
	}
}