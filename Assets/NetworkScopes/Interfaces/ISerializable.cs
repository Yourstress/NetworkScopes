
namespace NetworkScopes
{
	public interface ISerializable
	{
		void Serialize(ISignalWriter writer);
		void Deserialize(ISignalReader reader);
	}
}