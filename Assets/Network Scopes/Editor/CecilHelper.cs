using Mono.Cecil;
using System;

public static class CecilHelper
{
	public static TypeReference MakeGenericType (this TypeReference self, params TypeReference [] arguments)
	{
		if (self.GenericParameters.Count != arguments.Length)
			throw new ArgumentException ();

		var instance = new GenericInstanceType (self);
		foreach (var argument in arguments)
			instance.GenericArguments.Add (argument);

		return instance;
	}

	public static MethodReference MakeGeneric (this MethodReference self, params TypeReference [] arguments)
	{
		var reference = new MethodReference(self.Name,self.ReturnType) {
			DeclaringType = self.DeclaringType.MakeGenericType (arguments),
			HasThis = self.HasThis,
			ExplicitThis = self.ExplicitThis,
			CallingConvention = self.CallingConvention,
		};

		foreach (var parameter in self.Parameters)
			reference.Parameters.Add (new ParameterDefinition (parameter.ParameterType));

		foreach (var generic_parameter in self.GenericParameters)
			reference.GenericParameters.Add (new GenericParameter (generic_parameter.Name, reference));

		return reference;
	}

	public static FieldReference MakeGeneric(this FieldReference self, params TypeReference [] arguments)
	{
		GenericInstanceType declaringType = new GenericInstanceType(self.DeclaringType);

		foreach (TypeReference argument in arguments)
			declaringType.GenericArguments.Add(argument);

		return new FieldReference(self.Name, self.FieldType, declaringType);
	}
}