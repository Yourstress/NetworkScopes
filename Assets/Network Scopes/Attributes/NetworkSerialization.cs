
namespace NetworkScopes
{
	using System;

	public enum NetworkSerializeSettings
	{
		PublicFieldsOnly,
		PublicFieldsAndProperties,
		AllFields,
		AllFieldsAndProperties,
		OptIn,
		Custom,
	}

	public class NetworkSerialization : Attribute
	{
		public NetworkSerialization(NetworkSerializeSettings serializeSetting = NetworkSerializeSettings.PublicFieldsOnly)
		{
		}
	}
}