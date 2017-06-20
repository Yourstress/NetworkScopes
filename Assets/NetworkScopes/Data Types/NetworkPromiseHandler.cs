using System.Collections.Generic;
using NetworkScopes;
using UnityEngine;

public class NetworkPromiseHandler
{
    private Dictionary<int, INetworkPromise> pendingPromises = new Dictionary<int, INetworkPromise>();

    public int EnqueuePromise(INetworkPromise promise)
    {
        int promiseID = GenerateUniqueKey();
        pendingPromises[promiseID] = promise;
        return promiseID;
    }

    public void DequeueAndReceivePromise(int promiseID, ISignalReader reader)
    {
        INetworkPromise promise;
        if (!pendingPromises.TryGetValue(promiseID, out promise))
            Debug.LogFormat("Could not call previously registered promise.");

        promise.Receive(reader);

        pendingPromises.Remove(promiseID);
    }

    private int GenerateUniqueKey()
    {
        // find an unused key
        int unusedKey = pendingPromises.Count;
        while (pendingPromises.ContainsKey(unusedKey))
            unusedKey++;

        return unusedKey;
    }
}