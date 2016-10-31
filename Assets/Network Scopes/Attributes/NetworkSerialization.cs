using System.Reflection;


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
		public NetworkSerializeSettings SerializeSetting;

		public NetworkSerialization(NetworkSerializeSettings serializeSetting = NetworkSerializeSettings.PublicFieldsOnly)
		{
			SerializeSetting = serializeSetting;
		}

		public void GetSerializedMembers(Type paramType, out FieldInfo[] fields, out PropertyInfo[] props)
		{
			switch (SerializeSetting)
			{
			case NetworkSerializeSettings.AllFieldsAndProperties:
				fields = paramType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
				props = paramType.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
				break;

			case NetworkSerializeSettings.AllFields:
				fields = paramType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
				props = null;
				break;

			case NetworkSerializeSettings.PublicFieldsAndProperties:
				fields = paramType.GetFields(BindingFlags.Instance | BindingFlags.Public);
				props = paramType.GetProperties(BindingFlags.Instance | BindingFlags.Public);
				break;

			case NetworkSerializeSettings.PublicFieldsOnly:
				fields = paramType.GetFields(BindingFlags.Instance | BindingFlags.Public);
				props = null;
				break;

			default:
				throw new Exception("Failed to serialized using the setting " + SerializeSetting);
			}
		}
	}
}