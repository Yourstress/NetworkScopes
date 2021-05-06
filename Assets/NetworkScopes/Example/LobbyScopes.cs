namespace NetworkScopes.Examples
{
    [ClientScope(typeof(IMyServerLobby))]
    public interface IMyClientLobby
    {
        
    }
    
    [ServerScope(typeof(IMyClientLobby), typeof(MyPeer))]
    public interface IMyServerLobby
    {
        void JoinAnyMatch();
        bool JoinMatch(bool rankedOnly);
    }
}