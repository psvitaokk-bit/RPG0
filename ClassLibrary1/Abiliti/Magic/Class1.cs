using RimWorld;
using Verse;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Verse.Sound;

namespace MyRPGMod
{
    public class CompAbilityEffect_WindVacuum : CompAbilityEffect
    {
        // XMLから取得する設定（ダメージや範囲など）
        // ※別途 CompProperties クラスが必要ですが、ここでは省略します

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);
            Map map = parent.pawn.Map;
            IntVec3 center = target.Cell;
            float radius = parent.def.EffectRadius; // XMLの範囲


            // 2. 演出：外側から中心へ「吸い込まれる風」のエフェクト
            // 範囲内のセルをランダムに選び、そこから中心へ向かうパーティクルを飛ばす
            int fleckCount = (int)(radius * 15); // 範囲が広いほど多く
            for (int i = 0; i < fleckCount; i++)
            {
                // 範囲内のランダムなセルを取得（簡易的）
                Vector3 randomOffset = new Vector3(Rand.Range(-radius, radius), 0, Rand.Range(-radius, radius));

                // 円形の中に収まるように調整
                if (randomOffset.magnitude > radius) randomOffset = randomOffset.normalized * radius;

                Vector3 startPos = center.ToVector3Shifted() + randomOffset;

                // 中心へのベクトルを計算
                Vector3 velocity = (center.ToVector3Shifted() - startPos).normalized * 2.5f; // スピード調整

                // 煙や埃のエフェクトを、中心へ向かう速度を持たせて生成
                FleckCreationData data = FleckMaker.GetDataStatic(startPos, map, FleckDefOf.DustPuffThick, 1.5f);
                data.velocity = velocity; // ★重要：中心へ向かう速度
                data.rotationRate = Rand.Range(-60, 60);
                map.flecks.CreateFleck(data);
            }

            // 3. SE再生（風の音）
            SoundDefOf.Roof_Collapse.PlayOneShot(new TargetInfo(center, map)); // ドドーンという音（あるいは風系MODの音）

            // 4. 敵の吸い込み処理
            // 中心から半径内の敵を探す
            List<Pawn> victims = map.mapPawns.AllPawnsSpawned
                .Where(p => p.Position.DistanceTo(center) <= radius && p != parent.pawn && !p.Downed && !p.Dead)
                .ToList();

            foreach (Pawn victim in victims)
            {
                // 吸い込み前の位置にエフェクト（残像）を残す
                FleckMaker.ThrowDustPuffThick(victim.Position.ToVector3(), map, 1.0f, Color.white);

                // ★重要：強制移動（ワープ）
                // 完全に中心だと重なってバグるため、中心の周囲1マスにランダム配置
                IntVec3 destCell = center + GenAdj.AdjacentCells[Rand.Range(0, 8)];

                // 壁の中に入らないようにチェック
                if (destCell.InBounds(map) && destCell.Walkable(map))
                {
                    victim.Position = destCell;
                    victim.Notify_Teleported(true, false); // 描画更新
                }

                // ダメージ処理
                DamageInfo dinfo = new DamageInfo(DamageDefOf.Blunt, 15f, 0f, -1, parent.pawn); // 風圧ダメージ
                victim.TakeDamage(dinfo);

                // スタンさせる（吸い込まれてよろめいた演出）
                victim.stances.stunner.StunFor(60, parent.pawn);
            }

            // 画面揺らし（迫力アップ）
            Find.CameraDriver.shaker.DoShake(2.0f);
        }
    }
}