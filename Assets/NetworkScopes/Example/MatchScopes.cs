namespace NetworkScopes.Examples
{
    [ClientScope(typeof(IMyServerMatch))]
    public interface IMyClientMatch
    {
        void Test1();
        void Test2(string str);
    }
    
    [ServerScope(typeof(IMyClientMatch), typeof(MyPeer))]
    public interface IMyServerMatch
    {
        void Test1();
        void Test2(string str);
        int Test3();
        
        void LeaveMatch();
    }
}