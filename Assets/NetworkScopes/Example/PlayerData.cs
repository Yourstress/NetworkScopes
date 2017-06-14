using NetworkScopes;

namespace MyExamples
{
	[NetworkSerialize]
	public partial class PlayerData
	{
		public int playerID;
		public string playerName;

		private int privateValue;

		public PlayerData()
		{
		}

		public PlayerData(int playerID, string playerName)
		{
			this.playerID = playerID;
			this.playerName = playerName;
		}
	}
}