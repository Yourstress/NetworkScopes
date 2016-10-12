
#if UNITY_5_3_OR_NEWER
namespace NetworkScopesV2
{
	using UnityEngine.Networking;
	using UnityEngine.Networking.NetworkSystem;

	public class UNetClient : BaseClient
	{
		NetworkClient unetClient = null;

		protected override void ConnectInternal (string hostname, int port)
		{
			if (unetClient == null)
				SetupClient ();

			unetClient.Connect (serverHost, serverPort);
		}

		protected override void DisconnectInternal ()
		{
			unetClient.Disconnect ();
		}

		protected override void SendInternal (IMessageWriter writer)
		{
			UnetMessageWriter unetWriter = (UnetMessageWriter)writer;

			unetWriter.writer.FinishMessage ();

			#if UNITY_EDITOR && SCOPE_DEBUGGING
			// log outgoing signal
			ScopeDebugger.AddOutgoingSignal (this, typeof(TServerScope), new NetworkReader (writer.ToArray ()));
			#endif

			unetClient.SendWriter (unetWriter.writer, 0);

		}

		protected override void ShutdownClient ()
		{
			unetClient.Shutdown ();
			unetClient = null;
		}

		#region Client Creation/Destruction

		protected override void SetupClient ()
		{
			unetClient = new NetworkClient ();

			HostTopology topology = new HostTopology (UnetUtil.CreateConnectionConfig (), 3000);

			unetClient.Configure (topology);

			unetClient.RegisterHandler (MsgType.Connect, UnetOnConnect);
			unetClient.RegisterHandler (MsgType.Disconnect, UnetOnDisconnect);
			unetClient.RegisterHandler (MsgType.Error, UnetOnError);
			unetClient.RegisterHandler (ScopeMsgType.ScopeSignal, UnetOnScopeSignal);
			unetClient.RegisterHandler (ScopeMsgType.EnterScope, UnetOnEnterScope);
			unetClient.RegisterHandler (ScopeMsgType.ExitScope, UnetOnExitScope);
			unetClient.RegisterHandler (ScopeMsgType.SwitchScope, UnetOnSwitchScope);
			unetClient.RegisterHandler (ScopeMsgType.DisconnectMessage, UnetOnDisconnectMessage);
			unetClient.RegisterHandler (ScopeMsgType.RedirectMessage, UnetOnRedirectMessage);
		}

		protected override void DestroyClient ()
		{
			unetClient.UnregisterHandler (MsgType.Connect);
			unetClient.UnregisterHandler (MsgType.Disconnect);
			unetClient.UnregisterHandler (MsgType.Error);
			unetClient.UnregisterHandler (ScopeMsgType.ScopeSignal);
			unetClient.UnregisterHandler (ScopeMsgType.EnterScope);
			unetClient.UnregisterHandler (ScopeMsgType.ExitScope);
			unetClient.UnregisterHandler (ScopeMsgType.SwitchScope);
			unetClient.UnregisterHandler (ScopeMsgType.DisconnectMessage);
			unetClient.UnregisterHandler (ScopeMsgType.RedirectMessage);

			unetClient.Disconnect ();
			unetClient.Shutdown ();

			unetClient = null;
		}

		#endregion

		#region implemented abstract members of BaseClient

		public override IMessageWriter CreateWriter (short scopeChannel, int signalType)
		{
			IMessageWriter writer = new UnetMessageWriter (ScopeMsgType.ScopeSignal);
			writer.Write (scopeChannel);
			writer.Write (signalType);
			return writer;
		}

		public override void PrepareAndSendWriter (IMessageWriter writer)
		{
			SendInternal (writer);
		}

		#endregion

		#region UNet Network Messages

		void UnetOnConnect (NetworkMessage msg)
		{
			OnConnect ();
		}

		void UnetOnDisconnect (NetworkMessage msg)
		{
			OnDisconnect ();
		}

		void UnetOnError (NetworkMessage msg)
		{
			OnError (msg.ReadMessage<ErrorMessage> ().ToString ());
		}

		void UnetOnScopeSignal (NetworkMessage msg)
		{
			OnScopeSignal (new UnetMessageReader (msg.reader));
		}

		void UnetOnEnterScope (NetworkMessage msg)
		{
			// 1. read msgType
			short scopeMsgType = msg.reader.ReadInt16 ();

			// 2. read scopeIdentifier
			byte scopeIdentifier = msg.reader.ReadByte ();

			ProcessEnterScope (scopeIdentifier, scopeMsgType);
		}

		void UnetOnExitScope (NetworkMessage msg)
		{
			// 1. read msgType
			short scopeMsgType = msg.reader.ReadInt16 ();

			ProcessExitScope (scopeMsgType);
		}

		void UnetOnSwitchScope (NetworkMessage msg)
		{
			// 1. read msgType of prevScope
			short prevScopeMsgType = msg.reader.ReadInt16 ();

			// 2. read msgType of newScope
			short newScopeMsgType = msg.reader.ReadInt16 ();

			// 3. read scopeIdentifier of newScope
			byte newScopeIdentifier = msg.reader.ReadByte ();

			// simulate exit/enter signals in one go
			ProcessExitScope (prevScopeMsgType);
			ProcessEnterScope (newScopeIdentifier, newScopeMsgType);
		}

		void UnetOnDisconnectMessage (NetworkMessage msg)
		{
			lastDisconnectMsg = msg.reader.ReadByte ();

			Disconnect ();
		}

		void UnetOnRedirectMessage (NetworkMessage msg)
		{
		}

		#endregion
	}
}
#endif