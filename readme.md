
Network Scopes V2
=================
A lightweight, high-performance network library featuring strong-typed communication between peers for Unity3D.


FEATURES:
---------
- Strong-typed communication using C# method calls (named Signals).

- Scopes allow communication between two C# objects, defined by C# interfaces and automatically generated.

- Automatic serialization of primitive types and custom types.

- Optionally serialize custom types manually for full-flexibility.

- Built on lidgren (https://github.com/lidgren/lidgren-network-gen3). Can be customized to use ANY network library (UNet, Photon, C# sockets, etc..) by implementing an adapter.


SIGNAL STRUCTURE:
-----------------
A Signal is a network method call to a specific Scope (target network object). Each Signal (remote method call) is serialized and deserialized as follows (in order):

(short) scopeChannel: Tells the receiver which registered Scope should read and process this Signal.
(short) signalIdentifier: A unique identifier pointing to a specific method in the receiving Scope.
(optional...) The parameters that the above method expects, depending on their defined types.
