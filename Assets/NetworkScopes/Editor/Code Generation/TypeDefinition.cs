using System;
using System.Text;

namespace NetworkScopes.CodeGeneration
{
	public class TypeDefinition
	{
		public string Name;
		public string Namespace;

		public TypeDefinition(string name)
		{
			Name = name;
		}

		public TypeDefinition(string name, string namespaceName) : this(name)
		{
			Namespace = namespaceName;
		}

		public static TypeDefinition MakeGenericType(TypeDefinition type, params TypeDefinition[] genericParams)
		{
			return MakeGenericType(type, Array.ConvertAll(genericParams, t => t.Name));
		}

		public static TypeDefinition MakeGenericType(TypeDefinition type, params string[] genericParams)
		{
			StringBuilder nameBuilder = new StringBuilder(type.Name.Substring(0, type.Name.LastIndexOf('`')));

			if (genericParams != null)
			{
				nameBuilder.Append("<");
				for (var x = 0; x < genericParams.Length; x++)
				{
					string genParam = genericParams[x];

					nameBuilder.Append(genParam);

					if (x != genericParams.Length - 1)
						nameBuilder.Append(",");
				}
				nameBuilder.Append(">");
			}
			return new TypeDefinition(nameBuilder.ToString(), type.Namespace);
		}

		public static implicit operator TypeDefinition(Type t)
		{
			return new TypeDefinition(t.GetReadableName())
			{
				Namespace = t.Namespace
			};
		}
	}
}