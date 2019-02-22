
using System;
using System.Reflection;

public static class ReflectionUtility
{
	public static T GetAttribute<T>(MemberInfo member)
	{
		Type targetType = typeof(T);
		object[] attrs = member.GetCustomAttributes(false);
		for (int x = 0; x < attrs.Length; x++)
			if (targetType == attrs[x].GetType())
				return (T)attrs[x];

		return default(T);
	}

	public static bool ContainsAttribute<T>(MemberInfo member)
	{
		object[] attrs = member.GetCustomAttributes(false);
		return ContainsAttribute(attrs, typeof(T));
	}

	private static bool ContainsAttribute(object[] attrs, Type targetType)
	{
		for (int x = 0; x < attrs.Length; x++)
			if (targetType == attrs[x].GetType())
				return true;

		return false;
	}

	public static ConstructorInfo GetParameterlessConstructor(Type type)
	{
		foreach (ConstructorInfo ci in type.GetConstructors())
		{
			if (ci.GetParameters().Length == 0)
				return ci;
		}

		return null;
	}
}