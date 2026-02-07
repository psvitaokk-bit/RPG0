using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;

namespace MyRPGMod
{
    [StaticConstructorOnStartup]
    public static class Startup_InjectRPG
    {
        static Startup_InjectRPG()
        {
            Log.Message("[MyRPGMod] Start Injecting RPG system and Tabs...");

            Type tabType = typeof(ITab_Pawn_RPG);

            // 全ての種族Defをループ
            foreach (ThingDef def in DefDatabase<ThingDef>.AllDefs.Where(d => d.race != null && d.race.Humanlike))
            {
                // 1. データコンポーネントの注入（念のため）
                if (!def.HasComp(typeof(CompRPG)))
                {
                    if (def.comps == null) def.comps = new List<CompProperties>();
                    def.comps.Add(new CompProperties_RPG());
                }

                // 2. タブの注入
                // 型リスト (inspectorTabs) に追加
                if (def.inspectorTabs == null) def.inspectorTabs = new List<Type>();
                if (!def.inspectorTabs.Contains(tabType))
                {
                    def.inspectorTabs.Add(tabType);
                }

                // 3. 【重要】解決済みリスト (inspectorTabsResolved) に追加
                // これをやらないと、ゲーム画面にタブが表示されない！
                if (def.inspectorTabsResolved == null)
                {
                    def.inspectorTabsResolved = new List<InspectTabBase>();
                }

                // すでに同じ型のインスタンスが入っていないかチェック
                if (!def.inspectorTabsResolved.Any(t => t.GetType() == tabType))
                {
                    try
                    {
                        // タブを実際に生成してリストに加える
                        InspectTabBase tabInstance = (InspectTabBase)InspectTabManager.GetSharedInstance(tabType);
                        def.inspectorTabsResolved.Add(tabInstance);
                        // Log.Message($"[MyRPGMod] Tab injected into {def.defName}");
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"[MyRPGMod] Failed to inject tab into {def.defName}: {ex.Message}");
                    }
                }
            }
            Log.Message("[MyRPGMod] Injection Complete!");
        }
    }
}