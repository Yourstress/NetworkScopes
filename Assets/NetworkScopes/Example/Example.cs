
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using NetworkScopes.CodeGeneration;

namespace NetworkScopes.Examples
{
    public static class Example
    {
        public static void GenerateClasses()
        {
            NetworkScopeProcessor.GenerateNetworkScopes(logOnly: false);
        }
        
        public static async Task TestNetworkScopes()
        {
            NetworkServer server = NetworkServer.CreateLiteNetLibServer();
            MyServerMatch serverMatch = server.RegisterScope<MyServerMatch>(0);

            server.StartListening(7979);

            if (!server.IsListening)
                throw new Exception("Server not listening.");

            await Task.Delay(1000);
            
            NetworkClient client = NetworkClient.CreateLiteNetLibClient();
            MyClientMatch clientMatch = client.RegisterScope<MyClientMatch>(0);

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
                string line = Console.ReadLine();

                switch (line)
                {
                    case "1":
                        clientMatch.SendToServer.Test1();
                        break;
                    case "2":
                        clientMatch.SendToServer.Test2("str");
                        break;
                    case "3":
                        Stopwatch sw = new Stopwatch();
                        sw.Start();
                        await clientMatch.SendToServer.Test3().GetAsync();
                        sw.Stop();
                        
                        Console.WriteLine($"Took {sw.ElapsedMilliseconds}ms");
                    
                        break;
                }
            }
        }
        
        static void Log(string str) => Debug.Log(str);
    }
}