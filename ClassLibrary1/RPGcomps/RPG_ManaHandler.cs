using UnityEngine;
using Verse;

namespace MyRPGMod
{
    public class RPG_ManaHandler
    {
        public float currentMP = 0;

        // 最大MPの計算式（レベルに依存）
        public float GetMaxMP(int level) => 100f + level * 20f;

        public bool TryConsumeMP(float cost)
        {
            if (currentMP >= cost)
            {
                currentMP -= cost;
                return true;
            }
            return false;
        }

        public void RegenMP(int level)
        {
            float max = GetMaxMP(level);
            if (currentMP < max)
            {
                // TickRare(4秒)ごとの回復量
                currentMP = Mathf.Min(currentMP + 4.0f, max);
            }
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref currentMP, "currentMP", 0);
        }
    }
}