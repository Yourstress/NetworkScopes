
#if UNITY_2017_1_OR_NEWER
namespace NetworkScopes
{
	using System;
	using UnityEngine;

	public static class NetworkDebug
	{
		public static void Log(object obj)
		{
			Debug.Log(obj);
		}

		public static void LogFormat(string str, params object[] args)
		{
			Debug.LogFormat(str, args);
		}

		public static void LogWarning(object str)
		{
			Debug.LogWarning(str);
		}

		public static void LogWarningFormat(string str, params object[] args)
		{
			Debug.LogErrorFormat(str, args);
		}

		public static void LogError(string s)
		{
			Debug.LogError(s);
		}

		public static void LogErrorFormat(string str, params object[] args)
		{
			Debug.LogErrorFormat(str, args);
		}

		public static void LogException(Exception exception)
		{
			Debug.LogException(exception);
		}
	}
}
#endif