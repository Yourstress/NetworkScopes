
using System;
using System.Linq;

namespace CodeGeneration
{

	public class ParameterDefinition
	{
		public string Name;
		public string TypeName;
		public string TypeNamespace;
		public bool IsArray { get { return TypeName.EndsWith("[]"); } }

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

		public ParameterDefinition(string paramName, Type type)
		{
			Name = paramName;

			if (type.IsGenericType)
			{
				Type genericTypeDefinition = type.GetGenericTypeDefinition();
				string genericTypeName = genericTypeDefinition.Name;
				genericTypeName = genericTypeName.Substring(0, genericTypeName.LastIndexOf('`'));
				string[] genericTypeNames = type.GetGenericArguments().Select(arg => arg.Name).ToArray();
				TypeName = string.Format("{0}<{1}>", genericTypeName, string.Join(", ",genericTypeNames));
			}
			else
				TypeName = type.Name;

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