using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace Research_Requires_Resources
{
    public class JobDriver_DeliverResearchMaterial : JobDriver
    {
        private ResearchProjectDef materialProject;

        private string materialRequirementId;

        private int allocationId;

        public int AllocationId => allocationId;

        private void CaptureJobData()
        {
            if (materialProject != null)
            {
                return;
            }
            if (job is Job_DeliverResearchMaterial customJob)
            {
                materialProject = customJob.materialProject;
                materialRequirementId = customJob.materialRequirementId;
                allocationId = customJob.materialAllocationId;
            }
        }

        private ResearchMaterialAllocation FindAllocation()
        {
            ResearchMaterialGameComponent component = ResearchMaterialGameComponent.Instance;
            if (component == null)
            {
                return null;
            }
            foreach (ResearchMaterialAllocation allocation in component.Allocations)
            {
                if (allocation.id == allocationId)
                {
                    return allocation;
                }
            }
            return null;
        }

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            CaptureJobData();
            if (!pawn.Reserve(job.GetTarget(TargetIndex.A), job, 1, job.count, null, errorOnFailed))
            {
                ReleaseAllocationToken();
                return false;
            }
            return true;
        }

        private void ReleaseAllocationToken()
        {
            ResearchMaterialAllocation allocation = FindAllocation();
            if (allocation != null)
            {
                ResearchMaterialManager.ReleaseAllocation(allocation);
            }
        }

        public override string GetReport()
        {
            Thing bench = job.GetTarget(TargetIndex.B).Thing;
            if (bench == null || materialProject == null)
            {
                return base.GetReport();
            }
            return "RRR_DeliveringMaterialReport".Translate(materialProject.LabelCap, bench.LabelShort);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            CaptureJobData();

            this.FailOnDestroyedOrNull(TargetIndex.A);
            this.FailOnDestroyedNullOrForbidden(TargetIndex.B);
            this.FailOn(delegate
            {
                if (materialProject == null)
                {
                    return true;
                }
                ResearchProjectMaterialState state = ResearchMaterialManager.GetState(materialProject);
                return state == null || state.phase != MaterialLifecyclePhase.Active;
            });

            this.AddFinishAction(delegate
            {
                ReleaseAllocationToken();
            });

            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.ClosestTouch);
            yield return Toils_Haul.StartCarryThing(TargetIndex.A, putRemainderInQueue: false, subtractNumTakenFromJobCount: true, failIfStackCountLessThanJobCount: false, reserve: true, canTakeFromInventory: true);
            yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.Touch);

            Toil deliver = new Toil();
            deliver.initAction = delegate
            {
                Pawn actor = deliver.actor;
                ResearchMaterialAllocation allocation = FindAllocation();
                Thing carried = actor.carryTracker.CarriedThing;
                if (allocation != null && carried != null)
                {
                    ResearchMaterialManager.CommitDelivery(allocation, carried);
                }
                if (actor.carryTracker.CarriedThing != null)
                {
                    actor.carryTracker.TryDropCarriedThing(actor.Position, ThingPlaceMode.Near, out Thing _);
                }
            };
            deliver.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return deliver;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Defs.Look(ref materialProject, "materialProject");
            Scribe_Values.Look(ref materialRequirementId, "materialRequirementId");
            Scribe_Values.Look(ref allocationId, "allocationId", 0);
        }
    }
}
