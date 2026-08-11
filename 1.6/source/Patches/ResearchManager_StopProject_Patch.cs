using HarmonyLib;
using RimWorld;
using Verse;

namespace Research_Requires_Resources.Patches
{
    [HarmonyPatch(typeof(ResearchManager), nameof(ResearchManager.StopProject))]
    public static class ResearchManager_StopProject_Patch
    {
        [HarmonyPostfix]
        public static void Postfix(ResearchProjectDef proj)
        {
            if (proj != null && proj.baseCost > 0f)
            {
                ResearchMaterialManager.OnProjectDeselected(proj);
            }
        }
    }
}
