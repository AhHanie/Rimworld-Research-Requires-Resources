using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace Research_Requires_Resources
{
    public enum FundingPhase
    {
        NotRequired,
        Unstarted,
        Funding,
        Paid
    }

    public enum MaterialLifecyclePhase
    {
        Active,
        Paused,
        Canceling,
        Canceled,
        Completed
    }

    public class ResearchMaterialRequirementSnapshot : IExposable
    {
        public string id;

        public int requiredCount;

        public ThingDef thingDef;

        public ThingFilter filter;

        public string cachedLabel;

        public bool Allows(Thing thing)
        {
            if (thingDef != null)
            {
                return thing.def == thingDef;
            }
            return filter != null && filter.Allows(thing);
        }

        public bool AllowsDef(ThingDef def)
        {
            if (thingDef != null)
            {
                return def == thingDef;
            }
            return filter != null && filter.Allows(def);
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref id, "id");
            Scribe_Values.Look(ref requiredCount, "requiredCount", 0);
            Scribe_Defs.Look(ref thingDef, "thingDef");
            Scribe_Deep.Look(ref filter, "filter");
            Scribe_Values.Look(ref cachedLabel, "cachedLabel");
        }

        public static ResearchMaterialRequirementSnapshot FromRequirement(ResearchMaterialRequirement requirement, int scaledCount)
        {
            return new ResearchMaterialRequirementSnapshot
            {
                id = requirement.id,
                requiredCount = scaledCount,
                thingDef = requirement.thingDef,
                filter = requirement.filter,
                cachedLabel = requirement.LabelCap
            };
        }
    }

    public class ResearchMaterialBundleSnapshot : IExposable
    {
        public List<ResearchMaterialRequirementSnapshot> requirements = new List<ResearchMaterialRequirementSnapshot>();

        public float consumedRefundPercent;

        public ResearchMaterialRequirementSnapshot RequirementById(string id)
        {
            return requirements.FirstOrDefault(r => r.id == id);
        }

        public void ExposeData()
        {
            Scribe_Collections.Look(ref requirements, "requirements", LookMode.Deep);
            Scribe_Values.Look(ref consumedRefundPercent, "consumedRefundPercent", 0f);
            if (Scribe.mode == LoadSaveMode.PostLoadInit && requirements == null)
            {
                requirements = new List<ResearchMaterialRequirementSnapshot>();
            }
        }

        public static ResearchMaterialBundleSnapshot FromExtension(ResearchMaterialCostsExtension extension, ResearchProjectDef project)
        {
            ResearchMaterialBundleSnapshot snapshot = new ResearchMaterialBundleSnapshot
            {
                consumedRefundPercent = extension.consumedRefundPercent
            };
            foreach (ResearchMaterialRequirement requirement in extension.requirements)
            {
                int scaledCount = extension.scaleByProjectBaseCost
                    ? Mathf.CeilToInt(requirement.count * (1f + project.baseCost * extension.scalePerBaseCost))
                    : requirement.count;
                snapshot.requirements.Add(ResearchMaterialRequirementSnapshot.FromRequirement(requirement, scaledCount));
            }
            return snapshot;
        }
    }

    public class ResearchMaterialConsumedEntry : IExposable
    {
        public ThingDef def;

        public int count;

        public void ExposeData()
        {
            Scribe_Defs.Look(ref def, "def");
            Scribe_Values.Look(ref count, "count", 0);
        }
    }

    public class ResearchMaterialAllocation : IExposable
    {
        public int id;

        public ResearchProjectDef project;

        public string requirementId;

        public int count;

        public Pawn pawn;

        public Thing sourceThing;

        public int issuedTick;

        public void ExposeData()
        {
            Scribe_Values.Look(ref id, "id", 0);
            Scribe_Defs.Look(ref project, "project");
            Scribe_Values.Look(ref requirementId, "requirementId");
            Scribe_Values.Look(ref count, "count", 0);
            Scribe_References.Look(ref pawn, "pawn");
            Scribe_References.Look(ref sourceThing, "sourceThing");
            Scribe_Values.Look(ref issuedTick, "issuedTick", 0);
        }
    }

    public class ResearchMaterialPendingRefund : IExposable
    {
        public string transactionId;

        public ThingDef def;

        public int count;

        public void ExposeData()
        {
            Scribe_Values.Look(ref transactionId, "transactionId");
            Scribe_Defs.Look(ref def, "def");
            Scribe_Values.Look(ref count, "count", 0);
        }
    }

    public class ResearchProjectMaterialState : IExposable
    {
        public ResearchProjectDef project;

        public MaterialLifecyclePhase phase = MaterialLifecyclePhase.Active;

        public FundingPhase fundingPhase = FundingPhase.Unstarted;

        public ResearchMaterialBundleSnapshot snapshot;

        public List<ResearchMaterialConsumedEntry> consumed = new List<ResearchMaterialConsumedEntry>();

        public string cancellationId;

        public string definitionFingerprint;

        public bool invalid;

        public string invalidReason;

        public bool grandfathered;

        public bool completionFinalized;

        public void AddConsumed(ThingDef def, int count)
        {
            AddToLedger(consumed, def, count);
        }

        private static void AddToLedger(List<ResearchMaterialConsumedEntry> list, ThingDef def, int count)
        {
            ResearchMaterialConsumedEntry entry = list.FirstOrDefault(e => e.def == def);
            if (entry == null)
            {
                entry = new ResearchMaterialConsumedEntry { def = def, count = 0 };
                list.Add(entry);
            }
            entry.count += count;
        }

        public static int SumForRequirement(List<ResearchMaterialConsumedEntry> entries, ResearchMaterialRequirementSnapshot requirement)
        {
            return entries.Where(e => requirement.AllowsDef(e.def)).Sum(e => e.count);
        }

        public void ExposeData()
        {
            Scribe_Defs.Look(ref project, "project");
            Scribe_Values.Look(ref phase, "phase", MaterialLifecyclePhase.Active);
            Scribe_Values.Look(ref fundingPhase, "fundingPhase", FundingPhase.Unstarted);
            Scribe_Deep.Look(ref snapshot, "snapshot");
            Scribe_Collections.Look(ref consumed, "consumed", LookMode.Deep);
            Scribe_Values.Look(ref cancellationId, "cancellationId");
            Scribe_Values.Look(ref definitionFingerprint, "definitionFingerprint");
            Scribe_Values.Look(ref invalid, "invalid", false);
            Scribe_Values.Look(ref invalidReason, "invalidReason");
            Scribe_Values.Look(ref grandfathered, "grandfathered", false);
            Scribe_Values.Look(ref completionFinalized, "completionFinalized", false);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (consumed == null)
                {
                    consumed = new List<ResearchMaterialConsumedEntry>();
                }
            }
        }
    }
}
