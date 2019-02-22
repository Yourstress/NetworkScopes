namespace NetworkScopes.Editor
{
	public static class StringExtensions
	{
		public static string ConvertTabs(this string str)
		{
			return str.Replace("\t", "    ");
		}
	}
}