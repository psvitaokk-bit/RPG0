using RimWorld;
using Verse;
using System.Linq;

namespace MyRPGMod
{
    public class RPGAbilityStatWorker_HideSpeed : RPGAbilityStatWorker
    {
        // ★エラー解消のために追加: 必須のCalculateメソッド
        // 今回は表示文字列（GetDisplayString）ですべて処理するので、ここは0を返しておけばOK
        public override float Calculate(float baseVal, float perLevel, int level, Pawn caster, RPGAbilityDef abilityDef)
        {
            return 0f;
        }

        // 表示用メソッド（メイン処理）
        public override string GetDisplayString(AbilityStatEntry entry, int level, Pawn caster, RPGAbilityDef abilityDef)
        {
            // 1. Hediff定義の取得
            HediffDef hideDef = DefDatabase<HediffDef>.GetNamed("RPG_HiddenState", false);
            if (hideDef == null || hideDef.stages.NullOrEmpty()) return "???";

            // 2. 現在のレベルに対応するステージを探す
            // (minSeverity が 現在のレベル 以下の中で、最も大きいものを選ぶ)
            // level=1ならminSeverity=1, level=2ならminSeverity=2...
            HediffStage stage = hideDef.stages
                .Where(s => s.minSeverity <= level)
                .OrderByDescending(s => s.minSeverity)
                .FirstOrDefault();

            // レベル1未満や見つからない場合は、とりあえず最初のステージ（Lv1相当）を表示
            if (stage == null) stage = hideDef.stages.FirstOrDefault();
            if (stage == null) return "0 c/s"; // ステージ自体がない場合の安全策

            // 3. 移動速度(MoveSpeed)のボーナス値を取得
            float speedOffset = 0f;
            if (stage.statOffsets != null)
            {
                var speedStat = stage.statOffsets.FirstOrDefault(s => s.stat == StatDefOf.MoveSpeed);
                if (speedStat != null)
                {
                    speedOffset = speedStat.value;
                }
            }

            // 4. テキストとして整形して返す (例: "+0.40 c/s")
            // RimWorldの移動速度は "cells per second (c/s)"
            return $"{speedOffset:+#0.00;-#0.00} c/s";
        }
    }
}