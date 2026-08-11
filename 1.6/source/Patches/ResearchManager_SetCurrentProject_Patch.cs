using HarmonyLib;
using RimWorld;
using Verse;

namespace Research_Requires_Resources.Patches
{
    [HarmonyPatch(typeof(ResearchManager), nameof(ResearchManager.SetCurrentProject))]
    public static class ResearchManager_SetCurrentProject_Patch
    {
        [HarmonyPrefix]
        public static void Prefix(ResearchProjectDef proj, out ResearchProjectDef __state)
        {
            __state = proj != null && proj.baseCost > 0f ? Find.ResearchManager.GetProject() : null;
        }

        [HarmonyPostfix]
        public static void Postfix(ResearchProjectDef proj, ResearchProjectDef __state)
        {
            if (proj == null || proj.baseCost <= 0f)
            {
                return;
            }
            if (__state != null && __state != proj)
            {
                ResearchMaterialManager.OnProjectDeselected(__state);
            }
            ResearchMaterialManager.OnProjectSelected(proj);
        }
    }
}
