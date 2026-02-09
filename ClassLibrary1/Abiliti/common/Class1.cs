using RimWorld;
using Verse;
using UnityEngine;

namespace MyRPGMod
{
    public class Projectile_RPGWater : Projectile
    {
        protected override void Impact(Thing hitThing, bool blockedByShield = false)
        {
            Map map = base.Map; // ベースのImpactで消える前に確保
            base.Impact(hitThing, blockedByShield); // ここで弾自体は消滅する

            if (map == null) return;

            Pawn caster = this.launcher as Pawn;
            if (caster == null) return;

            // 1. 半径の計算
            float baseRadius = this.def.projectile.explosionRadius;
            float magicPower = RPGMagicCalculator.GetMagicPower(caster);

            // 魔力0でも最低限（0.1マス＝直撃のみ）は機能するようにMaxを取る
            float finalRadius = Mathf.Max(baseRadius * magicPower, 1.1f);

            // 2. 爆発（消火）の実行
            GenExplosion.DoExplosion(
                center: this.Position,
                map: map,
                radius: finalRadius,
                damType: DamageDefOf.Extinguish, // 消火ダメージ
                instigator: caster,
                damAmount: 999, // ★修正: 確実に火を消すために特大ダメージを与える
                armorPenetration: -1f,
                weapon: null,
                projectile: this.def,
                intendedTarget: this.intendedTarget.Thing,
                postExplosionSpawnThingDef: ThingDefOf.Filth_Water, // 水汚れ生成
                postExplosionSpawnChance: 1.0f,
                postExplosionSpawnThingCount: 1,
                applyDamageToExplosionCellsNeighbors: true
            );
        }
    }
}
