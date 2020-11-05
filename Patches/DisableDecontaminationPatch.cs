using HarmonyLib;
using LightContainmentZoneDecontamination;

namespace ArithFeather.ScatteredSurvival.Patches
{
	[HarmonyPatch(typeof(DecontaminationController), "Update")]
	internal static class DisableDecontaminationPatch
	{
		private static bool Prefix()
		{
			return !ScatteredSurvival.Configs.IsEnabled;
		}
	}
}