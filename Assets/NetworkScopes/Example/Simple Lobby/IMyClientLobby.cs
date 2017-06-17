using System.Text.RegularExpressions;
using NetworkScopes;

[NetworkSerialize]
public partial class LobbyMatch
{
    // any data about the match
}

[Scope(typeof(IMyServerLobby))]
public interface IMyClientLobby : IClientScope
{
    // this is a Signal thst is manually called at a later time.
    void FoundMatch(LobbyMatch match);
}