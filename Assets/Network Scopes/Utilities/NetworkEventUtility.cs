
namespace NetworkScopes
{
	using System;

	public static class NetworkEventUtility
	{
		private static string[] eventTypeNames = new string[]
		{
			typeof(NetworkSingleEvent).Name,
			typeof(NetworkSingleEvent<>).Name,
			typeof(NetworkSingleEvent<,>).Name,
			typeof(NetworkSingleEvent<,,>).Name,
			typeof(NetworkSingleEvent<,,,>).Name,
		};

		public static bool IsEventType(string typeName)
		{
			for (int x = 0; x < eventTypeNames.Length; x++)
			{
				if (typeName == eventTypeNames[x])
					return true;
			}

			return false;
		}
	}
}