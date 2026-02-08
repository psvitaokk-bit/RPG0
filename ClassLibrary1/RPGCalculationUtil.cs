using System.Collections.Generic;
using RimWorld;
using Verse;
using UnityEngine;
using System.Linq;

namespace MyRPGMod
{
    // static class にするのがポイント！
    public static class RPGCalculationUtil
    {
        // =============================================================
        //  1. 基礎ステータス計算
        // =============================================================

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

        // 2. 最終倍率の計算
        // （CompRPGから引っ越し！）
        public static float GetMagicMultiplier(Pawn pawn, RPGAbilityDef def)
        {
            // 上で作ったメソッドを使って、まずは魔力強度を出す
            float power = GetMagicPower(pawn);

            // 倍率計算式
            return 1.0f + (power - 1.0f) * def.magicPowerFactor;
        }

        // =============================================================
        //  2. キュア (Cure) 関連
        // =============================================================

        // 病気ランクの判定
        public static int GetDiseaseTier(string defName)
        {
            if (new HashSet<string> { "FibrousMechanites", "SensoryMechanites", "ScariaInfection" }.Contains(defName)) return 4;
            if (new HashSet<string> { "Malaria", "Plague", "Animal_Plague", "SleepingSickness", "LungRot", "LungRotExposure" }.Contains(defName)) return 3;
            if (new HashSet<string> { "GutWorms", "MuscleParasites" }.Contains(defName)) return 2;
            return 1;
        }

        // 成功率の計算
        public static float GetCureSuccessChance(Pawn caster, int diseaseTier)
        {
            if (caster == null) return 0f;

            float medicalLevel = caster.skills?.GetSkill(SkillDefOf.Medicine)?.Level ?? 0f;
            float magicPower = GetMagicPower(caster); // さっき作ったメソッドを再利用！

            // ランクごとの基礎確率
            float baseChance = 0.45f - (diseaseTier * 0.15f);

            // ボーナス加算
            float chance = baseChance + (medicalLevel * 0.03f) + (magicPower * 0.10f);

            return Mathf.Clamp01(chance);
        }

        // レベルごとの治療可能リスト
        public static HashSet<string> GetCurableDefNames(int level)
        {
            HashSet<string> curables = new HashSet<string>();
            if (level >= 1) curables.UnionWith(new[] { "WoundInfection", "Flu", "Animal_Flu", "FoodPoisoning" });
            if (level >= 2) curables.UnionWith(new[] { "GutWorms", "MuscleParasites" });
            if (level >= 3) curables.UnionWith(new[] { "Malaria", "Plague", "Animal_Plague", "SleepingSickness", "LungRot", "LungRotExposure" });
            if (level >= 4) curables.UnionWith(new[] { "FibrousMechanites", "SensoryMechanites", "ScariaInfection" });
            return curables;
        }

        // =============================================================
        //  3. リジェネ (Regenerate) 関連
        // =============================================================

        public static void GetRegenStats(Pawn caster, int abilityLevel, out int durationTicks, out float healAmountPerTick)
        {
            // 持続時間
            durationTicks = 900 + (abilityLevel * 300);

            if (caster != null)
            {
                float medicalLevel = caster.skills?.GetSkill(SkillDefOf.Medicine)?.Level ?? 0f;
                float magicPower = GetMagicPower(caster);

                // 回復量
                float baseHeal = 0.2f + (medicalLevel * 0.02f);
                healAmountPerTick = baseHeal * magicPower;
            }
            else
            {
                healAmountPerTick = 0.2f;
            }
        }

        // =============================================================
        //  4. 意識向上 (Boost) 関連
        // =============================================================

        public static void GetBoostStats(Pawn caster, int abilityLevel, out int durationTicks, out float offsetAmount)
        {
            // 持続時間: 基本30秒(1800) + レベルごとに10秒(600)
            durationTicks = 1800 + (abilityLevel * 600);

            if (caster != null)
            {
                // 魔力倍率を取得
                float magicPower = GetMagicPower(caster);

                // 上昇量: (基本10% + レベル毎5%) * 魔力
                // 例: Lv1(15%) * 魔力1.2 = 18%上昇
                float baseBoost = 0.10f + (abilityLevel * 0.05f);
                offsetAmount = baseBoost * magicPower;
            }
            else
            {
                offsetAmount = 0.1f;
            }
        }
    }
}