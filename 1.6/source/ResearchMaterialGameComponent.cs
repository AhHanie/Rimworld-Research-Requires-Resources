using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace Research_Requires_Resources
{
    public class DeliveryRequestOption
    {
        public ResearchProjectDef project;

        public ResearchMaterialRequirementSnapshot requirement;

        public int remaining;
    }

    public class ResearchMaterialGameComponent : GameComponent
    {
        private List<ResearchProjectMaterialState> states = new List<ResearchProjectMaterialState>();

        private List<ResearchMaterialAllocation> allocations = new List<ResearchMaterialAllocation>();

        private List<ResearchMaterialPendingRefund> pendingRefunds = new List<ResearchMaterialPendingRefund>();

        private int nextAllocationId = 1;

        private readonly Dictionary<ResearchProjectDef, ResearchProjectMaterialState> statesByProject = new Dictionary<ResearchProjectDef, ResearchProjectMaterialState>();

        public ResearchMaterialGameComponent(Game game)
        {
        }

        public static ResearchMaterialGameComponent Instance => Current.Game?.GetComponent<ResearchMaterialGameComponent>();

        public IReadOnlyList<ResearchProjectMaterialState> States => states;

        public IReadOnlyList<ResearchMaterialAllocation> Allocations => allocations;

        public ResearchProjectMaterialState GetState(ResearchProjectDef project)
        {
            if (project == null)
            {
                return null;
            }
            statesByProject.TryGetValue(project, out ResearchProjectMaterialState state);
            return state;
        }

        public bool HasMaterialCosts(ResearchProjectDef project)
        {
            ResearchMaterialCostsExtension extension = project?.GetModExtension<ResearchMaterialCostsExtension>();
            return extension != null && extension.HasRequirements;
        }

        public ResearchProjectMaterialState EnsureState(ResearchProjectDef project)
        {
            if (project == null)
            {
                return null;
            }
            ResearchProjectMaterialState existing = GetState(project);
            if (existing != null)
            {
                return existing;
            }
            ResearchMaterialCostsExtension extension = project.GetModExtension<ResearchMaterialCostsExtension>();
            if (extension == null || !extension.HasRequirements)
            {
                return null;
            }

            bool hasExistingProgress = Find.ResearchManager.GetProgress(project) > 0f;
            bool grandfather = hasExistingProgress && ModSettings.grandfatherExistingPartialProgress;

            ResearchProjectMaterialState state = new ResearchProjectMaterialState
            {
                project = project,
                phase = MaterialLifecyclePhase.Active,
                grandfathered = grandfather
            };

            state.snapshot = ResearchMaterialBundleSnapshot.FromExtension(extension, project);
            state.fundingPhase = grandfather ? FundingPhase.Paid : FundingPhase.Funding;

            states.Add(state);
            statesByProject[project] = state;
            return state;
        }

        public void OnProjectSelected(ResearchProjectDef project)
        {
            ResearchProjectMaterialState state = EnsureState(project);
            if (state != null && state.phase == MaterialLifecyclePhase.Paused)
            {
                state.phase = MaterialLifecyclePhase.Active;
            }
        }

        public void OnProjectDeselected(ResearchProjectDef project)
        {
            ResearchProjectMaterialState state = GetState(project);
            if (state != null && state.phase == MaterialLifecyclePhase.Active)
            {
                state.phase = MaterialLifecyclePhase.Paused;
            }
        }

        public bool CanResearchNow(ResearchProjectDef project)
        {
            ResearchProjectMaterialState state = GetState(project);
            if (state == null)
            {
                return true;
            }
            if (state.invalid)
            {
                return false;
            }
            if (state.phase != MaterialLifecyclePhase.Active)
            {
                return false;
            }
            if (state.fundingPhase != FundingPhase.Paid)
            {
                return false;
            }
            return true;
        }

        public bool CanCompleteNow(ResearchProjectDef project)
        {
            ResearchProjectMaterialState state = EnsureState(project);
            if (state == null)
            {
                return true;
            }
            if (state.phase == MaterialLifecyclePhase.Canceled || state.phase == MaterialLifecyclePhase.Completed)
            {
                return true;
            }
            if (state.fundingPhase != FundingPhase.Paid)
            {
                return false;
            }
            return true;
        }

        public bool AnyUnfundedActiveState()
        {
            return states.Any(s => (s.phase == MaterialLifecyclePhase.Active || s.phase == MaterialLifecyclePhase.Paused) && s.fundingPhase != FundingPhase.Paid);
        }

        public bool CanGainResearchProgress(ResearchProjectDef project)
        {
            ResearchProjectMaterialState state = EnsureState(project);
            if (state == null)
            {
                return true;
            }
            if (state.invalid)
            {
                return false;
            }
            if (state.phase != MaterialLifecyclePhase.Active)
            {
                return false;
            }
            return state.fundingPhase == FundingPhase.Paid;
        }

        public IEnumerable<DeliveryRequestOption> GetOpenDeliveryRequests()
        {
            ResearchProjectDef project = Find.ResearchManager.GetProject();
            if (project == null)
            {
                yield break;
            }
            ResearchProjectMaterialState state = GetState(project);
            if (state == null || state.phase != MaterialLifecyclePhase.Active)
            {
                yield break;
            }
            if (state.fundingPhase != FundingPhase.Funding)
            {
                yield break;
            }

            foreach (ResearchMaterialRequirementSnapshot requirement in state.snapshot.requirements)
            {
                int delivered = ResearchProjectMaterialState.SumForRequirement(state.consumed, requirement);
                int reserved = SumReservedGlobal(project, requirement.id);
                int remaining = requirement.requiredCount - delivered - reserved;
                if (remaining > 0)
                {
                    yield return new DeliveryRequestOption { project = project, requirement = requirement, remaining = remaining };
                }
            }
        }

        public ResearchMaterialAllocation TryAllocateDelivery(Pawn pawn, ResearchProjectDef project, string requirementId, Thing source, int desiredCount)
        {
            ResearchProjectMaterialState state = GetState(project);
            if (state == null || state.phase != MaterialLifecyclePhase.Active)
            {
                return null;
            }
            if (state.fundingPhase != FundingPhase.Funding)
            {
                return null;
            }
            ResearchMaterialRequirementSnapshot requirement = state.snapshot?.RequirementById(requirementId);
            if (requirement == null || !requirement.Allows(source))
            {
                return null;
            }

            int delivered = ResearchProjectMaterialState.SumForRequirement(state.consumed, requirement);
            int reserved = SumReservedGlobal(project, requirementId);
            int remainder = requirement.requiredCount - delivered - reserved;
            if (remainder <= 0)
            {
                return null;
            }

            int count = Mathf.Min(Mathf.Min(desiredCount, remainder), source.stackCount);
            if (count <= 0)
            {
                return null;
            }

            ResearchMaterialAllocation allocation = new ResearchMaterialAllocation
            {
                id = nextAllocationId++,
                project = project,
                requirementId = requirementId,
                count = count,
                pawn = pawn,
                sourceThing = source,
                issuedTick = Find.TickManager.TicksGame
            };
            allocations.Add(allocation);
            return allocation;
        }

        public void ReleaseAllocation(ResearchMaterialAllocation allocation)
        {
            if (allocation == null)
            {
                return;
            }
            allocations.Remove(allocation);
        }

        public int CommitDelivery(ResearchMaterialAllocation allocation, Thing carried)
        {
            if (allocation == null || !allocations.Contains(allocation))
            {
                return 0;
            }
            ResearchProjectMaterialState state = GetState(allocation.project);
            if (state == null || state.phase != MaterialLifecyclePhase.Active)
            {
                ReleaseAllocation(allocation);
                return 0;
            }
            if (state.fundingPhase != FundingPhase.Funding)
            {
                ReleaseAllocation(allocation);
                return 0;
            }
            ResearchMaterialRequirementSnapshot requirement = state.snapshot?.RequirementById(allocation.requirementId);
            if (requirement == null || !requirement.Allows(carried))
            {
                ReleaseAllocation(allocation);
                return 0;
            }

            int accept = Mathf.Min(allocation.count, carried.stackCount);
            ReleaseAllocation(allocation);
            if (accept <= 0)
            {
                return 0;
            }

            ThingDef def = carried.def;
            carried.SplitOff(accept).Destroy();

            state.AddConsumed(def, accept);
            RecalculateCompletion(state);
            return accept;
        }

        private void RecalculateCompletion(ResearchProjectMaterialState state)
        {
            if (state.fundingPhase != FundingPhase.Funding)
            {
                return;
            }
            foreach (ResearchMaterialRequirementSnapshot requirement in state.snapshot.requirements)
            {
                if (ResearchProjectMaterialState.SumForRequirement(state.consumed, requirement) < requirement.requiredCount)
                {
                    return;
                }
            }
            state.fundingPhase = FundingPhase.Paid;
            CancelAllocationsFor(state.project);
        }

        private void CancelAllocationsFor(ResearchProjectDef project)
        {
            allocations.RemoveAll(a => a.project == project);
        }

        public void Cancel(ResearchProjectDef project)
        {
            ResearchProjectMaterialState state = GetState(project);
            if (state == null || state.phase == MaterialLifecyclePhase.Canceling || state.phase == MaterialLifecyclePhase.Canceled)
            {
                return;
            }

            state.phase = MaterialLifecyclePhase.Canceling;
            CancelAllocationsFor(project);

            string transactionId = project.defName + "_" + Find.TickManager.TicksGame + "_" + Rand.Int;

            ResearchMaterialCostsExtension extension = project.GetModExtension<ResearchMaterialCostsExtension>();
            float consumedPercent = state.snapshot != null ? state.snapshot.consumedRefundPercent : (extension?.consumedRefundPercent ?? 0.5f);
            float scale = 1f;
            if (extension != null && extension.scaleConsumedRefundByRemainingProgress)
            {
                scale = 1f - Mathf.Clamp01(Find.ResearchManager.GetProgress(project) / project.Cost);
            }

            float refundPercent = state.fundingPhase == FundingPhase.Paid ? consumedPercent * scale : 1f;
            IssueConsumedRefunds(state.consumed, refundPercent, transactionId);

            state.cancellationId = transactionId;
            state.phase = MaterialLifecyclePhase.Canceled;
        }

        private void IssueConsumedRefunds(List<ResearchMaterialConsumedEntry> consumed, float percent, string transactionId)
        {
            if (percent <= 0f)
            {
                return;
            }
            foreach (ResearchMaterialConsumedEntry entry in consumed)
            {
                int count = Mathf.FloorToInt(entry.count * percent);
                if (count > 0)
                {
                    IssueRefund(entry.def, count, transactionId);
                }
            }
        }

        private void IssueRefund(ThingDef def, int count, string transactionId)
        {
            if (TryDropRefund(def, count))
            {
                return;
            }
            pendingRefunds.Add(new ResearchMaterialPendingRefund
            {
                transactionId = transactionId,
                def = def,
                count = count
            });
        }

        private bool TryDropRefund(ThingDef def, int count)
        {
            if (!ResearchBenchUtility.TryFindRefundDropCell(out IntVec3 cell, out Map map))
            {
                return false;
            }
            ThingDef stuff = def.MadeFromStuff ? GenStuff.DefaultStuffFor(def) : null;
            Thing refund = ThingMaker.MakeThing(def, stuff);
            refund.stackCount = count;
            return GenDrop.TryDropSpawn(refund, cell, map, ThingPlaceMode.Near, out _);
        }

        private void RetryPendingRefunds()
        {
            for (int i = pendingRefunds.Count - 1; i >= 0; i--)
            {
                ResearchMaterialPendingRefund pending = pendingRefunds[i];
                if (TryDropRefund(pending.def, pending.count))
                {
                    pendingRefunds.RemoveAt(i);
                }
            }
        }

        public void FinalizeCompletion(ResearchProjectDef project)
        {
            ResearchProjectMaterialState state = GetState(project);
            if (state == null || state.completionFinalized)
            {
                return;
            }

            CancelAllocationsFor(project);

            state.phase = MaterialLifecyclePhase.Completed;
            state.completionFinalized = true;
        }

        public int DeliveredCount(ResearchProjectDef project, ResearchMaterialRequirementSnapshot requirement)
        {
            ResearchProjectMaterialState state = GetState(project);
            if (state == null)
            {
                return 0;
            }
            return ResearchProjectMaterialState.SumForRequirement(state.consumed, requirement);
        }

        public int ReservedCount(ResearchProjectDef project, string requirementId)
        {
            return SumReservedGlobal(project, requirementId);
        }

        private int SumReservedGlobal(ResearchProjectDef project, string requirementId)
        {
            int total = 0;
            foreach (ResearchMaterialAllocation a in allocations)
            {
                if (a.project == project && a.requirementId == requirementId)
                {
                    total += a.count;
                }
            }
            return total;
        }

        public override void GameComponentTick()
        {
            int tick = Find.TickManager.TicksGame;
            if (tick % 250 == 0)
            {
                RetryPendingRefunds();
            }
            if (tick % 2000 == 0)
            {
                CleanupStaleAllocations();
            }
        }

        private void CleanupStaleAllocations()
        {
            allocations.RemoveAll(a =>
                a.pawn == null || a.pawn.Destroyed ||
                a.sourceThing == null || a.sourceThing.Destroyed ||
                GetState(a.project) == null || GetState(a.project).phase != MaterialLifecyclePhase.Active);
        }

        public override void FinalizeInit()
        {
            RebuildIndex();
        }

        public override void LoadedGame()
        {
            RebuildIndex();
            ValidateStatesAfterLoad();
            ReconcileAllocations();
        }

        private void RebuildIndex()
        {
            statesByProject.Clear();
            foreach (ResearchProjectMaterialState state in states)
            {
                if (state.project != null)
                {
                    statesByProject[state.project] = state;
                }
            }
        }

        private void ValidateStatesAfterLoad()
        {
            foreach (ResearchProjectMaterialState state in states)
            {
                if (state.invalid)
                {
                    continue;
                }
                bool broken = state.snapshot != null && state.snapshot.requirements.Any(r => r.thingDef == null && r.filter == null);
                if (broken)
                {
                    state.invalid = true;
                    state.invalidReason = "RRR_InvalidMissingRequirement".Translate();
                }
            }
        }

        private void ReconcileAllocations()
        {
            HashSet<int> liveIds = new HashSet<int>();
            foreach (Map map in Find.Maps)
            {
                foreach (Pawn pawn in map.mapPawns.AllPawnsSpawned)
                {
                    if (pawn.jobs?.curDriver is JobDriver_DeliverResearchMaterial driver && pawn.CurJob != null)
                    {
                        liveIds.Add(driver.AllocationId);
                    }
                }
            }
            allocations.RemoveAll(a => !liveIds.Contains(a.id));
        }

        public override void ExposeData()
        {
            Scribe_Collections.Look(ref states, "states", LookMode.Deep);
            Scribe_Collections.Look(ref allocations, "allocations", LookMode.Deep);
            Scribe_Collections.Look(ref pendingRefunds, "pendingRefunds", LookMode.Deep);
            Scribe_Values.Look(ref nextAllocationId, "nextAllocationId", 1);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (states == null)
                {
                    states = new List<ResearchProjectMaterialState>();
                }
                if (allocations == null)
                {
                    allocations = new List<ResearchMaterialAllocation>();
                }
                if (pendingRefunds == null)
                {
                    pendingRefunds = new List<ResearchMaterialPendingRefund>();
                }
                states.RemoveAll(s => s.project == null);
            }
        }
    }
}
