using HarmonyLib;
using RimWorld;
using Verse;

namespace Research_Requires_Resources.Patches
{
    [HarmonyPatch(typeof(WorkGiver_Researcher), nameof(WorkGiver_Researcher.HasJobOnThing))]
    public static class WorkGiver_Researcher_HasJobOnThing_Patch
    {
        [HarmonyPostfix]
        public static void Postfix(ref bool __result)
        {
            if (!__result)
            {
                return;
            }
            ResearchProjectDef project = Find.ResearchManager.GetProject();
            if (project != null && !ResearchMaterialManager.CanResearchNow(project))
            {
                __result = false;
            }
        }
    }
}
