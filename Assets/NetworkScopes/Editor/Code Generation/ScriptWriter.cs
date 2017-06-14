
using System.Collections;
using System.Collections.Generic;

namespace NetworkScopes.CodeGeneration
{
	using System.Text;

	public class ScriptWriter
	{
		StringBuilder sb = new StringBuilder();
		int tabCount = 0;
		int openScopes = 0;

		public void BeginScope()
		{
			openScopes++;
			WriteFullLine("{");
			tabCount++;
		}

		public void EndScope()
		{
			openScopes--;
			tabCount--;
			WriteFullLine("}");
		}

		public void Finish()
		{
			while (openScopes > 0)
				EndScope();
		}

		public void BeginWrite()
		{
			WriteTabs();
		}

		public void Write(string text)
		{
			sb.Append(text);
		}

		public void WriteFormat(string text, params object[] args)
		{
			sb.AppendFormat(text, args);
		}

		public void EndWrite()
		{
			sb.AppendLine();
		}

		public void WriteFullLine(string text)
		{
			WriteTabs();
			sb.AppendLine(text);
		}

		public void WriteFullLineFormat(string text, params object[] objs)
		{
			WriteTabs();
			sb.AppendLine(string.Format(text, objs));
		}

		public void NewLine()
		{
			sb.AppendLine();
		}

		private void WriteTabs()
		{
			for (int x = 0; x < tabCount; x++)
				sb.Append("\t");
		}

		public void WriteMultiple(IEnumerable<IWritable> writables)
		{
			foreach (IWritable writable in writables)
			{
				writable.Write(this);
			}

			NewLine();
		}

		public override string ToString ()
		{
			return sb.ToString();
		}
	}
}