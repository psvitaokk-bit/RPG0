using Verse;
using RimWorld;
using UnityEngine;

namespace MyRPGMod
{
    public class RPGAbilityStatWorker_TendQuality : RPGAbilityStatWorker_Standard
    {
        public override float Calculate(float baseVal, float perLevel, int level, Pawn caster, RPGAbilityDef abilityDef)
        {
            // 標準計算（基礎値 + レベル成長 * 魔力倍率）
            float val = base.Calculate(baseVal, perLevel, level, caster, abilityDef);

            // 医術スキルによるボーナス（Lv1につき2%加算など）
            if (caster?.skills != null)
            {
                int medical = caster.skills.GetSkill(SkillDefOf.Medicine).Level;
                val += (medical * 0.01f);
            }

            // 品質は 0% ～ 100% の間にクランプ
            return Mathf.Clamp(val, 0f, 1.0f);
        }
    }
}