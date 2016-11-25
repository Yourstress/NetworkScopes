
namespace NetworkScopes
{
	using System;

	public abstract class BaseServerProvider : IServerProvider
	{
		protected IServerCallbacks serverCallbacks { get; private set; }

		public int listenPort { get; private set; }
		public bool isListening { get; private set; }

		public abstract void StartServer();
		public abstract void StopServer();
		
		void IServerProvider.Initialize(IServerCallbacks serverCallbacks)
		{
			this.serverCallbacks = serverCallbacks;
		}

		public void StartListening (int listenPort)
		{
			if (isListening)
				throw new Exception("Already listening");
			
			this.listenPort = listenPort;

			StartServer();

			isListening = true;
		}

		public void StopListening ()
		{
			if (!isListening)
				throw new Exception("Not listening");

			StopServer();

			isListening = false;
		}
	}
}