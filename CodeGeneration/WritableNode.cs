
using System.Text;

namespace CodeGeneration
{
	public abstract class WritableNode
	{
		public void Write(StringBuilder sb)
		{
			WriteStart(sb);
			WriteBody(sb);
			WriteEnd(sb);
		}

		public abstract void WriteStart(StringBuilder sb);
		public abstract void WriteBody(StringBuilder sb);
		public abstract void WriteEnd(StringBuilder sb);
	}



}