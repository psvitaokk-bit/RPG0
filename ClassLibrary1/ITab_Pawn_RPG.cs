using UnityEngine;
using Verse;
using RimWorld;
using Verse.Sound;
using System.Collections.Generic;
using System.Linq; // データを検索・整理するために必要

namespace MyRPGMod
{
    public class ITab_Pawn_RPG : ITab
    {
        // スクロール位置を記憶しておく変数
        private Vector2 scrollPosition;

        public ITab_Pawn_RPG()
        {
            this.size = new Vector2(300f, 450f); // 少し縦長にしておいたよ
            this.labelKey = "RPG Stats";
        }

        protected override void FillTab()
        {
            Pawn pawn = SelPawn;
            if (pawn == null) return;

            CompRPG rpgComp = pawn.GetComp<CompRPG>();
            if (rpgComp == null) return;

            // --- 描画の準備 ---
            Rect rect = new Rect(0f, 0f, this.size.x, this.size.y).ContractedBy(10f);
            Listing_Standard listing = new Listing_Standard();
            listing.Begin(rect);

            // ==========================================
            // 1. 基本ステータス（ここは固定表示）
            // ==========================================
            Text.Font = GameFont.Medium;
            listing.Label($"Level: {rpgComp.level}");

            Text.Font = GameFont.Small;
            listing.Label($"XP: {rpgComp.currentXp:F0} / {rpgComp.XpToNextLevel:F0}");

            // 経験値バー
            Rect barRect = listing.GetRect(22f);
            Widgets.FillableBar(barRect, rpgComp.currentXp / rpgComp.XpToNextLevel);

            listing.Gap(5f);
            listing.Label($"Skill Points: {rpgComp.skillPoints}");
            listing.GapLine();

            listing.Gap(5f);

            listing.Gap(5f);
            listing.Label($"Skill Points: {rpgComp.skillPoints}");
            // ==========================================
            // 2. アビリティ一覧（スクロールエリア）
            // ==========================================

            // ★ポイント: 表示したいアビリティを全検索してリスト化
            // "RPG_" で始まるdefNameのアビリティを自動で集めるようにしたよ
            List<AbilityDef> rpgAbilities = DefDatabase<AbilityDef>.AllDefs
                .Where(def => def.defName.StartsWith("RPG_"))
                .ToList();

            // スクロールの中身の高さを計算（アビリティの数 × 1行の高さ）
            float lineHeight = 30f;
            float contentHeight = rpgAbilities.Count * lineHeight + 50f; // 少し余裕を持たせる
            if (Prefs.DevMode) contentHeight += 200f; // デバッグメニュー用

            // スクロール用の「窓」と「中身」の矩形を定義
            Rect outRect = listing.GetRect(rect.height - listing.CurHeight); // 残りのスペース全部
            Rect viewRect = new Rect(0f, 0f, outRect.width - 16f, contentHeight); // スクロールバーの幅(16px)を引く

            // スクロール開始
            Widgets.BeginScrollView(outRect, ref scrollPosition, viewRect);

            Listing_Standard scrollListing = new Listing_Standard();
            scrollListing.Begin(viewRect);

            scrollListing.Label("Abilities:");

            // 自動取得したリストをループで回してボタンを作る
            if (rpgComp.unlockedAbilities == null) rpgComp.unlockedAbilities = new List<string>();

            foreach (AbilityDef ability in rpgAbilities)
            {
                DrawAbilityRow(scrollListing, rpgComp, ability);
            }

            // ==========================================
            // 3. デバッグメニュー（スクロールの一番下）
            // ==========================================
            if (Prefs.DevMode)
            {
                scrollListing.GapLine();
                GUI.color = Color.red;
                scrollListing.Label("--- DEBUG MENU ---");
                GUI.color = Color.white;

                if (scrollListing.ButtonText("Add 1000 XP"))
                {
                    rpgComp.GainXp(1000f);
                    SoundDefOf.Click.PlayOneShotOnCamera();
                }
                if (scrollListing.ButtonText("Force Level Up"))
                {
                    float needed = rpgComp.XpToNextLevel - rpgComp.currentXp;
                    rpgComp.GainXp(needed);
                    SoundDefOf.Click.PlayOneShotOnCamera();
                }
                if (scrollListing.ButtonText("Add 10 Skill Points"))
                {
                    rpgComp.skillPoints += 10;
                    SoundDefOf.Click.PlayOneShotOnCamera();
                }
            }

            scrollListing.End();
            Widgets.EndScrollView();
            // スクロール終了

            listing.End();
        }

        // 行ごとの描画処理を別関数に分けたよ（コードがスッキリする！）
        private void DrawAbilityRow(Listing_Standard listing, CompRPG comp, AbilityDef ability)
        {
            // コスト設定（とりあえず全部一律2ポイントにしてるけど、XMLに拡張データ持たせてもいいね）
            int cost = 2;

            // 横並びレイアウト用の枠を確保
            Rect rowRect = listing.GetRect(30f);

            // 左側：アビリティ名
            Rect labelRect = new Rect(rowRect.x, rowRect.y, rowRect.width * 0.6f, rowRect.height);
            // 右側：ボタン
            Rect buttonRect = new Rect(rowRect.x + rowRect.width * 0.6f, rowRect.y, rowRect.width * 0.4f, rowRect.height);

            Widgets.Label(labelRect, ability.label);

            if (comp.unlockedAbilities.Contains(ability.defName))
            {
                // 習得済み
                GUI.color = Color.green;
                Widgets.Label(buttonRect, "Learned");
                GUI.color = Color.white;
            }
            else
            {
                // 未習得
                if (Widgets.ButtonText(buttonRect, $"Unlock ({cost} pt)"))
                {
                    if (comp.skillPoints >= cost)
                    {
                        comp.skillPoints -= cost;
                        comp.UnlockAbility(ability);
                        SoundDefOf.TechprintApplied.PlayOneShotOnCamera();
                    }
                    else
                    {
                        Messages.Message("Not enough skill points!", MessageTypeDefOf.RejectInput, false);
                    }
                }
            }
        }
    }
}