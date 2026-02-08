using RimWorld;
using Verse;
using UnityEngine;
using Verse.Sound;

namespace MyRPGMod
{
    // XML設定用
    public class CompProperties_Boost : CompProperties_AbilityEffect
    {
        public CompProperties_Boost()
        {
            this.compClass = typeof(CompAbilityEffect_Boost);
        }
    }

    // 実行用
    public class CompAbilityEffect_Boost : CompAbilityEffect
    {
        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);
            Pawn targetPawn = target.Pawn;
            Pawn caster = parent.pawn;

            if (targetPawn == null || caster == null) return;

            CompRPG rpgComp = caster.GetComp<CompRPG>();
            RPGAbilityDef rpgDef = parent.def as RPGAbilityDef;
            if (rpgComp == null) return;

            int abilityLevel = rpgComp.GetAbilityLevel(rpgDef);

            // 1. 計算機から数値を取得
            RPGCalculationUtil.GetBoostStats(caster, abilityLevel, out int duration, out float boostAmount);

            // 2. バフを適用
            HediffDef hediffDef = HediffDef.Named("RPG_ConsciousnessBuff");

            // 既にバフがあるなら、一旦消して付け直す（数値更新のため）
            Hediff existing = targetPawn.health.hediffSet.GetFirstHediffOfDef(hediffDef);
            if (existing != null)
            {
                targetPawn.health.RemoveHediff(existing);
            }

            // ★重要：専用クラスとして作成
            Hediff_MagicBoost newHediff = (Hediff_MagicBoost)HediffMaker.MakeHediff(hediffDef, targetPawn);

            // 数値を注入！
            newHediff.offset = boostAmount;

            // 時間制限を設定
            var disappearComp = newHediff.TryGetComp<HediffComp_Disappears>();
            if (disappearComp != null)
            {
                disappearComp.ticksToDisappear = duration;
            }

            targetPawn.health.AddHediff(newHediff);

            // エフェクト
            MoteMaker.ThrowText(targetPawn.DrawPos, targetPawn.Map, $"BOOST! (+{boostAmount.ToStringPercent()})", Color.cyan);
            SoundDefOf.EnergyShield_Reset.PlayOneShot(targetPawn);
        }
    }
}