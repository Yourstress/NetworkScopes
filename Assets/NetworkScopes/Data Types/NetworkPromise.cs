
using System;

namespace NetworkScopes
{
    public interface INetworkPromise
    {
        void Receive(ISignalReader reader);
    }

    public interface INetworkPromise<T> : INetworkPromise
    {
        bool HasValue { get; }
        T Value { get; }

        void ContinueWith(Action<T> onReceivePromise);
    }

    public class ValuePromise<T> : INetworkPromise<T> where T : IComparable
    {
        public bool HasValue { get; private set; }
        public T Value { get; private set; }
        private Action<T> _onReceivePromise;

        void INetworkPromise.Receive(ISignalReader reader)
        {
            Value = reader.ReadValue<T>();
            HasValue = true;

            if (_onReceivePromise != null)
                _onReceivePromise(Value);
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
        public bool HasValue { get; private set; }
        public T Value { get; private set; }
        private Action<T> _onReceivePromise;

        void INetworkPromise.Receive(ISignalReader reader)
        {
            Value = reader.ReadObject<T>();
            HasValue = true;

            if (_onReceivePromise != null)
                _onReceivePromise(Value);
            else
                UnityEngine.Debug.Log("Received unhandled promise");
        }

        public void ContinueWith(Action<T> onReceivePromise)
        {
            _onReceivePromise += onReceivePromise;
        }
    }
}