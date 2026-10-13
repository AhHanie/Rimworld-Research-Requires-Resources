using HarmonyLib;
using RimWorld;
using Verse;

namespace Research_Requires_Resources.Patches
{
    [HarmonyPatch(typeof(WorkGiver_Researcher), nameof(WorkGiver_Researcher.ShouldSkip))]
    public static class WorkGiver_Researcher_ShouldSkip_Patch
    {
        // Vanilla skips the work giver when GetProject() is null; hiding an unfunded project at
        // that single call reuses the check instead of re-deriving it in a whole-method postfix.
        [HarmonyInfix(typeof(ResearchManager), nameof(ResearchManager.GetProject))]
        public static ResearchProjectDef Postfix(ResearchProjectDef __result)
        {
            return __result != null && !ResearchMaterialManager.CanResearchNow(__result) ? null : __result;
        }
    }
}
