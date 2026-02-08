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
            if (rpgComp == null) return;

            // --- 計算式 ---
            int abilityLevel = rpgComp.GetAbilityLevel(rpgDef);
            float magicPower = rpgComp.MagicPower;
            float medicalLevel = caster.skills.GetSkill(SkillDefOf.Medicine).Level;

            // 1. 持続時間 (Ticks)
            // 基本15秒(900) + レベルごとに5秒(300)延長
            int duration = 900 + (abilityLevel * 300);

            // 2. 秒間回復量
            // 基本0.2 + (医術Lv * 0.02)
            // 最後に魔力倍率を掛ける
            float baseHeal = 0.2f + (medicalLevel * 0.02f);
            float finalHeal = baseHeal * magicPower;

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