using UnityEngine;
using Verse;
using Verse.AI;
using RimWorld;

namespace MyRPGMod
{
    // ターゲット情報を保存するためのカスタムHediffクラス
    public class Hediff_Taunted : HediffWithComps
    {
        // 挑発してきた相手（キャスター）を保存する変数
        public Pawn tauntTarget;
        public override void PostRemoved()
        {
            base.PostRemoved();

            // ポーンやジョブが存在するかチェック
            if (pawn != null && pawn.jobs != null && pawn.jobs.curJob != null)
            {
                // 「今まさに挑発相手を殴りに行っている最中」であれば中断させる
                if (pawn.jobs.curJob.def == JobDefOf.AttackMelee &&
                    pawn.jobs.curJob.targetA.Thing == tauntTarget)
                {
                    // ジョブを強制終了 (InterruptForced)
                    pawn.jobs.EndCurrentJob(JobCondition.InterruptForced);

                    // 必要であればAI思考をリセットして、すぐに棒立ちにならないようにする
                    // (これをしないと一瞬固まることがありますが、基本は次フレームで索敵が始まります)
                    pawn.mindState.enemyTarget = null;
                }
            }
        }
        public override void Tick()
        {
            base.Tick();

            // 処理軽減のため、毎フレームではなく10Tick(約0.16秒)ごとにチェック
            if (pawn.IsHashIntervalTick(10))
            {
                EnforceTaunt();
            }
            // 視覚効果は60Tickごとに
            if (pawn.IsHashIntervalTick(60))
            {
                DrawTauntEffect();
            }
        }

        private void EnforceTaunt()
        {
            // 前提条件のチェック
            if (pawn.Map == null || pawn.Downed || pawn.Dead || pawn.InMentalState) return;

            // ターゲットが無効（死亡、マップ外など）ならHediffを消去して終了
            if (tauntTarget == null || tauntTarget.Dead || tauntTarget.Map != pawn.Map)
            {
                pawn.health.RemoveHediff(this);
                return;
            }

            // 1. 思考ターゲットの強制固定
            if (pawn.mindState != null)
            {
                pawn.mindState.enemyTarget = tauntTarget;
            }

            // 2. 現在の行動をチェック
            // 「現在、挑発相手を攻撃(または移動)しようとしているか？」
            bool isAttackingTarget = false;
            if (pawn.jobs?.curJob != null)
            {
                // 攻撃ジョブであり、かつターゲットが挑発主である
                if ((pawn.jobs.curJob.def == JobDefOf.AttackMelee || pawn.jobs.curJob.def == JobDefOf.AttackStatic) &&
                    pawn.jobs.curJob.targetA.Thing == tauntTarget)
                {
                    isAttackingTarget = true;
                }
                // すでに待機中(Wait)や戦闘中の移動(Goto)の場合も、ターゲットが合っていれば許容する制御を入れても良いですが、
                // ここではシンプルに「攻撃ジョブでなければ強制」します。
            }

            // 違うことをしているなら、強制的に攻撃ジョブを発行
            if (!isAttackingTarget)
            {
                Job newJob = JobMaker.MakeJob(JobDefOf.AttackMelee, tauntTarget);
                newJob.maxNumMeleeAttacks = 1; // 1回殴ったらまたループでチェックが入る
                newJob.expiryInterval = 1200;
                newJob.checkOverrideOnExpire = true;
                newJob.collideWithPawns = true;

                // 現在の仕事を中断して割り込み実行
                pawn.jobs.TryTakeOrderedJob(newJob, JobTag.Misc);

                // 念のためスタンも解除（バランス調整次第）
                // pawn.stances.stunner.StunFor(0, null); 
            }
        }

        private void DrawTauntEffect()
        {
            // 怒りマークの表示
            FleckMaker.ThrowMetaIcon(pawn.Position, pawn.Map, FleckDefOf.IncapIcon);
        }

        // セーブデータの読み書き（ターゲット情報を保存するため必須）
        public int abilityLevel = 1; // ★追加：発動時のレベルを記憶

        // レベル3以上なら被ダメージを1.2倍にする等の判定用
        public float DamageMultiplier => (abilityLevel >= 3) ? 1.2f : 1.0f;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref tauntTarget, "tauntTarget");
            Scribe_Values.Look(ref abilityLevel, "abilityLevel", 1); // セーブ・ロード対応
        }
    }
}