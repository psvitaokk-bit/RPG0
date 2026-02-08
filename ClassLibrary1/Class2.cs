using System.Collections.Generic;
using RimWorld;
using Verse;
using UnityEngine;
using Verse.Sound;
using System.Linq;


namespace MyRPGMod
{
    public class CompProperties_Cure : CompProperties_AbilityEffect
    {
        public CompProperties_Cure()
        {
            this.compClass = typeof(CompAbilityEffect_Cure);
        }
    }

    public class CompAbilityEffect_Cure : CompAbilityEffect
    {
        // レベルごとの治療可能リストを作るメソッド
        // 静的(static)にしておくと、UI表示用のWorkerからも呼び出せて便利だよ！
        public static int GetDiseaseTier(string defName)
        {
            if (new[] { "FibrousMechanites", "SensoryMechanites", "ScariaInfection" }.Contains(defName)) return 4;
            if (new[] { "Malaria", "Plague", "Animal_Plague", "SleepingSickness", "LungRot", "LungRotExposure" }.Contains(defName)) return 3;
            if (new[] { "GutWorms", "MuscleParasites" }.Contains(defName)) return 2;
            return 1; // それ以外（インフルエンザなど）はランク1
        }
        public static HashSet<string> GetCurableDefNames(int level)
        {
            HashSet<string> curables = new HashSet<string>();

            // Lv 1: 基本
            if (level >= 1)
            {
                curables.Add("WoundInfection");
                curables.Add("Flu");
                curables.Add("Animal_Flu");
                curables.Add("FoodPoisoning");
            }
            // Lv 2: 寄生虫
            if (level >= 2)
            {
                curables.Add("GutWorms");
                curables.Add("MuscleParasites");
            }
            // Lv 3: 重篤な病気
            if (level >= 3)
            {
                curables.Add("Malaria");
                curables.Add("Plague");
                curables.Add("Animal_Plague");
                curables.Add("SleepingSickness");
                curables.Add("LungRot");
                curables.Add("LungRotExposure");
            }
            // Lv 4: 機械化・特殊
            if (level >= 4)
            {
                curables.Add("FibrousMechanites");
                curables.Add("SensoryMechanites");
                curables.Add("ScariaInfection");
            }

            return curables;
        }

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);
            Pawn targetPawn = target.Pawn;
            Pawn caster = parent.pawn;
            if (targetPawn == null || caster == null) return;

            CompRPG rpgComp = caster.GetComp<CompRPG>();
            RPGAbilityDef rpgDef = parent.def as RPGAbilityDef;
            if (rpgComp == null || rpgDef == null) return;

            int abilityLevel = rpgComp.GetAbilityLevel(rpgDef);
            HashSet<string> curableNames = GetCurableDefNames(abilityLevel);

            // ターゲットが持っている「今治せる病気」の中で一番ランクが高いものを探す
            int highestTier = 0;
            List<Hediff> targetHediffs = targetPawn.health.hediffSet.hediffs
                .Where(h => curableNames.Contains(h.def.defName) && h.Visible).ToList();

            if (!targetHediffs.Any())
            {
                MoteMaker.ThrowText(targetPawn.DrawPos, targetPawn.Map, "No curable disease", Color.gray);
                return;
            }

            highestTier = targetHediffs.Max(h => GetDiseaseTier(h.def.defName));

            // --- 成功率の計算 ---
            float medicalLevel = caster.skills.GetSkill(SkillDefOf.Medicine).Level;
            float magicPower = rpgComp.MagicPower;

            // 基本確率の計算
            // ランク1: 30%, ランク2: 15%, ランク3: 0%, ランク4: -15% （例）
            float baseChance = 0.45f - (highestTier * 0.15f);

            // スキルと魔力による加算
            float successChance = baseChance + (medicalLevel * 0.03f) + (magicPower * 0.10f);
            successChance = Mathf.Clamp01(successChance);

            // 判定
            if (Rand.Value > successChance)
            {
                MoteMaker.ThrowText(targetPawn.DrawPos, targetPawn.Map, $"FAILED ({successChance.ToStringPercent()})", Color.red);
                return;
            }

            // 成功：対象の全病気を除去（または一番高いランクのものだけ除去にするならここを調整）
            foreach (Hediff h in targetHediffs)
            {
                targetPawn.health.RemoveHediff(h);
            }
            MoteMaker.ThrowText(targetPawn.DrawPos, targetPawn.Map, "CURED!", Color.cyan);
            SoundDefOf.EnergyShield_Reset.PlayOneShot(targetPawn);
        }
    }
}