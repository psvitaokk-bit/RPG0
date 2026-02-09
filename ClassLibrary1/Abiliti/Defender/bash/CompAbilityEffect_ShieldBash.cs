using RimWorld;
using Verse;
using UnityEngine;
using System.Linq;

namespace MyRPGMod
{
    public class CompProperties_ShieldBash : CompProperties_AbilityEffect
    {
        public CompProperties_ShieldBash()
        {
            this.compClass = typeof(CompAbilityEffect_ShieldBash);
        }
    }

    public class CompAbilityEffect_ShieldBash : CompAbilityEffect
    {
        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);

            Pawn targetPawn = target.Pawn;
            Pawn caster = parent.pawn;

            // --- 1. 根本的な Null チェック ---
            if (targetPawn == null || caster == null || caster.Map == null) return;

            // 必要なコンポーネントとDefの取得
            CompRPG rpgComp = caster.GetComp<CompRPG>();
            RPGAbilityDef rpgDef = parent.def as RPGAbilityDef;

            // RPGデータかDefが取得できなければ中断
            if (rpgComp == null || rpgDef == null) return;

            // 演出用に現在の情報を保持
            Map map = caster.Map;
            Vector3 targetDrawPos = targetPawn.DrawPos;
            IntVec3 targetCell = targetPawn.Position;
            int level = rpgComp.GetAbilityLevel(rpgDef);

            // --- 2. 数値計算（Worker の Null チェックを強化） ---
            float damageAmount = 10f;
            float stunSeconds = 1.5f;

            if (rpgDef.stats != null)
            {
                // ダメージの計算
                var dmgStat = rpgDef.stats.FirstOrDefault(s => s.label == "Damage");
                if (dmgStat != null && dmgStat.Worker != null)
                {
                    damageAmount = dmgStat.Worker.Calculate(dmgStat.baseValue, dmgStat.valuePerLevel, level, caster, rpgDef);
                }

                // スタン時間の計算
                var stunStat = rpgDef.stats.FirstOrDefault(s => s.label == "Stun Duration");
                if (stunStat != null && stunStat.Worker != null)
                {
                    stunSeconds = stunStat.Worker.Calculate(stunStat.baseValue, stunStat.valuePerLevel, level, caster, rpgDef);
                }
            }
            int stunTicks = Mathf.RoundToInt(stunSeconds * 60f);

            // --- 3. 突進 (ダッシュ) 処理 ---
            IntVec3 bestCell = IntVec3.Invalid;
            // 周囲8マスから移動先を検索
            foreach (IntVec3 offset in GenAdj.AdjacentCells.InRandomOrder())
            {
                IntVec3 cell = targetCell + offset;
                if (cell.InBounds(map) && cell.Walkable(map))
                {
                    bestCell = cell;
                    break;
                }
            }

            if (bestCell.IsValid)
            {
                FleckMaker.ThrowDustPuffThick(caster.Position.ToVector3(), map, 1.0f, Color.white);
                caster.Position = bestCell;
                caster.Notify_Teleported(true, false);
            }

            // --- 4. ダメージとスタンの適用（ターゲット生存チェック） ---

            // ダメージ適用
            DamageInfo dinfo = new DamageInfo(DamageDefOf.Blunt, damageAmount, 0, -1, caster);
            targetPawn.TakeDamage(dinfo);

            // ダメージ後にターゲットが消滅・死亡していないか確認
            if (targetPawn != null && targetPawn.Spawned && !targetPawn.Dead)
            {
                targetPawn.stances?.stunner?.StunFor(stunTicks, caster);
                MoteMaker.ThrowText(targetDrawPos, map, $"BASH! ({stunSeconds:F1}s)", Color.cyan, 2.5f);
            }


            FleckMaker.ThrowMicroSparks(targetDrawPos, map);
            FleckMaker.ThrowExplosionCell(targetCell, map, FleckDefOf.ExplosionFlash, Color.white);
        }
    }
}