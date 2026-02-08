using RimWorld;
using Verse;

namespace MyRPGMod
{
    public class RPGAbilityStatWorker_RegenInfo : RPGAbilityStatWorker
    {
        public override float Calculate(float baseVal, float perLevel, int level, Pawn caster, RPGAbilityDef abilityDef)
        {
            // 合計回復量を計算して返す
            if (caster == null) return 0f;
            CompRPG rpgComp = caster.GetComp<CompRPG>();

            float magicPower = rpgComp != null ? rpgComp.MagicPower : 1f;
            float medicalLevel = caster.skills.GetSkill(SkillDefOf.Medicine).Level;

            int durationTicks = 900 + (level * 300);
            float durationSec = durationTicks / 60f;

            float healPerSec = (0.2f + (medicalLevel * 0.02f)) * magicPower;

            return healPerSec * durationSec; // 合計回復量
        }

        public override string GetDisplayString(AbilityStatEntry entry, int level, Pawn caster, RPGAbilityDef abilityDef)
        {
            if (caster == null) return "Total Heal: ???";

            // 計算ロジックの再利用（本来はメソッドに切り出すと綺麗だよ）
            CompRPG rpgComp = caster.GetComp<CompRPG>();
            float magicPower = rpgComp != null ? rpgComp.MagicPower : 1f;
            float medicalLevel = caster.skills.GetSkill(SkillDefOf.Medicine).Level;

            int durationTicks = 900 + (level * 300);
            float durationSec = durationTicks / 60f;
            float healPerSec = (0.2f + (medicalLevel * 0.02f)) * magicPower;
            float totalHeal = healPerSec * durationSec;

            return $"Total Heal: {totalHeal:F1} ({healPerSec:F2}/sec for {durationSec:F0}s)";
        }
    }
}