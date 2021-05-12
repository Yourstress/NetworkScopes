using System.Collections.Generic;
using NetworkScopes;

public class NetworkPromiseHandler
{
    private readonly Dictionary<int, INetworkPromise> pendingPromises = new Dictionary<int, INetworkPromise>();

    public int EnqueuePromise(INetworkPromise promise)
    {
        int promiseID = GenerateUniqueKey();
        pendingPromises[promiseID] = promise;
        return promiseID;
    }

    public void DequeueAndReceivePromise(int promiseID, ISignalReader reader)
    {
        if (pendingPromises.TryGetValue(promiseID, out INetworkPromise promise))
        {
            promise.Receive(reader);
            
            pendingPromises.Remove(promiseID);
        }
        else
        {
            NSDebug.Log($"Could not call previously registered promise with id {promiseID}.");
        }
    }

    private int GenerateUniqueKey()
    {
        // find an unused key
        int unusedKey = pendingPromises.Count;
        while (pendingPromises.ContainsKey(unusedKey))
            unusedKey++;

        return unusedKey;
    }

    public void ClearPromises()
    {
        pendingPromises.Clear();
    }
}