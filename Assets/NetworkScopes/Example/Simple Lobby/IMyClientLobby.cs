
using NetworkScopes;

[ClientScope(typeof(IMyServerLobby))]
public interface IMyClientLobby
{
    // this is a Signal thst is manually called at a later time.
    void FoundMatch();
}