using RimWorld;
using Verse;
using System.Linq;
using UnityEngine;

namespace MyRPGMod
{
    public class CompProperties_Tending : CompProperties_AbilityEffect
    {
        public CompProperties_Tending()
        {
            this.compClass = typeof(CompAbilityEffect_Tending);
        }
    }
    public class CompAbilityEffect_Tending : CompAbilityEffect
    {
        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);
            Pawn targetPawn = target.Pawn;
            Pawn caster = parent.pawn;

            if (targetPawn == null || caster == null) return;

            CompRPG rpgComp = caster.GetComp<CompRPG>();
            RPGAbilityDef rpgDef = parent.def as RPGAbilityDef; // マジカルケア自体の定義
            if (rpgComp == null || rpgDef == null) return;

            int level = rpgComp.GetAbilityLevel(rpgDef);

            // 1. マジカルケアの性能計算
            float quality = 0.05f;
            var qualityStat = rpgDef.stats.FirstOrDefault(s => s.label == "Tend Quality");
            if (qualityStat != null)
            {
                quality = qualityStat.Worker.Calculate(qualityStat.baseValue, qualityStat.valuePerLevel, level, caster, rpgDef);
            }
            // クランプ（0%~100%）
            quality = Mathf.Clamp01(quality);

            int maxInjuries = 1;
            var maxStat = rpgDef.stats.FirstOrDefault(s => s.label == "Max Injuries");
            if (maxStat != null)
            {
                maxInjuries = Mathf.RoundToInt(maxStat.Worker.Calculate(maxStat.baseValue, maxStat.valuePerLevel, level, caster, rpgDef));
            }

            // 2. ★同期処理★ RPG_MedicalTouch (Auto Heal) の情報を取得
            bool doAutoHeal = false;
            float autoHealAmount = 0f;
            float autoHealCost = 0f;
            RPGAbilityDef healDef = DefDatabase<RPGAbilityDef>.GetNamedSilentFail("RPG_MedicalTouch");

            if (rpgComp.autoHealEnabled && healDef != null)
            {
                int healLevel = rpgComp.GetAbilityLevel(healDef);
                if (healLevel > 0)
                {
                    doAutoHeal = true;
                    // AutoHealのMPコスト（レベルが上がるとコストが下がる計算例）
                    autoHealCost = Mathf.Max(1f, healDef.manaCost - (healLevel - 1));

                    // AutoHealの回復量計算 (Patch_TendUtility_DoTendと同じロジックまたはWorkerを使用)
                    // ここでは簡易的に Patch 側のロジックを再現します
                    float baseHeal = 1.75f + ((healLevel - 1) * 0.25f);

                    // 医術ボーナス
                    if (caster.skills != null)
                    {
                        baseHeal *= (1.0f + (caster.skills.GetSkill(SkillDefOf.Medicine).Level * 0.05f));
                    }
                    // 魔力ボーナス
                    baseHeal *= RPGMagicCalculator.GetMagicMultiplier(caster, healDef);

                    autoHealAmount = baseHeal;
                }
            }

            // 3. 実行
            ApplyTending(targetPawn, caster, quality, maxInjuries, doAutoHeal, autoHealAmount, autoHealCost);

            MoteMaker.ThrowText(targetPawn.DrawPos, targetPawn.Map, $"TENDED! (Quality: {quality.ToStringPercent()})", Color.cyan, 2f);
        }

        private void ApplyTending(Pawn patient, Pawn caster, float quality, int max, bool doHeal, float healAmount, float healCost)
        {
            int count = 0;
            CompRPG rpgComp = caster.GetComp<CompRPG>();

            var tendableInjuries = patient.health.hediffSet.hediffs
                .OfType<Hediff_Injury>()
                .Where(i => i.TendableNow())
                .OrderByDescending(i => i.Severity)
                .ToList(); // リスト化して固定

            foreach (var injury in tendableInjuries)
            {
                if (count >= max) break;

                // 1. 治療済み状態にする (マジカルケアの効果)
                injury.Tended(quality, 1.0f);
                count++;

                // 2. ★追加回復 (Auto Heal同期)
                // 条件: トグルON かつ MPが足りている場合
                if (doHeal && rpgComp != null)
                {
                    // コスト以上のMPがあるか？
                    if (rpgComp.currentMP >= healCost)
                    {
                        // MP消費
                        rpgComp.TryConsumeMP(healCost);

                        // 実際の回復（Tend Qualityによって補正を掛けるのが一般的）
                        float finalHeal = healAmount * quality;
                        injury.Severity -= finalHeal;

                        // 回復量のエフェクト
                        MoteMaker.ThrowText(patient.DrawPos, patient.Map, $"+{finalHeal:F1}", Color.green);
                    }
                }
            }
        }
    }
}