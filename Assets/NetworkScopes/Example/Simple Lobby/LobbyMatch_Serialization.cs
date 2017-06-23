using NetworkScopes;

[Generated]
public partial class LobbyMatch : ISerializable
{
	public void Serialize(ISignalWriter writer)
	{
		writer.WriteString(matchName);
	}

	public void Deserialize(ISignalReader reader)
	{
		matchName = reader.ReadString();
	}

}
