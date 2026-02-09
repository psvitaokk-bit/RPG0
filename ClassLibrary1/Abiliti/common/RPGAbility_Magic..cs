using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using RimWorld;

namespace MyRPGMod
{
    // 設定クラス（変更なし）
    public class CompProperties_MagicWaterskip : CompProperties_AbilityEffect
    {
        public float effectRadius;
        public CompProperties_MagicWaterskip()
        {
            this.compClass = typeof(CompAbilityEffect_MagicWaterskip);
        }
    }

    // 実行クラス
    public class CompAbilityEffect_MagicWaterskip : CompAbilityEffect
    {
        private new CompProperties_MagicWaterskip Props => (CompProperties_MagicWaterskip)this.props;

        // ★修正: 常にVerbと同じ計算式を使う
        private float GetRadius()
        {
            Pawn caster = parent.pawn;
            float baseRadius = Props.effectRadius;
            float magicPower = RPGMagicCalculator.GetMagicPower(caster);
            return Mathf.Max(baseRadius * magicPower, 1.1f);
        }

        private IEnumerable<IntVec3> AffectedCells(LocalTargetInfo target, Map map)
        {
            if (!target.Cell.InBounds(map)) yield break;

            // Verbを参照せずとも、ここで同じ計算を行うことで確実に同期させる
            float radius = GetRadius();

            foreach (IntVec3 item in GenRadial.RadialCellsAround(target.Cell, radius, useCenter: true))
            {
                if (item.InBounds(map) && GenSight.LineOfSightToEdges(target.Cell, item, map, skipFirstCell: true))
                {
                    yield return item;
                }
            }
        }

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);
            Map map = parent.pawn.Map;
            if (map == null) return;

            foreach (IntVec3 cell in AffectedCells(target, map))
            {
                List<Thing> list = cell.GetThingList(map);
                for (int i = list.Count - 1; i >= 0; i--)
                {
                    if (list[i] is Fire) list[i].Destroy();
                }

                if (!cell.Filled(map))
                {
                    FilthMaker.TryMakeFilth(cell, map, ThingDefOf.Filth_Water);
                }
 
            }
        }

        public override void DrawEffectPreview(LocalTargetInfo target)
        {
            GenDraw.DrawFieldEdges(AffectedCells(target, parent.pawn.Map).ToList(), Color.blue);
        }
    }
}