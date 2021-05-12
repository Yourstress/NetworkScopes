
using System;
using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using LiteNetLib;

namespace NetworkScopes
{
    public enum LogType
    {
        Info,
        Warning,
        Error,
    }
    public static class NSDebug
    {
        public static bool logFailedPeerRemovals = true;
        private const string _defaultCategoryName = "NetworkScopes";
        
        static string GetCurrentTimeString() => DateTime.Now.ToString(CultureInfo.InvariantCulture).Replace("/", "-").Replace(":", ".");

        private static void LogRaw(string str, LogType logType)
        {
            #if UNITY_EDITOR
            if (logType == LogType.Info)
                UnityEngine.Debug.Log(str);
            else if (logType == LogType.Warning)
                UnityEngine.Debug.LogWarning(str);
            else if (logType == LogType.Error)
                UnityEngine.Debug.LogError(str);
            #else
            Console.WriteLine(str);
            #endif
        }
        
        public static void Log(string str) => Log(_defaultCategoryName, str, LogType.Info);
        public static void Log(string category, string str, LogType logType = LogType.Info)
        {
            LogRaw($"[{category} {GetCurrentTimeString()}] {str}", logType);
        }

        public static void LogWarning(string str) => LogWarning(_defaultCategoryName, str);
        public static void LogWarning(string category, string str)
        {
            Log(category, str, LogType.Warning);
        }

        public static void LogError(string str) => LogError(_defaultCategoryName, str);
        public static void LogError(string category, string str)
        {
            Log(category, str, LogType.Error);
        }

        public static void LogException(Exception exception)
        {
            #if UNITY_EDITOR
            UnityEngine.Debug.LogException(exception);
            #else
            Log(_defaultCategoryName, exception.Message, LogType.Error);
            LogRaw(exception.StackTrace, LogType.Error);
            #endif
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