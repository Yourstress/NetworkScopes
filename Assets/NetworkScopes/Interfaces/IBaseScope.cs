namespace NetworkScopes
{
	public interface IBaseScope
	{
		string name { get; }
		bool isActive { get; }
	}
}