using System;
using Lidgren.Network;

public class LidgrenMessageReceiver : IDisposable
{
	public delegate void ReceiveMessageDelegate(NetIncomingMessage msg);

	private readonly NetPeer _peer;
	private readonly ReceiveMessageDelegate _onReceiveMessage;

    public LidgrenMessageReceiver(NetPeer peer, ReceiveMessageDelegate onReceiveMessage)
    {
        _peer = peer;
        _onReceiveMessage = onReceiveMessage;

        peer.RegisterReceivedCallback(HandleSendOrPostCallback);
    }

    public void Dispose()
    {
        _peer.UnregisterReceivedCallback(HandleSendOrPostCallback);
    }

    void HandleSendOrPostCallback(object state)
    {
        NetIncomingMessage msg = _peer.ReadMessage();

        if (msg != null)
        {
            // pass the message over to the subscriber of this object
            _onReceiveMessage(msg);

            // recycle the message to reduce garbage
            _peer.Recycle(msg);
        }
    }
}

