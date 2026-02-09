using RimWorld;
using UnityEngine; // Mathf.Powを使うために必要
using Verse;

namespace MyRPGMod
{
    public static class RPGMagicCalculator
    {
        public static float GetMagicPower(Pawn pawn)
        {
            if (pawn == null) return 1f;

            float sensitivity = pawn.GetStatValue(StatDefOf.PsychicSensitivity);
            float consciousness = pawn.health.capacities.GetLevel(PawnCapacityDefOf.Consciousness);

            float intellectLevel = 0f;
            SkillRecord skill = pawn.skills?.GetSkill(SkillDefOf.Intellectual);
            if (skill != null) intellectLevel = skill.Level;

            float intellectBonus = 0f;

            // --- 計算ロジックの分岐 ---

            // パターンA: Lv 0 ～ 2 (才能なし)
            if (intellectLevel <= 2)
            {
                intellectBonus = 0f;
            }
            // パターンB: Lv 3 ～ 8 (成長期：二次関数でカーブさせる)
            else if (intellectLevel <= 8)
            {
                // 2を基準(0)とし、8をゴール(1.0)とする割合を計算 (0.0 ～ 1.0)
                float t = (intellectLevel - 2f) / (8f - 2f);

                // その割合を2乗することで「最初は緩やか、後半急激」なカーブを作る
                intellectBonus = Mathf.Pow(t, 2f);
            }
            // パターンC: Lv 9以降 (達人：ここからは一定の割合で伸びる)
            else
            {
                // Lv8時点の 1.0 に加えて、Lvが1上がるごとに +5% ずつボーナス
                intellectBonus = 1.0f + ((intellectLevel - 8f) * 0.05f);
            }

            return sensitivity * consciousness * intellectBonus;
        }

        public static float GetMagicMultiplier(Pawn pawn, RPGAbilityDef def)
        {
            float power = GetMagicPower(pawn);
            // powerが0未満にならないように念のためMaxをとる
            return 1.0f + (Mathf.Max(0f, power) - 1.0f) * def.magicPowerFactor;
        }
    }
}