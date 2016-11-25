using NetworkScopes;
using System;

namespace MyCompany
{
	[Generated]
	public abstract class ExampleServerMatchScope : ServerScope<ExampleMatchPeer>
	{
		public abstract void Signal(int x, string s, bool y);
	}
}
