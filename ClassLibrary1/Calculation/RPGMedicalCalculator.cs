using System.Collections.Generic;
using RimWorld;
using Verse;
using UnityEngine;

namespace MyRPGMod
{
    public static class RPGMedicalCalculator
    {
        // 病気データの定義
        private static readonly HashSet<string> Tier4Diseases = new HashSet<string> { "FibrousMechanites", "SensoryMechanites", "ScariaInfection" };
        private static readonly HashSet<string> Tier3Diseases = new HashSet<string> { "Malaria", "Plague", "Animal_Plague", "SleepingSickness", "LungRot", "LungRotExposure" };
        private static readonly HashSet<string> Tier2Diseases = new HashSet<string> { "GutWorms", "MuscleParasites" };
        private static readonly string[] Tier1List = { "WoundInfection", "Flu", "Animal_Flu", "FoodPoisoning" };

        public static int GetDiseaseTier(string defName)
        {
            if (Tier4Diseases.Contains(defName)) return 4;
            if (Tier3Diseases.Contains(defName)) return 3;
            if (Tier2Diseases.Contains(defName)) return 2;
            return 1;
        }

        public static float GetCureSuccessChance(Pawn caster, int diseaseTier)
        {
            if (caster == null) return 0f;
            float medicalLevel = caster.skills?.GetSkill(SkillDefOf.Medicine)?.Level ?? 0f;
            float magicPower = RPGMagicCalculator.GetMagicPower(caster); // 分割したクラスを呼ぶ

            float baseChance = 0.45f - (diseaseTier * 0.15f);
            float chance = baseChance + (medicalLevel * 0.03f) + (magicPower * 0.10f);
            return Mathf.Clamp01(chance);
        }

        public static HashSet<string> GetCurableDefNames(int level)
        {
            HashSet<string> curables = new HashSet<string>();
            if (level >= 1) curables.UnionWith(Tier1List);
            if (level >= 2) curables.UnionWith(Tier2Diseases);
            if (level >= 3) curables.UnionWith(Tier3Diseases);
            if (level >= 4) curables.UnionWith(Tier4Diseases);
            return curables;
        }

        public static void GetRegenStats(Pawn caster, int abilityLevel, out int durationTicks, out float healAmountPerTick)
        {
            durationTicks = 900 + (abilityLevel * 300);
            if (caster != null)
            {
                float medicalLevel = caster.skills?.GetSkill(SkillDefOf.Medicine)?.Level ?? 0f;
                float magicPower = RPGMagicCalculator.GetMagicPower(caster);
                healAmountPerTick = (0.03f + (medicalLevel * 0.02f)) * (magicPower * 0.8f);
            }
            else healAmountPerTick = 0.2f;
        }
    }
}