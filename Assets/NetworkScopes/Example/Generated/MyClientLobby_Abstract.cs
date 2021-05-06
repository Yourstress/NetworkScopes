
namespace NetworkScopes.Examples
{
	[Generated]
	public abstract class MyClientLobby_Abstract : ClientScope<MyClientLobby_Abstract.ISender>, MyClientLobby_Abstract.ISender
	{
		[Generated]
		public interface ISender : IScopeSender
		{
			void JoinAnyMatch();
			ValuePromise<bool> JoinMatch(bool rankedOnly);
		}

		protected override ISender GetScopeSender()
		{
			return this;
		}

		void ISender.JoinAnyMatch()
		{
			ISignalWriter writer = CreateSignal(-1879468029 /*hash 'JoinAnyMatch'*/);
			SendSignal(writer);
		}

		ValuePromise<bool> ISender.JoinMatch(bool rankedOnly)
		{
			ValuePromise<bool> promise = new ValuePromise<bool>();
			ISignalWriter writer = CreatePromiseSignal(-1779229667, promise /*hash 'JoinMatch'*/);
			writer.Write(rankedOnly);
			SendSignal(writer);
			return promise;
		}

		protected void ReceivePromise_JoinMatch(ISignalReader reader)
		{
			ReceivePromise(reader);
		}

	}
}
