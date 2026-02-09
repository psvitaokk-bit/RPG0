using RimWorld;
using Verse;
using UnityEngine;
using System.Linq;

namespace MyRPGMod
{
    public class Verb_RPGMagic : Verb_CastAbility
    {
        // 1. 射程距離 (Range)
        public override float EffectiveRange
        {
            get
            {
                if (CasterPawn == null) return this.verbProps.range;
                float baseRange = this.verbProps.range;
                float magicPower = RPGMagicCalculator.GetMagicPower(CasterPawn);
                return baseRange * magicPower;
            }
        }

        public override bool CanHitTargetFrom(IntVec3 root, LocalTargetInfo targ)
        {
            if (targ.Cell.DistanceTo(root) > EffectiveRange) return false;
            return base.CanHitTargetFrom(root, targ);
        }

        // 2. 消火範囲 (Radius) ★ここを修正
        public float GetDynamicEffectRadius()
        {
            if (CasterPawn == null || ability == null) return 1.9f;

            // ★修正: AbilityDefの直下ではなく、Compのプロパティを見に行く
            float baseRadius = 0f;

            // アビリティが持っている「CompProperties_MagicWaterskip」を探す
            var compProps = ability.def.comps.OfType<CompProperties_MagicWaterskip>().FirstOrDefault();
            if (compProps != null)
            {
                baseRadius = compProps.effectRadius;
            }
            else
            {
                // 見つからなければ標準のEffectRadiusか、デフォルト値を使う
                baseRadius = ability.def.EffectRadius > 0 ? ability.def.EffectRadius : 1.9f;
            }

            float magicPower = RPGMagicCalculator.GetMagicPower(CasterPawn);
            return Mathf.Max(baseRadius * magicPower, 1.1f);
        }

        // 3. UI描画
        public override void DrawHighlight(LocalTargetInfo target)
        {
            GenDraw.DrawRadiusRing(CasterPawn.Position, EffectiveRange);

            if (target.IsValid)
            {
                GenDraw.DrawTargetHighlight(target);
                float radius = GetDynamicEffectRadius();
                if (radius > 0.1f)
                {
                    GenDraw.DrawRadiusRing(target.Cell, radius);
                }
            }
        }
    }
}