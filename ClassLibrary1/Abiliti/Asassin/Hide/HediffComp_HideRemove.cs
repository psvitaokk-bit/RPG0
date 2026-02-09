using RimWorld;
using Verse;
using UnityEngine;
using Verse.AI;

namespace MyRPGMod
{
    public class HediffCompProperties_HideRemove : HediffCompProperties
    {
        public HediffCompProperties_HideRemove()
        {
            this.compClass = typeof(HediffComp_HideRemove);
        }
    }

    public class HediffComp_HideRemove : HediffComp
    {
        private int castAbilityCount = 0;
        private Job initialJob = null;
        private Job lastJob = null;
        private bool initialized = false;

        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);

            Pawn pawn = this.parent.pawn;
            if (pawn == null || pawn.CurJob == null) return;

            // 1. 初期化（発動時のジョブを記録）
            if (!initialized)
            {
                initialJob = pawn.CurJob;
                lastJob = pawn.CurJob;
                initialized = true;
                return;
            }

            // 2. 発動時のジョブ（ハイド詠唱など）が終わるまでは何もしない
            if (pawn.CurJob == initialJob) return;

            // ★重要：移動中なら、攻撃ジョブであっても解除しない！
            // これにより「バックスタブのために敵に近づいている間」は透明が維持されます
            if (pawn.pather.Moving) return;

            // --- 3. アビリティ使用（バックスタブ等）の検知 ---
            if (pawn.CurJob.def == JobDefOf.CastAbilityOnThing ||
                pawn.CurJob.def == JobDefOf.CastAbilityOnWorldTile)
            {
                // ジョブが変わった（移動完了して詠唱に入った等）タイミングを検知したいが、
                // JobDef自体はずっと一緒なので、Movingが止まった時点で「使用開始」とみなす

                // ここでは単純に「移動しておらず、アビリティジョブ中なら解除」とします
                // ただし、即解除だと早すぎる場合があるので、構え(Stance)もチェックすると完璧です
                if (pawn.stances.curStance is Stance_Warmup ||
                    pawn.stances.curStance is Stance_Cooldown)
                {
                    RemoveInvisibility();
                    return;
                }
            }
            // --- 4. 通常攻撃（殴り・射撃）の検知 ---
            else if (IsAttacking(pawn.CurJob))
            {
                // 移動していない（＝敵の目の前にいる、または射撃位置にいる）
                // かつ、攻撃の構え（予備動作）に入っているなら解除
                // ※Stanceチェックを入れることで、ただ棒立ちしているだけの時は解除されなくなります
                if (pawn.stances.curStance is Stance_Warmup ||
                    pawn.stances.curStance is Stance_Cooldown)
                {
                    RemoveInvisibility();
                    return;
                }
            }
        }

        private bool IsAttacking(Job job)
        {
            if (job == null || job.def == null) return false;

            // アビリティは別枠で判定しているので除外
            if (job.def == JobDefOf.CastAbilityOnThing ||
                job.def == JobDefOf.CastAbilityOnWorldTile) return false;

            return job.def == JobDefOf.AttackMelee ||
                   job.def == JobDefOf.AttackStatic ||
                   job.def == JobDefOf.UseVerbOnThing ||
                   job.def == JobDefOf.Wait_Combat ||
                   job.def.defName == "AttackMelee";
        }

        private void RemoveInvisibility()
        {
            if (this.parent.pawn.health.hediffSet.HasHediff(this.parent.def))
            {
                this.parent.pawn.health.RemoveHediff(this.parent);
                if (this.parent.pawn.Map != null)
                {
                    // テキストの色を赤っぽくして攻撃による解除であることを強調
                    MoteMaker.ThrowText(this.parent.pawn.DrawPos, this.parent.pawn.Map, "ATTACK!", Color.red);
                }
            }
        }

        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_Values.Look(ref castAbilityCount, "castAbilityCount", 0);
            Scribe_Values.Look(ref initialized, "initialized", false);
        }
    }
}