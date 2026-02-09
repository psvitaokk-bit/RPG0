using HarmonyLib;
using Verse;
using RimWorld;
using UnityEngine;
using System.Linq;
using Verse.Sound;

namespace MyRPGMod
{
    // ダメージ適用前の処理にフック
    [HarmonyPatch(typeof(Pawn_HealthTracker), "PreApplyDamage")]
    public static class Patch_Pawn_HealthTracker_PreApplyDamage
    {
        // Prefix: 元の処理が走る前に実行される
        static void Prefix(Pawn_HealthTracker __instance, Pawn ___pawn, ref DamageInfo dinfo, out bool absorbed)
        {
            absorbed = false;

            // 無効なターゲット、死亡済み、爆発ダメージなどは除外
            if (___pawn == null || ___pawn.Dead || dinfo.Def.isExplosive) return;

            // RPGコンポーネントの取得
            var rpgComp = ___pawn.GetComp<CompRPG>();
            if (rpgComp == null) return;

            // パリィアビリティを習得しているかチェック
            // (名前で検索していますが、キャッシュするかDefを静的フィールドに持つとより高速です)
            var parryDef = DefDatabase<RPGAbilityDef>.GetNamedSilentFail("RPG_Parry");
            if (parryDef == null) return;

            int level = rpgComp.GetAbilityLevel(parryDef);
            if (level <= 0) return;

            // --- 1. 発動率の計算 ---
            float chance = 0f;
            var chanceStat = parryDef.stats.FirstOrDefault(s => s.label == "Parry Chance");
            if (chanceStat != null)
            {
                chance = chanceStat.Worker.Calculate(chanceStat.baseValue, chanceStat.valuePerLevel, level, ___pawn, parryDef);
            }

            // --- 2. 確率判定 ---
            if (Rand.Chance(chance))
            {
                // --- 3. 軽減率の計算 ---
                float reduction = 0f;
                var reduceStat = parryDef.stats.FirstOrDefault(s => s.label == "Damage Reduction");
                if (reduceStat != null)
                {
                    reduction = reduceStat.Worker.Calculate(reduceStat.baseValue, reduceStat.valuePerLevel, level, ___pawn, parryDef);
                }

                // ダメージを軽減
                float originalDamage = dinfo.Amount;
                float newDamage = originalDamage * (1.0f - reduction);

                // 軽減後のダメージをセット
                dinfo.SetAmount(newDamage);

                // もし100%カットなら「吸収(absorbed)」扱いにする
                if (newDamage <= 0.01f)
                {
                    absorbed = true;
                }

                // --- 4. 演出 (テキストと音) ---
                if (___pawn.Map != null)
                {
                    // テキスト表示 "PARRY! (-15)"
                    float reducedAmount = originalDamage - newDamage;
                    MoteMaker.ThrowText(___pawn.DrawPos, ___pawn.Map, $"PARRY! (-{reducedAmount:F0})", Color.yellow, 2.2f);

                    // 火花エフェクト
                    FleckMaker.ThrowMicroSparks(___pawn.DrawPos, ___pawn.Map);

                    // 金属音 (キンッ！)
                    SoundDefOf.MetalHitImportant.PlayOneShot(___pawn);
                }
            }
        }
    }
}