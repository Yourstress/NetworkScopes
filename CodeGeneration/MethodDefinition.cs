
using System;
using System.Collections.Generic;
using System.Reflection;

namespace CodeGeneration
{
	public enum MethodAccessModifier
	{
		Public,
		Protected,
		Private,
	}

	public enum MethodModifier
	{
		None,
		Virtual,
		Override,
	}

	public class MethodDefinition : IWritable, IImporter
	{
		public string Name;
		public bool IsConstructor;
		public bool IsStatic;
		public bool IsAsync;

		public string ReturnType = "void";

		public List<Type> importedTypes;

		public List<ParameterDefinition> parameters = new List<ParameterDefinition>();

		public MethodBodyDefinition instructions = new MethodBodyDefinition();

		public MethodAccessModifier accessModifier = MethodAccessModifier.Public;
		public MethodModifier modifier = MethodModifier.None;

		public MethodDefinition(MethodInfo methodInfo)
		{
			Name = methodInfo.Name;

			foreach (ParameterInfo pi in methodInfo.GetParameters())
			{
				ParameterDefinition paramDef = new ParameterDefinition(pi.Name, pi.ParameterType);
				parameters.Add(paramDef);
			}
		}

		public MethodDefinition(string methodName)
		{
			Name = methodName;
		}

		public void Import(params Type[] types)
		{
			if (importedTypes == null)
				importedTypes = new List<Type>(types.Length);

			importedTypes.AddRange(types);
		}

		public void Write(ScriptWriter writer)
		{
			// method signature
			{
				writer.BeginWrite();

				writer.Write(accessModifier.ToString().ToLower() + " ");

				if (modifier != MethodModifier.None)
					writer.Write(modifier.ToString().ToLower() + " ");

				if (IsStatic)
					writer.Write("static ");

				if (IsAsync)
					writer.Write("async ");
				
				if (!IsConstructor)
					writer.WriteFormat("{0} ", ReturnType);
				
				writer.Write(Name);

				// write parameters
				writer.Write("(");
				for (int x = 0; x < parameters.Count; x++)
				{
					writer.WriteFormat("{0} {1}", parameters[x].TypeName, parameters[x].Name);

					if (x+1 < parameters.Count)
						writer.Write(", ");
				}
				writer.Write(")");
				writer.EndWrite();

			}

			// write method definition
			instructions.Write(writer);

			writer.WriteFullLine(string.Empty);
		}
	}
	
}