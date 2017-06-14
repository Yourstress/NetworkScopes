
Signal packet structure
=======================
Each signal (remote method call) is serialized and deserialized as follows (in order):

(short) scopeChannel: Tells the receiver which registered Scope should read and process this Signal.
(short) signalIdentifier: A unique identifier pointing to a specific method in the receiving Scope.
(optional...) The parameters that the above method expects, depending on their defined types.