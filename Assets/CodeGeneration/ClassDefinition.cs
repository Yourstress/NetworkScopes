
namespace CodeGeneration
{
	using System.Collections.Generic;
	
	public class ClassDefinition
	{
		public string Name;
		public string Namespace;

		public string FullName
		{
			get { return string.IsNullOrEmpty(Namespace) ? Name : string.Format("{0}.{1}", Namespace, Name); }
		}
		public bool IsPartial;
		public bool IsStatic;

		public bool IsInvalid;

		public HashSet<string> imports { get; private set; }
		public List<FieldDefinition> fields { get; private set; }
		public List<PropertyDefinition> properties { get; private set; }
		public List<ClassDefinition> classes { get; private set; }
		public List<MethodDefinition> methods { get; private set; }

		public ClassDefinition(string name) : this(name, null, false)
		{
		}

		public ClassDefinition(string name, string namespaceName, bool partialClass)
		{
			Name = name;
			Namespace = namespaceName;

			IsPartial = partialClass;

			imports = new HashSet<string>();
			fields = new List<FieldDefinition>();
			properties = new List<PropertyDefinition>();
			classes = new List<ClassDefinition>(1);
			methods = new List<MethodDefinition>();
		}

		public FieldDefinition AddField(string name, string typeName, bool isPublic)
		{
			FieldDefinition field = new FieldDefinition(name, typeName, isPublic);

			fields.Add(field);
			return field;
		}

		public PropertyDefinition AddProperty(string name, string typeName)
		{
			PropertyDefinition prop = new PropertyDefinition(name, typeName, true);
			properties.Add(prop);
			return prop;
		}

		public void CollectImports(ref HashSet<string> imports)
		{
			foreach (string import in this.imports)
				imports.Add(import);
			
			for (int x = 0; x < classes.Count; x++)
				classes[x].CollectImports(ref imports);

			// add method parameter & body imports
			for (int x = 0; x < methods.Count; x++)
			{
				// add param imports
				MethodDefinition method = methods[x];
				for (int p = 0; p < method.parameters.Count; p++)
				{
					if (!string.IsNullOrEmpty(method.parameters[p].TypeNamespace))
						imports.Add(method.parameters[p].TypeNamespace);
				}

				// add body imports
				foreach (string importName in method.instructions.imports)
					imports.Add(importName);
			}
		}

		private void Write(ScriptWriter writer)
		{
			// write namespace
			if (!string.IsNullOrEmpty(Namespace))
			{
				writer.WriteFullLineFormat("namespace {0}", Namespace);
				writer.BeginScope();
			}

			// write class definition
			writer.BeginWrite();
			writer.Write("public ");
			if (IsStatic)
				writer.Write("static ");
			if (IsPartial)
				writer.Write("partial ");
			writer.Write("class " + Name);
			writer.EndWrite();

			writer.BeginScope();

			// write fields and properties
			for (int x = 0; x < fields.Count; x++)
				fields[x].Write(writer);

			for (int x = 0; x < properties.Count; x++)
				properties[x].Write(writer);

			// write methods
			for (int x = 0; x < methods.Count; x++)
				methods[x].Write(writer);
			
			// write child classes
			for (int x = 0; x < classes.Count; x++)
				classes[x].Write(writer);

			writer.EndScope();
		}

		public override string ToString ()
		{
			ScriptWriter writer = new ScriptWriter();

			// write imports at the very top
			HashSet<string> imports = new HashSet<string>();
			CollectImports(ref imports);
			foreach (string imp in imports)
				writer.WriteFullLineFormat("using {0};", imp);
			writer.NewLine();

			Write(writer);

			writer.Finish();
			
			return writer.ToString();
		}
	}
	
}