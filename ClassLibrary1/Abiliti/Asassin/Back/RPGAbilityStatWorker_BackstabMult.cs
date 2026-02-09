using RimWorld;
using Verse;
using UnityEngine;

namespace MyRPGMod
{
    public class RPGAbilityStatWorker_BackstabDamage : RPGAbilityStatWorker
    {
        // 計算用メソッド（念のため実装しますが、メインはGetDisplayStringです）
        public override float Calculate(float baseVal, float perLevel, int level, Pawn caster, RPGAbilityDef abilityDef)
        {
            // 1. 倍率の計算 (例: 2.5)
            float multiplier = baseVal + (perLevel * Mathf.Max(0, level - 1));

            // 2. 武器威力の取得
            float weaponDmg = GetWeaponDamage(caster);

            return weaponDmg * multiplier;
        }

        public override string GetDisplayString(AbilityStatEntry entry, int level, Pawn caster, RPGAbilityDef abilityDef)
        {
            // 1. 倍率
            float multiplier = entry.baseValue + (entry.valuePerLevel * Mathf.Max(0, level - 1));

            // 2. 武器威力
            float weaponDmg = GetWeaponDamage(caster);

            // 3. 最終ダメージ
            float finalDamage = weaponDmg * multiplier;

            // 表示形式: "Est. Damage: 25.0 (x2.5)"
            // 武器を持っていない場合やキャスター不明な場合のケアも入れます
            if (caster == null)
            {
                return $"Multiplier: x{multiplier:F1}";
            }
            else
            {
                return $"Est. Damage: <color=yellow>{finalDamage:F1}</color> (x{multiplier:F1})";
            }
        }

        // 武器のDPSを取得するヘルパーメソッド
        private float GetWeaponDamage(Pawn caster)
        {
            if (caster?.equipment?.Primary != null)
            {
                // 近接DPSを取得
                return caster.equipment.Primary.GetStatValue(StatDefOf.MeleeWeapon_AverageDPS);
            }
            // 武器なし（素手）の基本値。バニラの人間は大体 DPS 1.5～2.0 くらいですが、
            // 分かりやすく5.0(格闘強者)程度を仮定しておきます
            return 5.0f;
        }
    }
}