using NetworkScopes;
using System;

namespace MyCompany
{
	[Generated]
	public class ExampleAuthenticator : ExampleAuthenticatorScope
	{
		public override NetworkPromise<bool,string> AuthenticateForMatchmaker(string username, string password)
		{
			return NetworkPromise<bool,string>.Create(true, "none");
		}
		public override NetworkPromise<bool,string> AuthenticateForAdmin(string secret)
		{
			return NetworkPromise<bool,string>.Create(false, "NOPE!");
		}
	}
}
