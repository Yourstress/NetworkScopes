
namespace NetworkScopes.Examples
{
    [ServerScope(typeof(IMyClientMatch))]
    public interface IMyServerMatch
    {
        void Test1();
        void Test2(string str);
        int Test3();
    }
}