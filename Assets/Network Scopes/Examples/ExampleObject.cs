
using NetworkScopes;

[NetworkSerialization(NetworkSerializeSettings.AllFieldsAndProperties)]
public class ExampleObject
{
	public int num = 0;
	public string str = "";
	public float flt = 0;

	[NetworkNonSerialized]
	public int numNonSerialized = 0;

	public override string ToString ()
	{
		return string.Format ("[ExampleObject] num={0}, str={1}, flt={2}, numNonSerialized={3}", num, str, flt, numNonSerialized);
	}
}