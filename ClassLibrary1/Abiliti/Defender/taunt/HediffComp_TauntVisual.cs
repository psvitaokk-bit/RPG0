using UnityEngine;
using Verse;
using RimWorld;

namespace MyRPGMod
{
    // 挑発中の視覚効果を担当するコンポーネント
    public class HediffComp_TauntVisual : HediffComp
    {
        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);

            // 1秒(60Tick)ごとにエフェクト判定
            if (Pawn.IsHashIntervalTick(60))
            {
                if (Pawn.Map != null && !Pawn.Downed && !Pawn.Dead)
                {
                    // 頭上に「激怒」のようなエフェクトを出す
                    // バニラの "Mote_FeedbackAttack" (剣のアイコン) や "MentalStateBroken" などが使えます
                    FleckMaker.ThrowMetaIcon(Pawn.Position, Pawn.Map, FleckDefOf.IncapIcon);
                    // ※IncapIconは赤いドクロですが、適宜好みのFleckDefに変更してください。
                    // Modで専用のアイコンを追加している場合はそれを使います。
                }
            }
        }
    }

    public class HediffCompProperties_TauntVisual : HediffCompProperties
    {
        public HediffCompProperties_TauntVisual()
        {
            this.compClass = typeof(HediffComp_TauntVisual);
        }
    }
}