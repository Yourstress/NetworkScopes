
namespace NetworkScopes
{
	using WebSocketSharp.Server;

	public class WebSocketConnectionHandler : WebSocketBehavior
	{
		protected override void OnOpen ()
		{
			base.OnOpen ();

			WebSocketServerDispatcher.Enqueue (new WSEvent (this, WSEventType.Open));
		}

		protected override void OnClose (WebSocketSharp.CloseEventArgs e)
		{
			base.OnClose (e);
			WebSocketServerDispatcher.Enqueue (new WSEvent (this, WSEventType.Close));
		}

		protected override void OnError (WebSocketSharp.ErrorEventArgs e)
		{
			base.OnError (e);
			UnityEngine.Debug.LogError("WebSocket error: " + e.Message);
			WebSocketServerDispatcher.Enqueue (new WSEvent (this, WSEventType.Error));
		}

		protected override void OnMessage (WebSocketSharp.MessageEventArgs e)
		{
			base.OnMessage (e);
			WebSocketServerDispatcher.Enqueue (new WSEvent (this, WSEventType.Message, e.RawData));
	
		}
	}
}
