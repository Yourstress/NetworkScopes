using UnityEngine;


namespace NetworkScopes.Editor
{
	using Mono.Cecil;
	using Mono.Cecil.Cil;
	using System;
	using System.Collections.Generic;

	public static class NetworkScopeILUtility
	{
		public static TypeDefinition FindTypeDefinition(AssemblyDefinition def, Type t)
		{
			foreach (var module in def.Modules)
			{
				foreach (var type in module.Types)
				{ 
					if (type.FullName == t.FullName)
						return type; 
				} 
			}
			return null;
		}
		

		
		
//		public static void FindFieldsWithAttribute(TypeDefinition rootType, Type attributeType, bool recursive)
//		{
//			foreach (FieldDefinition field in rootType.Fields)
//			{
//			Debug.Log("Checking field " + field.Name);
//				foreach (var customAttr in field.CustomAttributes)
//				{
////					if (customAttr.AttributeType.FullName == attributeType.FullName)
//					{
//					Debug.Log("Found attr " + customAttr.AttributeType.Name);
//					}
//				}
//			}
//			
//			// don't find fields inside MonoBehaviours
//			if (recursive && rootType.BaseType.FullName != typeof(MonoBehaviour).FullName)
//				FindFieldsWithAttribute(rootType.BaseType.Resolve(), attributeType, recursive);
//		}

//		public static FieldReference FindGenericField(TypeDefinition classTypeDefinition, TypeDefinition parameterType, bool findInChildren)
//		{
//			for (int x = 0; x < classTypeDefinition.GenericParameters.Count; x++)
//			{
//				UnityEngine.Debug.Log(classTypeDefinition.GenericParameters[x].Resolve().Name + " by " + classTypeDefinition.Name);
//			}
//
//			if (classTypeDefinition.BaseType != null)
//				return FindGenericField(classTypeDefinition.BaseType.Resolve(), genericParameterType, findInChildren);
//
//			return null;
//		}

		public static FieldReference FindField(TypeDefinition classTypeDefinition, TypeDefinition fieldTypeDefinition, bool findInChildren)
		{
			if (classTypeDefinition.BaseType is GenericInstanceType)
			{
				Debug.Log(classTypeDefinition + " is gen1"); 

				GenericInstanceType genInstance = (GenericInstanceType)classTypeDefinition.BaseType;

				UnityEngine.Debug.Log("Elem type is " + genInstance.ElementType + " with count "  +genInstance.ElementType.GenericParameters.Count);

//				int x = 0;
				foreach (var param in genInstance.ElementType.GenericParameters)
				{
					Debug.Log(param.DeclaringType + " of type " + param.DeclaringType.GetElementType().GenericParameters[0]);
				}
			}
//			UnityEngine.Debug.Log("Finding field in type " + classTypeDefinition); 
//			int genx = 0;
//			foreach (var gen in classTypeDefinition.GenericParameters)
//			{
//				UnityEngine.Debug.Log("Generic arg " + genx++);
//
//				GenericInstanceType t = (GenericInstanceType)gen.GetElementType();
//				
//				Debug.Log(t); 
//				foreach (var genParam in t.GenericParameters)
//					UnityEngine.Debug.Log("GENPARAM "  + genParam);
//			}
//			
//			for (int x = 0; x < classTypeDefinition.Fields.Count; x++)
//			{
//				UnityEngine.Debug.Log(x + "  comparing " + classTypeDefinition.Fields[x].FieldType + " to " + fieldTypeDefinition.DeclaringType);
//				if (classTypeDefinition.Fields[x].FieldType == fieldTypeDefinition)
//				{
//					return classTypeDefinition.Fields[x];
//				}
//			}

			if (findInChildren && classTypeDefinition.BaseType != null)
				return FindField(classTypeDefinition.BaseType.Resolve(), fieldTypeDefinition, findInChildren);

			return null;
		}

	
	}
}