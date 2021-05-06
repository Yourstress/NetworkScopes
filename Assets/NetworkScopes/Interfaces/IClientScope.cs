
namespace NetworkScopes
{
	public interface IClientScope : IBaseScope
	{
		ScopeIdentifier scopeIdentifier { get; }
		ScopeChannel channel { get; }

		void Initialize(IClientSignalProvider serviceProvider, ScopeIdentifier scopeIdentifier);
		void EnterScope(ScopeChannel channel);
		void ExitScope();
		void ProcessSignal(ISignalReader signal);
	}
}