using Verse;
using RimWorld;
using System.Collections.Generic;

namespace MyRPGMod
{
    public class Hediff_MagicBoost : HediffWithComps
    {
        public float offset = 0.1f; // これが意識の上昇量

        private HediffStage cachedStage;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref offset, "offset", 0.1f);

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                cachedStage = null;
            }
        }

        public override HediffStage CurStage
        {
            get
            {
                if (cachedStage == null)
                {
                    cachedStage = new HediffStage();
                    cachedStage.capMods = new List<PawnCapacityModifier>
                    {
                        // 1. 意識の上昇 (そのまま)
                        new PawnCapacityModifier
                        {
                            capacity = PawnCapacityDefOf.Consciousness,
                            offset = this.offset
                        },
                        // 2. ★追加：運動能力の上昇 (0.4倍)
                        new PawnCapacityModifier
                        {
                            capacity = PawnCapacityDefOf.Moving,
                            offset = this.offset * 0.4f
                        }
                    };
                }

                return cachedStage;
            }
        }

        // ラベル表示も少しリッチにして、移動速度も上がってることを分かるようにしよう！
        public override string LabelInBrackets
        {
            get
            {
                // 残り時間を計算 (1秒 = 60Tick)
                var disappear = this.TryGetComp<HediffComp_Disappears>();
                string timeStr = (disappear != null) ? $", {(disappear.ticksToDisappear / 60f):F0}s" : "";

                return $"+{offset.ToStringPercent()}, Move +{(offset * 0.4f).ToStringPercent()}{timeStr}";
            }
        }
    }
}