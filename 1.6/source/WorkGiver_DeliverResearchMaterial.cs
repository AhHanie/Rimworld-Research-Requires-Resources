using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace Research_Requires_Resources
{
    public class Job_DeliverResearchMaterial : Job
    {
        public ResearchProjectDef materialProject;

        public string materialRequirementId;

        public int materialAllocationId;

        public Job_DeliverResearchMaterial()
        {
        }

        public Job_DeliverResearchMaterial(JobDef def, LocalTargetInfo targetA, LocalTargetInfo targetB)
            : base(def, targetA, targetB)
        {
        }
    }

    public class WorkGiver_DeliverResearchMaterial : WorkGiver_Scanner
    {
        public override ThingRequest PotentialWorkThingRequest => ThingRequest.ForGroup(ThingRequestGroup.HaulableEver);

        public override PathEndMode PathEndMode => PathEndMode.ClosestTouch;

        public override bool ShouldSkip(Pawn pawn, bool forced = false)
        {
            if (Find.ResearchManager.GetProject() == null)
            {
                return true;
            }
            return !ResearchMaterialManager.GetOpenDeliveryRequests().Any();
        }

        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            if (t.IsForbidden(pawn) || !HaulAIUtility.PawnCanAutomaticallyHaulFast(pawn, t, forced))
            {
                return false;
            }
            foreach (DeliveryRequestOption option in ResearchMaterialManager.GetOpenDeliveryRequests())
            {
                if (option.requirement.Allows(t) && ResearchBenchUtility.FindNearestReachableBench(pawn) != null)
                {
                    return true;
                }
            }
            return false;
        }

        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            if (t.IsForbidden(pawn) || !HaulAIUtility.PawnCanAutomaticallyHaulFast(pawn, t, forced))
            {
                return null;
            }
            foreach (DeliveryRequestOption option in ResearchMaterialManager.GetOpenDeliveryRequests())
            {
                if (!option.requirement.Allows(t))
                {
                    continue;
                }
                int carryCapacity = pawn.carryTracker.MaxStackSpaceEver(t.def);
                int desired = Mathf.Min(option.remaining, carryCapacity);
                if (desired <= 0)
                {
                    continue;
                }
                ResearchMaterialAllocation allocation = ResearchMaterialManager.TryAllocateDelivery(pawn, option.project, option.requirement.id, t, desired);
                if (allocation == null)
                {
                    continue;
                }
                if (!pawn.CanReserve(t, 1, allocation.count, null, forced))
                {
                    ResearchMaterialManager.ReleaseAllocation(allocation);
                    continue;
                }
                Thing bench = ResearchBenchUtility.FindNearestReachableBench(pawn);
                if (bench == null)
                {
                    ResearchMaterialManager.ReleaseAllocation(allocation);
                    continue;
                }
                return new Job_DeliverResearchMaterial(ResearchMaterialJobDefOf.RRR_DeliverResearchMaterial, t, bench)
                {
                    count = allocation.count,
                    materialProject = option.project,
                    materialRequirementId = option.requirement.id,
                    materialAllocationId = allocation.id
                };
            }
            return null;
        }
    }
}
