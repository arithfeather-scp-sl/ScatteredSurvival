using Exiled.API.Interfaces;

namespace ArithFeather.ScatteredSurvival {
	public class Config : IConfig {
		public bool IsEnabled { get; set; } = true;

		public bool ShowGameStartMessage { get; set; } = true;

		public int SafeTeamSpawnDistance { get; set; } = 30;

		public int SafeEnemySpawnDistance { get; set; } = 60;

		public string TeamSpawnQueue { get; set; } = "40444440444440444444044444440444444404444444404444444440444444444404444444444";
	}
}