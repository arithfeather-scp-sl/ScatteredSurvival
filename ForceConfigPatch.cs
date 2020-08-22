using System;
using HarmonyLib;

namespace ArithFeather.ScatteredSurvival {
	[HarmonyPatch(typeof(YamlConfig), "GetString")]
	internal static class ForceConfigPatch {

		private static bool Prefix(ref string __result, string key, string def = "")
		{
			if (!ScatteredSurvival.Configs.IsEnabled) return true;

			if (key.Equals("team_respawn_queue", StringComparison.InvariantCultureIgnoreCase))
			{
				__result = ScatteredSurvival.Configs.TeamSpawnQueue;
				return false;
			}

			return true;
		}
	}
}
