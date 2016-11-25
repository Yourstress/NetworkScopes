
namespace NetworkScopes.CodeProcessing
{
	public class FieldDefinition
	{
		public string Name;
		public string TypeName;
		public bool IsPublic;

		public FieldDefinition(string name, string typeName, bool isPublic)
		{
			Name = name;
			TypeName = typeName;
			IsPublic = isPublic;
		}

		public virtual void Write(ScriptWriter writer)
		{
			writer.WriteFullLineFormat("{0} {1} {2};", IsPublic ? "public":"private", TypeName, Name);
		}
	}

}