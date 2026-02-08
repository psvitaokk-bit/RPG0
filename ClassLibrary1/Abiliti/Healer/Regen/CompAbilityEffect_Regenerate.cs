using RimWorld;
using Verse;
using UnityEngine;
using Verse.Sound;

namespace MyRPGMod
{
    public class CompAbilityEffect_Regenerate : CompAbilityEffect
    {
        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);
            Pawn targetPawn = target.Pawn;
            Pawn caster = parent.pawn;
            if (targetPawn == null || caster == null) return;

            CompRPG rpgComp = caster.GetComp<CompRPG>();
            RPGAbilityDef rpgDef = parent.def as RPGAbilityDef;
            if (rpgComp == null) return; // ここまではOK

            // ★この行を追加してね！
            // これがないと abilityLevel が何なのか分からなくてエラーになるよ
            int abilityLevel = rpgComp.GetAbilityLevel(rpgDef);

            RPGCalculationUtil.GetRegenStats(caster, abilityLevel, out int duration, out float finalHeal);

            // --- Hediffの適用 ---
            HediffDef hediffDef = HediffDef.Named("RPG_RegenerateBuff");

            // 既にバフがある場合は、一度消して付け直す（数値を更新するため）
            Hediff existing = targetPawn.health.hediffSet.GetFirstHediffOfDef(hediffDef);
            if (existing != null)
            {
                targetPawn.health.RemoveHediff(existing);
            }

            // 新しいバフを作成
            Hediff newHediff = HediffMaker.MakeHediff(hediffDef, targetPawn);

            // Compを取得して、計算した回復量を注入！
            var regenComp = newHediff.TryGetComp<HediffComp_Regenerate>();
            if (regenComp != null)
            {
                regenComp.healAmountPerTick = finalHeal;
            }

            // 時間制限を設定 (HediffComp_Disappears)
            var disappearComp = newHediff.TryGetComp<HediffComp_Disappears>();
            if (disappearComp != null)
            {
                disappearComp.ticksToDisappear = duration;
            }

            // ポーンに追加
            targetPawn.health.AddHediff(newHediff);

            // エフェクト
            MoteMaker.ThrowText(targetPawn.DrawPos, targetPawn.Map, "REGEN!", Color.green);
            SoundDefOf.EnergyShield_Reset.PlayOneShot(targetPawn);
        }
    }
}