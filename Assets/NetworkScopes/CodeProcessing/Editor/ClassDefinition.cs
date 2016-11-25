using System.IO;
using System.Reflection;


namespace NetworkScopes.CodeProcessing
{
	using System.Collections.Generic;
	using System;
	using System.Text;

	public class ClassDefinition
	{
		public string Name;
		public string Namespace;
		public string BaseClass;

		public string FullName
		{
			get { return string.IsNullOrEmpty(Namespace) ? Name : string.Format("{0}.{1}", Namespace, Name); }
		}
		public bool IsPartial;
		public bool IsStatic;
		public bool IsAbstract;
		public bool IsInterface;

		public bool IsInvalid;

		public string[] genericArgs;

		public HashSet<string> imports { get; private set; }
		public List<FieldDefinition> fields { get; private set; }
		public List<PropertyDefinition> properties { get; private set; }
		public List<ClassDefinition> classes { get; private set; }
		public List<MethodDefinition> methods { get; private set; }

		public List<ClassDefinition> dependancies { get; private set; }

		public ClassDefinition() : this(null, null)
		{
		}

		public ClassDefinition(string name) : this(name, null)
		{
		}

		public ClassDefinition(string name, string namespaceName)
		{
			Name = name;
			Namespace = namespaceName;

			imports = new HashSet<string>();
			fields = new List<FieldDefinition>();
			properties = new List<PropertyDefinition>();
			classes = new List<ClassDefinition>(1);
			methods = new List<MethodDefinition>();
			dependancies = new List<ClassDefinition>();

			imports.Add(typeof(GeneratedAttribute).Namespace);
		}

		protected static string CleanGenericTypeName(string name)
		{
			int genericCharIndex = name.LastIndexOf('`');
			return name.Remove(genericCharIndex, name.Length-genericCharIndex);
		}

		protected static string AddGenericTypeParameters(string name, Type[] genericArgs)
		{
			StringBuilder nameBuilder = new StringBuilder(name);
			nameBuilder.Append("<");

			for (int x = 0; x < genericArgs.Length; x++)
			{
				Type arg = genericArgs[x].IsByRef ? genericArgs[x].GetElementType() : genericArgs[x];

				if (x != 0)
					nameBuilder.Append(",");
				nameBuilder.Append(ParameterDefinition.MakeTypeName(arg));
			}

			nameBuilder.Append(">");
			return nameBuilder.ToString();
		}

		public void SetBaseClass(Type baseClass, Type optionalGenericType = null)
		{
			string name = baseClass.Name;
			if (!string.IsNullOrEmpty(baseClass.Namespace))
				imports.Add(baseClass.Namespace);

			if (optionalGenericType != null)
			{
				Type[] genericArgs = optionalGenericType.GetGenericArguments();

				if (genericArgs.Length != 0)
				{
					name = CleanGenericTypeName(name);

					name = AddGenericTypeParameters(name, genericArgs);
				}
			}

			BaseClass = name;
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

		public void AddMethods(Type type, bool isAbstract, bool isOverride)
		{
			// add methods
			MethodInfo[] typeMethods = type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
			for (int x = 0; x < typeMethods.Length; x++)
			{
				AddMethod(typeMethods[x], isAbstract, isOverride);
			}
		}

		public virtual MethodDefinition AddMethod(MethodInfo method, bool isAbstract, bool isOverride)
		{
			MethodDefinition methodDef = new MethodDefinition(method, true);
			
			if (isAbstract)
				methodDef.IsAbstract = true;
			else if (isOverride)
				methodDef.IsOverride = true;
			
			methods.Add(methodDef);

			return methodDef;
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
			writer.WriteFullLineFormat("[{0}]", typeof(GeneratedAttribute).Name.Replace("Attribute", string.Empty));
			writer.BeginWrite();
			writer.Write("public ");
			if (IsStatic)
				writer.Write("static ");
			if (IsAbstract)
				writer.Write("abstract ");
			if (IsPartial)
				writer.Write("partial ");

			if (IsInterface)
				writer.Write("interface ");
			else
				writer.Write("class ");

			writer.Write(Name);

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
			
			if (!string.IsNullOrEmpty(BaseClass))
				writer.WriteFormat(" : {0}", BaseClass);
			
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

		public void WriteToFile(string path, bool createNamespaceDirectory, bool createIfExists)
		{
			Directory.CreateDirectory(path);

			// group scripts by namespaces
			if (createNamespaceDirectory && !string.IsNullOrEmpty(Namespace))
			{
				if (Namespace.Contains("."))
					path = Path.Combine(path, Namespace.Substring(0, Namespace.IndexOf(".")));
				else
					path = Path.Combine(path, Namespace);

				Directory.CreateDirectory(path);
			}

			path = Path.Combine(path, string.Format("{0}.cs", Name));

			if (!createIfExists && File.Exists(path))
				return;

			File.WriteAllText(path, ToString());

			// write all dependancies
			for (int x = 0; x < dependancies.Count; x++)
			{
				dependancies[x].WriteToFile(path, false, true);
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