
namespace MyCompany
{
	using NetworkScopes;

	public class ExampleClientMatch : ClientScope<ExampleClientMatch>
	{
	}

}

namespace NetworkScopes
{

	public partial class ClientScope<T>
	{
		void Start()
		{
		}
	}
}