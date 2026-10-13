using HarmonyLib;
using RimWorld;
using Verse;

namespace Research_Requires_Resources.Patches
{
    [HarmonyPatch(typeof(ResearchManager), nameof(ResearchManager.FinishProject))]
    public static class ResearchManager_FinishProject_Patch
    {
        [HarmonyPrefix]
        public static bool Prefix(ResearchProjectDef proj)
        {
            if (proj == null)
            {
                return true;
            }
            if (Find.GameInitData != null && Find.GameInitData.gameToLoad.NullOrEmpty())
            {
                // New-game bootstrap (scenario parts, faction/ideology starting research) must
                // finish for free; consulting the material manager here would create an unfunded
                // state for every such project instead of letting vanilla grant it.
                return true;
            }
            if (!ResearchMaterialManager.CanCompleteNow(proj))
            {
                Messages.Message("RRR_CannotFinishUnfunded".Translate(proj.LabelCap), MessageTypeDefOf.RejectInput, historical: false);
                return false;
            }
            return true;
        }

        [HarmonyPostfix]
        public static void Postfix(ResearchProjectDef proj)
        {
            if (proj != null)
            {
                ResearchMaterialManager.FinalizeCompletion(proj);
            }
        }
    }
}
