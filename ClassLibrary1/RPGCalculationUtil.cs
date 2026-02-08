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
        private static readonly HashSet<string> Tier4Diseases = new HashSet<string>
        {
            "FibrousMechanites", "SensoryMechanites", "ScariaInfection"
        };

        private static readonly HashSet<string> Tier3Diseases = new HashSet<string>
        {
            "Malaria", "Plague", "Animal_Plague", "SleepingSickness", "LungRot", "LungRotExposure"
        };

        private static readonly HashSet<string> Tier2Diseases = new HashSet<string>
        {
            "GutWorms", "MuscleParasites"
        };
        private static readonly string[] Tier1List = { "WoundInfection", "Flu", "Animal_Flu", "FoodPoisoning" };
        public static int GetDiseaseTier(string defName)
        {
            if (Tier4Diseases.Contains(defName)) return 4;
            if (Tier3Diseases.Contains(defName)) return 3;
            if (Tier2Diseases.Contains(defName)) return 2;
            return 1;
        }

        // 成功率の計算
        public static float GetCureSuccessChance(Pawn caster, int diseaseTier)
        {
            if (caster == null) return 0f;

            float medicalLevel = caster.skills?.GetSkill(SkillDefOf.Medicine)?.Level ?? 0f;
            float magicPower = GetMagicPower(caster);

            float baseChance = 0.45f - (diseaseTier * 0.15f);
            float chance = baseChance + (medicalLevel * 0.03f) + (magicPower * 0.10f);

            return Mathf.Clamp01(chance);
        }

        // レベルごとの治療可能リスト
        public static HashSet<string> GetCurableDefNames(int level)
        {
            HashSet<string> curables = new HashSet<string>();
            // ここも事前に作ったリストを使い回すようにしたよ！
            if (level >= 1) curables.UnionWith(Tier1List);
            if (level >= 2) curables.UnionWith(Tier2Diseases);
            if (level >= 3) curables.UnionWith(Tier3Diseases);
            if (level >= 4) curables.UnionWith(Tier4Diseases);
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