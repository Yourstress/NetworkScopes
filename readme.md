
Network Scopes V2
=================
A lightweight, high-performance network library featuring strong-typed communication between server and client(s) for Unity3D.


Features
--------
- Strong-typed communication via direct method calls (named **Signals**).

- Automatically-managed channels for segmenting peers between lobbies or matches (**Scopes**).

- High-performance binary serialization. Automatically make virtually any class serializable, or write your own serializer/deserializer methods exactly to your needs.

- Automatic generation of boilerplate code for Signals, Scopes and object serialization.

- Automatic generation of object serialization code for best performance at the lowest cost.

- Built on lidgren (https://github.com/lidgren/lidgren-network-gen3). Can be customized to use ANY network library (UNet, Photon, C# sockets, etc..) while retaining all the above features by implementing an adapter for your desired library.


Getting Started
---------------
- **Scopes** allow communication between two C# objects (server and client), defined by C# interfaces and automatically generated.
- **Signals** are C# methods defined within a **Scope** that the server or client can receive with any number of parameters.

To define your client and Server scopes, simply write two interfaces that inherit **IServerScope** and **IClientScope** and define the methods (Signals) each entity can receive:
```
[Scope(typeof(IMyClientLobby))]
public interface IMyServerLobby : IServerScope
{
    // this is a "Promise" Signal. The client will be able to register a callback when the server returns the player count.
    int GetOnlinePlayerCount();
    
    // this is a Signal. When the client calls the method, it will trigger the server's LookForMatch() scope implementation.
    void LookForMatch();
}

[Scope(typeof(IMyServerLobby))]
public interface IMyClientLobby : IClientScope
{
    void FoundMatch(Match match);
}
```

In Unity, select the menu item *Tools/Network Scopes/Generate Scopes*. This will generate the server and client Scopes MyServerLobby and MyClientLobby implementing IMyServerLobby and IMyClientLobby, respectively:

```

```




Signal Structure
-----------------
A Signal is a network method call to a specific Scope (target network object). Each Signal is serialized and deserialized as follows (in order):

(short) scopeChannel: Tells the receiver which registered Scope should read and process this Signal.
(short) signalIdentifier: A unique identifier pointing to a specific method in the receiving Scope.
(optional...) The parameters that the above method expects, depending on their defined types.


Upcoming Features
------------------


- Automatically serialize Arrays, Lists, and Dictionaries of any serializable types.
- Customizable connection approval validation. Easily define how clients should authenticate with the server, including which parameters are sent and how it's validated.