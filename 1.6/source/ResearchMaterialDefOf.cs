using RimWorld;
using Verse;

namespace Research_Requires_Resources
{
    [DefOf]
    public static class ResearchMaterialJobDefOf
    {
        public static JobDef RRR_DeliverResearchMaterial;

        static ResearchMaterialJobDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(ResearchMaterialJobDefOf));
        }
    }
}
