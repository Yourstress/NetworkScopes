using System;
using System.Collections.Generic;

namespace NetworkScopes.CodeGeneration
{
	public class MethodBody
	{
		public List<string> instructions = new List<string>();

		public HashSet<string> imports = null;

		public void Import(string import)
		{
			if (imports == null)
				imports = new HashSet<string>();

			imports.Add(import);
		}

		/// <summary>
		/// [Type] [VarName] = [Value];
		/// </summary>
		public void AddAssignmentInstruction(TypeDefinition type, string varName, string assignmentValue)
		{
			AddRawInstruction(string.Format("{0} {1} = {2};", type.Name, varName, assignmentValue));
		}

		/// <summary>
		/// [VarName] = [Value];
		/// </summary>
		public void AddAssignmentInstruction(string varName, string assignmentValue)
		{
			AddRawInstruction(string.Format("{0} = {1};", varName, assignmentValue));
		}

		/// <summary>
		/// return [Value];
		/// </summary>
		public void AddReturnStatement(string value)
		{
			AddRawInstruction(string.Format("return {0};", value));
		}

		/// <summary>
		/// [AssignmentType] [AssignmentName] = [ObjectName].[MethodName]([Parameters...]);
		/// </summary>
		public void AddMethodCallWithAssignment(string assignmentName, string assignmentType, string objectName, String methodName/*, params string[] parameters*/)
		{
			AddRawInstruction(string.Format("{0} {1} = {2}.{3}();", assignmentType, assignmentName, objectName, methodName));
		}

		/// <summary>
		/// [AssignmentName] = [ObjectName].[MethodName]([Parameters...]);
		/// </summary>
		public void AddMethodCallWithAssignment(string assignmentName, string objectName, String methodName/*, params string[] parameters*/)
		{
			AddRawInstruction(string.Format("{0} = {1}.{2}();", assignmentName, objectName, methodName));
		}

		/// <summary>
		/// [ObjectName].[MethodName]([Parameters...]);
		/// </summary>
		public void AddMethodCall(string objectName, string methodName, params string[] parameters)
		{
			AddRawInstruction(string.Format("{0}.{1}({2});", objectName, methodName, string.Join(", ", parameters)));
		}

		/// <summary>
		/// [MethodName]([Parameters...]);
		/// </summary>
		public void AddLocalMethodCall(string methodName, params string[] parameters)
		{
			AddRawInstruction(string.Format("{0}({1});", methodName, string.Join(", ", parameters)));
		}

		/// <summary>
		/// [AssignmentType] [AssignmentName] = [MethodName]([Parameters...]);
		/// </summary>
		public void AddLocalMethodCallWithAssignment(string methodName, string assignmentType, string assignmentName,
			params string[] parameters)
		{
			AddRawInstruction(string.Format("{0} {1} = {2}({3});", assignmentType, assignmentName, methodName, string.Join(", ", parameters)));
		}

		private void AddRawInstruction(string instruction)
		{
			if (indentStr.Length > 0)
				instruction = indentStr + instruction;

			instructions.Add(instruction);
		}

		private string indentStr = "";

		private void Indent()
		{
			indentStr += "\t";
		}

		private void Unindent()
		{
			if (indentStr.Length >= 1)
				indentStr = indentStr.Remove(indentStr.Length - 1, 1);
		}

		public void BeginForIntLoop(string variableName, string startValue, string maxValueExclusive)
		{
			AddRawInstruction(string.Format("for (int {0} = {1}; {0} < {2}; {0}++)", variableName, startValue, maxValueExclusive));
			AddRawInstruction("{");
			Indent();
		}

		public void BeginForEachLoop(TypeDefinition elementType, string elementName, string enumerableObject)
		{
			AddRawInstruction(string.Format("foreach ({0} {1} in {2})", elementType, elementName, enumerableObject));
			AddRawInstruction("{");
			Indent();
		}

		public void EndLoop()
		{
			Unindent();
			AddRawInstruction("}");
		}
	}
}