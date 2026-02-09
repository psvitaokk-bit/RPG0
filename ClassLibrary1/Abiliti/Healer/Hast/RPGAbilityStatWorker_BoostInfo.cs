using Verse;
using RimWorld;

namespace MyRPGMod
{
    public class RPGAbilityStatWorker_BoostInfo : RPGAbilityStatWorker
    {
        public override float Calculate(float baseVal, float perLevel, int level, Pawn caster, RPGAbilityDef abilityDef)
        {
            if (caster == null) return 0f;
            // 修正：引数の 'level' を使う。'abilityLevel' ではないよ
            RPGBuffCalculator.GetBoostStats(caster, level, out int durationTicks, out float offset);
            return offset;
        }

        public override string GetDisplayString(AbilityStatEntry entry, int level, Pawn caster, RPGAbilityDef abilityDef)
        {
            if (caster == null) return "Effect: ???";

            // 修正：RPGCalculationUtil ではなく RPGBuffCalculator を呼ぶ
            RPGBuffCalculator.GetBoostStats(caster, level, out int durationTicks, out float offset);

            float durationSec = durationTicks / 60f;
            float moveOffset = offset * 0.4f;

            return $"Boost: +{offset.ToStringPercent()}, Move: +{moveOffset.ToStringPercent()} ({durationSec:F0}s)";
        }
    }
}