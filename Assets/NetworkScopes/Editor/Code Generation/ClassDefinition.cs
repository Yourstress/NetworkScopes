using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace NetworkScopes.CodeGeneration
{
	public class ClassDefinition
	{
		public TypeDefinition type;
		public TypeDefinition baseType;

		public List<TypeDefinition> interfaces = new List<TypeDefinition>();

		public string[] genericArgs = new string[0];

		public bool isAbstract;
		public bool isPublic;
		public bool isStatic;
		public bool isPartial;
		public bool isInterface;

		public bool isCommentedOut = false;

		public List<FieldDefinition> fields = new List<FieldDefinition>();
		public List<PropertyDefinition> properties = new List<PropertyDefinition>();
		public List<MethodDefinition> methods = new List<MethodDefinition>();
		public List<DelegateDefinition> delegates = new List<DelegateDefinition>(0);
		public List<EventDefinition> events = new List<EventDefinition>(0);

		public List<ClassDefinition> nestedClasses = new List<ClassDefinition>();
		public List<TypeDefinition> attributes = new List<TypeDefinition>(1);

		private HashSet<string> imports = new HashSet<string>();

		public ClassDefinition(string name)
		{
			type = new TypeDefinition(name);
			attributes.Add(typeof(GeneratedAttribute));

			ResolveImportType(typeof(GeneratedAttribute));
		}

		public ClassDefinition(string name, string namespaceName) : this(name)
		{
			type.Namespace = namespaceName;
		}

		public ScriptWriter ToScriptWriter()
		{
			ScriptWriter writer = new ScriptWriter();
			Write(writer);
			return writer;
		}

		private void Write(ScriptWriter writer, bool writeImports = true)
		{
			if (isCommentedOut)
				writer.WriteFullLine("/*");

			if (writeImports)
			{
				CollectAndWriteImports(writer);
				writer.NewLine();
			}

			// write namespace
			bool hasNamespace = !string.IsNullOrEmpty(type.Namespace);
			if (hasNamespace)
			{
				writer.WriteFullLineFormat("namespace {0}", type.Namespace);
				writer.BeginScope();
			}

			// write attributes
			foreach (TypeDefinition attributeType in attributes)
			{
				writer.WriteFullLineFormat("[{0}]", attributeType.Name.Replace("Attribute", string.Empty));
			}

			// write class definition
			writer.BeginWrite();
			writer.Write("public ");
			if (isStatic)
				writer.Write("static ");
			if (isAbstract)
				writer.Write("abstract ");
			if (isPartial)
				writer.Write("partial ");

			if (isInterface)
				writer.Write("interface ");
			else
				writer.Write("class ");

			writer.Write(type.Name);

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

			// write base type
			if (baseType != null)
				writer.WriteFormat(" : {0}", baseType.Name);

			if (interfaces.Count > 0)
			{
				writer.Write(baseType != null ? ", " : " : ");
			}

			for (var x = 0; x < interfaces.Count; x++)
			{
				writer.Write(interfaces[x].Name);
				if (x != interfaces.Count - 1)
					writer.Write(", ");
			}

			writer.EndWrite();

			writer.BeginScope();

			// write nested classes
			for (var x = 0; x < nestedClasses.Count; x++)
				nestedClasses[x].Write(writer, false);
			if (nestedClasses.Count > 0)
				writer.NewLine();

			// write delegates
			if (delegates.Count > 0)
			{
				for (var x = 0; x < delegates.Count; x++)
					delegates[x].Write(writer);
				writer.NewLine();
			}

			// write events
			if (events.Count > 0)
			{
				for (var x = 0; x < events.Count; x++)
					events[x].Write(writer);
				writer.NewLine();
			}

			// write fields and properties
			for (int x = 0; x < fields.Count; x++)
				fields[x].Write(writer);
			if (fields.Count > 0)
				writer.NewLine();

			for (int x = 0; x < properties.Count; x++)
				properties[x].Write(writer);
			if (properties.Count > 0)
				writer.NewLine();

			// write methods
			for (int x = 0; x < methods.Count; x++)
				methods[x].Write(writer);

			writer.EndScope();

			if (hasNamespace)
				writer.EndScope();

			if (isCommentedOut)
				writer.WriteFullLine("*/");
		}

		private void CollectAndWriteImports(ScriptWriter writer)
		{
			// collect all imports from nested classes
			foreach (ClassDefinition nestedClass in nestedClasses)
			{
				CollectImports(nestedClass.imports);
			}

			// ..and methods
			foreach (MethodDefinition method in methods)
			{
				if (method.Body != null && method.Body.imports != null)
					CollectImports(method.Body.imports);
				
				// as well as method parameters
				foreach (ParameterDefinition parameter in method.Parameters)
				{
					if (!string.IsNullOrEmpty(parameter.type.Namespace))
					{
						if (parameter.type.Namespace != nameof(System))
							imports.Add(parameter.type.Namespace);
					}
				}
			}

			// write them to the script writer
			foreach (string import in imports)
			{
				if (import == nameof(NetworkScopes))
				{
					bool shouldImportNS = !type.Namespace.StartsWith(nameof(NetworkScopes));

					if (!shouldImportNS)
						continue;
				}
				if (!string.IsNullOrEmpty(import) && !type.Namespace.StartsWith(import) )
					writer.WriteFullLineFormat("using {0};", import);
			}
		}

		public void CollectImports(HashSet<string> imports)
		{
			foreach (string import in imports)
			{
				AddImport(import);
			}

			imports.Clear();
		}

		public void ResolveImportType(TypeDefinition importType)
		{
			AddImport(importType.Namespace);
		}

		public void ResolveImportType(Type importType)
		{
			AddImport(importType.Namespace);
		}

		void AddImport(string namespaceName)
		{
			imports.Add(namespaceName);
		}
	}
}