using System;
using System.Net;
using System.Net.Sockets;
using LiteNetLib;

namespace NetworkScopes.ServiceProviders.LiteNetLib
{
    public class LiteNetClient : NetworkClient, INetEventListener, INetworkDispatcher
    {
        private NetManager _netClient;
        private NetworkDispatcher dispatcher;

        public override bool IsConnecting { get; protected set; }
    
        public override bool IsConnected { get; protected set; }

        public override int LatencyInMs { get; protected set; }

        protected override void ConnectInternal(string hostnameOrIP, int port)
        {
            if (_netClient == null)
            {
                _netClient = new NetManager(this);
                _netClient.UpdateTime = 15;
                _netClient.Start();

                dispatcher = NetworkDispatcher.CreateDispatcher(this);
            }

            try
            {
                IsConnecting = true;
                _netClient.Connect(hostnameOrIP, port, "d%3G7#$u29u(c$N*C");
            }
            catch (Exception e)
            {
                DidFailToConnect();
				
                Debug.Log($"Cloud not connect. {e.Message}");
            }
        }

        protected override void DisconnectInternal()
        {
            _netClient.DisconnectAll();
        }

        public override ISignalWriter CreateSignal(short channelId)
        {
            return new SignalWriter(channelId);
        }

        public override void SendSignal(ISignalWriter signal)
        {
            _netClient.SendToAll(signal as SignalWriter, DeliveryMethod.ReliableOrdered);
        }
        
        #region INetEventListener implementation
        void INetEventListener.OnPeerConnected(NetPeer peer)
        {
            DidConnect();
        }

        void INetEventListener.OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
        {
            WillDisconnect();
            DidDisconnect((byte)disconnectInfo.Reason);
        }

        void INetEventListener.OnNetworkError(IPEndPoint endPoint, SocketError socketError)
        {
            Debug.LogNetworkError(endPoint, socketError);
        }

        void INetEventListener.OnNetworkReceive(NetPeer peer, NetPacketReader reader, DeliveryMethod deliveryMethod)
        {
            ISignalReader signal = new SignalReader(reader);
            ProcessSignal(signal);
        }

        void INetEventListener.OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType)
        {
            Debug.LogUnconnectedMessage(remoteEndPoint, reader, messageType);
        }

        void INetEventListener.OnNetworkLatencyUpdate(NetPeer peer, int latency)
        {
            LatencyInMs = latency;
        }

        void INetEventListener.OnConnectionRequest(ConnectionRequest request)
        {
            Debug.LogError("Client cannot request connections.");
        }
        #endregion

        #region ILiteNetLibListener implementation
        void INetworkDispatcher.TickNetwork()
        {
            _netClient.PollEvents();
        }

        void INetworkDispatcher.ApplicationWillQuit()
        {
            _netClient.DisconnectAll();
        }
        #endregion
    }
}