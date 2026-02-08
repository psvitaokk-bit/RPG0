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
        // 外部（アビリティ）から注入される数値
        public float healAmountPerTick = 0.1f; // 1回あたりの回復量

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
            // 永久的な傷（古傷）は除外して、現在進行形の傷だけを対象にする
            var injury = pawn.health.hediffSet.hediffs
                .OfType<Hediff_Injury>()
                .Where(x => x.Visible && !x.IsPermanent())
                .OrderByDescending(x => x.Severity) // 一番ひどい傷から治す
                .FirstOrDefault();

            if (injury != null)
            {
                // 傷の深刻度を減らす（＝回復）
                injury.Severity -= healAmountPerTick;

                // 視覚効果（緑のキラキラ）
                if (pawn.Map != null)
                {
                    FleckMaker.ThrowMetaIcon(pawn.Position, pawn.Map, FleckDefOf.HealingCross);
                }
            }
        }

        // セーブデータの保存・読み込み
        // これを忘れると、ロードした瞬間に回復量が0になっちゃうよ！
        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_Values.Look(ref healAmountPerTick, "healAmountPerTick", 0.1f);
        }

        // ステータス画面（健康タブ）で、マウスを乗せた時に回復量を表示する
        public override string CompTipStringExtra
        {
            get
            {
                return $"Healing: {healAmountPerTick} / sec";
            }
        }
    }
}