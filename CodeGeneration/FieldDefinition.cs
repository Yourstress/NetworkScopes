using System.Collections.Generic;

//using System.Reflection;

namespace CodeGeneration
{
	public class FieldDefinition : IWritable
	{
		public string Name;
		public string TypeName;
		public bool IsPublic;
		public bool IsReadonly;
		public string assignment = null;

		public FieldDefinition(string name, string typeName, bool isPublic)
		{
			Name = name;
			TypeName = typeName;
			IsPublic = isPublic;
		}

		public virtual void Write(ScriptWriter writer)
		{
			string assignmentStr = string.Empty;

			if (!string.IsNullOrEmpty(assignment))
				assignmentStr = " = " + assignment;

			List<string> mods = new List<string>(4);
			mods.Add(IsPublic ? "public" : "private");
			if (IsReadonly)
				mods.Add("readonly");
			mods.Add(TypeName);
			mods.Add(Name+assignmentStr);

			writer.WriteFullLine(string.Join(" ", mods.ToArray()) + ";");
		}
	}
	
}