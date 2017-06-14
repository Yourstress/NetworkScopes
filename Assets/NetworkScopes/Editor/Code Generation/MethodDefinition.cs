using System.Collections.Generic;
using System.Reflection;

namespace NetworkScopes.CodeGeneration
{
	public enum AccessModifier
	{
		Public,
		Protected,
		Private,
	}

	public enum MethodModifier
	{
		None,
		Abstract,
		Virtual,
		Override,

	}
	public class MethodDefinition : IWritable
	{
		public string Name;
		public AccessModifier AccessModifier;
		public MethodModifier MethodModifier;
		public TypeDefinition ReturnType;

		public MethodBody Body = new MethodBody();

		public List<ParameterDefinition> Parameters = new List<ParameterDefinition>(1);

		public MethodDefinition(MethodInfo method) : this(method.Name)
		{
		}

		public MethodDefinition(string name)
		{
			Name = name;
		}

		public MethodDefinition(string name, AccessModifier accessModifier, MethodModifier methodModifier = MethodModifier.None) : this(name)
		{
			AccessModifier = accessModifier;
			MethodModifier = methodModifier;
		}

		public MethodDefinition Copy()
		{
			MethodDefinition method = new MethodDefinition(Name)
			{
				AccessModifier = AccessModifier,
				MethodModifier = MethodModifier,
				ReturnType = ReturnType,
				Parameters = Parameters,
			};

			return method;
		}

		public void Write(ScriptWriter writer)
		{
			writer.BeginWrite();

			// access modifier (public, protected, private)
			if (AccessModifier != AccessModifier.Private)
				writer.Write(AccessModifier.ToString().ToLower() + " ");

			// method modifier (virtual, override, etc..)
			if (MethodModifier != MethodModifier.None)
				writer.Write(MethodModifier.ToString().ToLower() + " ");

			// return type
			writer.WriteFormat("{0} ", (ReturnType == null) ? "void" : ReturnType.Name);

			// method name
			writer.Write(Name);

			// parameters
			writer.Write("(");
			for (var x = 0; x < Parameters.Count; x++)
			{
				ParameterDefinition paramDef = Parameters[x];

				writer.WriteFormat("{0} {1}", paramDef.type.Name, paramDef.name);

				if (x != Parameters.Count-1)
					writer.Write(", ");
			}
			writer.Write(")");

			if (Body == null || MethodModifier == MethodModifier.Abstract)
				writer.Write(";");
			else
			{
				writer.NewLine();
				writer.BeginScope();
				foreach (string statement in Body.instructions)
				{
					writer.WriteFullLine(statement);
				}
				writer.EndScope();
			}
			writer.EndWrite();
		}
	}
}