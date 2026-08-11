using HarmonyLib;
using RimWorld;
using Verse;

namespace Research_Requires_Resources.Patches
{
    [HarmonyPatch(typeof(ResearchManager), nameof(ResearchManager.DebugSetAllProjectsFinished))]
    public static class ResearchManager_DebugSetAllProjectsFinished_Patch
    {
        [HarmonyPrefix]
        public static bool Prefix()
        {
            if (ResearchMaterialManager.AnyUnfundedActiveState())
            {
                Messages.Message("RRR_DebugFinishAllBlocked".Translate(), MessageTypeDefOf.RejectInput, historical: false);
                return false;
            }
            return true;
        }
    }
}
