using HarmonyLib;
using RimWorld;
using Verse;

namespace Research_Requires_Resources.Patches
{
    [HarmonyPatch(typeof(ResearchManager), nameof(ResearchManager.AddProgress))]
    public static class ResearchManager_AddProgress_Patch
    {
        [HarmonyPrefix]
        public static bool Prefix(ResearchProjectDef proj, float amount)
        {
            if (amount <= 0f)
            {
                return true;
            }
            return ResearchMaterialManager.CanGainResearchProgress(proj);
        }
    }
}
