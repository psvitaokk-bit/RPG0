using RimWorld;
using Verse;
using UnityEngine;
using System.Linq;

namespace MyRPGMod
{
    public class CompProperties_ImmovableStance : CompProperties_AbilityEffect
    {
        public CompProperties_ImmovableStance()
        {
            this.compClass = typeof(CompAbilityEffect_ImmovableStance);
        }
    }

    public class CompAbilityEffect_ImmovableStance : CompAbilityEffect
    {
        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);
            Pawn caster = parent.pawn;
            if (caster == null) return;

            CompRPG rpgComp = caster.GetComp<CompRPG>();
            RPGAbilityDef rpgDef = parent.def as RPGAbilityDef;
            if (rpgComp == null || rpgDef == null) return;

            int level = rpgComp.GetAbilityLevel(rpgDef);

            // 1. 効果時間と軽減率の計算 (固定値Workerを使用)
            float durationSec = 10f;
            float reduction = 0.5f;

            var durStat = rpgDef.stats.FirstOrDefault(s => s.label == "Duration");
            if (durStat != null) durationSec = durStat.Worker.Calculate(durStat.baseValue, durStat.valuePerLevel, level, caster, rpgDef);

            var redStat = rpgDef.stats.FirstOrDefault(s => s.label == "Damage Factor");
            if (redStat != null) reduction = redStat.Worker.Calculate(redStat.baseValue, redStat.valuePerLevel, level, caster, rpgDef);

            // 2. Hediffの付与
            HediffDef def = DefDatabase<HediffDef>.GetNamed("RPG_ImmovableStanceBuff");
            var existing = caster.health.hediffSet.GetFirstHediffOfDef(def);
            if (existing != null) caster.health.RemoveHediff(existing);

            Hediff_ImmovableStance hediff = (Hediff_ImmovableStance)HediffMaker.MakeHediff(def, caster);
            hediff.damageReductionFactor = reduction;

            var disappearComp = hediff.TryGetComp<HediffComp_Disappears>();
            if (disappearComp != null)
            {
                disappearComp.ticksToDisappear = Mathf.RoundToInt(durationSec * 60f);
            }

            caster.health.AddHediff(hediff);

            // 演出
            MoteMaker.ThrowText(caster.DrawPos, caster.Map, $"IRON DEFENSE!", Color.cyan);
        }
    }
}