
namespace NetworkScopes.Examples
{
	[Generated]
	public abstract class MyServerMatch_Abstract : ServerScope<MyPeer,MyServerMatch_Abstract.ISender>, MyServerMatch_Abstract.ISender
	{
		[Generated]
		public interface ISender : IScopeSender
		{
			void Test1();
			void Test2(string str);
		}

		protected override ISender GetScopeSender()
		{
			return this;
		}

		void ISender.Test1()
		{
			ISignalWriter writer = CreateSignal(723512716 /*hash 'Test1'*/);
			SendSignal(writer);
		}

		void ISender.Test2(string str)
		{
			ISignalWriter writer = CreateSignal(-2005370642 /*hash 'Test2'*/);
			writer.Write(str);
			SendSignal(writer);
		}

		protected abstract void Test1();
		protected abstract void Test2(string str);
		protected abstract int Test3();
		protected abstract void LeaveMatch();
		protected void ReceiveSignal_Test1(ISignalReader reader)
		{
			Test1();
		}

		protected void ReceiveSignal_Test2(ISignalReader reader)
		{
			string str = reader.ReadString();
			Test2(str);
		}

		protected void ReceiveSignal_Test3(ISignalReader reader)
		{
			int promiseID = reader.ReadPromiseID();
			int promiseValue = Test3();
			ISignalWriter writer = CreateSignal(1330978604 /*hash '#Test3'*/);
			writer.Write(promiseID);
			writer.Write(promiseValue);
			SendSignal(writer, SenderPeer);
		}

		protected void ReceiveSignal_LeaveMatch(ISignalReader reader)
		{
			LeaveMatch();
		}

	}
}
