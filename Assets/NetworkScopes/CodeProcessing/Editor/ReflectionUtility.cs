
namespace NetworkScopes
{
	using System;
	using System.Reflection;

	public class ReflectionUtility
	{
		public static Type writerType { get { return typeof(INetworkWriter); } }
		public static Type readerType { get { return typeof(INetworkReader); } }

		public static MethodInfo FindSerializer(Type type)
		{
			return FindMethodWithParameterType(writerType.GetMethods(), type);
		}

		public static MethodInfo FindMethodWithParameterType(MethodInfo[] methods, Type type)
		{
			for (int x = 0; x < methods.Length; x++)
			{
				ParameterInfo[] methodParams = methods[x].GetParameters();

				if (methodParams.Length == 1 && methodParams[0].ParameterType == type)
					return methods[x];
			}
			return null;
		}
	}
}