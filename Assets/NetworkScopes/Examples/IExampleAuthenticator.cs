

namespace MyCompany
{
	using NetworkScopes;

	public interface IExampleAuthenticator : IServerAuthenticator
	{
		void AuthenticateForMatchmaker(string username, string password, out bool success, out string optionalError);
		void AuthenticateForAdmin(string secret, out bool success, out string optionalError);
	}
}