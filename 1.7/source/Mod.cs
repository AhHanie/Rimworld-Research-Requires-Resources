using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace Research_Requires_Resources
{
    public class Mod : Verse.Mod
    {
        public Mod(ModContentPack content) : base(content)
        {
            LongEventHandler.QueueLongEvent(Init, "RRR.LoadingLabel", doAsynchronously: true, null);
        }

        private void Init()
        {
            ResearchMaterialRequirementIdInitializer.AssignIds();
            GetSettings<ModSettings>();
            new Harmony("sk.researchresources").PatchAll();
        }

        public override string SettingsCategory()
        {
            return "RRR.SettingsTitle".Translate();
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            ModSettingsWindow.Draw(inRect);
            base.DoSettingsWindowContents(inRect);
        }
    }
}
