using System;
using System.Runtime.CompilerServices;

namespace NetworkScopes
{
	public enum NetworkSerializeType
	{
		/// <summary>
		/// Generates serialization code for public fields, or any fields with the 'SerializeField' attribute.
		/// </summary>
		SerializableFields,
	}

	public class NetworkSerializeAttribute : Attribute
	{
		public NetworkSerializeType serializeType;

		public NetworkSerializeAttribute(NetworkSerializeType serializeType = NetworkSerializeType.SerializableFields)
		{
			this.serializeType = serializeType;
		}
	}
	
	public class NetworkPropertyAttribute : Attribute
	{
		public int order;
		public int lineNumber;

		public NetworkPropertyAttribute(int order = 0, [CallerLineNumber] int lineNumber = 0)
		{
			this.order = order;
			this.lineNumber = lineNumber;
		}
	}
}