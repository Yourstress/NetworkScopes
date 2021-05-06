
namespace NetworkScopes.Examples
{
	[Generated]
	public abstract class MyServerLobby_Abstract : ServerScope<MyPeer,MyServerLobby_Abstract.ISender>, MyServerLobby_Abstract.ISender
	{
		[Generated]
		public interface ISender : IScopeSender
		{
		}

		protected override ISender GetScopeSender()
		{
			return this;
		}

		protected abstract void JoinAnyMatch();
		protected abstract bool JoinMatch(bool rankedOnly);
		protected void ReceiveSignal_JoinAnyMatch(ISignalReader reader)
		{
			JoinAnyMatch();
		}

		protected void ReceiveSignal_JoinMatch(ISignalReader reader)
		{
			bool rankedOnly = reader.ReadBoolean();
			int promiseID = reader.ReadPromiseID();
			bool promiseValue = JoinMatch(rankedOnly);
			ISignalWriter writer = CreateSignal(983991160 /*hash '#JoinMatch'*/);
			writer.Write(promiseID);
			writer.Write(promiseValue);
			SendSignal(writer, SenderPeer);
		}

	}
}
