using NetworkScopes;

[Generated]
public class MyServerMatch : MyServerMatch_Abstract
{
    public bool isFull { get { return peers.Count >= 4; }}
}
