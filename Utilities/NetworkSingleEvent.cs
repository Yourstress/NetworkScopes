
namespace NetworkScopes
{
	public delegate void NetworkSingleEvent();
	public delegate void NetworkSingleEvent<T>(T arg);
	public delegate void NetworkSingleEvent<T1,T2>(T1 arg1, T2 arg2);
	public delegate void NetworkSingleEvent<T1,T2,T3>(T1 arg1, T2 arg2, T3 arg3);
	public delegate void NetworkSingleEvent<T1,T2,T3,T4>(T1 arg1, T2 arg2, T3 arg3, T4 arg4);
}