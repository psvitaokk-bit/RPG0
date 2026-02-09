using RimWorld;
using Verse;
using UnityEngine;

namespace MyRPGMod
{
    public class RPGAbilityStatWorker_ProjectileCount : RPGAbilityStatWorker
    {
        public override float Calculate(float baseVal, float perLevel, int level, Pawn caster, RPGAbilityDef abilityDef)
        {
            // 基本本数 + (レベル-1) * 増加量
            // 例: Base=1, PerLevel=1 なら Lv1=1本, Lv2=2本, Lv3=3本...
            return baseVal + (perLevel * Mathf.Max(0, level - 1));
        }

        public override string GetDisplayString(AbilityStatEntry entry, int level, Pawn caster, RPGAbilityDef abilityDef)
        {
            float val = Calculate(entry.baseValue, entry.valuePerLevel, level, caster, abilityDef);
            return $"Count: {val:F0}";
        }
    }
}