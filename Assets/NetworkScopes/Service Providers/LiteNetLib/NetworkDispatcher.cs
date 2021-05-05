
using System;
using System.Threading;

#if UNITY_ENGINE
using UnityEngine;
#else
using System.Threading.Tasks;
#endif


namespace NetworkScopes.ServiceProviders.LiteNetLib
{
    public interface INetworkDispatcher
    {
        void TickNetwork();
        void ApplicationWillQuit();
    }

    public class NetworkDispatcher
    #if UNITY_ENGINE
     : MonoBehaviour
    #endif
    {
        private INetworkDispatcher _listener;
        
        #if !UNITY_ENGINE
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        #endif

        public static NetworkDispatcher CreateDispatcher(INetworkDispatcher listener)
        {
            #if UNITY_ENGINE
            GameObject dispatcherGO = new GameObject(nameof(NetworkDispatcher));
            //dispatcherGO.hideFlags = HideFlags.HideAndDontSave;

            NetworkDispatcher dispatcher = dispatcherGO.AddComponent<NetworkDispatcher>();
            #else
            NetworkDispatcher dispatcher = new NetworkDispatcher();
            #endif
            
            dispatcher.Initialize(listener);
            
            return dispatcher;
        }
        
        public void Initialize(INetworkDispatcher listener)
        {
            _listener = listener;
            
            RunUpdateAsync();
        }

        async void RunUpdateAsync()
        {
            while (!_cancellationTokenSource.IsCancellationRequested)
            {
                Update();

                await Task.Delay(15);
            }
        }

        void Update()
        {
            #if UNITY_ENGINE
            _listener.TickNetwork();
            #else
            try
            {
                _listener.TickNetwork();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
            #endif
        }

        #if UNITY_ENGINE
        private void OnApplicationQuit()
        {
            _listener.ApplicationWillQuit();
        }
        #endif

        public void DestroyDispatcher()
        {
            #if UNITY_ENGINE
            if (this != null)
                Destroy(gameObject);
            #else
            if (_cancellationTokenSource != null)
            {
                _cancellationTokenSource.Cancel();
            }
            #endif
        }
    }
}