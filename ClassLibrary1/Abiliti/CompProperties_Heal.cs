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
                    // 1. レベルによる基本値を計算
                    int currentLevel = rpgComp.GetAbilityLevel(rpgDef);
                    float baseHeal = 10f;

                    var stat = rpgDef.stats.FirstOrDefault(s => s.label == "Heal Amount");
                    if (stat != null)
                    {
                        baseHeal = stat.baseValue + (stat.valuePerLevel * Mathf.Max(0, currentLevel - 1));
                    }

                    // 2. 魔力倍率を考慮した最終回復量を計算
                    float multiplier = rpgComp.GetMagicMultiplier(rpgDef);
                    float finalHeal = baseHeal * multiplier;

                    // 3. ★実際の回復処理を実行★
                    HealInjuries(targetPawn, finalHeal);

                    // 画面上に回復量を表示
                    MoteMaker.ThrowText(targetPawn.DrawPos, targetPawn.Map, $"HEAL! (+{finalHeal:F1})", Color.green, 2f);
                }
            }
        }

        // 傷を治すための補助メソッド
        private void HealInjuries(Pawn pawn, float amount)
        {
            float remainingHeal = amount;

            // ポーンが持っている「怪我(Hediff_Injury)」をリストアップ
            // 古傷(Scars)は除外し、現在進行形で痛んでいる怪我だけを対象にするのが一般的だよ
            var injuries = pawn.health.hediffSet.hediffs
                .OfType<Hediff_Injury>()
                .Where(i => i.CanHealNaturally() && i.Severity > 0)
                .ToList();

            foreach (var injury in injuries)
            {
                if (remainingHeal <= 0) break;

                // 回復させる量（怪我の重症度か、残り回復ポイントの小さい方）
                float healPower = Mathf.Min(injury.Severity, remainingHeal);

                // 重症度を下げる（0になると自動的に消滅するよ）
                injury.Severity -= healPower;
                remainingHeal -= healPower;
            }

        }
    }
}