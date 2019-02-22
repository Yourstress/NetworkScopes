using System;
using System.Net;
using System.Net.Sockets;
using System.Reflection;

namespace Lidgren.Network
{
    /// <summary>
    /// Extension class for .NET 4.5 functions that are not available in earlier versions of .NET
    /// </summary>
    public static class NetExtensions
    {
        /// <summary>
        /// Used to steal private values from a class
        /// </summary>
        internal static object GetInstanceField(Type type, object instance, string fieldName)
        {
            BindingFlags bindFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
                                     | BindingFlags.Static;
            FieldInfo field = type.GetField(fieldName, bindFlags);
            return field.GetValue(instance);
        }

        /// <summary>
        /// Converts an IPv6 address to IPv4
        /// </summary>
        public static IPAddress MapToIPv4(this IPAddress ipa)
        {
            ushort[] m_Numbers = GetInstanceField(typeof(IPAddress), ipa, "m_Numbers") as ushort[];

            foreach (ushort u in m_Numbers)
            {
                Console.WriteLine(u);
            }

            if (ipa.AddressFamily == AddressFamily.InterNetwork)
                return ipa;
            if (ipa.AddressFamily != AddressFamily.InterNetworkV6)
                throw new Exception("Only AddressFamily.InterNetworkV6 can be converted to IPv4");

            //Test for 0000 0000 0000 0000 0000 FFFF xxxx xxxx
            for (int i = 0; i < 5; i++)
            {
                if (m_Numbers[i] != 0x0000)
                    throw new Exception("Address does not have the ::FFFF prefix");
            }
            if (m_Numbers[5] != 0xFFFF)
                throw new Exception("Address does not have the ::FFFF prefix");

            //We've got an IPv4 address
            byte[] ipv4Bytes = new byte[4];
            Buffer.BlockCopy(m_Numbers, 12, ipv4Bytes, 0, 4);
            return new IPAddress(ipv4Bytes);
        }

        /// <summary>
        /// Converts an IPv6 address to IPv4
        /// </summary>
        public static IPAddress MapToIPv6(this IPAddress ipa)
        {
            if (ipa.AddressFamily == AddressFamily.InterNetworkV6)
                return ipa;
            if (ipa.AddressFamily != AddressFamily.InterNetwork)
                throw new Exception("Only AddressFamily.InterNetworkV4 can be converted to IPv6");

            byte[] ipv4Bytes = ipa.GetAddressBytes();
            byte[] ipv6Bytes = new byte[16] {
                0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0xFF, 0xFF,
                ipv4Bytes [0], ipv4Bytes [1], ipv4Bytes [2], ipv4Bytes [3]
            };
            return new IPAddress(ipv6Bytes);
        }
    }
}
