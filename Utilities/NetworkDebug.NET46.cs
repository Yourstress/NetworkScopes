
#if !UNITY_2017_1_OR_NEWER
namespace NetworkScopes
{
	using System;

	public static class NetworkDebug
	{
		public static void Log(object obj)
		{
			Console.WriteLine(obj.ToString());
		}

		public static void LogFormat(string str, params object[] args)
		{
			Log(string.Format(str, args));
		}

		public static void LogWarning(object str)
		{
			Log(str);
		}

		public static void LogWarningFormat(string str, params object[] args)
		{
			Log(string.Format(str, args));
		}


		public static void LogError(string s)
		{
			Log("[Error]" + s);
		}

		public static void LogErrorFormat(string str, params object[] args)
		{
			Log(string.Format(str, args));
		}

		public static void LogException(Exception exception)
		{
			Log($"[Exception] {exception.Message}");
		}
	}

	#if UNITY_2017_1_OR_NEWER
#endif
}
#endif