using HarmonyLib;
using Verse;
using RimWorld;
using UnityEngine;
using System.Linq;

namespace MyRPGMod
{
    // パッチ対象を Pawn_HealthTracker ではなく Pawn 本体に変更します（より確実に介入するため）
    [HarmonyPatch(typeof(Pawn), "PreApplyDamage")]
    public static class Patch_GuardianRedirect
    {
        // 無限ループ防止用のフラグ
        public static bool isRedirectingDamage = false;

        static bool Prefix(Pawn __instance, ref DamageInfo dinfo, out bool absorbed)
        {
            absorbed = false;

            // 1. ループ防止ガード: 自分が再発行したダメージなら何もしないで通す
            if (isRedirectingDamage) return true;

            // 2. 通常のチェック
            if (dinfo.Def.defName == "RPG_GuardianTransfer") return true;
            if (__instance == null || __instance.Dead || !__instance.Spawned) return true;

            // 3. 守護Hediffを持っているか？
            var oathHediff = __instance.health.hediffSet.hediffs
                .OfType<Hediff_GuardianOath>()
                .FirstOrDefault();

            if (oathHediff != null && oathHediff.guardian != null)
            {
                Pawn guardian = oathHediff.guardian;

                // ガーディアンが守れる状態か
                if (!guardian.Dead && !guardian.Downed && guardian.Map == __instance.Map)
                {
                    float originalDmg = dinfo.Amount;
                    if (originalDmg <= 0) return true;

                    float transferDmg = originalDmg * oathHediff.redirectPct;
                    float remainDmg = originalDmg - transferDmg;

                    // --- 処理の核心 ---

                    // フラグを立てて「今から自分がダメージ処理をする」と宣言
                    isRedirectingDamage = true;
                    try
                    {
                        // A. ガーディアンにダメージを与える
                        DamageDef transferDef = DefDatabase<DamageDef>.GetNamed("RPG_GuardianTransfer");
                        DamageInfo transferInfo = new DamageInfo(
                            transferDef, transferDmg, 0, -1, dinfo.Instigator, null, null
                        );
                        guardian.TakeDamage(transferInfo);

                        // B. ターゲット本人に「減ったダメージ」を改めて与える
                        // ※ここでTakeDamageを呼ぶとまたこのPrefixが呼ばれるが、
                        //   冒頭の isRedirectingDamage チェックでスルーされるため安全
                        DamageInfo reducedInfo = new DamageInfo(
                            dinfo.Def, remainDmg, dinfo.ArmorPenetrationInt, dinfo.Angle,
                            dinfo.Instigator, dinfo.HitPart, dinfo.Weapon,
                            dinfo.Category, dinfo.IntendedTarget
                        );
                        __instance.TakeDamage(reducedInfo);

                        // 演出
                        MoteMaker.ThrowText(guardian.DrawPos, guardian.Map, $"Cover! (-{transferDmg:F0})", Color.blue, 2.0f);
                    }
                    finally
                    {
                        // 必ずフラグを下ろす
                        isRedirectingDamage = false;
                    }

                    // --- 元のダメージをキャンセル ---
                    // RimWorld本体の処理には「ダメージは吸収された（なかったことになった）」と伝える
                    absorbed = true;
                    return false; // 元のメソッドを実行させない
                }
            }

            return true;
        }
    }
}