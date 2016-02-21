
using UnityEngine;
using NetworkScopes;
using System;
using UnityEngine.Networking;

[NetworkSerialization(NetworkSerializeSettings.AllFields)]
public class TestSerializedClass
{
	public string name;

	[NetworkNonSerialized]
	public int id;

	[NetworkSerialized]
	public string propName { get; set; }

	public override string ToString ()
	{
		return string.Format ("[TestClass: propName={0} name={1} id={2}]", propName, name, id);
	}

	public static void Deserialize(TestSerializedClass value, NetworkReader reader)
	{
		
	}

	public static void Serialize(TestSerializedClass value, NetworkWriter writer)
	{

	}
}
