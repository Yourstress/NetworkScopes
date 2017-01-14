
namespace NetworkScopes
{
	public class BaseClientScope<TServerScope> : IClientScope where TServerScope : IServerScope
	{
		protected TServerScope server { get; private set; }
	}
}