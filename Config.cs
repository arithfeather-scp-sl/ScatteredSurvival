using Exiled.API.Interfaces;

namespace ArithFeather.ScatteredSurvival {
	public class Config : IConfig
	{
		public bool IsEnabled { get; set; } = true;

		public bool ShowGameStartMessage { get; set; } = true;

		public int SafeTeamSpawnDistance { get; set; } = 30;

		public int SafeEnemySpawnDistance { get; set; } = 60;
	}
}
