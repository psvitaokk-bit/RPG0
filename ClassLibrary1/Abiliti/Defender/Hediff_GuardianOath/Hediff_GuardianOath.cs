using Verse;

namespace MyRPGMod
{
    public class Hediff_GuardianOath : HediffWithComps
    {
        public Pawn guardian;
        public float redirectPct = 0.5f;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref guardian, "guardian");
            Scribe_Values.Look(ref redirectPct, "redirectPct", 0.5f);
        }

        public override string LabelBase
        {
            get
            {
                string name = (guardian != null) ? guardian.LabelShort : "Unknown";
                return base.LabelBase + $" ({name})";
            }
        }

        // 守り手が死んだら解除する処理
        public override void Tick()
        {
            base.Tick();
            if (pawn.IsHashIntervalTick(60))
            {
                if (guardian == null || guardian.Dead || !guardian.Spawned || guardian.Map != pawn.Map)
                {
                    pawn.health.RemoveHediff(this);
                }
            }
        }
    }
}