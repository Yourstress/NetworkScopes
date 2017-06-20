
using System;

namespace NetworkScopes
{
    public interface INetworkPromise
    {
        void Receive(ISignalReader reader);
    }

    public interface INetworkPromise<T> : INetworkPromise
    {
        void ContinueWith(Action<T> onReceivePromise);
    }

    public class ValuePromise<T> : INetworkPromise<T> where T : IComparable
    {
        private Action<T> _onReceivePromise;

        void INetworkPromise.Receive(ISignalReader reader)
        {
            T value = reader.ReadValue<T>();

            if (_onReceivePromise != null)
                _onReceivePromise(value);
            else
                UnityEngine.Debug.Log("Received unhandled promise");
        }

        public void ContinueWith(Action<T> onReceivePromise)
        {
            _onReceivePromise += onReceivePromise;
        }
    }
    public class ObjectPromise<T> : INetworkPromise<T> where T : ISerializable, new()
    {
        private Action<T> _onReceivePromise;

        void INetworkPromise.Receive(ISignalReader reader)
        {
            T value = reader.ReadObject<T>();

            if (_onReceivePromise != null)
                _onReceivePromise(value);
            else
                UnityEngine.Debug.Log("Received unhandled promise");
        }

        public void ContinueWith(Action<T> onReceivePromise)
        {
            _onReceivePromise += onReceivePromise;
        }
    }
}