using System;

namespace NetworkScopes.Examples
{
	[Generated]
	public abstract class MyClientMatch_Abstract : ClientScope<MyClientMatch_Abstract.ISender>, MyClientMatch_Abstract.ISender
	{
		[Generated]
		public interface ISender : IScopeSender
		{
			void Test1();
			void Test2(string str);
			ValuePromise<int> Test3();
		}

		protected override ISender GetScopeSender()
		{
			return this;
		}

		void ISender.Test1()
		{
			DateTime dateTime = DateTime.Now;
			ISignalWriter writer = CreateSignal(723512716 /*hash 'Test1'*/);
			SendSignal(writer);
		}

		void ISender.Test2(string str)
		{
			ISignalWriter writer = CreateSignal(-2005370642 /*hash 'Test2'*/);
			writer.Write(str);
			SendSignal(writer);
		}

		ValuePromise<int> ISender.Test3()
		{
			ValuePromise<int> promise = new ValuePromise<int>();
			ISignalWriter writer = CreatePromiseSignal(-439286700, promise /*hash 'Test3'*/);
			SendSignal(writer);
			return promise;
		}

		protected abstract void Test1();
		protected abstract void Test2(string str);
		protected void ReceiveSignal_Test1(ISignalReader reader)
		{
			Test1();
		}

		protected void ReceiveSignal_Test2(ISignalReader reader)
		{
			string str = reader.ReadString();
			Test2(str);
		}

		protected void ReceivePromise_Test3(ISignalReader reader)
		{
			ReceivePromise(reader);
		}

	}
}
