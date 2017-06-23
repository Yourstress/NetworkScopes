
using NetworkScopes;

[ServerScope(typeof(IMyClientLobby))]
public interface IMyServerLobby
{
    int GetOnlinePlayerCount();

    void JoinAnyMatch();
}