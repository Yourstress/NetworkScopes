

namespace CodeGeneration
{
	using System;

	public abstract class InjectAttribute : Attribute
	{
		public abstract void ProcessClass (Type localType, ClassDefinition classDef);
	}
}

