using Verse;

namespace MyRPGMod
{
    public static class RPGBuffCalculator
    {
        public static void GetBoostStats(Pawn caster, int abilityLevel, out int durationTicks, out float offsetAmount)
        {
            durationTicks = 1800 + (abilityLevel * 600);
            if (caster != null)
            {
                float magicPower = RPGMagicCalculator.GetMagicPower(caster);
                offsetAmount = (0.10f + (abilityLevel * 0.05f)) * magicPower;
            }
            else offsetAmount = 0.1f;
        }
    }
}