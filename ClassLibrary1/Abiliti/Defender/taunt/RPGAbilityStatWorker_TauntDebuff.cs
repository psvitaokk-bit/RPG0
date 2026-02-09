using RimWorld;
using Verse;

namespace MyRPGMod
{
    public class RPGAbilityStatWorker_TauntDebuff : RPGAbilityStatWorker
    {
        public override float Calculate(float baseVal, float perLevel, int level, Pawn caster, RPGAbilityDef abilityDef)
        {
            // Lv3未満なら 0%、Lv3以上なら 20% (0.2) を返す
            return (level >= 3) ? 0.2f : 0f;
        }

        public override string GetDisplayString(AbilityStatEntry entry, int level, Pawn caster, RPGAbilityDef abilityDef)
        {
            float val = Calculate(entry.baseValue, entry.valuePerLevel, level, caster, abilityDef);

            if (level < 3)
            {
                // Lv3未満の場合はグレー文字で予告
                return $"Bonus Dmg: <color=gray>None (Unlock at Lv3)</color>";
            }
            else
            {
                // Lv3以上は強調表示
                return $"Bonus Dmg: <color=yellow>+{val.ToStringPercent()}</color>";
            }
        }
    }
}