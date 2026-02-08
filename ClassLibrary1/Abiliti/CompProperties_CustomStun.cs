using RimWorld;
using Verse;
using System.Linq;
using UnityEngine;

namespace MyRPGMod
{
    // --- 1. 設定用クラス ---
    public class CompProperties_CustomStun : CompProperties_AbilityEffect
    {
        public CompProperties_CustomStun()
        {
            this.compClass = typeof(CompAbilityEffect_CustomStun);
        }
    }

    // --- 2. 実行用クラス ---
    public class CompAbilityEffect_CustomStun : CompAbilityEffect
    {
        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);

            Pawn victim = target.Pawn;
            Pawn caster = parent.pawn;

            if (victim != null && caster != null)
            {
                CompRPG rpgComp = caster.GetComp<CompRPG>();
                if (rpgComp == null) return;

                int currentLevel = rpgComp.GetAbilityLevel(parent.def);
                if (currentLevel < 1) currentLevel = 1;

                RPGAbilityDef rpgDef = parent.def as RPGAbilityDef;
                if (rpgDef == null) return;

                // 1. 基本となる持続時間をXMLの <stats> から取得
                float durationSeconds = 3f;
                if (rpgDef.stats != null)
                {
                    var stat = rpgDef.stats.FirstOrDefault(s => s.label == "Stun Duration");
                    if (stat != null)
                    {
                        durationSeconds = stat.baseValue + (stat.valuePerLevel * (currentLevel - 1));
                    }
                }

                // 2. ★魔力倍率を適用★
                // ポーンの「魔力強度」とアビリティの「影響度」を計算した倍率を取得
                float multiplier = rpgComp.GetMagicMultiplier(rpgDef);
                float finalDurationSeconds = durationSeconds * multiplier;

                // 3. 秒をTicksに変換 (1秒 = 60Ticks)
                int finalTicks = Mathf.RoundToInt(finalDurationSeconds * 60f);

                // 4. 実行
                victim.stances.stunner.StunFor(finalTicks, caster, true, true);

                // 5. エフェクト表示 (計算後の最終的な秒数を表示)
                MoteMaker.ThrowText(victim.DrawPos, victim.Map, $"STUN! ({finalDurationSeconds:F1}s)", 2f);
            }
        }
    }
}