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

            // 司令塔：全体のレイアウトを決めて、各担当を呼ぶだけ！
            protected override void FillTab()
            {
                Pawn pawn = SelPawn;
                CompRPG rpgComp = pawn?.GetComp<CompRPG>();
                if (rpgComp == null) return;

                Rect rect = new Rect(0f, 0f, size.x, size.y).ContractedBy(10f);

                // 左右分割
                Rect leftRect = rect.LeftPart(0.45f);
                Rect rightRect = rect.RightPart(0.53f);

                // 左側の描画
                DrawLeftColumn(leftRect, rpgComp);

                // 区切り線
                Widgets.DrawLineVertical(leftRect.xMax + 5f, rect.y, rect.height);

                // 右側の描画
                DrawRightColumn(rightRect, rpgComp);
            }

        // --- 左側の描画処理まとめ ---
        private void DrawLeftColumn(Rect rect, CompRPG rpgComp)
        {
            Listing_Standard list = new Listing_Standard();
            list.Begin(rect);

            // 1. ステータス
            DrawStatusSection(list, rpgComp);
            list.Gap(10f);

            // 2. クラス
            DrawClassSection(list, rpgComp);
            list.Gap(10f);

            // ★修正ポイント1：横線をここで引く！
            // 先に線を引いて高さを消費させることで、正確な「残り」を計算できるようにするよ
            list.GapLine();

            // 3. アビリティリスト（残りの高さを計算）
            float remainingHeight = rect.height - list.CurHeight;

            // 安全のために少し余裕を持ってチェック
            if (remainingHeight > 50f)
            {
                DrawAbilityList(list, remainingHeight, rpgComp);
            }
            float debugHeight = 0f;
            if (Prefs.DevMode)
            {
                debugHeight = 125f; // ボタン3つ分くらいの高さ
                Rect debugRect = new Rect(rect.x, rect.yMax - debugHeight, rect.width, debugHeight);

                // どこがデバッグエリアか分かりやすくするために薄く枠を引くよ
                Widgets.DrawBoxSolidWithOutline(debugRect, new Color(0.1f, 0.1f, 0.1f, 0.5f), Color.red);

                Listing_Standard debugList = new Listing_Standard();
                debugList.Begin(debugRect.ContractedBy(5f));
                DrawDebugSection(debugList, rpgComp);
                debugList.End();
            }
            list.End();
        }

        // --- 各パーツの描画処理 ---

        private void DrawStatusSection(Listing_Standard list, CompRPG rpgComp)
            {
                // レベルとXP
                Text.Font = GameFont.Medium;
                list.Label($"Level: {rpgComp.level}");
                Text.Font = GameFont.Small;
                list.Label($"XP: {rpgComp.currentXp:F0} / {rpgComp.XpToNextLevel:F0}");

                Rect xpBar = list.GetRect(18f);
                Widgets.FillableBar(xpBar, rpgComp.currentXp / rpgComp.XpToNextLevel);
                list.Gap(5f);
            Text.Font = GameFont.Small;
            float magicPower = RPGCalculationUtil.GetMagicPower(rpgComp.parent as Pawn);
            list.Label($"Magic Power: {magicPower.ToStringPercent()}");
            // MPバー
            list.Label($"MP: {rpgComp.currentMP:F0} / {rpgComp.MaxMP:F0}");
                Rect mpBar = list.GetRect(18f);
                Widgets.DrawBoxSolid(mpBar, Color.black);
                Rect mpFill = mpBar.ContractedBy(1f);
                mpFill.width *= (rpgComp.currentMP / rpgComp.MaxMP);
                Widgets.DrawBoxSolid(mpFill, new Color(0.2f, 0.2f, 0.8f));

                list.Gap(5f);

                // スキルポイント
                list.Label($"Skill Points: {rpgComp.skillPoints}");
            }

            private void DrawClassSection(Listing_Standard list, CompRPG rpgComp)
            {
                string currentClassLabel = rpgComp.currentClass != null ? rpgComp.currentClass.label : "None";
                list.Label($"Class: {currentClassLabel}");

                if (list.ButtonText("Select Class"))
                {
                    List<FloatMenuOption> options = new List<FloatMenuOption>();
                    options.Add(new FloatMenuOption("None", () => rpgComp.currentClass = null));
                    foreach (var def in DefDatabase<RPGClassDef>.AllDefs)
                    {
                        options.Add(new FloatMenuOption(def.label, () =>
                        {
                            rpgComp.currentClass = def;
                            // 職業を変えたら選択中のアビリティもリセットしたほうが安全かも
                            selectedAbility = null;
                        }));
                    }
                    Find.WindowStack.Add(new FloatMenu(options));
                }
            }

        // クラスのメンバ変数に追加
        private enum AbilityTab { All, General, Class }
        private AbilityTab curTab = AbilityTab.All;

        // DrawAbilityList メソッドの冒頭をこう変える
        private void DrawAbilityList(Listing_Standard list, float height, CompRPG rpgComp)
        {
            // --- 1. タブボタンの描画エリア ---
            Rect tabRect = list.GetRect(30f);
            WidgetRow row = new WidgetRow(tabRect.x, tabRect.y);

            // タブボタンを押したらモードを切り替える
            if (row.ButtonText("All")) curTab = AbilityTab.All;
            if (row.ButtonText("General")) curTab = AbilityTab.General;
            if (row.ButtonText("Class")) curTab = AbilityTab.Class;

            list.Gap(5f);

            // --- 2. リストの準備 ---
            Rect scrollRect = list.GetRect(height - 40f); // ボタン分引いておく
            Rect viewRect = new Rect(0f, 0f, scrollRect.width - 16f, 1000f); // 本来はアビリティ数から計算すべき

            Widgets.BeginScrollView(scrollRect, ref scrollPosition, viewRect);

            // グリッド表示にするために Listing ではなく手動計算する例
            float x = 0f;
            float y = 0f;
            float iconSize = 48f; // アイコンの大きさ
            float gap = 5f;

            foreach (var def in DefDatabase<AbilityDef>.AllDefs.OfType<RPGAbilityDef>())
            {
                // --- フィルター処理 ---
                if (curTab == AbilityTab.General && def.requiredClass != null) continue;
                if (curTab == AbilityTab.Class && (def.requiredClass == null || def.requiredClass != rpgComp.currentClass)) continue;
                // Allの時も、自分のクラス以外の専用スキルは隠す
                if (curTab == AbilityTab.All && def.requiredClass != null && def.requiredClass != rpgComp.currentClass) continue;

                // --- アイコン描画 ---
                Rect iconRect = new Rect(x, y, iconSize, iconSize);

                // 選択中のハイライト
                if (selectedAbility == def) Widgets.DrawHighlightSelected(iconRect);
                else Widgets.DrawHighlightIfMouseover(iconRect);

                // アイコン画像があれば表示（なければ文字）
                if (def.uiIcon != null)
                {
                    GUI.DrawTexture(iconRect, def.uiIcon);
                }
                else
                {
                    Widgets.Label(iconRect, def.label.Substring(0, 1)); // 仮の文字
                }

                // クリック処理
                if (Widgets.ButtonInvisible(iconRect))
                {
                    selectedAbility = def;
                    SoundDefOf.Click.PlayOneShotOnCamera();
                }

                // 次のアイコンの座標計算（横に並べて、端まで行ったら改行）
                x += iconSize + gap;
                if (x + iconSize > viewRect.width)
                {
                    x = 0f;
                    y += iconSize + gap;
                }
            }

            Widgets.EndScrollView();
        }

        // デバッグセクション（引数に渡す Listing_Standard を使うのがコツ！）
        private void DrawDebugSection(Listing_Standard list, CompRPG rpgComp)
        {
            GUI.color = Color.red;
            list.Label("--- DEBUG MENU ---");
            GUI.color = Color.white;

            if (list.ButtonText("Add 1000 XP"))
            {
                rpgComp.GainXp(1000f);
            }

            if (list.ButtonText("Force Level Up"))
            {
                float needed = rpgComp.XpToNextLevel - rpgComp.currentXp;
                rpgComp.GainXp(needed);
            }

            if (list.ButtonText("Add 10 Skill Points"))
            {
                rpgComp.skillPoints += 10;
            }
        }

        // --- 右側の描画処理 ---
        private void DrawRightColumn(Rect rect, CompRPG rpgComp)
            {
                if (selectedAbility != null)
                {
                    // ここは今まで通り DrawDetail を呼ぶだけ
                    DrawDetail(rect, rpgComp, selectedAbility);
                }
                else
                {
                    Text.Anchor = TextAnchor.MiddleCenter;
                    GUI.color = Color.gray;
                    Widgets.Label(rect, "Select an ability to see details");
                    GUI.color = Color.white;
                    Text.Anchor = TextAnchor.UpperLeft;
                }
            }
        private Vector2 detailScrollPosition;
        private void DrawDetail(Rect rect, CompRPG comp, RPGAbilityDef def)
        {
            // --- 1. エリア分け ---
            // ヘッダー: 60px (タイトルとタイプ)
            // フッター: 80px (マナコストとボタン)
            // ボディ: 残りのスペース (スクロールする部分)
            Rect headerRect = new Rect(rect.x, rect.y, rect.width, 60f);
            Rect footerRect = new Rect(rect.x, rect.yMax - 80f, rect.width, 80f);
            Rect bodyRect = new Rect(rect.x, headerRect.yMax, rect.width, rect.height - headerRect.height - footerRect.height);

            // --- 2. ヘッダー部分（固定表示） ---
            Listing_Standard header = new Listing_Standard();
            header.Begin(headerRect);

            Text.Font = GameFont.Medium;
            header.Label(def.label);

            Text.Font = GameFont.Tiny;
            GUI.color = Color.cyan;
            header.Label($"Type: {def.rpgCategory}");
            GUI.color = Color.white;

            header.GapLine();
            header.End();

            // --- 3. ボディ部分（スクロール可能） ---
            // 中身の高さを仮で 1000f 確保しておく（中身が多くても大丈夫なように）
            Rect viewRect = new Rect(0f, 0f, bodyRect.width - 16f, 1000f);
            Widgets.BeginScrollView(bodyRect, ref detailScrollPosition, viewRect);

            Listing_Standard list = new Listing_Standard();
            list.Begin(viewRect);

            // 説明文
            Text.Font = GameFont.Small;
            list.Label(def.description);
            list.Gap();

            // ステータス表示（お兄ちゃんのロジック）
            int curLv = comp.GetAbilityLevel(def);

            // アビリティごとの最終倍率を取得（今のところ表示に使ってないけど、必要ならここで！）
            float multiplier = comp.GetMagicMultiplier(def);

            foreach (var stat in def.stats)
            {
                Pawn caster = comp.parent as Pawn;

                // 現在値の取得
                string curStr = stat.Worker.GetDisplayString(stat, curLv, caster, def);
                string text = curStr;

                // 次レベルの予測表示
                if (curLv < def.maxLevel)
                {
                    // 次のレベルの数値を計算
                    // 注意：Workerによっては数値計算しない(0を返す)ものもあるので、テキスト系Workerの場合はif文で弾く工夫が必要かも
                    float nextVal = stat.Worker.Calculate(stat.baseValue, stat.valuePerLevel, curLv + 1, caster, def);


                    // 数値系のステータスだけ予測値を出すための簡易チェック
                    if (stat.label != "Effectiveness" && stat.label != "Success Chance")
                    {
                        text += $" -> <color=green>{nextVal.ToString(stat.formatString)}{stat.unit}</color>";
                    }
                }

                list.Label(text);
            }

            list.End();
            Widgets.EndScrollView();

            // --- 4. フッター部分（固定表示：ボタンなど） ---
            Listing_Standard footer = new Listing_Standard();
            footer.Begin(footerRect);

            footer.GapLine();
            Text.Font = GameFont.Small;
            footer.Label($"Mana Cost: {def.manaCost}");

            // ボタンの描画
            Rect btnRect = footer.GetRect(30f);
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