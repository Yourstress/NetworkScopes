
namespace CodeGeneration
{
	using System;
	using System.Collections.Generic;

	public interface IWritable
	{
		void Write(ScriptWriter writer);
	}

	public interface IImporter
	{
		void Import(params Type[] types);
	}

	public class ClassDefinition : IWritable, IImporter
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

		public string[] genericArgs;

		// if not set, Name is used instead
		private string _fileName;
		public string FileName
		{
			get => _fileName ?? Name;
			set => _fileName = value;
		}

		public HashSet<string> imports { get; private set; }
		public List<FieldDefinition> fields { get; private set; }
		public List<PropertyDefinition> properties { get; private set; }
		public List<ClassDefinition> classes { get; private set; }
		public List<MethodDefinition> methods { get; private set; }

		public List<ClassDefinition> dependancies { get; private set; }

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
			dependancies = new List<ClassDefinition>();
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

		public void AddMethodImports()
		{
			foreach (MethodDefinition method in methods)
			{
				// add method imports
				if (method.importedTypes != null)
				{
					Import(method.importedTypes.ToArray());
				}
			}
		}

		public void Write(ScriptWriter writer)
		{
			AddMethodImports();

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

			if (genericArgs != null && genericArgs.Length > 0)
			{
				writer.Write("<");
				for (int x = 0; x < genericArgs.Length; x++)
				{
					if (x != 0)
						writer.Write(",");
					writer.Write(genericArgs[x]);
				}
				writer.Write(">");
			}
			writer.EndWrite();

			writer.BeginScope();

			// write fields and properties
			WriteMultiple(writer, fields);

			WriteMultiple(writer, properties);

			// write methods
			WriteMultiple(writer, methods);

			// write child classes
			WriteMultiple(writer, classes);

			writer.EndScope();
		}

		void WriteMultiple(ScriptWriter writer, IEnumerable<IWritable> writables)
		{
			bool didWriteAny = false;
			foreach (IWritable writable in writables)
			{
				didWriteAny = true;
				writable.Write(writer);
			}

			if (didWriteAny)
				writer.NewLine();
		}

		public void Import(params Type[] types)
		{
			foreach (Type t in types)
			{
				string ns = t.Namespace;

				if (!string.IsNullOrEmpty(ns) && ns != Namespace)
					imports.Add(ns);
			}
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