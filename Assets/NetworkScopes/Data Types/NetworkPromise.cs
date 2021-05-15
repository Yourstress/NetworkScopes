
using System;
using System.Threading;
using System.Threading.Tasks;

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
        private T lastValue;
        private Action<T> _onReceivePromise;

        private int lastValueId = 0;

        void INetworkPromise.Receive(ISignalReader reader)
        {
            T value = lastValue = reader.ReadValue<T>();
            
            lastValueId++;

            if (_onReceivePromise != null)
                _onReceivePromise(value);
            // else
                // Debug.Log("Received unhandled promise");
        }

        public void ContinueWith(Action<T> onReceivePromise)
        {
            _onReceivePromise += onReceivePromise;
        }

        public async System.Threading.Tasks.Task<T> GetAsync(int timeoutInSeconds = 3)
        {
            DateTime cutoffTime = DateTime.Now.AddSeconds(timeoutInSeconds);

            int valueId = lastValueId;

            do
            {
                await System.Threading.Tasks.Task.Delay(1);
            }
            while (valueId == lastValueId && DateTime.Now < cutoffTime);
            
            if (DateTime.Now >= cutoffTime)
                NSDebug.LogError("Time expired!");
            
            return lastValue;
        }
    }
    public class ObjectPromise<T> : INetworkPromise<T> where T : ISerializable, new()
    {
        private T lastValue;
        private Action<T> _onReceivePromise;
        
        private int lastValueId = 0;
        void INetworkPromise.Receive(ISignalReader reader)
        {
            T value = lastValue = reader.ReadObject<T>();

            lastValueId++;

            if (_onReceivePromise != null)
                _onReceivePromise(value);
            // else
                // NSDebug.Log("Received unhandled promise");
        }

        public void ContinueWith(Action<T> onReceivePromise)
        {
            _onReceivePromise += onReceivePromise;
        }
        
        public async System.Threading.Tasks.Task<T> GetAsync(CancellationToken cancelToken, int timeoutInSeconds = 3)
        {
            DateTime cutoffTime = DateTime.Now.AddSeconds(timeoutInSeconds);

            int valueId = lastValueId;

            do
            {
                await System.Threading.Tasks.Task.Delay(1, cancelToken);
            }
            while (valueId == lastValueId && DateTime.Now < cutoffTime);
            
            if (DateTime.Now >= cutoffTime)
                NSDebug.LogError("Time expired!");
            
            return lastValue;
        }
    }
}