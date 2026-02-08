using HarmonyLib;
using MyRPGMod;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace MyRPGMod
{
    [HarmonyPatch(typeof(TendUtility), "DoTend")]
    public static class Patch_TendUtility_DoTend
    {
        // ★ヘルパーメソッド：アビリティ定義とレベルを取得する
        // 毎回DefDatabaseを検索するのは重いから、静的変数にキャッシュしてもいいけど、
        // ここでは分かりやすさ優先で直接書くね。
        private static bool TryGetHealAbility(CompRPG rpgComp, out RPGAbilityDef def, out int level)
        {
            def = (RPGAbilityDef)RPGDefOf.RPG_MedicalTouch;
            level = 0;

            if (def == null || rpgComp == null) return false;

            // アビリティを習得しているか？ (Level > 0)
            level = rpgComp.GetAbilityLevel(def);
            return level > 0;
        }

        public static void Prefix(Pawn doctor, Pawn patient, out List<Hediff_Injury> __state)
        {
            __state = new List<Hediff_Injury>();
            if (doctor == null || patient == null || doctor.Dead) return;

            CompRPG rpgComp = doctor.GetComp<CompRPG>();

            // ★変更：クラス名ではなく「アビリティ習得」と「トグル」をチェック
            if (rpgComp != null && rpgComp.autoHealEnabled && TryGetHealAbility(rpgComp, out _, out _))
            {
                __state = patient.health.hediffSet.hediffs
                    .OfType<Hediff_Injury>()
                    .Where(h => h.TendableNow())
                    .ToList();
            }
        }

        public static void Postfix(Pawn doctor, Pawn patient, List<Hediff_Injury> __state)
        {
            if (__state == null || __state.Count == 0) return;

            CompRPG rpgComp = doctor.GetComp<CompRPG>();
            if (rpgComp == null) return;

            // ★変更：アビリティ情報を取得
            if (rpgComp.autoHealEnabled && TryGetHealAbility(rpgComp, out RPGAbilityDef abilityDef, out int level))
            {
                // XMLで設定したマナコストを使う（レベルアップで減らすなどの計算も可能！）
                // 例: 基本コスト - (Lv-1)
                float manaCost = Mathf.Max(1f, abilityDef.manaCost - (level - 1));

                if (rpgComp.currentMP < manaCost) return;

                var justTendedInjuries = new List<Hediff_Injury>();
                foreach (var injury in __state)
                {
                    if (injury.Severity > 0 && !injury.TendableNow())
                    {
                        justTendedInjuries.Add(injury);
                    }
                }

                if (justTendedInjuries.Count > 0)
                {
                    if (rpgComp.TryConsumeMP(manaCost))
                    {
                        foreach (var injury in justTendedInjuries)
                        {
                            var tendComp = injury.TryGetComp<HediffComp_TendDuration>();
                            float quality = (tendComp != null) ? tendComp.tendQuality : 0f;

                            // 基本倍率（Lv1: 1.75, Lv2: 2.0...）
                            // ※XMLの baseValue(1.75) + valuePerLevel(0.25) と合わせる
                            float levelBonus = 1.75f + ((level - 1) * 0.25f);

                            float healAmount = levelBonus * quality;
                            float skillBonus = 1.0f + (doctor.skills.GetSkill(SkillDefOf.Medicine).Level * 0.05f);
                            healAmount *= skillBonus;

                            // ★ここを修正！
                            // rpgComp.MagicPower を直接掛けるのではなく、GetMagicMultiplierを使う
                            // これで XMLの <magicPowerFactor>1.0</magicPowerFactor> が反映されるよ
                            healAmount *= rpgComp.GetMagicMultiplier(abilityDef);

                            injury.Severity -= healAmount;
                            MoteMaker.ThrowText(patient.DrawPos, patient.Map, $"+{healAmount:F1}", Color.green);
                        }
                    }
                }
            }
        }
    }
}
