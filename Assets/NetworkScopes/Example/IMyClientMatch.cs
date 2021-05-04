namespace NetworkScopes.Examples
{
    [ClientScope(typeof(IMyServerMatch))]
    public interface IMyClientMatch
    {
        void Test1();
        void Test2(string str);
    }
}