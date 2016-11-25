
namespace NetworkScopes
{
	using System;

	public class NetworkPromise<T>
	{
		private bool didReturn = false;

		public void Return(T value)
		{
			if (didReturn)
				throw new Exception("Promise already returned.");
		}

		public static NetworkPromise<T> ReturnImmediately(T value)
		{
			return new NetworkPromise<T>();
		}
	}

	public class NetworkPromise<T1,T2>
	{
		public T1 argument1 { get; private set; }
		public T2 argument2 { get; private set; }

		public static NetworkPromise<T1,T2> Create(T1 value1, T2 value2)
		{
			var prom = new NetworkPromise<T1,T2>();
			prom.argument1 = value1;
			prom.argument2 = value2;
			return prom;
		}
	}
}