using System.Collections.Generic;
using System.Linq;
using Verse;

namespace Research_Requires_Resources
{
    public static class ResearchMaterialManager
    {
        private static ResearchMaterialGameComponent Component => ResearchMaterialGameComponent.Instance;

        public static bool HasMaterialCosts(ResearchProjectDef project)
        {
            return Component != null && Component.HasMaterialCosts(project);
        }

        public static ResearchProjectMaterialState GetState(ResearchProjectDef project)
        {
            return Component?.GetState(project);
        }

        public static void OnProjectSelected(ResearchProjectDef project)
        {
            Component?.OnProjectSelected(project);
        }

        public static void OnProjectDeselected(ResearchProjectDef project)
        {
            Component?.OnProjectDeselected(project);
        }

        public static bool CanResearchNow(ResearchProjectDef project)
        {
            return Component == null || Component.CanResearchNow(project);
        }

        public static bool CanCompleteNow(ResearchProjectDef project)
        {
            return Component == null || Component.CanCompleteNow(project);
        }

        public static bool AnyUnfundedActiveState()
        {
            return Component != null && Component.AnyUnfundedActiveState();
        }

        public static bool CanGainResearchProgress(ResearchProjectDef project)
        {
            return Component == null || Component.CanGainResearchProgress(project);
        }

        public static void FinalizeCompletion(ResearchProjectDef project)
        {
            Component?.FinalizeCompletion(project);
        }

        public static void Cancel(ResearchProjectDef project)
        {
            Component?.Cancel(project);
        }

        public static IEnumerable<DeliveryRequestOption> GetOpenDeliveryRequests()
        {
            return Component?.GetOpenDeliveryRequests() ?? Enumerable.Empty<DeliveryRequestOption>();
        }

        public static ResearchMaterialAllocation TryAllocateDelivery(Pawn pawn, ResearchProjectDef project, string requirementId, Thing source, int desiredCount)
        {
            return Component?.TryAllocateDelivery(pawn, project, requirementId, source, desiredCount);
        }

        public static void ReleaseAllocation(ResearchMaterialAllocation allocation)
        {
            Component?.ReleaseAllocation(allocation);
        }

        public static int CommitDelivery(ResearchMaterialAllocation allocation, Thing carried)
        {
            return Component?.CommitDelivery(allocation, carried) ?? 0;
        }

        public static int DeliveredCount(ResearchProjectDef project, ResearchMaterialRequirementSnapshot requirement)
        {
            return Component?.DeliveredCount(project, requirement) ?? 0;
        }

        public static int ReservedCount(ResearchProjectDef project, string requirementId)
        {
            return Component?.ReservedCount(project, requirementId) ?? 0;
        }

        public static string GetOverviewMaterialCostDisplay(ResearchProjectDef project)
        {
            ResearchMaterialCostsExtension extension = project?.GetModExtension<ResearchMaterialCostsExtension>();
            if (extension == null || !extension.HasRequirements)
            {
                return null;
            }

            ResearchProjectMaterialState state = GetState(project);
            ResearchMaterialBundleSnapshot snapshot = state?.snapshot ?? ResearchMaterialBundleSnapshot.FromExtension(extension, project);
            if (snapshot == null || snapshot.requirements.Count == 0)
            {
                return null;
            }

            return string.Join(", ", snapshot.requirements.Select(r => r.cachedLabel + " x" + r.requiredCount));
        }

        public static string GetOverviewFundingStatusLabel(ResearchProjectDef project)
        {
            ResearchMaterialCostsExtension extension = project?.GetModExtension<ResearchMaterialCostsExtension>();
            if (extension == null || !extension.HasRequirements)
            {
                return "RRR_StatusNotRequired".Translate();
            }

            ResearchProjectMaterialState state = GetState(project);
            if (state == null)
            {
                return "RRR_StatusNotStarted".Translate();
            }
            if (state.invalid)
            {
                return state.invalidReason.NullOrEmpty() ? (string)"RRR_StatusInvalidFallback".Translate() : state.invalidReason;
            }
            if (state.phase == MaterialLifecyclePhase.Canceled)
            {
                return "RRR_StatusCanceled".Translate();
            }
            if (state.fundingPhase == FundingPhase.Paid)
            {
                return "RRR_StatusPaid".Translate();
            }
            if (state.fundingPhase == FundingPhase.Funding || state.fundingPhase == FundingPhase.Unstarted)
            {
                return "RRR_StatusFunding".Translate();
            }
            return "RRR_StatusOverviewFallback".Translate();
        }
    }
}
