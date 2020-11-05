using HarmonyLib;
using Respawning;

namespace ArithFeather.ScatteredSurvival.Patches
{
	[HarmonyPatch(typeof(RespawnManager), "ForceSpawnTeam")]
	internal static class AllowTeamSpawnPatch
	{

		private static bool Prefix()
		{
			if (!ScatteredSurvival.Configs.IsEnabled) return true;
			OnForceTeamSpawn?.Invoke();
			return false;
		}

		public delegate void ForceTeamSpawn();
		public static event ForceTeamSpawn OnForceTeamSpawn;
	}
}
