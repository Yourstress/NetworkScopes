using System;
using System.Collections.Generic;
using Lidgren.Network;

namespace NetworkScopes.ServiceProviders.Lidgren
{
    public class LidgrenServer : NetworkServer
    {
        private NetServer _netServer;
        private LidgrenMessageReceiver _receiver;

        private readonly Dictionary<NetConnection, LidgrenPeer> _peers = new Dictionary<NetConnection, LidgrenPeer>();

        /// <summary>
        /// Override to customize server configuration.
        /// </summary>
        /// <returns></returns>
        protected virtual NetPeerConfiguration CreateNetPeerConfiguration()
        {
            return LidgrenUtilities.CreateDefaultConfiguration();
        }

        public override bool IsListening
        {
            get { return _netServer != null && _netServer.Status == NetPeerStatus.Running; }
        }

        public int ListenPort
        {
            get { return _netServer == null ? -1 : _netServer.Port; }
        }

        public override bool StartListening(int port)
        {
            if (_netServer == null)
            {
                _netServer = new NetServer(CreateNetPeerConfiguration());
                _receiver = new LidgrenMessageReceiver(_netServer, OnReceiveMessage);
            }

            if (IsListening)
                return false;

            _netServer.Configuration.Port = port;
            _netServer.Start();

            return true;
        }

        public override void StopListening()
        {
            if (IsListening)
            {
                _receiver.Dispose();
                _receiver = null;

                _netServer.Shutdown(null);
                _netServer = null;

                RemoveAllPeers();
            }
        }

        public void RemoveAllPeers()
        {
            List<LidgrenPeer> peers = new List<LidgrenPeer>(_peers.Values);
            foreach (INetworkPeer peer in peers)
                peer.TriggerDisconnectEvent();
        }

        public override ISignalWriter CreateSignal(short scopeChannel)
        {
            return new LidgrenSignalWriter(_netServer.CreateMessage(), scopeChannel);
        }

        public override void SendSignal(PeerTarget target, ISignalWriter writer)
        {
            if (!target.isMultipleTargets)
            {
                if (target.TargetPeer == null)
                    throw new Exception("Target peer is not set.");

                target.TargetPeer.SendSignal(writer);
            }
            else
            {
                foreach (INetworkPeer networkPeer in target.TargetPeerGroup)
                {
                    networkPeer.SendSignal(writer);
                }
            }
        }

        public IEnumerable<LidgrenPeer> peers
        {
            get { return _peers.Values; }
        }

        public int peerCount
        {
            get { return _peers.Count; }
        }

        // handle message received by the library
        private void OnReceiveMessage(NetIncomingMessage msg)
        {
            switch (msg.MessageType)
            {
                case NetIncomingMessageType.StatusChanged:
                {
                    NetConnectionStatus status = (NetConnectionStatus) msg.ReadByte();

                    if (status == NetConnectionStatus.Connected)
                    {
                        LidgrenPeer peer = new LidgrenPeer(msg.SenderConnection);
                        _peers[msg.SenderConnection] = peer;

                        // trigger peer connected event
                        PeerConnected(peer);
                    }
                    else if (status == NetConnectionStatus.Disconnected)
                    {
                        INetworkPeer peer = _peers[msg.SenderConnection];
                        _peers.Remove(msg.SenderConnection);

                        // trigger peer disconnected event
                        PeerDisconnected(peer);
                    }

                    break;
                }
                case NetIncomingMessageType.Data:
                    ProcessSignal(new LidgrenSignalReader(msg), _peers[msg.SenderConnection]);
                    break;
                default:
                    LidgrenUtilities.ParseMessage("[Server] ", msg);
                    break;
            }
        }
    }
}