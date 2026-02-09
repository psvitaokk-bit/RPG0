using RimWorld;
using Verse;
using System.Linq;
using UnityEngine;

namespace MyRPGMod
{
    public class CompProperties_GuardianOath : CompProperties_AbilityEffect
    {
        public CompProperties_GuardianOath()
        {
            this.compClass = typeof(CompAbilityEffect_GuardianOath);
        }
    }
    public class CompAbilityEffect_GuardianOath : CompAbilityEffect
    {
        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);
            Pawn targetPawn = target.Pawn;
            Pawn caster = parent.pawn;

            if (targetPawn == null || caster == null) return;

            CompRPG rpgComp = caster.GetComp<CompRPG>();
            RPGAbilityDef rpgDef = parent.def as RPGAbilityDef;
            if (rpgComp == null || rpgDef == null) return;

            int level = rpgComp.GetAbilityLevel(rpgDef);

            // --- 1. 軽減率の計算 ---
            float pct = 0.3f;
            var stat = rpgDef.stats.FirstOrDefault(s => s.label == "Redirect Pct");
            if (stat != null)
            {
                pct = stat.Worker.Calculate(stat.baseValue, stat.valuePerLevel, level, caster, rpgDef);
            }
            if (pct > 0.9f) pct = 0.9f;

            // --- 2. ★修正★ 効果時間(秒)の計算 ---
            // XMLの数値をそのまま「秒数」として扱います
            float durationSeconds = 60.0f; // デフォルト60秒
            var statDur = rpgDef.stats.FirstOrDefault(s => s.label == "Duration");
            if (statDur != null)
            {
                durationSeconds = statDur.Worker.Calculate(statDur.baseValue, statDur.valuePerLevel, level, caster, rpgDef);
            }

            // ★変換: 1秒 = 60 Tick
            int durationTicks = Mathf.RoundToInt(durationSeconds * 60f);

            // --- 3. Hediffの適用 ---
            HediffDef def = DefDatabase<HediffDef>.GetNamed("RPG_GuardianOathBuff");

            var existing = targetPawn.health.hediffSet.GetFirstHediffOfDef(def);
            if (existing != null) targetPawn.health.RemoveHediff(existing);

            Hediff_GuardianOath hediff = (Hediff_GuardianOath)HediffMaker.MakeHediff(def, targetPawn);
            hediff.guardian = caster;
            hediff.redirectPct = pct;
            hediff.Severity = 1.0f;

            // 消滅時間をセット
            var disappearComp = hediff.TryGetComp<HediffComp_Disappears>();
            if (disappearComp != null)
            {
                disappearComp.ticksToDisappear = durationTicks;
            }

            targetPawn.health.AddHediff(hediff);

            // ★修正★ 演出: 秒数で表示 (例: "Oath: 120s")
            MoteMaker.ThrowText(targetPawn.DrawPos, targetPawn.Map, $"Oath: {durationSeconds:F0}s", Color.blue, 3f);
            FleckMaker.ThrowMetaIcon(targetPawn.Position, targetPawn.Map, FleckDefOf.PsycastAreaEffect);
        }
    }
}