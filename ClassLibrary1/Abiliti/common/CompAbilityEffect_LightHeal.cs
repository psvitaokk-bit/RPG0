using RimWorld;
using Verse;
using System.Linq;
using UnityEngine;
using Verse.Sound;

namespace MyRPGMod
{
    public class CompProperties_LightHeal : CompProperties_AbilityEffect
    {
        public CompProperties_LightHeal()
        {
            this.compClass = typeof(CompAbilityEffect_LightHeal);
        }
    }

    public class CompAbilityEffect_LightHeal : CompAbilityEffect
    {
        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);
            Pawn targetPawn = target.Pawn;
            Pawn caster = parent.pawn;

            if (targetPawn != null && caster != null)
            {
                CompRPG rpgComp = caster.GetComp<CompRPG>();
                RPGAbilityDef rpgDef = parent.def as RPGAbilityDef;

                if (rpgComp != null && rpgDef != null)
                {
                    int currentLevel = rpgComp.GetAbilityLevel(rpgDef);

                    // 1. 回復量の計算 (控えめな基礎値)
                    float healAmount = 5f;
                    var healStat = rpgDef.stats.FirstOrDefault(s => s.label == "Heal Amount");
                    if (healStat != null)
                    {
                        healAmount = healStat.Worker.Calculate(healStat.baseValue, healStat.valuePerLevel, currentLevel, caster, rpgDef);
                    }

                    // 2. 最大治療数の計算 (汎用のため少なめ)
                    int maxInjuries = 1;
                    var targetStat = rpgDef.stats.FirstOrDefault(s => s.label == "Max Injuries");
                    if (targetStat != null)
                    {
                        maxInjuries = Mathf.RoundToInt(targetStat.Worker.Calculate(targetStat.baseValue, targetStat.valuePerLevel, currentLevel, caster, rpgDef));
                    }

                    // 3. 治療済み（Tended）の傷のみを回復
                    float totalRecovered = HealTendedInjuries(targetPawn, healAmount, maxInjuries);

                    if (totalRecovered > 0)
                    {
                        MoteMaker.ThrowText(targetPawn.DrawPos, targetPawn.Map, $"Light Heal! (+{totalRecovered:F1})", Color.green, 2f);
                        SoundDefOf.EnergyShield_Reset.PlayOneShot(new TargetInfo(targetPawn.Position, targetPawn.Map));
                    }
                    else
                    {
                        // 治療済みの傷がない場合の通知
                        MoteMaker.ThrowText(targetPawn.DrawPos, targetPawn.Map, "No tended injuries", Color.gray, 2f);
                    }
                }
            }
        }

        private float HealTendedInjuries(Pawn pawn, float amount, int maxInjuries)
        {
            float remainingHeal = amount;
            float totalHealed = 0f;
            int treatedCount = 0;

            // 治療済みの傷のみをSeverityの高い順に取得
            var injuries = pawn.health.hediffSet.hediffs
                .OfType<Hediff_Injury>()
                .Where(i => i.Severity > 0 && IsTended(i))
                .OrderByDescending(i => i.Severity)
                .ToList();

            foreach (var injury in injuries)
            {
                if (remainingHeal <= 0 || treatedCount >= maxInjuries) break;

                float healPower = Mathf.Min(injury.Severity, remainingHeal);
                injury.Severity -= healPower;
                remainingHeal -= healPower;
                totalHealed += healPower;
                treatedCount++;
            }

            return totalHealed;
        }

        // 治療済みかどうかの判定 (バニラのコンポーネントを参照)
        private bool IsTended(Hediff_Injury injury)
        {
            var tendComp = injury.TryGetComp<HediffComp_TendDuration>();
            return tendComp != null && tendComp.IsTended;
        }
    }
}