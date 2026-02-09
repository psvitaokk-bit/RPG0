using RimWorld;
using Verse;
using System.Linq;
using UnityEngine;

namespace MyRPGMod
{
    // XML設定用クラス
    public class CompProperties_Heal : CompProperties_AbilityEffect
    {
        public CompProperties_Heal()
        {
            this.compClass = typeof(CompAbilityEffect_Heal);
        }
    }

    // --- 実行用クラス (Heal) ---
    public class CompAbilityEffect_Heal : CompAbilityEffect
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

                    // 1. 回復可能最大量の計算
                    float finalHeal = 10f;
                    var healStat = rpgDef.stats.FirstOrDefault(s => s.label == "Heal Amount");
                    if (healStat != null)
                    {
                        finalHeal = healStat.Worker.Calculate(healStat.baseValue, healStat.valuePerLevel, currentLevel, caster, rpgDef);
                    }

                    // 2. 最大治療数の計算
                    int maxInjuries = 999;
                    var targetStat = rpgDef.stats.FirstOrDefault(s => s.label == "Max Injuries");
                    if (targetStat != null)
                    {
                        maxInjuries = Mathf.RoundToInt(targetStat.Worker.Calculate(targetStat.baseValue, targetStat.valuePerLevel, currentLevel, caster, rpgDef));
                    }

                    // 3. 治療実行と「実際に回復した量」の取得
                    float totalRecovered = HealInjuries(targetPawn, finalHeal, maxInjuries);

                    // 4. 合計回復量を表示
                    MoteMaker.ThrowText(targetPawn.DrawPos, targetPawn.Map, $"HEAL! (+{totalRecovered:F1})", Color.green, 2f);
                }
            }
        }

        // 戻り値を float に変更し、実際に減らした Severity の合計を返す
        private float HealInjuries(Pawn pawn, float amount, int maxInjuries)
        {
            float remainingHeal = amount;
            float totalHealed = 0f; // 実際に回復した合計値
            int treatedCount = 0;

            var injuries = pawn.health.hediffSet.hediffs
                .OfType<Hediff_Injury>()
                .Where(i => i.CanHealNaturally() && i.Severity > 0)
                .OrderByDescending(i => i.Severity)
                .ToList();

            foreach (var injury in injuries)
            {
                if (remainingHeal <= 0 || treatedCount >= maxInjuries) break;

                // この傷に対して適用する回復量
                float healPower = Mathf.Min(injury.Severity, remainingHeal);

                injury.Severity -= healPower;
                remainingHeal -= healPower;
                totalHealed += healPower; // 合計に加算

                treatedCount++;
            }

            return totalHealed; // 合計値を返す
        }
    }
}