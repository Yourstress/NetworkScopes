using System.Collections.Generic;

namespace NetworkScopes.CodeGeneration
{
	public class EventDefinition : IWritable
	{
		public string definition;
		public string name;
		public List<ParameterDefinition> parameters;

		public EventDefinition(string eventName, DelegateDefinition sourceDelegate)
		{
			definition = sourceDelegate.name;
			name = eventName;
			parameters = sourceDelegate.parameters;
		}

		public void Write(ScriptWriter writer)
		{
			writer.WriteFullLineFormat("public event {0} {1} = delegate {2};", definition, name, "{}");
		}
	}
}