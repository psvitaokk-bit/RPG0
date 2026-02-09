using UnityEngine;
using Verse;
using RimWorld;
using System.Linq;
using Verse.Sound;

namespace MyRPGMod
{
    public static class RPGAbilityListDrawer
    {
        public static void Draw(Rect rect, CompRPG rpgComp, ITab_Pawn_RPG.AbilityTab curTab, ref Vector2 scrollPosition, ref RPGAbilityDef selectedAbility)
        {
            // 1. タブボタンの描画 (All / General / Class)
            Rect tabRect = new Rect(rect.x, rect.y, rect.width, 30f);
            DrawTabs(tabRect, ref curTab);

            // 2. スクロールエリアの準備
            Rect scrollRect = new Rect(rect.x, rect.y + 35f, rect.width, rect.height - 35f);
            // 本来はアビリティ数から計算すべきですが、一旦1000f確保
            Rect viewRect = new Rect(0f, 0f, scrollRect.width - 16f, 1000f);

            Widgets.BeginScrollView(scrollRect, ref scrollPosition, viewRect);

            float x = 0f;
            float y = 0f;
            float iconSize = 48f;
            float gap = 5f;

            foreach (var def in DefDatabase<AbilityDef>.AllDefs.OfType<RPGAbilityDef>())
            {
                // フィルター処理
                if (curTab == ITab_Pawn_RPG.AbilityTab.General && def.requiredClass != null) continue;
                if (curTab == ITab_Pawn_RPG.AbilityTab.Class && (def.requiredClass == null || def.requiredClass != rpgComp.currentClass)) continue;
                if (curTab == ITab_Pawn_RPG.AbilityTab.All && def.requiredClass != null && def.requiredClass != rpgComp.currentClass) continue;

                Rect iconRect = new Rect(x, y, iconSize, iconSize);

                // ハイライトとクリック判定
                if (selectedAbility == def) Widgets.DrawHighlightSelected(iconRect);
                else Widgets.DrawHighlightIfMouseover(iconRect);

                if (def.uiIcon != null) GUI.DrawTexture(iconRect, def.uiIcon);
                else Widgets.Label(iconRect, def.label.Substring(0, 1));

                if (Widgets.ButtonInvisible(iconRect))
                {
                    selectedAbility = def;
                    SoundDefOf.Click.PlayOneShotOnCamera();
                }

                x += iconSize + gap;
                if (x + iconSize > viewRect.width)
                {
                    x = 0f;
                    y += iconSize + gap;
                }
            }

            Widgets.EndScrollView();
        }

        private static void DrawTabs(Rect rect, ref ITab_Pawn_RPG.AbilityTab curTab)
        {
            WidgetRow row = new WidgetRow(rect.x, rect.y);
            if (row.ButtonText("All")) curTab = ITab_Pawn_RPG.AbilityTab.All;
            if (row.ButtonText("General")) curTab = ITab_Pawn_RPG.AbilityTab.General;
            if (row.ButtonText("Class")) curTab = ITab_Pawn_RPG.AbilityTab.Class;
        }
    }
}