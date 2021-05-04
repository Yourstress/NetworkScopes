
using System;
using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using LiteNetLib;

namespace NetworkScopes
{
    public static class Debug
    {
        public static bool logFailedPeerRemovals = true;
        
        static string GetCurrentTimeString() => DateTime.Now.ToString(CultureInfo.InvariantCulture).Replace("/", "-").Replace(":", ".");

        public static void Log(string str)
        {
            Console.WriteLine($"[NetworkScopes {GetCurrentTimeString()}] {str}");
            
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
            Console.WriteLine(exception.StackTrace);
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