using NetworkScopes;

[Scope(typeof(IMyClientLobby))]
public interface IMyServerLobby : IServerScope
{
    // this is a Signal that MUST return a number
    int GetOnlinePlayerCount();
    
    // this is a Signal that doesn't return anything immediately, but 
    void LookForMatch();
}