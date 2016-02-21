
namespace NetworkScopes
{
	using System;
	using System.IO;
	using System.Collections.Generic;
	using System.Collections;

	public static class TypeSerializer
	{
		public delegate object Deserializer (BinaryReader reader);

		public delegate void Serializer (object obj,BinaryWriter writer);

		private static Dictionary<Type,Serializer> Serializers = new Dictionary<Type, Serializer> () {
			{ typeof(bool), SerializeBool },
			{ typeof(int), SerializeInt },
			{ typeof(string), SerializeString },
			{ typeof(byte), SerializeByte },
		};
	
		private static Dictionary<Type,Deserializer> Deserializers = new Dictionary<Type, Deserializer> () {
			{ typeof(bool), DeserializeBool },
			{ typeof(int), DeserializeInt },
			{ typeof(string), DeserializeString },
			{ typeof(byte), DeserializeByte },
		};

		public static void Register<T> (Serializer serializer, Deserializer deserializer)
		{
			Serializers [typeof(T)] = serializer;
			Deserializers [typeof(T)] = deserializer;
		}

		private static Serializer GetSerializer (Type objType)
		{
			Serializer serializer;
	
			// find the serializer for this object's type
			if (!Serializers.TryGetValue (objType, out serializer))
				throw new Exception ("Network Scope could not serialize object of type " + objType);
	
			return serializer;
		}

		private static Deserializer GetDeserializer (Type objType)
		{
			Deserializer deserializer;
	
			// find the deserializer for this object's type
			if (!Deserializers.TryGetValue (objType, out deserializer))
				throw new Exception ("Network Scope could not deserialize object of type " + objType);
	
			return deserializer;
		}

		public static void Serialize (object obj, BinaryWriter writer)
		{
			Type objType = obj.GetType ();
	
			Serializer serializer;
	
			if (objType.IsArray) {
				Array arr = (Array)obj;
				Type elemType = objType.GetElementType ();

				if (elemType == typeof(byte)) {
					byte[] objData = (byte[])obj;
					writer.Write (objData.Length);
					writer.Write (objData);
				} else {
					serializer = GetSerializer (elemType);
	
					writer.Write ((int)arr.Length);
					for (int x = 0; x < arr.Length; x++)
						serializer (arr.GetValue (x), writer);
				}
			} else if (objType.IsGenericType && objType.GetGenericTypeDefinition () == typeof(List<>)) {
				IList list = (IList)obj;
	
				Type elemType = objType.GetGenericArguments () [0];
				serializer = GetSerializer (elemType);
	
				writer.Write ((int)list.Count);
				for (int x = 0; x < list.Count; x++)
					serializer (list [x], writer);
			} else {
				// if it happens to be an enum, make sure to convert it to its underlying type
				if (objType.IsEnum) {
					objType = Enum.GetUnderlyingType (objType);
					obj = Convert.ChangeType (obj, objType);
				}
	
				serializer = GetSerializer (objType);
	
				serializer (obj, writer);
			}
		}

		public static object Deserialize (Type objectType, BinaryReader reader)
		{
			Deserializer deserializer;
	
			if (objectType.IsArray)
			{
				int count = reader.ReadInt32 ();
				Type elemType = objectType.GetElementType ();
	
				deserializer = GetDeserializer (elemType);
	
				Array arr = Array.CreateInstance (elemType, count);

				for (int x = 0; x < count; x++)
					arr.SetValue (deserializer (reader), x);
	
					return arr;
			}
			else if (objectType.IsGenericType && objectType.GetGenericTypeDefinition () == typeof(List<>))
			{
				int count = reader.ReadInt32 ();
				Type elemType = objectType.GetGenericArguments () [0];
	
				IList list = (IList)Activator.CreateInstance (typeof(List<>).MakeGenericType (elemType));
	
				deserializer = GetDeserializer (elemType);
	
				for (int x = 0; x < count; x++)
				{
					list.Add (deserializer (reader));
				}
	
				return list;
			}
			else
			{
				// if it happens to be an enum, make sure to convert it to its underlying type
				if (objectType.IsEnum)
				{
					objectType = Enum.GetUnderlyingType (objectType);
				}
	
				deserializer = GetDeserializer (objectType);
				return deserializer (reader);
			}
		}

		private static void SerializeBool (object obj, BinaryWriter writer)
		{
			writer.Write ((bool)obj);
		}

		private static void SerializeInt (object obj, BinaryWriter writer)
		{
			writer.Write ((int)obj);
		}

		private static void SerializeString (object obj, BinaryWriter writer)
		{
			writer.Write ((string)obj);
		}

		private static void SerializeByte (object obj, BinaryWriter writer)
		{
			writer.Write ((byte)obj);
		}

		private static object DeserializeBool (BinaryReader reader)
		{
			return reader.ReadBoolean ();
		}

		private static object DeserializeInt (BinaryReader reader)
		{
			return reader.ReadInt32 ();
		}

		private static object DeserializeString (BinaryReader reader)
		{
			return reader.ReadString ();
		}

		private static object DeserializeByte (BinaryReader reader)
		{
			return reader.ReadByte ();
		}
	}
}