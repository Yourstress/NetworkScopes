
namespace NetworkScopes
{
	public static class ScopeUtils
	{
		#region Logging
		public static void LogBytes(string title, byte[] bytes, int count, string color)
		{
			System.Text.StringBuilder sb = new System.Text.StringBuilder();

			sb.Append(title + ": ");

			for (int x = 0; x < count; x++)
				sb.AppendFormat("{0} ", (int)bytes[x]);

			Log("<color={0}>{1}</color>", color, sb);
		}

		public static void Log(string text)
		{
			#if UNITY_5_3_OR_NEWER
			UnityEngine.Debug.Log(text);
			#else
			System.Console.WriteLine(text);
			#endif
		}

		public static void Log(string text, params object[] args)
		{
			#if UNITY_5_3_OR_NEWER
			UnityEngine.Debug.LogFormat(text, args);
			#else
			System.Console.WriteLine(text, args);
			#endif
		}

		public static void LogError(string text)
		{
			#if UNITY_5_3_OR_NEWER
			UnityEngine.Debug.LogError(text);
			#else
			System.Console.WriteLine("Error: " + text);
			#endif
		}

		public static void LogError(string text, params object[] args)
		{
			#if UNITY_5_3_OR_NEWER
			UnityEngine.Debug.LogErrorFormat(text, args);
			#else
			System.Console.WriteLine("Error: " + text, args);
			#endif
		}
		#endregion
	}
}