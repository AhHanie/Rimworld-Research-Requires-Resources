using RimWorld;
using UnityEngine;
using Verse;

namespace Research_Requires_Resources
{
    public static class ResearchMaterialUI
    {
        private const float RowHeight = 22f;

        private const float SectionGap = 6f;

        public static float DrawProjectCosts(Rect rect, ResearchProjectDef project)
        {
            ResearchMaterialCostsExtension extension = project.GetModExtension<ResearchMaterialCostsExtension>();
            if (extension == null || !extension.HasRequirements)
            {
                return 0f;
            }

            ResearchProjectMaterialState state = ResearchMaterialManager.GetState(project);
            float x = rect.x;
            float width = rect.width;
            float y = rect.y;
            GameFont prevFont = Text.Font;
            Text.Font = GameFont.Small;

            y += DrawLabel(x, y, width, "RRR_MaterialCostsHeading".Translate());

            y += DrawLabel(x, y, width, "RRR_FundingHeading".Translate() + ": " + FundingStatusLabel(state));
            if (state != null && state.snapshot != null)
            {
                y += DrawRequirementRows(x, y, width, state.snapshot, project);
            }
            else
            {
                y += DrawLiveRequirementPreview(x, y, width, extension);
            }
            y += SectionGap;

            y += DrawRefundSummary(x, y, width, extension);

            string blockReason = GetBlockReason(project, state);
            Color prevColor = GUI.color;
            GUI.color = blockReason.NullOrEmpty() ? new Color(0.4f, 1f, 0.4f) : new Color(1f, 0.75f, 0.3f);
            Widgets.Label(new Rect(x, y, width, RowHeight), blockReason.NullOrEmpty() ? (string)"RRR_StatusReady".Translate() : blockReason);
            GUI.color = prevColor;
            y += RowHeight;

            if (state != null && state.phase != MaterialLifecyclePhase.Canceled && state.phase != MaterialLifecyclePhase.Completed)
            {
                Rect buttonRect = new Rect(x, y, 220f, RowHeight);
                if (Widgets.ButtonText(buttonRect, "RRR_CancelMaterialProgram".Translate()))
                {
                    OpenCancelConfirmation(project);
                }
                y += RowHeight + SectionGap;
            }

            Text.Font = prevFont;
            return y - rect.y;
        }

        public static string GetStatus(ResearchProjectDef project)
        {
            ResearchProjectMaterialState state = ResearchMaterialManager.GetState(project);
            string reason = GetBlockReason(project, state);
            return reason.NullOrEmpty() ? (string)"RRR_StatusReady".Translate() : reason;
        }

        private static float DrawLabel(float x, float y, float width, string text)
        {
            float height = Mathf.Max(RowHeight, Text.CalcHeight(text, width));
            Rect r = new Rect(x, y, width, height);
            Widgets.Label(r, text);
            return height;
        }

        private static string FundingStatusLabel(ResearchProjectMaterialState state)
        {
            if (state == null || state.fundingPhase != FundingPhase.Paid)
            {
                return "RRR_StatusFunding".Translate();
            }
            return "RRR_StatusPaid".Translate();
        }

        private static float DrawRequirementRows(float x, float y, float width, ResearchMaterialBundleSnapshot snapshot, ResearchProjectDef project)
        {
            if (snapshot == null || snapshot.requirements.Count == 0)
            {
                return 0f;
            }
            float startY = y;
            foreach (ResearchMaterialRequirementSnapshot requirement in snapshot.requirements)
            {
                int delivered = ResearchMaterialManager.DeliveredCount(project, requirement);
                int reserved = ResearchMaterialManager.ReservedCount(project, requirement.id);
                string line = requirement.cachedLabel + ": " + delivered + " / " + requirement.requiredCount;
                if (reserved > 0)
                {
                    line += " (" + "RRR_ReservedSuffix".Translate(reserved) + ")";
                }
                y += DrawLabel(x, y, width, line);
            }
            return y - startY;
        }

        private static float DrawLiveRequirementPreview(float x, float y, float width, ResearchMaterialCostsExtension extension)
        {
            if (extension == null || extension.requirements.Count == 0)
            {
                return 0f;
            }
            float startY = y;
            foreach (ResearchMaterialRequirement requirement in extension.requirements)
            {
                string line = requirement.LabelCap + ": 0 / " + requirement.count;
                y += DrawLabel(x, y, width, line);
            }
            return y - startY;
        }

        private static float DrawRefundSummary(float x, float y, float width, ResearchMaterialCostsExtension extension)
        {
            string text = "RRR_RefundSummary".Translate(Mathf.RoundToInt(extension.consumedRefundPercent * 100f));
            return DrawLabel(x, y, width, text);
        }

        private static string GetBlockReason(ResearchProjectDef project, ResearchProjectMaterialState state)
        {
            if (state == null)
            {
                return null;
            }
            if (state.invalid)
            {
                return state.invalidReason;
            }
            if (state.phase == MaterialLifecyclePhase.Canceled)
            {
                return "RRR_StatusCanceled".Translate();
            }
            if (state.phase == MaterialLifecyclePhase.Completed)
            {
                return null;
            }
            if (state.phase == MaterialLifecyclePhase.Paused)
            {
                return "RRR_StatusPaused".Translate();
            }
            if (state.fundingPhase != FundingPhase.Paid)
            {
                return "RRR_StatusAwaitingFunding".Translate();
            }
            return null;
        }

        private static void OpenCancelConfirmation(ResearchProjectDef project)
        {
            string text = "RRR_CancelConfirmation".Translate(project.LabelCap);
            Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(text, delegate
            {
                ResearchMaterialManager.Cancel(project);
            }, destructive: true));
        }
    }
}
