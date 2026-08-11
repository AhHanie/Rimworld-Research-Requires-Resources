using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace Research_Requires_Resources
{
    public class Dialog_ResearchOverview : Window
    {
        private const float RowHeight = 28f;

        private const float HeaderHeight = 24f;

        private const float SearchHeight = 24f;

        private const float TitleHeight = 30f;

        private const float RowSpacing = 4f;

        private const float ScrollBarAllowance = 16f;

        private const float ColResearchPct = 0.34f;

        private const float ColPointsPct = 0.14f;

        private const float ColCostPct = 0.32f;

        private readonly List<ResearchProjectDef> allProjects;

        private List<ResearchProjectDef> filteredProjects;

        private readonly QuickSearchWidget searchWidget = new QuickSearchWidget();

        private Vector2 scrollPosition;

        public override Vector2 InitialSize => new Vector2(820f, 620f);

        public Dialog_ResearchOverview()
        {
            doCloseX = true;
            draggable = true;
            resizeable = true;

            allProjects = DefDatabase<ResearchProjectDef>.AllDefsListForReading
                .OrderBy(p => (string)p.LabelCap, StringComparer.OrdinalIgnoreCase)
                .ThenBy(p => p.defName, StringComparer.Ordinal)
                .ToList();
            filteredProjects = allProjects;
        }

        public override void DoWindowContents(Rect inRect)
        {
            float y = inRect.y;

            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(inRect.x, y, inRect.width, TitleHeight), "RRR_ResearchOverviewWindowTitle".Translate());
            Text.Font = GameFont.Small;
            y += TitleHeight;

            Rect searchRect = new Rect(inRect.x, y, inRect.width, SearchHeight);
            searchWidget.OnGUI(searchRect, RebuildFilteredList);
            y += SearchHeight + RowSpacing;

            Rect headerRect = new Rect(inRect.x, y, inRect.width, HeaderHeight);
            DrawHeaderRow(headerRect);
            y += HeaderHeight;

            Rect tableOutRect = new Rect(inRect.x, y, inRect.width, inRect.height - (y - inRect.y));
            DrawTable(tableOutRect);
        }

        private void RebuildFilteredList()
        {
            filteredProjects = searchWidget.filter.Active
                ? allProjects.Where(p => searchWidget.filter.Matches(p.LabelCap)).ToList()
                : allProjects;
            searchWidget.noResultsMatched = filteredProjects.Count == 0;
            scrollPosition = Vector2.zero;
        }

        private void GetColumnRects(float x, float y, float width, float height, out Rect research, out Rect points, out Rect cost, out Rect status)
        {
            float researchW = width * ColResearchPct;
            float pointsW = width * ColPointsPct;
            float costW = width * ColCostPct;
            float statusW = width - researchW - pointsW - costW;

            research = new Rect(x, y, researchW, height);
            points = new Rect(research.xMax, y, pointsW, height);
            cost = new Rect(points.xMax, y, costW, height);
            status = new Rect(cost.xMax, y, statusW, height);
        }

        private void DrawHeaderRow(Rect rect)
        {
            float contentWidth = rect.width - ScrollBarAllowance;
            GetColumnRects(rect.x, rect.y, contentWidth, rect.height, out Rect researchRect, out Rect pointsRect, out Rect costRect, out Rect statusRect);

            GUI.color = new Color(0.85f, 0.85f, 0.85f);
            Widgets.Label(researchRect, "RRR_ColumnResearch".Translate());
            Widgets.Label(pointsRect, "RRR_ColumnResearchPoints".Translate());
            Widgets.Label(costRect, "RRR_ColumnMaterialCost".Translate());
            Widgets.Label(statusRect, "RRR_ColumnFundingStatus".Translate());
            GUI.color = Color.white;

            Widgets.DrawLineHorizontal(rect.x, rect.yMax - 1f, rect.width);
        }

        private void DrawTable(Rect outRect)
        {
            float contentWidth = outRect.width - ScrollBarAllowance;
            float viewHeight = filteredProjects.Count * RowHeight;
            Rect viewRect = new Rect(0f, 0f, contentWidth, Mathf.Max(viewHeight, outRect.height));

            Widgets.BeginScrollView(outRect, ref scrollPosition, viewRect);

            if (filteredProjects.Count == 0)
            {
                Widgets.Label(new Rect(0f, 0f, contentWidth, RowHeight), "RRR_ResearchOverviewNoResults".Translate());
            }
            else
            {
                int first = Mathf.Clamp(Mathf.FloorToInt(scrollPosition.y / RowHeight), 0, filteredProjects.Count);
                int last = Mathf.Clamp(Mathf.CeilToInt((scrollPosition.y + outRect.height) / RowHeight) + 1, first, filteredProjects.Count);

                for (int i = first; i < last; i++)
                {
                    DrawRow(i, contentWidth);
                }
            }

            Widgets.EndScrollView();
        }

        private void DrawRow(int index, float width)
        {
            ResearchProjectDef project = filteredProjects[index];
            float y = index * RowHeight;
            Rect rowRect = new Rect(0f, y, width, RowHeight);
            if (index % 2 == 1)
            {
                Widgets.DrawBoxSolid(rowRect, new Color(1f, 1f, 1f, 0.03f));
            }

            GetColumnRects(0f, y, width, RowHeight, out Rect researchRect, out Rect pointsRect, out Rect costRect, out Rect statusRect);

            DrawTruncatedLabel(researchRect, project.LabelCap);
            Widgets.Label(pointsRect, GetProgressLabel(project));
            DrawTruncatedLabel(costRect, ResearchMaterialManager.GetOverviewMaterialCostDisplay(project) ?? "-");
            Widgets.Label(statusRect, ResearchMaterialManager.GetOverviewFundingStatusLabel(project));
        }

        private static void DrawTruncatedLabel(Rect rect, string text)
        {
            string truncated = text.Truncate(rect.width - 4f);
            Widgets.Label(rect, truncated);
            if (truncated != text && Mouse.IsOver(rect))
            {
                TooltipHandler.TipRegion(rect, text);
            }
        }

        private static string GetProgressLabel(ResearchProjectDef project)
        {
            float cost = project.Cost;
            if (cost <= 0f)
            {
                return "-";
            }
            string format = project.baseCost > 0f ? "F0" : "F2";
            float progress = Find.ResearchManager.GetProgress(project);
            return progress.ToString(format) + " / " + cost.ToString(format);
        }
    }
}
