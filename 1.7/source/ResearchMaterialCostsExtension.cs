using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace Research_Requires_Resources
{
    public class ResearchMaterialRequirement
    {
        public string id;

        public int count;

        public ThingDef thingDef;

        public ThingFilter filter;

        public bool UsesFilter => filter != null;

        public ThingDef ResolvedSingleDef => thingDef;

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

        public string LabelCap
        {
            get
            {
                if (thingDef != null)
                {
                    return thingDef.LabelCap;
                }
                if (filter != null)
                {
                    return filter.Summary;
                }
                return id;
            }
        }

        public IEnumerable<string> ConfigErrors()
        {
            if (count <= 0)
            {
                yield return "ResearchMaterialRequirement '" + id + "' has nonpositive count";
            }
            bool hasDef = thingDef != null;
            bool hasFilter = filter != null;
            if (hasDef == hasFilter)
            {
                yield return "ResearchMaterialRequirement '" + id + "' must specify exactly one of thingDef or filter";
            }
            if (hasDef)
            {
                if (!thingDef.EverHaulable)
                {
                    yield return "ResearchMaterialRequirement '" + id + "' thingDef " + thingDef.defName + " is not haulable";
                }
                if (!thingDef.EverStorable(willMinifyIfPossible: false))
                {
                    yield return "ResearchMaterialRequirement '" + id + "' thingDef " + thingDef.defName + " is not storable";
                }
            }
            if (hasFilter)
            {
                if (filter.AllowedThingDefs.EnumerableNullOrEmpty())
                {
                    yield return "ResearchMaterialRequirement '" + id + "' filter is empty";
                }
                if (filter.AllowedHitPointsPercents != FloatRange.ZeroToOne)
                {
                    yield return "ResearchMaterialRequirement '" + id + "' filter restricts hit points, which is unsupported";
                }
                if (filter.AllowedQualityLevels != QualityRange.All)
                {
                    yield return "ResearchMaterialRequirement '" + id + "' filter restricts quality, which is unsupported";
                }
                foreach (ThingDef def in filter.AllowedThingDefs)
                {
                    if (!def.EverHaulable || !def.EverStorable(willMinifyIfPossible: false))
                    {
                        yield return "ResearchMaterialRequirement '" + id + "' filter allows non-haulable or non-storable def " + def.defName;
                    }
                }
            }
        }
    }

    public static class ResearchMaterialRequirementIdInitializer
    {
        public static void AssignIds()
        {
            foreach (ResearchProjectDef project in DefDatabase<ResearchProjectDef>.AllDefsListForReading)
            {
                ResearchMaterialCostsExtension extension = project.GetModExtension<ResearchMaterialCostsExtension>();
                if (extension?.requirements == null)
                {
                    continue;
                }
                for (int i = 0; i < extension.requirements.Count; i++)
                {
                    ResearchMaterialRequirement requirement = extension.requirements[i];
                    if (requirement == null)
                    {
                        continue;
                    }
                    requirement.id = project.defName + "_" + i;
                }
            }
        }
    }

    public class ResearchMaterialCostsExtension : DefModExtension
    {
        public List<ResearchMaterialRequirement> requirements = new List<ResearchMaterialRequirement>();

        public float consumedRefundPercent = 0.5f;

        public bool scaleConsumedRefundByRemainingProgress;

        public bool scaleByProjectBaseCost;

        public float scalePerBaseCost;

        public bool HasRequirements => !requirements.NullOrEmpty();

        private ResearchProjectDef parent;

        public override void ResolveReferences(Def parentDef)
        {
            base.ResolveReferences(parentDef);
            parent = parentDef as ResearchProjectDef;
            if (requirements == null)
            {
                return;
            }
            foreach (ResearchMaterialRequirement requirement in requirements)
            {
                requirement?.filter?.ResolveReferences();
            }
        }

        public override IEnumerable<string> ConfigErrors()
        {
            foreach (string error in base.ConfigErrors())
            {
                yield return error;
            }
            ResearchProjectDef project = parent;
            if (project == null)
            {
                yield return "ResearchMaterialCostsExtension can only be used on ResearchProjectDef";
                yield break;
            }
            if (project.modExtensions != null && project.modExtensions.Count((DefModExtension e) => e is ResearchMaterialCostsExtension) > 1)
            {
                yield return "ResearchProjectDef " + project.defName + " has more than one ResearchMaterialCostsExtension";
            }
            if (project.knowledgeCost > 0f)
            {
                yield return "ResearchMaterialCostsExtension cannot be used on Anomaly knowledge projects (knowledgeCost > 0)";
            }
            if (!HasRequirements)
            {
                yield return "ResearchMaterialCostsExtension defines no requirements";
                yield break;
            }

            foreach (ResearchMaterialRequirement requirement in requirements)
            {
                foreach (string error in requirement.ConfigErrors())
                {
                    yield return error;
                }
            }
            for (int i = 0; i < requirements.Count; i++)
            {
                for (int j = i + 1; j < requirements.Count; j++)
                {
                    if (RequirementsOverlap(requirements[i], requirements[j]))
                    {
                        yield return "requirements '" + requirements[i].id + "' and '" + requirements[j].id + "' overlap; a single item stack cannot satisfy two requirements in the same bundle";
                    }
                }
            }
            if (consumedRefundPercent < 0f || consumedRefundPercent > 1f)
            {
                yield return "consumedRefundPercent must be between 0 and 1";
            }
            if (scaleByProjectBaseCost && scalePerBaseCost < 0f)
            {
                yield return "scalePerBaseCost must not be negative";
            }
        }

        private static bool RequirementsOverlap(ResearchMaterialRequirement a, ResearchMaterialRequirement b)
        {
            if (a.thingDef != null && b.thingDef != null)
            {
                return a.thingDef == b.thingDef;
            }
            if (a.thingDef != null && b.filter != null)
            {
                return b.filter.Allows(a.thingDef);
            }
            if (a.filter != null && b.thingDef != null)
            {
                return a.filter.Allows(b.thingDef);
            }
            if (a.filter != null && b.filter != null)
            {
                return a.filter.AllowedThingDefs.Any(def => b.filter.Allows(def));
            }
            return false;
        }
    }
}
