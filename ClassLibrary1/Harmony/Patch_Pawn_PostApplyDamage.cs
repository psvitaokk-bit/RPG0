using HarmonyLib;
using Verse;
using RimWorld;
using UnityEngine;

namespace MyRPGMod
{
    // ダメージ処理の「完了後」に介入
    [HarmonyPatch(typeof(Pawn), "PostApplyDamage")]
    public static class Patch_Pawn_PostApplyDamage
    {
        public static void Postfix(Pawn __instance, DamageInfo dinfo, float totalDamageDealt)
        {
            // ダメージがない、または死んでいる場合は無視
            if (__instance == null || __instance.Dead || totalDamageDealt <= 0) return;

            CompRPG rpgComp = __instance.GetComp<CompRPG>();
            if (rpgComp == null) return;

            // ★クラスごとの判定を入れるとよりRPGらしい！
            // ディフェンダーなら多く貰える、それ以外は少しだけ、など。
            float xpMultiplier = 0.5f; // 基本はダメージの半分

            if (rpgComp.currentClass != null && rpgComp.currentClass.defName == "RPG_Defender")
            {
                xpMultiplier = 2.0f; // ディフェンダーならダメージの2倍のXP！
            }

            float gainXp = totalDamageDealt * xpMultiplier;

            // 最低1XPは保証
            if (gainXp < 1f) gainXp = 1f;

            rpgComp.GainXp(gainXp);

            // タンク役の場合、XP獲得が見えると「耐えてる感」が出るので表示推奨
            if (xpMultiplier >= 1.0f && __instance.Map != null)
            {
                // MoteMaker.ThrowText(__instance.DrawPos, __instance.Map, $"+{gainXp:F0} XP", Color.gray);
            }
        }
    }
}