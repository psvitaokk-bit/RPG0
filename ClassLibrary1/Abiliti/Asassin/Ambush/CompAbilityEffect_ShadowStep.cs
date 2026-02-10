using RimWorld;
using Verse;
using UnityEngine;
using System.Linq;

namespace MyRPGMod
{
    public class CompProperties_ShadowStep : CompProperties_AbilityEffect
    {
        public CompProperties_ShadowStep()
        {
            this.compClass = typeof(CompAbilityEffect_ShadowStep);
        }
    }

    public class CompAbilityEffect_ShadowStep : CompAbilityEffect
    {
        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);

            Pawn targetPawn = target.Thing as Pawn;
            Pawn caster = parent.pawn;

            if (targetPawn == null || caster == null || caster.Map == null) return;

            CompRPG rpgComp = caster.GetComp<CompRPG>();
            RPGAbilityDef rpgDef = parent.def as RPGAbilityDef;

            if (rpgComp == null || rpgDef == null) return;

            Map map = caster.Map;
            int level = rpgComp.GetAbilityLevel(rpgDef);

            // --- 1. 数値計算（固定値Workerを使用） ---
            float stunSeconds = 2.0f;
            var stunStat = rpgDef.stats.FirstOrDefault(s => s.label == "Stun Duration");
            if (stunStat != null)
            {
                // 魔力補正のない Fixed Worker で計算
                stunSeconds = stunStat.Worker.Calculate(stunStat.baseValue, stunStat.valuePerLevel, level, caster, rpgDef);
            }
            int stunTicks = Mathf.RoundToInt(stunSeconds * 60f);

            // --- 2. 背後の位置を計算 ---
            // 敵が向いている方向の「逆」の隣接セルを特定する
            IntVec3 facingDir = targetPawn.Rotation.FacingCell;
            IntVec3 backCell = targetPawn.Position - facingDir;

            // もし背後が壁などで移動不可なら、空いている隣接マスを探す
            if (!backCell.InBounds(map) || !backCell.Walkable(map))
            {
                backCell = IntVec3.Invalid;
                foreach (IntVec3 adj in GenAdj.AdjacentCells.InRandomOrder())
                {
                    IntVec3 temp = targetPawn.Position + adj;
                    if (temp.InBounds(map) && temp.Walkable(map))
                    {
                        backCell = temp;
                        break;
                    }
                }
            }

            // --- 3. 移動とスタン実行 ---
            if (backCell.IsValid)
            {
                // 移動前のエフェクト
                FleckMaker.ThrowDustPuffThick(caster.Position.ToVector3(), map, 1.0f, Color.black);

                // 瞬間移動
                caster.Position = backCell;
                caster.Notify_Teleported(true, false);

                // 移動後のエフェクト（影から現れる演出）
                FleckMaker.ThrowMicroSparks(caster.DrawPos, map);

                // ターゲットをスタンさせる
                targetPawn.stances?.stunner?.StunFor(stunTicks, caster);

                // 演出
                MoteMaker.ThrowText(targetPawn.DrawPos, map, $"SHADOW STEP! ({stunSeconds:F1}s)", Color.white, 2.5f);

                // 暗殺者の硬直（非常に短い）
                caster.stances.stunner.StunFor(30, null, showMote: false);
            }
            else
            {
                Messages.Message("No space to shadow step.", MessageTypeDefOf.RejectInput, false);
            }
        }
    }
}