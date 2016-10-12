
using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;

namespace CodeGeneration
{
	public class MethodDefinition
	{
		public string Name;
		public bool IsConstructor;
		public bool IsStatic;
		public string ReturnType = "void";

		public List<ParameterDefinition> parameters = new List<ParameterDefinition>();

		public MethodBodyDefinition instructions = new MethodBodyDefinition();


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

		public void Write(ScriptWriter writer)
		{
			// method signature
			{
				writer.BeginWrite();

				writer.Write("public ");

				if (IsStatic)
					writer.Write("static ");
				
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