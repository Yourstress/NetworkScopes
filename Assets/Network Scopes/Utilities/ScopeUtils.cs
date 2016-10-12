
namespace NetworkScopesV2
{
	public static class ScopeUtils
	{
		public static void SendScopeSwitchedMessage<TPeer>(BaseServerScope<TPeer> prevScope, BaseServerScope<TPeer> newScope, TPeer peer) where TPeer : NetworkPeer
		{
			IMessageWriter writer = peer.CreateWriter(ScopeMsgType.SwitchScope);

			// 1. msgType: Send prev scope channel
			writer.Write(prevScope.scopeChannel);

			// 2. msgType: Send new scope channel
			writer.Write(newScope.scopeChannel);

			// 2. scopeIdentifier: The value which identifier the counterpart (new) client scope
			writer.Write(newScope.scopeIdentifier);

			peer.Send(writer);
		}

		public static void SendScopeEnteredMessage<TPeer>(BaseServerScope<TPeer> scope, TPeer peer) where TPeer : NetworkPeer
		{
			IMessageWriter writer = peer.CreateWriter(ScopeMsgType.EnterScope);

			// 1. scopeIdentifier: The value which identifier the counterpart client class
			writer.Write(scope.scopeChannel);

			// 2. msgType: Determines which channel to communicate on
			writer.Write(scope.scopeIdentifier);

			peer.Send(writer);
		}

		public static void SendScopeExitedMessage<TPeer>(BaseServerScope<TPeer> scope, TPeer peer) where TPeer : NetworkPeer
		{
			IMessageWriter writer = peer.CreateWriter(ScopeMsgType.ExitScope);

			// 1. msgType: Determines which channel to communicate on
			writer.Write(scope.scopeChannel);

			peer.Send(writer);
		}

		public static int GetConsistentHashCode(this string source)
		{
			int hash1 = 5381;
			int hash2 = hash1;

			int c;
			for (int x = 0; x < source.Length; x+= 2)
			{
				c = source[x];

				hash1 = ((hash1 << 5) + hash1) ^ c;

				if (x+1 < source.Length)
					c = source[x+1];
				
				hash2 = ((hash2 << 5) + hash2) ^ c; 
			}

			return hash1 + (hash2 * 1566083941);
		}

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

		public static void LogWarning(string text)
		{
			#if UNITY_5_3_OR_NEWER
			UnityEngine.Debug.LogWarning(text);
			#else
			System.Console.WriteLine("Warning: " + text);
			#endif
		}

		public static void LogWarning(string text, params object[] args)
		{
			#if UNITY_5_3_OR_NEWER
			UnityEngine.Debug.LogWarningFormat(text, args);
			#else
			System.Console.WriteLine("Warning: " + string.Format(text, args));
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

		public static void LogException(System.Exception e)
		{
			#if UNITY_5_3_OR_NEWER
			UnityEngine.Debug.LogException(e);
			#else
			System.Console.WriteLine("Exception: " + e.Message);
			#endif
		}
		#endregion
	}
}