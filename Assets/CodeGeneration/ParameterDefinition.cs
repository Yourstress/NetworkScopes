
using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;

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