using RimWorld;
using Verse;
using UnityEngine;

namespace MyRPGMod
{
    public class RPGAbilityStatWorker_ParryChance : RPGAbilityStatWorker_Standard
    {
        public override float Calculate(float baseVal, float perLevel, int level, Pawn caster, RPGAbilityDef abilityDef)
        {
            // 基本計算 (XMLのmagicPowerFactorが0なら魔力補正なしの数値が返る)
            float val = base.Calculate(baseVal, perLevel, level, caster, abilityDef);

            // 格闘スキルボーナス (Lv1につき +0.5%)
            if (caster != null && caster.skills != null)
            {
                int meleeLevel = caster.skills.GetSkill(SkillDefOf.Melee).Level;
                val += meleeLevel * 0.005f;
            }

            return Mathf.Clamp(val, 0f, 1.0f);
        }
    }
}