using RimWorld;
using Verse;
using System.Linq;
using UnityEngine;

namespace MyRPGMod
{
    // XML設定用
    public class CompProperties_MeleeDamage : CompProperties_AbilityEffect
    {
        public CompProperties_MeleeDamage()
        {
            this.compClass = typeof(CompAbilityEffect_MeleeDamage);
        }
    }

    // 実行部分
    public class CompAbilityEffect_MeleeDamage : CompAbilityEffect
    {
        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);

            Pawn victim = target.Pawn;
            Pawn caster = parent.pawn;

            if (victim != null && caster != null)
            {
                CompRPG rpgComp = caster.GetComp<CompRPG>();
                RPGAbilityDef rpgDef = parent.def as RPGAbilityDef;

                if (rpgComp != null && rpgDef != null)
                {
                    // 1. レベルとWorker（計算係）を使ってダメージ量を計算
                    int currentLevel = rpgComp.GetAbilityLevel(rpgDef);
                    float finalDamage = 10f; // デフォルト

                    var stat = rpgDef.stats.FirstOrDefault(s => s.label == "Melee Damage");
                    if (stat != null)
                    {
                        // 前回のWorkerシステムを導入しているならこれだけでOK！
                        finalDamage = stat.Worker.Calculate(stat.baseValue, stat.valuePerLevel, currentLevel, caster, rpgDef);
                    }


                    DamageInfo dinfo = new DamageInfo(DamageDefOf.Blunt, finalDamage, 0f, -1f, caster);
                    victim.TakeDamage(dinfo);
                    // 攻撃処理の後に追加
                    // 攻撃処理の最後に追加
                    int stunTicks = 60; 

                    // 第3引数(addMote)を false にすると「STUN!」の文字が出なくなるよ
                    // 第4引数(showResultReadout)を false にすると、ログなどの通知も出なくなるよ
                    caster.stances.stunner.StunFor(stunTicks, null, showMote: false, disableRotation: false);

                    // 念のため、AIが「次の攻撃」を考えるのを防ぐために現在のジョブも強制終了させるとより確実だよ
                    if (caster.jobs != null && caster.CurJob != null)
                    {
                        caster.jobs.EndCurrentJob(Verse.AI.JobCondition.InterruptForced);
                    }
                }
            }
        }
    }
}