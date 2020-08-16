using System.Reflection;
using HarmonyLib;
using LightContainmentZoneDecontamination;

namespace ArithFeather.ScatteredSurvival {
	[HarmonyPatch(typeof(DecontaminationController), "Start")]
	internal static class DisableDecontaminationPatch {
		private static readonly FieldInfo DisableDecontamination =
			typeof(DecontaminationController).GetField("_disableDecontamination",
				BindingFlags.NonPublic | BindingFlags.Instance);

		private static bool Prefix(DecontaminationController __Instance) {
			DisableDecontamination.SetValue(__Instance, true);
			return false;
		}
	}
}