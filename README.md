NetworkScopes for Unity
=======================
NetworkScopes is a powerful and lightweight Networking library for Unity built on UNet LLAPI focused on two things:
- Delivering great performance while using the minimum bandwidth.
- Allowing clean, oraganized and strong-typed network code.

Features
--------
- Easy network communication via direct method calls (Signals).
- Automatically-managed channels for segmenting peers between lobbies or matches (Scopes).
- High-performance binary class serialization that is generated at compile time. Automatically make virtually any class serializable, or custom-serialize it exactly according to your needs.
- Built on LLAPI and fully open-source, and can be further customized and improved.

Scopes
------
Scopes allow two classes (Server and Client, usually) to communicate through direct method calls (called Signals).

When defining Scopes, notice how the Client references the Server type and vice-versa. This gives each access to the other's methods, though direct methods will be replaced with Network send calls during compile-time.

Let's start defining a Lobby Scope that can host Matches for Peers.

    public class ExampleServerLobby : ServerScope<Peer, ExampleClientLobby>
    {
    }

A server-side Scope MUST inherit from ServerScope, which takes two generic parameters:

1: TPeer: Defines the class that represents a Peer on the server. Must implement IScopePeer.

2: TClientScope: Defines this Scope's Client counterpart class type.

    public class ExampleClientLobby : ClientScope<Peer, ExampleServerLobby>
    {
    }

A client-side Scope MUST inherit from ClientScope, which takes two generic parameters:

1: TPeer: Defines the class that represents a Peer on the server. Must implement IScopePeer.

2: TClientScope: Defines this Scope's Server counterpart class type.

Signals
-------
Signals are method calls (attributed with [Signal]) that travel across the network between Scopes. They are very easy to define:

    public class ExampleServerLobby : ServerScope<Peer, ExampleClientLobby>
    {
      [Signal]
      public void JoinMatch(string matchName)
      {
        // tell the client the game doesn't exist
        Client.OnError("match not found");
      }
    }


    public class ExampleClientLobby : ClientScope<Peer, ExampleServerLobby>
    {
      [Signal]
      public void OnError(string errorMsg)
      {
        // handle match not found
      }
    }

Somewhere in the client, where you have the Lobby instance, sending a Signal to the server is easy:

    ExampleClientLobby lobby = // set elsewhere
    lobby.Server.JoinMatch("My Game");
    
Signal Target
-------------
In the previous example, the Server's JoinMatch method sends the client an OnError Signal. By default, Signals are sent directly to the last sender when inside a Signal method. However, it's possible to send to another Peer, or multiple Peers.

      [Signal]
      public void JoinMatch(string matchName)
      {
        // it's possible to send this to another peer by setting the TargetPeer property before calling Client.X
        // this next line has no effect, as the default TargetPeer is set to SenderPeer when receiving a Signal.
        Client.TargetPeer = SenderPeer;
        
        // it's also possible to send to a group of peers by setting the TargetPeerGroup to any IEnumerable<TPeer>.
        // this next line sets the target to all the peers that are in the current scope
        Client.TargetPeerGroup = Peers.Values;
        
        // tell the client the game doesn't exist
        Client.OnError("match not found");
      }

Server Setup
-----
Setting up a server is easy:
- Create class that inherits <b>MasterServer<TPeer></b>
- Call RegisterScope to create your <b>default</b> Scope. If you plan to authenticate users, you may have an Authentication scope that will process them before they enter the Lobby.
- Call RegisterScope to create your other Scopes with the proper channels that match the Client Scopes.
- Override CreatePeer and DestroyPeer to handle the creation/destruction of your Peer class.
```
     public class ExampleServer : MasterServer<ExamplePeer>
     {
     	public static ExampleServerLobby Lobby { get; private set; }


     	public ExampleServer()
     	{
          // register a new authentication scope and set it as the default
          RegisterScope<ExampleServerAuthenticator>(0, true); // authenticator will use "channel" 0 (client authenticator must match it)

          // register a new server scope to which authenticated users will be redirected
          Lobby = RegisterScope<ExampleServerLobby>(1, false); // lobby will use "channel" 1 (client lobby must match it)
     	}

     	#region implemented abstract members of MasterServerScope
     	protected override ExamplePeer CreatePeer (NetworkConnection connection)
     	{
	     	return new ExamplePeer(connection);
     	}

     	protected override void DestroyPeer (ExamplePeer peer)
     	{
     		// nothing to clean-up
	     }
     	#endregion
     }
```

Now the server is set-up! Now all you need to do is call StartServer(port) and wait for connections to come in.

