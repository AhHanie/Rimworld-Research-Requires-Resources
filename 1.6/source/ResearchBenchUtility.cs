using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;

namespace Research_Requires_Resources
{
    public static class ResearchBenchUtility
    {
        public static Thing FindNearestReachableBench(Pawn pawn)
        {
            return GenClosest.ClosestThingReachable(pawn.Position, pawn.Map, ThingRequest.ForGroup(ThingRequestGroup.ResearchBench), PathEndMode.Touch, TraverseParms.For(pawn), 9999f, delegate (Thing candidate)
            {
                if (candidate.IsForbidden(pawn))
                {
                    return false;
                }
                return pawn.CanReach(candidate, PathEndMode.Touch, Danger.Some);
            });
        }

        public static bool TryFindRefundDropCell(out IntVec3 cell, out Map map)
        {
            Thing bench = FindAnyBenchOnMap(Find.CurrentMap);
            if (bench == null)
            {
                foreach (Map candidateMap in Find.Maps.Where(m => m.IsPlayerHome))
                {
                    bench = FindAnyBenchOnMap(candidateMap);
                    if (bench != null)
                    {
                        break;
                    }
                }
            }
            if (bench == null)
            {
                cell = IntVec3.Invalid;
                map = null;
                return false;
            }
            cell = bench.Position;
            map = bench.Map;
            return true;
        }

        private static Thing FindAnyBenchOnMap(Map map)
        {
            return map?.listerThings.ThingsMatching(ThingRequest.ForGroup(ThingRequestGroup.ResearchBench)).FirstOrDefault();
        }
    }
}
