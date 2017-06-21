using System;
using System.Linq;
using System.Text;

namespace NetworkScopes.CodeGeneration
{
	public class TypeDefinition
	{
		public string Name;
		public string Namespace;

		public TypeDefinition(Type type)
		{
			Name = type.GetReadableName();
			Namespace = type.Namespace;

			if (type.IsGenericType)
				SetGenericType(type.GetGenericArguments().Select(arg => arg.Name).ToArray());
		}

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
			TypeDefinition td = new TypeDefinition(type.Name, type.Namespace);
			td.SetGenericType(genericParams);
			return td;
		}

		private void CleanGenericName()
		{
			char[] genericChars = {'`', '<'};
			int genericCharIndex = Name.LastIndexOfAny(genericChars);

			if (genericCharIndex != -1)
				Name = Name.Substring(0, genericCharIndex);
		}

		private void SetGenericType(params string[] genericParams)
		{
			CleanGenericName();

			StringBuilder nameBuilder = new StringBuilder(Name);

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

			Name = nameBuilder.ToString();
		}

		public static implicit operator TypeDefinition(Type t)
		{
			return new TypeDefinition(t);
		}
	}
}