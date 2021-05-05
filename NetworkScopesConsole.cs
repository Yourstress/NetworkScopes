
using System.Threading.Tasks;
using NetworkScopes.Examples;

namespace NetworkScopesConsole
{
    class NetworkScopesConsole
    {
        static async Task Main(string[] args)
        {
            await Example.TestNetworkScopes();
        }
    }
}