Client Setup
-----
Setting up a client is just as easy as setting up a server, if not easier:
- Create class that inherits <b>MasterClient</b>
- Call RegisterScope to create your Scopes with the proper channels that match the Server Scopes, and optionally save a reference.
```
public class ExampleClient : MasterClient
{
	public ExampleClientLobby Lobby { get; private set; }
	public ExampleClientMatch Match { get; private set; }

	public ExampleClient()
	{
		// register Scopes to receive Signals from the server
		RegisterScope<ExampleClientAuthenticator>(0);
		Lobby = RegisterScope<ExampleClientLobby>(1);
		Match = RegisterScope<ExampleClientMatch>(2);
	}
}
```

Now the client is ready to connect too! Once you have an instance of it, call Connect(host, port).

Now the server is set-up! Now all you need to do is call StartServer(port) and wait for connections to come in.

Once a client is connected, it will "Enter" the <b>default</b> Scope specified on the Server.

Scope Events
------------
You may override the ClientScope's <b>OnEnterScope/OnExitScope</b> to insert any custom logic on the client. Consider the following client-side authenticator that automatically send credentials upon entering:

```
public class ExampleClientAuthenticator : ClientScope<ExamplePeer,ExampleServerAuthenticator>
{
	protected override void OnEnterScope ()
	{
	  // tell the server to authenticate as soon as this scope is activated
		Server.Authenticate("sour", "testpw");
	}
}
```

It's also possible to override the ServerScope's <b>OnPeerEnteredScope/OnPeerExitedScope</b> on the server. It is called for every Peer that is added or removed from the Scope. 

```
public class ExampleServerLobby : ServerScope<ExamplePeer, ExampleClientLobby>
{
	public Dictionary<string,ExampleServerMatch> matches { get; private set; }

	public override void Initialize (MasterServer<ExamplePeer> server)
	{
		base.Initialize (server);

		// initialize the dictionary that will contain all running matches (expecting 100 running matches)
		matches = new Dictionary<string,ExampleServerMatch>(100);
		
		// create 3 empty Matches (Scopes) that peers can join and play in - you can optionally create games on demand
		matches.Add("test game 1", Master.RegisterScope<ExampleServerMatch>(2, false));
		matches.Add("test game 2", Master.RegisterScope<ExampleServerMatch>(2, false));
		matches.Add("test game 3", Master.RegisterScope<ExampleServerMatch>(2, false));
	}

	protected override void OnPeerEnteredScope (ExamplePeer peer)
	{
		// get the list of available matches and send them to this Peer
		string[] matchNames = new List<string>(matches.Keys).ToArray();

		// we must explicitly target this peer when not inside a [Scope]-attributed method
		TargetPeer = peer;
		
		// send list of matches to peer
		Client.OnMatchList(matchNames);
	}
}
```

Serialization
-------------
Primitive types are automatically serialized:
int,short,float,double,... (anything NetworkWriter/Reader can serialize)

Object types are automatically serialized:
string, Array (any serializable type)

Custom classes can also be serialized by using the [NetworkSerialization] attribute and supplying the type of serialization needed. In the following example, the flag NetworkSerializeSettings.AllFieldsAndProperties sets all fields and properties to be serialized when this object is sent to a Signal method.
```
[NetworkSerialization(NetworkSerializeSettings.AllFieldsAndProperties)]
public class ExampleObject
{
	public int num = 0;
	public string str = "";
	public float flt = 0;

	[NetworkNonSerialized]
	public int numNonSerialized = 0;
}
```

There are a number of possible settings for NetworkSerialization:

```
	public enum NetworkSerializeSettings
	{
		PublicFieldsOnly,
		PublicFieldsAndProperties,
		AllFields,
		AllFieldsAndProperties,
		OptIn,				// only serialize members with [NetworkSerialize]
		Custom,				// use custom serializer/deserializer
	}
```

The attribute [NetworkNonSerialized] may be used to exclude a field or property from being serialized.
The attribute [NetworkSerialized] may be used to include a field or property when serializing an object (typycally with the NetworkSerializeSettings.OptIn flag).

When using NetworkSerializeSettings.Custom, the compiler will check for the serializer/deserializer methods defined within the class, defined like the following:

```
[NetworkSerialization(NetworkSerializeSettings.Custom)]
public class ExampleObject
{
	public int num = 0;
	public string str = "";
	public float flt = 0;

	//[NetworkNonSerialized] // this isn't really needed with Custom serialization
	public int numNonSerialized = 0;
	
	public static void NetworkSerialize(ExampleObject obj, NetworkWriter writer)
	{
		writer.Write(obj.num);
		writer.Write(obj.str);
		writer.Write(obj.flt);
	}

	public static void NetworkDeserialize(ExampleObject obj, NetworkReader reader)
	{
		obj.num = reader.ReadInt32();
		obj.str = reader.ReadString();
		obj.flt = reader.ReadSingle();
	}
}
```

