using Verse;
using RimWorld;
using UnityEngine;
using System.Linq;

namespace MyRPGMod
{
    // XMLで指定するためのプロパティクラス
    public class HediffCompProperties_Regenerate : HediffCompProperties
    {
        public HediffCompProperties_Regenerate()
        {
            this.compClass = typeof(HediffComp_Regenerate);
        }
    }

    // 実際の回復処理を行うクラス
    public class HediffComp_Regenerate : HediffComp
    {
        // 外部から注入される1秒あたりの基本回復量
        public float healAmountPerTick = 0.1f;

        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);

            Pawn pawn = this.Pawn;
            // 60 Tick = 1秒ごとに実行
            if (pawn.IsHashIntervalTick(60))
            {
                DoHeal(pawn);
            }
        }

        private void DoHeal(Pawn pawn)
        {
            // 怪我（Injury）を探す
            var injury = pawn.health.hediffSet.hediffs
                .OfType<Hediff_Injury>()
                .Where(x => x.Visible && !x.IsPermanent())
                .OrderByDescending(x => x.Severity)
                .FirstOrDefault();

            if (injury != null)
            {
                // ★エラー解消ポイント：
                // 直接 injury.IsTended を参照するのではなく、手当てコンポーネントをチェックします。
                // これにより、環境によるプロパティ/メソッドの違いを回避できます。
                var tendComp = injury.TryGetComp<HediffComp_TendDuration>();
                bool isTended = (tendComp != null && tendComp.IsTended);

                // 倍率の決定（手当て済みなら1倍、未治療なら0.7倍）
                float multiplier = isTended ? 1.0f : 0.6f;
                float actualHeal = healAmountPerTick * multiplier;

                // 計算した回復量を適用
                injury.Severity -= actualHeal;

                // 視覚効果
                if (pawn.Map != null)
                {
                    FleckMaker.ThrowMetaIcon(pawn.Position, pawn.Map, FleckDefOf.HealingCross);
                }
                if (Prefs.DevMode)
                {
                    // 画面上に浮き出る文字で表示 (例: "Regen: 0.14 (x0.7)")
                    string debugMote = $"Regen: {actualHeal:F2} (x{multiplier:F1})";
                    Color moteColor = isTended ? Color.green : Color.yellow;
                    MoteMaker.ThrowText(pawn.DrawPos, pawn.Map, debugMote, moteColor);

                    // コンソールログにも詳細を出力
                    // Log.Message($"[RPG Debug] {pawn.LabelShort} - Injury: {injury.Label}, Tended: {isTended}, Heal: {actualHeal:F4}");
                }
            }
        }

        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_Values.Look(ref healAmountPerTick, "healAmountPerTick", 0.1f);
        }

        // 健康タブのツールチップ表示も更新
        public override string CompTipStringExtra
        {
            get
            {
                return $"Healing: {healAmountPerTick:F2} / sec (Untended: x0.7)";
            }
        }
    }
}