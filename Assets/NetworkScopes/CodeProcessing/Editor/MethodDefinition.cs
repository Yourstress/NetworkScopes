
namespace NetworkScopes.CodeProcessing
{
	using System.Collections.Generic;
	using System.Reflection;

	
	public class MethodDefinition
	{
		public string Name;
		public bool IsConstructor;
		public bool IsStatic;
		public bool IsAbstract;
		public bool IsOverride;
		public string ReturnType = "void";

		public List<ParameterDefinition> parameters = new List<ParameterDefinition>();

		public MethodBodyDefinition instructions = new MethodBodyDefinition();

		public MethodDefinition(MethodInfo methodInfo, bool trimOutParams)
		{
			Name = methodInfo.Name;

			foreach (ParameterInfo pi in methodInfo.GetParameters())
			{
				if (trimOutParams && pi.IsOut)
					continue;
				
				ParameterDefinition paramDef = new ParameterDefinition(pi.Name, pi.ParameterType);
				parameters.Add(paramDef);
			}
		}

		public MethodDefinition(string methodName)
		{
			Name = methodName;
		}

		public void Write(ScriptWriter writer)
		{
			// method signature
			{
				writer.BeginWrite();

				writer.Write("public ");

				if (IsStatic)
					writer.Write("static ");

				if (IsAbstract)
					writer.Write("abstract ");
				else if (IsOverride)
					writer.Write("override ");

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

				if (IsAbstract)
					writer.Write(";");
				
				writer.EndWrite();

			}

			// write method definition
			if (!IsAbstract)
				instructions.Write(writer);

//			writer.WriteFullLine(string.Empty);
		}
	}

}