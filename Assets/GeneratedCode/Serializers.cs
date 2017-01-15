using UnityEngine.Networking;
using System;

public class NetworkSerializer
{
	public static class ExampleObjectSerializer
	{
		public static void NetworkSerialize(ExampleObject value, NetworkWriter writer)
		{
			writer.Write(value.num);
			writer.Write(value.str);
			writer.Write(value.flt);
			writer.Write(value.numNonSerialized);
		}
		
		public static void NetworkDeserialize(ExampleObject value, NetworkReader reader)
		{
			value.num = reader.ReadInt32();
			value.str = reader.ReadString();
			value.flt = reader.ReadSingle();
			value.numNonSerialized = reader.ReadInt32();
		}
		
	}
}
