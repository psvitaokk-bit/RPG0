using HarmonyLib;
using Verse;
using Verse.AI;
using RimWorld;
using System.Linq;
using UnityEngine;

namespace MyRPGMod
{
    // --- 1. アビリティ効果クラス ---
    public class CompProperties_Hide : CompProperties_AbilityEffect
    {
        public CompProperties_Hide() { this.compClass = typeof(CompAbilityEffect_Hide); }
    }

    public class CompAbilityEffect_Hide : CompAbilityEffect
    {
        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            Pawn caster = parent.pawn;
            if (caster == null) return;

            // 持続時間の計算
            RPGAbilityDef rpgDef = parent.def as RPGAbilityDef;
            CompRPG rpgComp = caster.GetComp<CompRPG>();
            int level = rpgComp.GetAbilityLevel(rpgDef);

            float duration = 15f;
            var stat = rpgDef.stats.FirstOrDefault(s => s.label == "Duration");
            if (stat != null) duration = stat.Worker.Calculate(stat.baseValue, stat.valuePerLevel, level, caster, rpgDef);

            HediffDef hideDef = DefDatabase<HediffDef>.GetNamed("RPG_HiddenState", false);
            if (hideDef == null) return;

            // 重複削除
            var existing = caster.health.hediffSet.GetFirstHediffOfDef(hideDef);
            if (existing != null) caster.health.RemoveHediff(existing);

            // 3. Hediff作成
            Hediff hediff = HediffMaker.MakeHediff(hideDef, caster);

            // ★追加: Severity（強度）にレベルを代入します
            // これにより、XML側で <minSeverity> を使ってレベルごとの性能を分岐できます
            hediff.Severity = (float)level;

            // Disappearsコンポーネント設定
            var disappearComp = hediff.TryGetComp<HediffComp_Disappears>();
            if (disappearComp != null)
            {
                disappearComp.ticksToDisappear = Mathf.RoundToInt(duration * 60f);
            }

            // 4. Hediff付与
            caster.health.AddHediff(hediff);

            MoteMaker.ThrowText(caster.DrawPos, caster.Map, "HIDE", Color.white);
        }
    }
}