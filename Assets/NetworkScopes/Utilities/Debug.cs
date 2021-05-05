
using System;
using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using LiteNetLib;

namespace NewNetworkScopes
{
    public static class Debug
    {
        public static bool logFailedPeerRemovals = true;
        
        static string GetCurrentTimeString() => DateTime.Now.ToString(CultureInfo.InvariantCulture).Replace("/", "-").Replace(":", ".");

        private static void LogRaw(string str)
        {
            #if UNITY_EDITOR
            UnityEngine.Debug.Log(str);
            #else
            Console.WriteLine();
            #endif
        }

        public static void Log(string str)
        {
            LogRaw($"[NetworkScopes {GetCurrentTimeString()}] {str}");
        }

        public static void LogWarning(string str)
        {
            Log(str);
        }

        public static void LogError(string str)
        {
            Log(str);
        }

        public static void LogException(Exception exception)
        {
            Log(exception.Message);
            LogRaw(exception.StackTrace);
        }

        public static void LogUnconnectedMessage(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType)
        {
            Log($"Ignoring unconnected message ({messageType}) from " + remoteEndPoint.Address);
        }

        public static void LogNetworkError(IPEndPoint endPoint, SocketError socketError)
        {
            Log($"Network error ({socketError}) encountered for " + endPoint.Address);
        }
    }

}