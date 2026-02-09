using RimWorld;
using Verse;

namespace MyRPGMod
{
    public static class RPGMagicCalculator
    {
        // 魔力強度 (Magic Power) の計算
        public static float GetMagicPower(Pawn pawn)
        {
            if (pawn == null) return 1f;

            float sensitivity = pawn.GetStatValue(StatDefOf.PsychicSensitivity);
            float consciousness = pawn.health.capacities.GetLevel(PawnCapacityDefOf.Consciousness);

            float intellectLevel = 0f;
            SkillRecord skill = pawn.skills?.GetSkill(SkillDefOf.Intellectual);
            if (skill != null) intellectLevel = skill.Level;

            float intellectBonus = 1.0f + (intellectLevel * 0.05f);

            return sensitivity * consciousness * intellectBonus;
        }

        // 最終倍率の計算
        public static float GetMagicMultiplier(Pawn pawn, RPGAbilityDef def)
        {
            float power = GetMagicPower(pawn);
            return 1.0f + (power - 1.0f) * def.magicPowerFactor;
        }
    }
}