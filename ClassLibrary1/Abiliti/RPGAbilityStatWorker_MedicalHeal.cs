// 新しいファイル: RPGAbilityStatWorker_MedicalHeal.cs
using Verse;
using RimWorld;
using UnityEngine;

namespace MyRPGMod
{
    // 標準の「魔力補正」に加えて「医術スキル補正」を乗せる計算係
    public class RPGAbilityStatWorker_MedicalHeal : RPGAbilityStatWorker_Standard
    {
        public override float Calculate(float baseVal, float perLevel, int level, Pawn caster, RPGAbilityDef abilityDef)
        {
            // 1. まずは標準の計算（基礎値 + レベル成長 × 魔力倍率）をさせる
            float val = base.Calculate(baseVal, perLevel, level, caster, abilityDef);

            // 2. 医術スキルによるボーナスを計算
            if (caster != null && caster.skills != null)
            {
                // 医術スキルを取得（SkillDefOf.Medicine）
                int medicalLevel = caster.skills.GetSkill(SkillDefOf.Medicine).Level;

                // 例：1レベルにつき +5% の回復量ボーナス
                // レベル20なら +100%（2倍）になる計算だよ
                float medicalBonus = 1.0f + (medicalLevel * 0.05f);

                val *= medicalBonus;
            }

            return val;
        }
    }
}