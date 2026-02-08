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
        // ★削除：GetDiseaseTier と GetCurableDefNames はもう不要なので消す

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

            // ★修正：Utilからリストを取得
            HashSet<string> curableNames = RPGCalculationUtil.GetCurableDefNames(abilityLevel);

            // ターゲットが持っている「今治せる病気」の中で一番ランクが高いものを探す
            int highestTier = 0;
            List<Hediff> targetHediffs = targetPawn.health.hediffSet.hediffs
                .Where(h => curableNames.Contains(h.def.defName) && h.Visible).ToList();

            if (!targetHediffs.Any())
            {
                MoteMaker.ThrowText(targetPawn.DrawPos, targetPawn.Map, "No curable disease", Color.gray);
                return;
            }

            // ★修正：Utilでランク判定
            highestTier = targetHediffs.Max(h => RPGCalculationUtil.GetDiseaseTier(h.def.defName));

            // ★修正：Utilで成功率計算
            float successChance = RPGCalculationUtil.GetCureSuccessChance(caster, highestTier);

            // 判定
            if (Rand.Value > successChance)
            {
                MoteMaker.ThrowText(targetPawn.DrawPos, targetPawn.Map, $"FAILED ({successChance.ToStringPercent()})", Color.red);
                return;
            }

            // 成功処理
            foreach (Hediff h in targetHediffs)
            {
                targetPawn.health.RemoveHediff(h);
            }
            MoteMaker.ThrowText(targetPawn.DrawPos, targetPawn.Map, "CURED!", Color.cyan);
            if (rpgDef.soundImpact != null) rpgDef.soundImpact.PlayOneShot(targetPawn);
            else SoundDefOf.EnergyShield_Reset.PlayOneShot(targetPawn);
        }
    }
}