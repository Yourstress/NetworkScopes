using System;

namespace AssemblyCSharp
{
	// only used to hard-link one side's implementation with another's interface without adding any data to it
	public interface IServerScope
	{
	}

	public interface IClientScope
	{
	}

	public interface IMatchServer : IServerScope
	{
		void Test();
	}

	public interface IMatchClient : IClientScope
	{
	}
		
	public class BaseClientScope<TServerScope> : IClientScope where TServerScope : IServerScope
	{
		protected TServerScope server { get; private set; }
	}

	public class BaseServerScope<TClientScope> : IServerScope where TClientScope : IClientScope
	{
		protected TClientScope client { get; private set; }
	}

	public class MatchClient : BaseClientScope<IMatchServer>
	{
		public void Start()
		{
			server.Test();
//			server.Test();
		}
	}

}

