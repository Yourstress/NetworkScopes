using NetworkScopes.Examples;
using System;

namespace NetworkScopes.Examples
{
	[Generated]
	public class MyServerMatch : MyServerMatch_Abstract
	{
		protected override void Test1()
		{
			Debug.Log("Server <-- Test1()");
		}

		protected override void Test2(string str)
		{
			Debug.Log($"Server <-- Test1({str})");
		}

		protected override int Test3()
		{
			int ret = new Random().Next();
			Debug.Log($"Server <--> Test3(), returning {ret}");
			return ret;
		}

	}
}
