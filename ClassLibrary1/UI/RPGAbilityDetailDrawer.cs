using UnityEngine;
using Verse;
using RimWorld;
using System.Linq;
using Verse.Sound;

namespace MyRPGMod
{
    public static class RPGAbilityDetailDrawer
    {
        public static void Draw(Rect rect, CompRPG comp, RPGAbilityDef def, ref Vector2 scrollPosition)
        {
            if (def == null)
            {
                Text.Anchor = TextAnchor.MiddleCenter;
                GUI.color = Color.gray;
                Widgets.Label(rect, "Select an ability to see details");
                GUI.color = Color.white;
                Text.Anchor = TextAnchor.UpperLeft;
                return;
            }

            // エリア分け (ヘッダー / ボディ / フッター)
            Rect headerRect = new Rect(rect.x, rect.y, rect.width, 60f);
            Rect footerRect = new Rect(rect.x, rect.yMax - 80f, rect.width, 80f);
            Rect bodyRect = new Rect(rect.x, headerRect.yMax, rect.width, rect.height - headerRect.height - footerRect.height);

            // 1. ヘッダー (タイトルとカテゴリ)
            DrawHeader(headerRect, comp, def);

            // 2. ボディ (説明文とステータス - スクロールエリア)
            DrawBody(bodyRect, comp, def, ref scrollPosition);

            // 3. フッター (マナコストと習得・強化ボタン)
            DrawFooter(footerRect, comp, def);
        }

        // RPGAbilityDetailDrawer.cs

        private static void DrawHeader(Rect rect, CompRPG comp, RPGAbilityDef def)
        {
            // 1. 現在の習得レベルを取得
            int curLv = comp.GetAbilityLevel(def);

            Listing_Standard header = new Listing_Standard();
            header.Begin(rect);

            Text.Font = GameFont.Medium;

            // 2. ラベルの作成。名前の後に (Lv: 現在 / 最大) を付け足す
            // <color=...> タグを使うと、レベル部分だけ色を変えて目立たせることができます
            string labelText = $"{def.label} <color=orange>(Lv: {curLv} / {def.maxLevel})</color>";

            header.Label(labelText);

            Text.Font = GameFont.Tiny;
            GUI.color = Color.cyan;
            header.Label($"Type: {def.rpgCategory}");
            GUI.color = Color.white;

            header.GapLine();
            header.End();
        }

        private static void DrawBody(Rect rect, CompRPG comp, RPGAbilityDef def, ref Vector2 scrollPosition)
        {
            Rect viewRect = new Rect(0f, 0f, rect.width - 16f, 1000f); // 高さは内容に応じて調整
            Widgets.BeginScrollView(rect, ref scrollPosition, viewRect);

            Listing_Standard list = new Listing_Standard();
            list.Begin(viewRect);

            Text.Font = GameFont.Small;
            list.Label(def.description);
            list.Gap();

            int curLv = comp.GetAbilityLevel(def);
            Pawn caster = comp.parent as Pawn;

            foreach (var stat in def.stats)
            {
                string text = stat.Worker.GetDisplayString(stat, curLv, caster, def);

                // 次レベルの予測表示 (Workerロジックを活用)
                if (curLv < def.maxLevel && stat.label != "Effectiveness" && stat.label != "Success Chance")
                {
                    float nextVal = stat.Worker.Calculate(stat.baseValue, stat.valuePerLevel, curLv + 1, caster, def);
                    text += $" -> <color=green>{nextVal.ToString(stat.formatString)}{stat.unit}</color>";
                }
                list.Label(text);
            }

            list.End();
            Widgets.EndScrollView();
        }

        private static void DrawFooter(Rect rect, CompRPG comp, RPGAbilityDef def)
        {
            Listing_Standard footer = new Listing_Standard();
            footer.Begin(rect);
            footer.GapLine();
            Text.Font = GameFont.Small;
            footer.Label($"Mana Cost: {def.manaCost}");

            Rect btnRect = footer.GetRect(30f);
            int curLv = comp.GetAbilityLevel(def);

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
                Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.Label(btnRect, "MAX LEVEL REACHED");
                Text.Anchor = TextAnchor.UpperLeft;
                GUI.color = Color.white;
            }
            footer.End();
        }
    }
}