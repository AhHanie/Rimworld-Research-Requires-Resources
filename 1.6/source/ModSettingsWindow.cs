using UnityEngine;
using Verse;

namespace Research_Requires_Resources
{
    public static class ModSettingsWindow
    {
        public static void Draw(Rect parent)
        {
            Listing_Standard listing = new Listing_Standard();
            listing.Begin(parent);
            listing.CheckboxLabeled("RRR_GrandfatherExistingPartialProgressLabel".Translate(), ref ModSettings.grandfatherExistingPartialProgress, "RRR_GrandfatherExistingPartialProgressDesc".Translate());
            listing.CheckboxLabeled("RRR_ShowResearchOverviewButtonLabel".Translate(), ref ModSettings.showResearchOverviewButton, "RRR_ShowResearchOverviewButtonDesc".Translate());
            listing.End();
        }
    }
}
