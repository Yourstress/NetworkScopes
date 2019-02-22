using System;
using System.Runtime.CompilerServices;

namespace NetworkScopes
{
	public class NetworkTask : INotifyCompletion
	{
		#region INotifyCompletion implementation

		protected Action _continuation;

		public void OnCompleted(Action continuation)
		{
			_continuation = continuation;
		}

		#endregion

		public bool IsCompleted { get; protected set; }
	}

	public class NetworkTask<T> : NetworkTask
	{
		public T Result { get; private set; }

		public void OnCompleted(T value)
		{
			Result = value;

			IsCompleted = true;
			_continuation?.Invoke();
		}

		public T GetResult()
		{
			return Result;
		}

		public NetworkTask<T> GetAwaiter()
		{
			return this;
		}
	}
}