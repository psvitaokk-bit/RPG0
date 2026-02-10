using Verse;
using RimWorld;
using System.Collections.Generic;

namespace MyRPGMod
{
    public class Hediff_ImmovableStance : HediffWithComps
    {
        // XMLから注入、または計算機から渡される軽減率（例: 0.5f ならダメージ50%カット）
        public float damageReductionFactor = 0.5f;

        private HediffStage cachedStage;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref damageReductionFactor, "damageReductionFactor", 0.5f);
        }

        public override HediffStage CurStage
        {
            get
            {
                if (cachedStage == null)
                {
                    cachedStage = new HediffStage();
                    cachedStage.statOffsets = new List<StatModifier>();

                    // 1. 移動速度を大幅に下げる (例: -3.0 c/s)
                    cachedStage.statOffsets.Add(new StatModifier
                    {
                        stat = StatDefOf.MoveSpeed,
                        value = -3.0f
                    });

                    // 2. 被ダメージ係数を設定
                    // 0.4f なら受けるダメージが 40% になる（60%カット）
                    cachedStage.statFactors = new List<StatModifier>();
                    cachedStage.statFactors.Add(new StatModifier
                    {
                        stat = StatDef.Named("IncomingDamageFactor"),
                        value = damageReductionFactor
                    });
                }
                return cachedStage;
            }
        }

        public override string LabelInBrackets
        {
            get
            {
                var disappear = this.TryGetComp<HediffComp_Disappears>();
                string timeStr = (disappear != null) ? $", {(disappear.ticksToDisappear / 60f):F0}s" : "";

                return $"Damage x{damageReductionFactor.ToStringPercent()}{timeStr}";
            }
        }

    }
}