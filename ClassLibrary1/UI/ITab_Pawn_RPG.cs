using UnityEngine;
using Verse;
using RimWorld;

namespace MyRPGMod
{
    public class ITab_Pawn_RPG : ITab
    {
        private Vector2 listScrollPosition;
        private Vector2 detailScrollPosition;
        private RPGAbilityDef selectedAbility;
        private AbilityTab curTab = AbilityTab.All;

        public enum AbilityTab { All, General, Class }

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

            // 1. 左側エリア (ステータス・クラス・スキル一覧)
            Rect leftRect = rect.LeftPart(0.45f);
            Listing_Standard list = new Listing_Standard();
            list.Begin(leftRect);

            RPGStatusDrawer.DrawStatusSection(list, rpgComp);
            list.Gap(10f);
            RPGStatusDrawer.DrawClassSection(list, rpgComp, () => selectedAbility = null);
            list.GapLine();

            // アビリティリスト描画 (Drawerを呼び出し)
            float listHeight = leftRect.height - list.CurHeight - (Prefs.DevMode ? 100f : 0f);
            Rect listRect = list.GetRect(listHeight);
            RPGAbilityListDrawer.Draw(listRect, rpgComp, curTab, ref listScrollPosition, ref selectedAbility);

            // デバッグメニュー
            RPGStatusDrawer.DrawDebugSection(list, rpgComp);

            list.End();

            // 2. 中央の区切り線
            Widgets.DrawLineVertical(leftRect.xMax + 5f, rect.y, rect.height);

            // 3. 右側エリア (アビリティ詳細)
            Rect rightRect = rect.RightPart(0.53f);
            RPGAbilityDetailDrawer.Draw(rightRect, rpgComp, selectedAbility, ref detailScrollPosition);
        }
    }
}