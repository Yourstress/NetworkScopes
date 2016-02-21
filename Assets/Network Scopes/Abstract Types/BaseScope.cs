
namespace NetworkScopes
{
	using UnityEngine;
	using UnityEngine.Networking;
	using System.Collections.Generic;
	using System.Collections;
	using System.IO;
	using System.Linq;
	using System;
	using System.Reflection;

	public abstract class BaseScope
	{
		protected bool isInitialized { get; private set; }

		public short msgType { get; protected set; }
		public byte scopeIdentifier { get; protected set; }

		public delegate void ScopeDeserializerDelegate(NetworkReader reader);

		private Dictionary<int,MethodInfo> cachedDeserializers = null;

		protected void Initialize()
		{
			if (!isInitialized)
			{
				BindReceiveMethods();

				isInitialized = true;
			}
		}

		public void SetScopeIdentifier(byte identifier)
		{
			scopeIdentifier = identifier;
		}

		public void ProcessMessage (NetworkMessage msg)
		{
			int signalType = msg.reader.ReadInt32();

			try
			{
				cachedDeserializers[signalType].Invoke(this, new object[] { msg.reader });
			}
			catch (Exception e)
			{
				Debug.LogErrorFormat("Failed to call method with hash {0} in {1}", signalType, GetType().Name);
				Debug.LogException(e);
			}
		}

		private void BindReceiveMethods()
		{
			cachedDeserializers = new Dictionary<int, MethodInfo>();

			Type t = GetType();

			List<MethodInfo> methods = t.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly).Where(m => !m.IsSpecialName).ToList();

			List<FieldInfo> eventFields = t.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly).Where(f =>
				{
					return NetworkEventUtility.IsEventType(f.FieldType.Name);
				}).ToList();

			foreach (FieldInfo field in eventFields)
			{
				MethodInfo recvMethod = methods.Find(m => m.Name == string.Format("Receive_{0}", field.Name));

				// if no method was found, don't create the delegate
				if (recvMethod == null)
				{
					Debug.LogFormat("Network Scope: Skipping method {0} in {1} because no \"Receive_{0}\" function was found. The type is probably missing the [ClientSignalSync(typeof(SERVER_TYPE))] or [ServerSignalSync(typeof(CLIENT_TYPE))] attribute", field.Name, t.Name);
					continue;
				}

				cachedDeserializers.Add(field.Name.GetHashCode(), recvMethod);
			}

			foreach (MethodInfo method in methods)
			{
				// skip nonpublic or any send/receive methods
				if (!method.IsPublic || method.Name.StartsWith("Receive_") || method.Name.StartsWith("Send_"))
					continue;

				// find corresponding receive method
				MethodInfo recvMethod = methods.Find(m => m.Name == string.Format("Receive_{0}", method.Name));

				// if no method was found, then don't create the delegate
				if (recvMethod == null)
//				{
//					Debug.LogFormat("Network Scope: Skipping method {0} in {1} because no \"Receive_{0}\" function was found. The type is probably missing the [ClientSignalSync(typeof(SERVER_TYPE))] or [ServerSignalSync(typeof(CLIENT_TYPE))] attribute", method.Name, method.DeclaringType.Name);
					continue;
//				}
//				Debug.Log("Binding " + recvMethod.Name);

//				Debug.LogFormat("Binding {0}.{1} to hash {2}", GetType().Name, method.Name, method.Name.GetHashCode());

				cachedDeserializers.Add(method.Name.GetHashCode(), recvMethod);
			}
		}
	}
}
