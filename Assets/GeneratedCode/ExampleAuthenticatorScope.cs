using NetworkScopes;
using System;

namespace MyCompany
{
	[Generated]
	public abstract class ExampleAuthenticatorScope : BaseAuthenticator
	{
		public abstract void AuthenticateForMatchmaker(string username, string password);
		public abstract void AuthenticateForAdmin(string secret);
	}
}
