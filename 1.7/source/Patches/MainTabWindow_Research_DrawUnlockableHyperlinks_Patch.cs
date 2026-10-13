using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace Research_Requires_Resources.Patches
{
    [HarmonyPatch(typeof(MainTabWindow_Research), "DrawProjectScrollView")]
    public static class MainTabWindow_Research_DrawUnlockableHyperlinks_Patch
    {
        // Wraps the one DrawUnlockableHyperlinks call in the project scroll view: rect and
        // project are the inner call's arguments and __result is the height it consumed.
        [HarmonyInfix(typeof(MainTabWindow_Research), "DrawUnlockableHyperlinks")]
        public static float Postfix(float __result, Rect rect, ResearchProjectDef project)
        {
            if (project == null || !ResearchMaterialManager.HasMaterialCosts(project))
            {
                return __result;
            }
            Rect drawRect = new Rect(rect.x, rect.y + __result, rect.width, 999f);
            return __result + ResearchMaterialUI.DrawProjectCosts(drawRect, project);
        }
    }
}
