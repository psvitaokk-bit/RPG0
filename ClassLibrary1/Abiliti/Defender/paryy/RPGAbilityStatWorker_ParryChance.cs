using RimWorld;
using Verse;
using UnityEngine;

namespace MyRPGMod
{
    public class RPGAbilityStatWorker_ParryChance : RPGAbilityStatWorker_Standard
    {
        public override float Calculate(float baseVal, float perLevel, int level, Pawn caster, RPGAbilityDef abilityDef)
        {
            // 1. 基本計算 (XMLのBase + レベル補正)
            float val = base.Calculate(baseVal, perLevel, level, caster, abilityDef);

            if (caster != null)
            {
                // 2. 格闘スキルボーナス (Lv1につき +0.5%)
                if (caster.skills != null)
                {
                    int meleeLevel = caster.skills.GetSkill(SkillDefOf.Melee).Level;
                    val += meleeLevel * 0.005f;
                }

                // 3. 生体能力による補正 (意識と移動能力)
                // GetLevelは通常 1.0 (100%) を返しますが、怪我をしていると下がります。
                float consciousness = caster.health.capacities.GetLevel(PawnCapacityDefOf.Consciousness);
                float moving = caster.health.capacities.GetLevel(PawnCapacityDefOf.Moving);

                // 意識と移動能力を掛ける
                // 例: 確率20% * 意識0.5(朦朧) * 移動0.8(足に傷) = 最終確率 8%
                val *= consciousness * moving;
            }

            // 0% ～ 100% の範囲に収める
            return Mathf.Clamp(val, 0f, 1.0f);
        }
    }
}