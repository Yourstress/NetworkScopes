
using System;

namespace NetworkScopes.Examples
{
	[Generated]
	public class MyServerMatch : MyServerMatch_Abstract
	{
		protected override void Test1()
		{
			Debug.Log($"Server received Test1");
		}

		protected override void Test2(string str)
		{
			Debug.Log($"Server received Test2 " + str);
		}

		protected override int Test3()
		{
			int value = (new Random().Next() % 100) + 1;
			Debug.Log($"Server received Test3 - sending back random number between 1-100: " + value);
			return value;
		}

	}
}
