using NetworkScopes;

[Scope(typeof(IMyServerLobby))]
public interface IMyClientLobby : IClientScope
{
    // this is a Signal thst is manually called at a later time.
    void FoundMatch(LobbyMatch match);

    void FoundMatches(LobbyMatch[] matches);
}