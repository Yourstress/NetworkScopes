
namespace NetworkScopes.CodeProcessing
{
	using System;
	using System.Collections.Generic;
	
	public class ParameterDefinition
	{
		public string Name;
		public string TypeName;
		public string TypeNamespace;
		public bool IsArray { get { return TypeName.EndsWith("[]"); } }

		private static Dictionary<string,string> typeNames = new Dictionary<string, string>()
		{
			{ typeof(int).Name, "int" },
			{ typeof(short).Name, "short" },
			{ typeof(float).Name, "float" },
			{ typeof(double).Name, "double" },
			{ typeof(bool).Name, "bool" },
			{ typeof(string).Name, "string" },
		};

		public string TypeFullName
		{
			get
			{
				if (string.IsNullOrEmpty(TypeNamespace))
					return TypeName;
				else
					return string.Format("{0}.{1}", TypeNamespace, TypeName);
			}
		}

		public static string MakeTypeName(Type type)
		{
			string name;

			if (!typeNames.TryGetValue(type.Name, out name))
			{
				return type.Name;
			}
			return name;
		}

		public ParameterDefinition(string paramName, Type type)
		{
			Name = paramName;

			TypeName = MakeTypeName(type);

			if (type.DeclaringType != null)
				TypeName = string.Format("{0}.{1}", type.DeclaringType.Name, TypeName);

			TypeNamespace = type.Namespace;
		}

		public ParameterDefinition(string paramName, string typeName)
		{
			Name = paramName;
			TypeName = typeName;
			TypeNamespace = string.Empty;
		}
	}
}