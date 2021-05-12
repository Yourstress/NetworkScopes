
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using NetworkScopes.CodeGeneration;
using NetworkScopes.ServiceProviders.LiteNetLib;

namespace NetworkScopes.Examples
{
    public static class Example
    {
        public static void GenerateClasses()
        {
            NetworkScopeProcessor.GenerateNetworkScopes(logOnly: false);
        }
        
        private class Command
        {
            public string text;
            public Action action;

            public static implicit operator Command((string match, Action action) tuple)
            {
                return new Command()
                {
                    text = tuple.match,
                    action = tuple.action
                };
            }
        }
        
        public static async Task TestNetworkScopes()
        {
            INetworkServer server = new LiteNetServer<MyPeer>();
            MyServerLobby serverLobby = server.RegisterScope<MyServerLobby>(0);

            // not explicitally needed - the first registered scope on the server is the default
            server.defaultScope = serverLobby;

            server.StartListening(7979);

            if (!server.IsListening)
                throw new Exception("Server not listening.");

            await Task.Delay(1000);
            
            NetworkClient client = NetworkClient.CreateLiteNetLibClient();
            MyClientLobby clientLobby = client.RegisterScope<MyClientLobby>(0);
            MyClientMatch clientMatch = client.RegisterScope<MyClientMatch>(1);
            

            client.OnStateChanged += OnClientStateChanged;
            client.Connect("localhost", 7979);

            if (!client.IsConnecting)
                throw new Exception("Client not connecting.");

            bool didStateChange = false;

            void OnClientStateChanged(NetworkState state)
            {
                Log($"Client state changed to {state}");
                
                didStateChange = true;
            }

            while (!didStateChange)
                await Task.Delay(100);

            // Log("Client --> Test3() to server.");
            // clientMatch.SendToServer.Test2("hello");
            //
            // await Task.Delay(500);
            
            // Log("Client --> Test3() to server.");
            // int value = await clientMatch.SendToServer.Test3().GetAsync();
            // Log($"Client <-- {value}");

            while (true)
            {
                await DrawGlobalCommands();
                
                Task DrawGlobalCommands()
                {
                    Command[] lobbyCommands = new Command[]
                    {
                        ($"Join match ({serverLobby.matches.Count} running)", () => clientLobby.SendToServer.JoinAnyMatch()),
                        ("Join any match (promise)", () => clientLobby.SendToServer.JoinMatch(false)),
                        ("Generate", () => NetworkScopeProcessor.GenerateNetworkScopes(false))
                    };
            
                    Command[] matchCommands = new Command[]
                    {
                        ("Test1", () => clientMatch.SendToServer.Test1()),
                        ("Test2", () => clientMatch.SendToServer.Test2("str")),
                        ("Test3", () => clientMatch.SendToServer.Test3()),
                        ("LeaveMatch", () => clientMatch.SendToServer.LeaveMatch()),
                        ("Generate", () => NetworkScopeProcessor.GenerateNetworkScopes(false))
                    };
                    
                    if (clientLobby.IsActive)
                        DrawCommands(lobbyCommands);
                    else if (clientMatch.IsActive)
                        DrawCommands(matchCommands);
                    
                    return Task.Delay(200);
                }


                void DrawCommands(Command[] commands)
                {
                    Console.WriteLine("Available commands: ");
                    int num = 1;
                    foreach (Command command in commands)
                        Console.WriteLine($" [{num++}] {command.text}");
                    
                    Console.Write($"Choose a command: ");
                    
                    string line = Console.ReadLine();

                    if (!int.TryParse(line, out num) || num < 1 || num > commands.Length)
                    {
                        Console.WriteLine("Invalid command.");
                        Console.WriteLine();
                        return;
                    }

                    Command cmd = commands[num - 1];
                    cmd.action();
                }
            }
        }
        
        static void Log(string str) => NSDebug.Log(str);
    }
}