using UnityEngine;
using Verse;
using RimWorld;
using System.Collections.Generic;

namespace MyRPGMod
{
    public static class RPGStatusDrawer
    {
        public static void DrawStatusSection(Listing_Standard list, CompRPG rpgComp)
        {
            Text.Font = GameFont.Medium;
            list.Label($"Level: {rpgComp.level}");
            Text.Font = GameFont.Small;
            list.Label($"XP: {rpgComp.currentXp:F0} / {rpgComp.XpToNextLevel:F0}");

            Rect xpBar = list.GetRect(18f);
            Widgets.FillableBar(xpBar, rpgComp.currentXp / rpgComp.XpToNextLevel);

            list.Gap(5f);
            float magicPower = RPGMagicCalculator.GetMagicPower(rpgComp.parent as Pawn);
            list.Label($"Magic Power: {magicPower.ToStringPercent()}");

            list.Label($"MP: {rpgComp.currentMP:F0} / {rpgComp.MaxMP:F0}");
            Rect mpBar = list.GetRect(18f);
            Widgets.DrawBoxSolid(mpBar, Color.black);
            Rect mpFill = mpBar.ContractedBy(1f);
            mpFill.width *= (rpgComp.currentMP / rpgComp.MaxMP);
            Widgets.DrawBoxSolid(mpFill, new Color(0.2f, 0.2f, 0.8f));

            list.Gap(5f);
            list.Label($"Skill Points: {rpgComp.skillPoints}");
        }

        public static void DrawClassSection(Listing_Standard list, CompRPG rpgComp, System.Action onClassChanged)
        {
            string currentClassLabel = rpgComp.currentClass != null ? rpgComp.currentClass.label : "None";
            list.Label($"Class: {currentClassLabel}");

            if (list.ButtonText("Select Class"))
            {
                List<FloatMenuOption> options = new List<FloatMenuOption>();
                options.Add(new FloatMenuOption("None", () => { rpgComp.SetClass(null); onClassChanged(); }));
                foreach (var def in DefDatabase<RPGClassDef>.AllDefs)
                {
                    options.Add(new FloatMenuOption(def.label, () => { rpgComp.SetClass(def); onClassChanged(); }));
                }
                Find.WindowStack.Add(new FloatMenu(options));
            }
        }

        public static void DrawDebugSection(Listing_Standard list, CompRPG rpgComp)
        {
            if (!Prefs.DevMode) return;
            GUI.color = Color.red;
            list.Label("--- DEBUG ---");
            GUI.color = Color.white;
            if (list.ButtonText("Add 1000 XP")) rpgComp.GainXp(1000f);
            if (list.ButtonText("Add 10 SP")) rpgComp.skillPoints += 10;
        }
    }
}