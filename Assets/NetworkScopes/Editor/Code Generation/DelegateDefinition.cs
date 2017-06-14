using System.Collections.Generic;
using System.Linq;

namespace NetworkScopes.CodeGeneration
{
	public class DelegateDefinition : IWritable
	{
		public AccessModifier accessModifier = AccessModifier.Public;
		public TypeDefinition returnType;
		public string name;
		public List<ParameterDefinition> parameters;

		public DelegateDefinition(string name)
		{
			this.name = name;
		}

		public DelegateDefinition(string name, IEnumerable<ParameterDefinition> parameters) : this(name)
		{
			this.parameters = new List<ParameterDefinition>(parameters);
		}

		public void Write(ScriptWriter writer)
		{
			string returnTypeStr = (returnType == null) ? "void" : returnType.Name;

			writer.WriteFullLineFormat("{0} delegate {1} {2}({3});", accessModifier.ToString().ToLower(), returnTypeStr, name, ParameterDefinition.GetString(parameters));
		}
	}
}