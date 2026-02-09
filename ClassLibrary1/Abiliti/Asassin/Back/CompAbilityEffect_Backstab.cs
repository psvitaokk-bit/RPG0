using RimWorld;
using Verse;
using UnityEngine;
using System.Linq; // ★これを忘れると .FirstOrDefault が動きません
using Verse.Sound;

namespace MyRPGMod
{
    public class CompProperties_Backstab : CompProperties_AbilityEffect
    {
        public CompProperties_Backstab()
        {
            this.compClass = typeof(CompAbilityEffect_Backstab);
        }
    }

    public class CompAbilityEffect_Backstab : CompAbilityEffect
    {
        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            // base.Apply は演出用なので、ターゲット確認後に呼ぶのが安全ですが、ここでもOK
            base.Apply(target, dest);

            Pawn caster = parent.pawn;

            // ★修正1: target.Pawn というプロパティは存在しない場合があるため、安全な書き方に変更
            Pawn targetPawn = target.Thing as Pawn;

            // 1. 基本チェック
            if (caster == null || targetPawn == null) return;

            // マップが存在しない場合は処理を中断（エラー落ち防止）
            if (caster.Map == null) return;

            if (caster.equipment?.Primary == null)
            {
                // 武器を持っていないと発動できない
                MoteMaker.ThrowText(caster.DrawPos, caster.Map, "Need Weapon!", Color.red);
                return;
            }

            CompRPG rpgComp = caster.GetComp<CompRPG>();
            RPGAbilityDef rpgDef = parent.def as RPGAbilityDef;

            // 定義がない場合は処理中断
            if (rpgComp == null || rpgDef == null) return;

            int level = rpgComp.GetAbilityLevel(rpgDef);

            // 2. ダメージ倍率の取得 (XMLから)
            float baseMult = 1.0f;
            float backstabMult = 2.5f;

            // ★statsがnullの場合のエラー回避を追加
            if (rpgDef.stats != null)
            {
                var statMult = rpgDef.stats.FirstOrDefault(s => s.label == "Backstab Multiplier");
                if (statMult != null)
                {
                    backstabMult = statMult.Worker.Calculate(statMult.baseValue, statMult.valuePerLevel, level, caster, rpgDef);
                }
            }

            // 3. 背後判定 (Backstab Check)
            bool isBackstab = false;

            // 相手の向き(Rotation)と位置関係のチェック
            // North=上, South=下, East=右, West=左
            if (targetPawn.Rotation == Rot4.North && caster.Position.z < targetPawn.Position.z) isBackstab = true;      // 敵が上向き、自分が下にいる
            else if (targetPawn.Rotation == Rot4.South && caster.Position.z > targetPawn.Position.z) isBackstab = true; // 敵が下向き、自分が上にいる
            else if (targetPawn.Rotation == Rot4.East && caster.Position.x < targetPawn.Position.x) isBackstab = true;  // 敵が右向き、自分が左にいる
            else if (targetPawn.Rotation == Rot4.West && caster.Position.x > targetPawn.Position.x) isBackstab = true;  // 敵が左向き、自分が右にいる

            // 状態異常による確定バックスタブ
            if (!targetPawn.Awake() || targetPawn.Downed || targetPawn.stances.stunner.Stunned)
            {
                isBackstab = true;
            }

            // 4. ダメージ計算
            // GetStatValue はステータス (StatDefOf) を使う
            float weaponDmg = 5f; // 素手ダメージなどのデフォルト値
            if (caster.equipment.Primary != null)
            {
                weaponDmg = caster.equipment.Primary.GetStatValue(StatDefOf.MeleeWeapon_AverageDPS);
            }

            float finalMult = isBackstab ? backstabMult : baseMult;
            float finalDamage = weaponDmg * finalMult;

            // 5. ダメージ適用
            // DamageInfo の引数はバージョンによって微妙に異なりますが、標準的な構成は以下の通り
            // (Def, Amount, ArmorPenetration, Angle, Instigator)
            DamageInfo dinfo = new DamageInfo(DamageDefOf.Cut, finalDamage, 0.5f, -1, caster);
            targetPawn.TakeDamage(dinfo);

            // 6. 演出 (エラー落ち対策済み)
            // ターゲットが死亡してMapから消えている可能性があるため、caster.Map を使うか nullチェックを入れる
            if (caster.Map != null)
            {
                if (isBackstab)
                {
                    // 成功時
                    SoundDefOf.Designate_PlanAdd.PlayOneShot(new TargetInfo(targetPawn.Position, caster.Map));
                    MoteMaker.ThrowText(targetPawn.DrawPos, caster.Map, $"BACKSTAB!! ({finalDamage:F0})", Color.red, 3.0f);
                    FleckMaker.ThrowExplosionCell(targetPawn.Position, caster.Map, FleckDefOf.ExplosionFlash, Color.red);
                }
                else
                {
                    // 失敗時（正面）
                    SoundDefOf.Pawn_Melee_Punch_HitPawn.PlayOneShot(new TargetInfo(targetPawn.Position, caster.Map));
                    MoteMaker.ThrowText(targetPawn.DrawPos, caster.Map, $"Hit ({finalDamage:F0})", Color.white, 2.0f);
                }
            }

            // 攻撃後の硬直
            if (caster.stances != null && caster.stances.stunner != null)
            {
                caster.stances.stunner.StunFor(30, null, showMote: false);
            }
        }
    }
}