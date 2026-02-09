using RimWorld;
using Verse;
using Verse.AI;
using UnityEngine;

namespace MyRPGMod
{
    public class CompProperties_Taunt : CompProperties_AbilityEffect
    {
        public CompProperties_Taunt()
        {
            this.compClass = typeof(CompAbilityEffect_Taunt);
        }
    }

    public class CompAbilityEffect_Taunt : CompAbilityEffect
    {
        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);
            Pawn targetPawn = target.Pawn;
            Pawn caster = parent.pawn;

            if (targetPawn == null || caster == null || targetPawn.Dead || targetPawn.Downed) return;
            if (!targetPawn.HostileTo(caster) || targetPawn.InMentalState)
            {
                MoteMaker.ThrowText(targetPawn.DrawPos, targetPawn.Map, "Ignored", Color.gray);
                return;
            }

            CompRPG rpgComp = caster.GetComp<CompRPG>();
            RPGAbilityDef rpgDef = parent.def as RPGAbilityDef;
            if (rpgComp == null || rpgDef == null) return;

            int level = rpgComp.GetAbilityLevel(rpgDef);

            // 1. 効果時間の計算
            int durationTicks = 600;
            var durationStat = rpgDef.stats.FirstOrDefault(s => s.label == "Duration");
            if (durationStat != null)
            {
                float durationSec = durationStat.Worker.Calculate(durationStat.baseValue, durationStat.valuePerLevel, level, caster, rpgDef);
                durationTicks = Mathf.RoundToInt(durationSec * 60f);
            }

            // 2. 専用Hediffの付与
            HediffDef tauntHediffDef = DefDatabase<HediffDef>.GetNamedSilentFail("RPG_TauntedDebuff");
            if (tauntHediffDef != null)
            {
                // 重複削除
                Hediff existing = targetPawn.health.hediffSet.GetFirstHediffOfDef(tauntHediffDef);
                if (existing != null) targetPawn.health.RemoveHediff(existing);

                // カスタムHediffクラスとして作成
                Hediff_Taunted tauntHediff = (Hediff_Taunted)HediffMaker.MakeHediff(tauntHediffDef, targetPawn);

                // ★ここでターゲット（術者）を覚えさせる★
                tauntHediff.tauntTarget = caster;

                // 消滅コンポーネントの設定
                var disappearComp = tauntHediff.TryGetComp<HediffComp_Disappears>();
                if (disappearComp != null)
                {
                    disappearComp.ticksToDisappear = durationTicks;
                }

                targetPawn.health.AddHediff(tauntHediff);
            }

            MoteMaker.ThrowText(targetPawn.DrawPos, targetPawn.Map, $"TAUNTED! ({durationTicks / 60f:F1}s)", Color.red, 2.5f);
            FleckMaker.ThrowMicroSparks(targetPawn.DrawPos, targetPawn.Map);
        }
    }
}