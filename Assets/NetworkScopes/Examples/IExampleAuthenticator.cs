
namespace MyCompany
{
	using NetworkScopes;

	public interface INetworkSerializable
	{
	}

	public class NetworkEither<T1,T2> : INetworkSerializable
	{
		public bool isFirstArgument { get; private set; }

		public T1 firstArgument;
		public T2 secondArgument;

		public NetworkEither(T1 firstArg)
		{
			isFirstArgument = true;
			firstArgument = firstArg;
		}

		public NetworkEither(T2 secondArg)
		{
			isFirstArgument = false;
			secondArgument = secondArg;
		}
	}

	public interface IExampleAuthenticator : IAuthenticator
	{
		void AuthenticateForMatchmaker(string username, string password, out bool success, out string optionalError);
		void AuthenticateForAdmin(string secret, out bool success, out string optionalError);
	}
}