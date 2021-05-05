using System;
using System.CodeDom;
using System.Linq;
using System.Reflection;
using Microsoft.CSharp;

namespace NetworkScopes.CodeGeneration
{
	public static class SignalUtility
	{
		public static string GetReadableName(this Type type)
		{
			return GetReadableTypeName(type.Name);
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
				default:
					return typeName;
			}
		}
	}

	/// <summary>
	/// Finds a method in ISignalReader that reads the specified type.
	/// </summary>
	public class SignalReaderMethod : SignalMethodBase
	{
		public SignalReaderMethod(Type type) : base(type)
		{
			method = typeof(ISignalReader)
				.GetMethods()
				.FirstOrDefault(m => m.ReturnType == underlyingType);
		}

		public void AddMethodCall(MethodBody targetMethod, string variableName, DeserializationOptions deserializationOptions)
		{
			if (deserializationOptions == DeserializationOptions.AllocateVariable)
			{
				string cast = IsEnum ? $"({type.Name})" : "";
				targetMethod.AddMethodCallWithAssignment(variableName, type.GetReadableName(), cast+"reader", method.Name);
			}
			else if (deserializationOptions == DeserializationOptions.DontAllocateVariable)
				targetMethod.AddMethodCallWithAssignment(variableName, "reader", method.Name);
			else
				throw new Exception("Undefined DeserializaOption");
		}
	}
	
	/// <summary>
	/// Finds a method in ISignalWriter that writes the specified type.
	/// </summary>
	public class SignalWriterMethod : SignalMethodBase
	{
		public SignalWriterMethod(Type type) : base(type)
		{
			method = typeof(ISignalWriter)
				.GetMethods()
				.FirstOrDefault(m =>
				{
					ParameterInfo[] parameters = m.GetParameters();
					return parameters.Length == 1 && parameters[0].ParameterType == underlyingType;
				});
		}

		public void AddMethodCall(MethodBody targetMethod, string variableName)
		{
			// if types are not the same (i.e. Enum) cast it
			if (type != underlyingType)
				variableName = $"({underlyingType.GetReadableName()}){variableName}";
				
			targetMethod.AddMethodCall("writer", method.Name, variableName);
		}
	}
	
	public abstract class SignalMethodBase
	{
		public readonly Type type;
		public MethodInfo method { get; protected set; }

		public bool IsEnum => type.IsEnum;

		public bool IsAvailable => method != null;

		public readonly Type underlyingType; 

		public SignalMethodBase(Type type)
		{
			this.type = type;
			underlyingType = type.IsEnum ? Enum.GetUnderlyingType(type) : type;
		}
	}
}