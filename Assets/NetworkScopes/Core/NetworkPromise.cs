
namespace NetworkScopes
{
	using System;

	public class NetworkPromise<T>
	{
		public T value { get; private set; }

		public NetworkPromise() {}

		public NetworkPromise(T val)
		{
			value = val;
		}
	}

	public class NetworkPromise<T1,T2>
	{
		public T1 value1 { get; private set; }
		public T2 value2 { get; private set; }

		public NetworkPromise() {}

		public NetworkPromise(T1 val1, T2 val2)
		{
			value1 = val1;
			value2 = val2;
		}
	}

	public class NetworkPromise<T1,T2,T3>
	{
		public T1 value1 { get; private set; }
		public T2 value2 { get; private set; }
		public T3 value3 { get; private set; }

		public NetworkPromise() {}

		public NetworkPromise(T1 val1, T2 val2, T3 val3)
		{
			value1 = val1;
			value2 = val2;
			value3 = val3;
		}
	}
}