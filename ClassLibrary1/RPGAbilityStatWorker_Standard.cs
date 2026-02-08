// 新しいファイル: RPGAbilityStatWorker_Standard.cs
using Verse;
using UnityEngine;

namespace MyRPGMod
{
    // 今までの計算ロジックをここにカプセル化！
    public class RPGAbilityStatWorker_Standard : RPGAbilityStatWorker
    {
        public override float Calculate(float baseVal, float perLevel, int level, Pawn caster, RPGAbilityDef abilityDef)
        {
            // 1. 基本値の計算
            float val = baseVal + (perLevel * Mathf.Max(0, level - 1));

            // 2. 魔力倍率の適用
            // CompRPGを持っていない場合や、casterがnullの場合は倍率1.0とする
            CompRPG comp = caster?.GetComp<CompRPG>();
            if (comp != null)
            {
                float multiplier = comp.GetMagicMultiplier(abilityDef);
                val *= multiplier;
            }

            return val;
        }
    }
    
    // おまけ：魔力の影響を受けない固定値用のWorker
    public class RPGAbilityStatWorker_Fixed : RPGAbilityStatWorker
    {
        public override float Calculate(float baseVal, float perLevel, int level, Pawn caster, RPGAbilityDef abilityDef)
        {
            // レベルだけで決まり、魔力補正を受けない（例：範囲、消費MPなど）
            return baseVal + (perLevel * Mathf.Max(0, level - 1));
        }
    }
}