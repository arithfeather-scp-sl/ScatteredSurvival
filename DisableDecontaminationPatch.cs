using HarmonyLib;
using LightContainmentZoneDecontamination;
using UnityEngine;

namespace ArithFeather.ScatteredSurvival {
	[HarmonyPatch(typeof(DecontaminationController), "Update")]
	internal static class DisableDecontaminationPatch {
		private static bool Prefix()
		{
			if (!ScatteredSurvival.Configs.IsEnabled) return true;

			return false;
		}
	}
}