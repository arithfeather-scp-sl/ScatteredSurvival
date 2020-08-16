using Exiled.API.Interfaces;

namespace ArithFeather.ScatteredSurvival {
	public class Config : IConfig
	{
		public bool IsEnabled { get; set; } = true;

		public bool ShowGameStartMessage { get; set; } = true;
	}
}
