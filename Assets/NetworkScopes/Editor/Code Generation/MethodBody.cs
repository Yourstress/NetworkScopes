using System;
using System.CodeDom;
using System.Collections.Generic;
using Microsoft.CSharp;

namespace NetworkScopes.CodeGeneration
{
	public class MethodBody
	{
		public List<string> instructions = new List<string>();

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
		public void AddMethodCallWithAssignment(string assignmentType, string assignmentName, string objectName, String methodName/*, params string[] parameters*/)
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

		private void AddRawInstruction(string instruction)
		{
			instructions.Add(instruction);
		}
	}
}