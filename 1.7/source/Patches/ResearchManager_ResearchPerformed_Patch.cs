using HarmonyLib;
using RimWorld;
using Verse;

namespace Research_Requires_Resources.Patches
{
    [HarmonyPatch(typeof(ResearchManager), nameof(ResearchManager.ResearchPerformed))]
    public static class ResearchManager_ResearchPerformed_Patch
    {
        [HarmonyPrefix]
        public static bool Prefix()
        {
            ResearchProjectDef project = Find.ResearchManager.GetProject();
            return ResearchMaterialManager.CanGainResearchProgress(project);
        }
    }
}
