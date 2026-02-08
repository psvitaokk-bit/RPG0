using Verse;
using RimWorld;

namespace MyRPGMod
{
    public class RPGAbilityStatWorker_BoostInfo : RPGAbilityStatWorker
    {
        // Calculateは「メインの効果（意識上昇量）」を返すままでOK
        public override float Calculate(float baseVal, float perLevel, int level, Pawn caster, RPGAbilityDef abilityDef)
        {
            if (caster == null) return 0f;
            RPGCalculationUtil.GetBoostStats(caster, level, out int duration, out float offset);
            return offset;
        }

        // 表示文字列の方をリッチにするよ！
        public override string GetDisplayString(AbilityStatEntry entry, int level, Pawn caster, RPGAbilityDef abilityDef)
        {
            if (caster == null) return "Effect: ???";

            // 計算機から数値をもらう
            RPGCalculationUtil.GetBoostStats(caster, level, out int durationTicks, out float offset);

            // 秒数に変換
            float durationSec = durationTicks / 60f;

            // 移動速度の上昇量（意識の0.4倍）を計算
            float moveOffset = offset * 0.4f;

            // 表示形式: "Boost: +50%, Move: +20% (60s)"
            return $"Boost: +{offset.ToStringPercent()}, Move: +{moveOffset.ToStringPercent()} ({durationSec:F0}s)";
        }
    }
}