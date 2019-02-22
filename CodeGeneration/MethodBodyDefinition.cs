
using System;
using System.Collections.Generic;
using System.Reflection;

namespace CodeGeneration
{
	public class MethodBodyDefinition
	{
		//ScriptWriter writer;
		List<string> instructions = new List<string>();
		public HashSet<string> imports = new HashSet<string>();

		public int Count { get { return instructions.Count; } }
		public string this[int index] { get { return instructions[index]; } }

		public void InsertInstruction(int index, string text, params object[] args)
		{
			instructions.Insert(index, string.Format(text, args));
		}

		public void AddInstruction(string text)
		{
			instructions.Add(text);
		}

		public void AddInstruction(string text, params object[] args)
		{
			instructions.Add(string.Format(text, args));
		}

		public void AddVariableInstruction(string name, Type type, ConstructorInfo constructor = null, params string[] constructorParams)
		{
			string constructorInst = string.Empty;

			// add imported type
			if (!string.IsNullOrEmpty(type.Namespace))
				imports.Add(type.Namespace);

			if (constructor != null)
				constructorInst = string.Format(" = new {0}({1})", type.Name, string.Join(", ", constructorParams));

			string instruction = string.Format("{0} {1}{2};", type.Name, name, constructorInst);

			instructions.Add(instruction);
		}

		public void AddInlineIfCheck(string condition, string trueAction)
		{
			instructions.Add(string.Format("if ({0})", condition));
			instructions.Add(string.Format("\t{0}", trueAction));
		}

		public void AddMethodCall(string methodName, params string[] methodParams)
		{
			string inst = string.Format("{0}({1});", methodName, string.Join(", ", methodParams));

			instructions.Add(inst);
		}

		public void AddMethodCall(string variableName, MethodInfo method, params string[] methodParams)
		{
			string inst = string.Format("{0}.{1}({2});", variableName, method.Name, string.Join(", ", methodParams));

			instructions.Add(inst);
		}

		public void AddMethodCall(string variableName, string methodName, params string[] methodParams)
		{
			string inst = string.Format("{0}.{1}({2});", variableName, methodName, string.Join(", ", methodParams));

			instructions.Add(inst);
		}

		public void AddMethodCallWithAssignment(string assignTarget, Type targetType, string variableName, bool cast, MethodInfo method, bool createVariable, params string[] methodParams)
		{
			string typeName = targetType.DeclaringType != null ? string.Format("{0}.{1}", targetType.DeclaringType.Name, targetType.Name) : targetType.Name;

			string createVarMod = createVariable ? string.Format("{0} ", typeName) : string.Empty;
			string castMod = cast ? string.Format("({0})", typeName) : string.Empty;

			if (!string.IsNullOrEmpty(targetType.Namespace))
				imports.Add(targetType.Namespace);

			string inst = string.Format("{0}{1} = {2}{3}.{4}({5});", createVarMod, assignTarget, castMod, variableName, method.Name, string.Join(", ", methodParams));

			instructions.Add(inst);
		}

		public void Write(ScriptWriter writer)
		{
			writer.BeginScope();

			for (int x = 0; x < instructions.Count; x++)
			{
				writer.WriteFullLine(instructions[x]);
			}

			writer.EndScope();
		}
	}
	
}