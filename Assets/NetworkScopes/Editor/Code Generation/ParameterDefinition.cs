using System.Collections.Generic;
using System.Linq;

namespace NetworkScopes.CodeGeneration
{
	public class ParameterDefinition
	{
		public string name;
		public TypeDefinition type;

		public ParameterDefinition(string name, TypeDefinition type)
		{
			this.name = name;
			this.type = type;
		}

		public static string GetString(IEnumerable<ParameterDefinition> parameters)
		{
			if (parameters == null)
				return string.Empty;

			string[] parametersArr = parameters.Select(p => string.Format("{0} {1}", p.type.Name, p.name)).ToArray();
			return string.Join(", ", parametersArr);
		}
	}
}