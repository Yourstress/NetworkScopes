namespace NetworkScopes
{
	public static class SignalExtensions
	{
		public static ScopeChannel ReadScopeChannel(this ISignalReader reader)
		{
			return reader.ReadShort();
		}

		public static void WriteScopeChannel(this ISignalWriter writer, ScopeChannel channel)
		{
			writer.WriteShort(channel);
		}

		public static ScopeIdentifier ReadScopeIdentifier(this ISignalReader reader)
		{
			return reader.ReadByte();
		}

		public static void WriteScopeIdentifier(this ISignalWriter writer, ScopeIdentifier channel)
		{
			writer.WriteByte(channel);
		}
	}
}