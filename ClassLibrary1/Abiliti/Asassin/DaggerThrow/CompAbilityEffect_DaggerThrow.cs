using RimWorld;
using Verse;
using UnityEngine;
using System.Linq;
using Verse.Sound;

namespace MyRPGMod
{
    public class CompProperties_DaggerThrow : CompProperties_AbilityEffect
    {
        public CompProperties_DaggerThrow()
        {
            this.compClass = typeof(CompAbilityEffect_DaggerThrow);
        }
    }

    public class CompAbilityEffect_DaggerThrow : CompAbilityEffect
    {
        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);
            Pawn caster = parent.pawn;

            if (caster == null || target.Cell == default) return;

            CompRPG rpgComp = caster.GetComp<CompRPG>();
            RPGAbilityDef rpgDef = parent.def as RPGAbilityDef;
            if (rpgComp == null || rpgDef == null) return;

            int level = rpgComp.GetAbilityLevel(rpgDef);

            // 1. 投げる本数を取得
            int shots = 1;
            var countStat = rpgDef.stats.FirstOrDefault(s => s.label == "Dagger Count");
            if (countStat != null)
            {
                shots = Mathf.RoundToInt(countStat.Worker.Calculate(countStat.baseValue, countStat.valuePerLevel, level, caster, rpgDef));
            }

            // 2. 弾の定義を取得
            ThingDef projectileDef = DefDatabase<ThingDef>.GetNamed("RPG_FlyingDagger");

            // 3. ループして発射
            for (int i = 0; i < shots; i++)
            {
                // 弾を生成
                Projectile projectile = (Projectile)GenSpawn.Spawn(projectileDef, caster.Position, caster.Map);

                // ヒットフラグ
                ProjectileHitFlags hitFlags = ProjectileHitFlags.IntendedTarget | ProjectileHitFlags.All;

                // ★修正箇所: ターゲットはずらさない！
                // 確実に当てるため、aimTarget は常に target (敵) に固定します。
                LocalTargetInfo aimTarget = target;

                // 発射位置の微調整 (これで見た目の重なりを防ぎます)
                Vector3 launchPos = caster.DrawPos;

                if (shots > 1)
                {
                    // 扇状に散らしたい場合や、前後左右にランダムに散らしたい場合の計算
                    // ここではシンプルにキャスター周辺からランダムに発射させます
                    launchPos += Vector3Utility.FromAngleFlat(Rand.Range(0, 360)) * Rand.Range(0f, 0.4f);
                }

                // 発射！
                projectile.Launch(
                    caster,
                    launchPos,
                    aimTarget,
                    target,
                    hitFlags,
                    false,
                    null
                );
            }

            // 演出
            SoundDefOf.MetalHitImportant.PlayOneShot(caster);
            MoteMaker.ThrowText(caster.DrawPos, caster.Map, $"THROW! x{shots}", Color.white, 2f);
        }
    }
}