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

    // 実行用クラス
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
                int currentLevel = rpgComp?.GetAbilityLevel(parent.def) ?? 1;

                // ★追加：魔力強度を取得★
                float magicMultiplier = rpgComp?.MagicPower ?? 1.0f;
                if (currentLevel < 1) currentLevel = 1;

                // XMLの <stats> から "Heal Amount" を探す
                float baseHealAmount = 5f;
                RPGAbilityDef rpgDef = parent.def as RPGAbilityDef;
                if (rpgDef?.stats != null)
                {
                    var stat = rpgDef.stats.FirstOrDefault(s => s.label == "Heal Amount");
                    if (stat != null)
                    {
                        baseHealAmount = stat.baseValue + (stat.valuePerLevel * (currentLevel - 1));
                    }
                }

                // ★最終計算：基本量 × 魔力強度★
                // 例：基本10 × 魔力1.5 = 15回復！
                float finalHealAmount = baseHealAmount * magicMultiplier;

                // 実際の回復処理（finalHealAmount を使う）
                float remainingHeal = finalHealAmount;
                // ... (中略) ...
                // 自然治癒可能な怪我のリストを取得
                var injuries = targetPawn.health.hediffSet.hediffs
                    .OfType<Hediff_Injury>()
                    .Where(i => i.CanHealNaturally())
                    .ToList();

                foreach (var injury in injuries)
                {
                    if (remainingHeal <= 0) break;

                    float healPower = Mathf.Min(injury.Severity, remainingHeal);
                    injury.Severity -= healPower;
                    remainingHeal -= healPower;
                }

                // 画面上に回復量を表示
                MoteMaker.ThrowText(targetPawn.DrawPos, targetPawn.Map, $"HEAL! (+{finalHealAmount:F1})", Color.green, 2f);
            }
        }
    }
}