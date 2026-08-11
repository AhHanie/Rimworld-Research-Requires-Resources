using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace Research_Requires_Resources.Patches
{
    [HarmonyPatch(typeof(MainTabWindow_Research), "DrawUnlockableHyperlinks")]
    public static class MainTabWindow_Research_DrawUnlockableHyperlinks_Patch
    {
        [HarmonyPrefix]
        public static void Prefix(Rect rect, out Rect __state)
        {
            __state = rect;
        }

        [HarmonyPostfix]
        public static void Postfix(ResearchProjectDef project, ref float __result, Rect __state)
        {
            if (project == null || !ResearchMaterialManager.HasMaterialCosts(project))
            {
                return;
            }
            Rect original = __state;
            Rect drawRect = new Rect(original.x, original.y + __result, original.width, 999f);
            float height = ResearchMaterialUI.DrawProjectCosts(drawRect, project);
            __result += height;
        }
    }
}
