using Exiled.API.Interfaces;

namespace ArithFeather.ScatteredSurvival {
	public class Config : IConfig {
		public bool IsEnabled { get; set; } = true;

		/// <summary>
		/// 0 - Version number
		/// </summary>
		public string GameModeInformation { get; set; } =
			"<size=50><color=#23eb44><color=#38a8b5>Scattered Survival Mode v{0}!</color> Press ` to open the console and enter '<color=#38a8b5>.sshelp</color>' for mod information!</color></size>";

		public int SafeTeamSpawnDistance { get; set; } = 30;

		public int SafeEnemySpawnDistance { get; set; } = 60;

		public float RespawnSpeed { get; set; } = 30;

		public string TeamSpawnQueue { get; set; } = "40444440444440444444044444440444444404444444404444444440444444444404444444444";

		public bool EnableHalfHpFemurBreaker { get; set; } = true;

		public bool EnableGeneratorWinCondition { get; set; } = true;

		// Player Lives
		
		/// <summary>
		/// {0} Is lives left.
		/// </summary>
		public string DisplayMessageFormat { get; set; } = "<size=50>Player Lives: <color=#23eb44>{0}</color></size>";
		public int MessageRefreshTime { get; set; } = 10;
		public ushort MessageTime { get; set; } = 3;
		public float LifeScalingIncreasePerScp { get; set; } = 1.5f;
		public int LivesPerScp { get; set; } = 5;

		// Spooky Lights

		public bool EnableSpookyLights { get; set; } = true;
		public int MinTimeBetweenFlickers { get; set; } = 120; // Every 2 - 5 minutes
		public int MaxTimeBetweenFlickers { get; set; } = 450;
		public int MinDurationOfFlicker { get; set; } = 30; // For 30 - 60 seconds
		public int MaxDurationOfFlicker { get; set; } = 60;
		public bool EnableFlickerAnnouncements { get; set; } = true;
	}
}