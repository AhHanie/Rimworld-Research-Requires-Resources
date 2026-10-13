using Verse;

namespace Research_Requires_Resources
{
    public class ModSettings : Verse.ModSettings
    {
        public static bool grandfatherExistingPartialProgress = true;
        public static bool showResearchOverviewButton = true;

        public override void ExposeData()
        {
            Scribe_Values.Look(ref grandfatherExistingPartialProgress, "grandfatherExistingPartialProgress", true);
            Scribe_Values.Look(ref showResearchOverviewButton, "showResearchOverviewButton", true);
        }
    }
}
