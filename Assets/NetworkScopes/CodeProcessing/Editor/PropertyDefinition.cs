
namespace NetworkScopes.CodeProcessing
{

	public class PropertyDefinition : FieldDefinition
	{
		public MethodBodyDefinition getterBody;
		public MethodBodyDefinition setterBody;

		public PropertyDefinition(string name, string typeName, bool isPublic) : base(name, typeName, isPublic)
		{
		}

		public override void Write(ScriptWriter writer)
		{
			writer.WriteFullLineFormat("{0} {1} {2}", IsPublic ? "public":"private", TypeName, Name);

			// write property getter/setter
			writer.BeginScope();

			if (getterBody != null)
			{
				writer.WriteFullLine("get");
				getterBody.Write(writer);
			}

			if (setterBody != null)
			{
				writer.WriteFullLine("set");
				setterBody.Write(writer);
			}

			writer.EndScope();
		}
	}

}