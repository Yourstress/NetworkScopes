using System;
using CodeGeneration;

namespace NetworkScopes
{
	public static class SerialiationGenerator
	{
		public static ClassDefinition GenerateObjectSerialization(Type targetType, string className, string classNamespace)
		{
			ClassDefinition gen = new ClassDefinition(className) { Namespace = classNamespace };

			return gen;
		}
	}
}