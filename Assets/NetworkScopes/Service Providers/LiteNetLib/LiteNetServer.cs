
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using LiteNetLib;

namespace NetworkScopes.ServiceProviders.LiteNetLib
{
    public class LiteNetServer<TPeer> : NetworkServer<TPeer>, INetEventListener, INetworkDispatcher where TPeer : LiteNetPeer, new()
    {
        private NetManager _netServer;
        private NetworkDispatcher _dispatcher;

        private readonly Dictionary<NetPeer, TPeer> _connectionPeers = new Dictionary<NetPeer, TPeer>();
        
        #region NetworkServer implementation
        public override bool IsListening => _netServer?.IsRunning ?? false;

        public override IReadOnlyCollection<TPeer> Peers => _connectionPeers.Values;
        
        public override bool StartListening(int port)
        {
            if (_netServer == null)
                _netServer = new NetManager(this);
            
            if (!_netServer.IsRunning)
            {
                _netServer.Start(port);
                _netServer.UpdateTime = 15;

                _dispatcher = NetworkDispatcher.CreateDispatcher(this);

                return _netServer.IsRunning;
            }

            return _netServer.IsRunning;
        }

        public override void StopListening()
        {
            if (_dispatcher != null)
                _dispatcher.DestroyDispatcher();
            
            _netServer?.Stop();
        }

        public override ISignalWriter CreateSignal(short scopeIdentifier)
        {
            return new SignalWriter(scopeIdentifier);
        }

        public override void SendSignal(PeerTarget target, ISignalWriter writer)
        {
            if (!target.isMultipleTargets)
            {
                target.TargetPeer.SendSignal(writer);
            }
            else
            {
                foreach (INetworkPeer peer in target.TargetPeerGroup)
                {
                    if (peer != target.TargetPeerGroupException)
                        peer.SendSignal(writer);
                }
            }
        }
        #endregion

        #region INetEventListener implementation
        void INetEventListener.OnPeerConnected(NetPeer netPeer)
        {
            TPeer peer = new TPeer();
            peer.InitializeNetPeer(netPeer);
            
            _connectionPeers[netPeer] = peer;
            PeerConnected(peer);
        }

        void INetEventListener.OnPeerDisconnected(NetPeer netPeer, DisconnectInfo disconnectInfo)
        {
            if (_connectionPeers.TryGetValue(netPeer, out TPeer peer))
            {
                _connectionPeers.Remove(netPeer);

                // trigger peer disconnected event
                PeerDisconnected(peer);
            }
            else if (Debug.logFailedPeerRemovals)
            {
                Debug.Log($"Failed to remove peer {netPeer.EndPoint.Address}");
            }
        }

        void INetEventListener.OnNetworkError(IPEndPoint endPoint, SocketError socketError)
        {
            Debug.LogNetworkError(endPoint, socketError);
        }

        void INetEventListener.OnNetworkReceive(NetPeer netPeer, NetPacketReader reader, DeliveryMethod deliveryMethod)
        {
            if (_connectionPeers.TryGetValue(netPeer, out TPeer peer))
            {
                ISignalReader signal = new SignalReader(reader);
                ProcessSignal(signal, peer);                
            }
            else
            {
                Debug.LogError("Received message from unknown peer.");
            }
        }

        void INetEventListener.OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType)
        {
            Debug.LogUnconnectedMessage(remoteEndPoint, reader, messageType);
        }

        void INetEventListener.OnNetworkLatencyUpdate(NetPeer peer, int latency)
        {
            // not handled
        }

        void INetEventListener.OnConnectionRequest(ConnectionRequest request)
        {
            request.AcceptIfKey("d%3G7#$u29u(c$N*C");
        }
        #endregion

        #region ILiteNetLibListener implementation
        void INetworkDispatcher.TickNetwork()
        {
            _netServer.PollEvents();
        }
        
        void INetworkDispatcher.ApplicationWillQuit()
        {
            _netServer.DisconnectAll();
        }
        #endregion
    }
}