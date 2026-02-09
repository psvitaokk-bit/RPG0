// 新しいファイル: RPGAbilityStatWorker.cs
using Verse;
using UnityEngine;

namespace MyRPGMod
{
    // 全ての計算係の親玉
    public abstract class RPGAbilityStatWorker
    {
        // レベルと使用者を受け取って、実際の数値を返す
        public abstract float Calculate(float baseVal, float perLevel, int level, Pawn caster, RPGAbilityDef abilityDef);

        // UI表示用の文字列を作る
        public virtual string GetDisplayString(AbilityStatEntry entry, int level, Pawn caster, RPGAbilityDef abilityDef)
        {
            float val = Calculate(entry.baseValue, entry.valuePerLevel, level, caster, abilityDef);
            return $"{entry.label}: {val.ToString(entry.formatString)}{entry.unit}";
        }
    }
}