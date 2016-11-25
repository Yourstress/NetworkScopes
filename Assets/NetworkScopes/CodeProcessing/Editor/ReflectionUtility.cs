
namespace NetworkScopes
{
	using System;
	using System.Reflection;

	public class ReflectionUtility
	{
		public static MethodInfo FindSerializer(Type type)
		{
			Type writerType = typeof(INetworkWriter);

			return FindMethodWithParameterType(writerType.GetMethods(), type);
		}

		public static MethodInfo FindMethodWithParameterType(MethodInfo[] methods, Type type)
		{
			for (int x = 0; x < methods.Length; x++)
			{
				ParameterInfo[] methodParams = methods[x].GetParameters();

				if (methodParams.Length == 1 && methodParams[x].ParameterType == type)
					return methods[x];
			}
			return null;
		}
	}
}