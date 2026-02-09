using RimWorld;
using Verse;

namespace MyRPGMod
{
    public class RPGAbilityStatWorker_RegenInfo : RPGAbilityStatWorker
    {
        public override float Calculate(float baseVal, float perLevel, int level, Pawn caster, RPGAbilityDef abilityDef)
        {
            if (caster == null) return 0f;

            // 1. RPGCalculationUtil から RPGMedicalCalculator へ変更
            RPGMedicalCalculator.GetRegenStats(caster, level, out int durationTicks, out float healPerSec);

            float durationSec = durationTicks / 60f;
            return healPerSec * durationSec; // 合計回復量
        }

        public override string GetDisplayString(AbilityStatEntry entry, int level, Pawn caster, RPGAbilityDef abilityDef)
        {
            if (caster == null) return "Total Heal: ???";

            RPGMedicalCalculator.GetRegenStats(caster, level, out int durationTicks, out float healPerSec);

            float durationSec = durationTicks / 60f;
            float totalHeal = healPerSec * durationSec;

            // 注釈を追加
            return $"<color=gray>※Untended wounds heal 30% slower</color>\nTotal Heal: {totalHeal:F1} ({healPerSec:F2}/sec)";
        }
    }
}