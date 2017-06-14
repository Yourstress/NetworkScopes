using System;
using System.CodeDom;
using System.Linq;
using System.Reflection;
using Microsoft.CSharp;

namespace NetworkScopes.CodeGeneration
{
	public static class SignalUtility
	{
		private static CSharpCodeProvider provider;

		public static string GetReadableName(this Type type)
		{
			switch (type.Name)
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
				default:
					return type.Name;
			}
		}

		/// <summary>
		/// Finds a method in ISignalReader that reads the specified type.
		/// </summary>
		public static MethodInfo GetReaderMethod(Type type)
		{
			return typeof(ISignalReader).GetMethods().FirstOrDefault(m => m.ReturnType == type);
		}

		public static MethodInfo GetWriterMethod(Type type)
		{
			return typeof(ISignalWriter).GetMethods().FirstOrDefault(m =>
			{
				ParameterInfo[] parameters = m.GetParameters();

				return parameters.Length == 1 && parameters[0].ParameterType == type;
			});
		}
	}
}