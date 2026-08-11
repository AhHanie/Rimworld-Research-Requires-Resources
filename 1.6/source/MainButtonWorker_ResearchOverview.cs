using RimWorld;
using Verse;

namespace Research_Requires_Resources
{
    public class MainButtonWorker_ResearchOverview : MainButtonWorker
    {
        public override bool Visible => base.Visible && ModSettings.showResearchOverviewButton;

        public override void Activate()
        {
            Dialog_ResearchOverview existing = Find.WindowStack.WindowOfType<Dialog_ResearchOverview>();
            if (existing != null)
            {
                existing.Close();
                return;
            }

            Find.WindowStack.Add(new Dialog_ResearchOverview());
        }
    }
}
