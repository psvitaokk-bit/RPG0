using UnityEngine;
using Verse;
using RimWorld;
using Verse.Sound;
using System.Collections.Generic;
using System.Linq;

namespace MyRPGMod
{
    public class ITab_Pawn_RPG : ITab
    {
        private Vector2 scrollPosition;
        private RPGAbilityDef selectedAbility;

        public ITab_Pawn_RPG()
        {
            this.size = new Vector2(600f, 480f);
            this.labelKey = "RPG Stats";
        }

        protected override void FillTab()
        {
            Pawn pawn = SelPawn;
            CompRPG rpgComp = pawn?.GetComp<CompRPG>();
            if (rpgComp == null) return;

            Rect rect = new Rect(0f, 0f, size.x, size.y).ContractedBy(10f);

            // 左右分割
            Rect leftRect = rect.LeftPart(0.45f);
            Rect rightRect = rect.RightPart(0.53f);

            // --- 左側：ステータス ＆ リスト ---
            Listing_Standard leftList = new Listing_Standard();
            leftList.Begin(leftRect);

            // 1. レベルと経験値
            Text.Font = GameFont.Medium;
            leftList.Label($"Level: {rpgComp.level}");
            Text.Font = GameFont.Small;
            leftList.Label($"XP: {rpgComp.currentXp:F0} / {rpgComp.XpToNextLevel:F0}");

            Rect xpBar = leftList.GetRect(18f);
            Widgets.FillableBar(xpBar, rpgComp.currentXp / rpgComp.XpToNextLevel);

            leftList.Gap(5f);

            // 2. MPバー
            leftList.Label($"MP: {rpgComp.currentMP:F0} / {rpgComp.MaxMP:F0}");
            Rect mpBar = leftList.GetRect(18f);
            Widgets.DrawBoxSolid(mpBar, Color.black);
            Rect mpFill = mpBar.ContractedBy(1f);
            mpFill.width *= (rpgComp.currentMP / rpgComp.MaxMP);
            Widgets.DrawBoxSolid(mpFill, new Color(0.2f, 0.2f, 0.8f));

            leftList.Gap(5f);

            // 3. スキルポイント
            leftList.Label($"Skill Points: {rpgComp.skillPoints}");
            leftList.GapLine();

            // 4. アビリティリスト（スクロール）
            Rect scrollRect = leftList.GetRect(leftRect.height - leftList.CurHeight);
            Rect viewRect = new Rect(0f, 0f, scrollRect.width - 16f, 1000f);

            Widgets.BeginScrollView(scrollRect, ref scrollPosition, viewRect);
            Listing_Standard scrollList = new Listing_Standard();
            scrollList.Begin(viewRect);

            // アビリティ一覧
            foreach (var def in DefDatabase<AbilityDef>.AllDefs.OfType<RPGAbilityDef>())
            {
                Rect r = scrollList.GetRect(30f);
                if (selectedAbility == def) Widgets.DrawHighlightSelected(r);
                else Widgets.DrawHighlightIfMouseover(r);

                if (Widgets.ButtonInvisible(r)) { selectedAbility = def; SoundDefOf.Click.PlayOneShotOnCamera(); }

                int curLv = rpgComp.GetAbilityLevel(def);
                string label = $"{def.label} (Lv.{curLv})";
                if (curLv == 0) GUI.color = Color.gray;
                Widgets.Label(r.ContractedBy(2f), label);
                GUI.color = Color.white;
            }

            // ★追加：デバッグメニュー★
            if (Prefs.DevMode)
            {
                scrollList.GapLine();
                GUI.color = Color.red;
                scrollList.Label("--- DEBUG MENU ---");
                GUI.color = Color.white;

                if (scrollList.ButtonText("Add 1000 XP"))
                {
                    rpgComp.GainXp(1000f);
                    SoundDefOf.Click.PlayOneShotOnCamera();
                }
                if (scrollList.ButtonText("Force Level Up"))
                {
                    float needed = rpgComp.XpToNextLevel - rpgComp.currentXp;
                    rpgComp.GainXp(needed);
                    SoundDefOf.Click.PlayOneShotOnCamera();
                }
                if (scrollList.ButtonText("Add 10 Skill Points"))
                {
                    rpgComp.skillPoints += 10;
                    SoundDefOf.Click.PlayOneShotOnCamera();
                }
            }

            scrollList.End();
            Widgets.EndScrollView();
            leftList.End();

            // --- 右側：詳細表示 ---
            Widgets.DrawLineVertical(leftRect.xMax + 5f, rect.y, rect.height);
            if (selectedAbility != null)
            {
                DrawDetail(rightRect, rpgComp, selectedAbility);
            }
            else
            {
                Text.Anchor = TextAnchor.MiddleCenter;
                GUI.color = Color.gray;
                Widgets.Label(rightRect, "Select an ability");
                GUI.color = Color.white;
                Text.Anchor = TextAnchor.UpperLeft;
            }
        }

        private void DrawDetail(Rect rect, CompRPG comp, RPGAbilityDef def)
        {
            Listing_Standard listing = new Listing_Standard();
            listing.Begin(rect);

            Text.Font = GameFont.Medium;
            listing.Label(def.label);
            Text.Font = GameFont.Tiny;
            GUI.color = Color.cyan;
            listing.Label($"Type: {def.rpgCategory}");
            GUI.color = Color.white;
            listing.GapLine();

            Text.Font = GameFont.Small;
            listing.Label(def.description);
            listing.Gap();

            int curLv = comp.GetAbilityLevel(def);
            if (def.stats != null)
            {
                foreach (var stat in def.stats)
                {
                    float cur = stat.baseValue + (stat.valuePerLevel * Mathf.Max(0, curLv - 1));
                    float next = stat.baseValue + (stat.valuePerLevel * curLv);
                    string text = $"{stat.label}: {cur}{stat.unit}";
                    if (curLv < def.maxLevel) text += $" -> <color=green>{next}{stat.unit}</color>";
                    listing.Label(text);
                }
            }

            listing.Gap();
            listing.Label($"Mana Cost: {def.manaCost}");

            Rect btnRect = new Rect(rect.x, rect.yMax - 40f, rect.width, 35f);
            if (curLv < def.maxLevel)
            {
                string btnLabel = curLv == 0 ? $"Learn ({def.upgradeCost}pt)" : $"Upgrade ({def.upgradeCost}pt)";
                if (Widgets.ButtonText(btnRect, btnLabel))
                {
                    comp.UpgradeAbility(def);
                    SoundDefOf.TechprintApplied.PlayOneShotOnCamera();
                }
            }
            else
            {
                GUI.color = Color.gray;
                Widgets.Label(btnRect, "MAX LEVEL REACHED");
                GUI.color = Color.white;
            }

            listing.End();
        }
    }
}