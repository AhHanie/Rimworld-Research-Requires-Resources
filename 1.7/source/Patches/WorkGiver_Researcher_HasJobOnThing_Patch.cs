using HarmonyLib;
using RimWorld;
using Verse;

namespace Research_Requires_Resources.Patches
{
    [HarmonyPatch(typeof(WorkGiver_Researcher), nameof(WorkGiver_Researcher.HasJobOnThing))]
    public static class WorkGiver_Researcher_HasJobOnThing_Patch
    {
        // Vanilla returns false when GetProject() is null, before any bench, reservation or
        // history-event checks run.
        [HarmonyInfix(typeof(ResearchManager), nameof(ResearchManager.GetProject))]
        public static ResearchProjectDef Postfix(ResearchProjectDef __result)
        {
            return __result != null && !ResearchMaterialManager.CanResearchNow(__result) ? null : __result;
        }
    }
}
