// 新しいファイル: RPGAbilityStatWorker_WeaponDamage.cs
using Verse;
using RimWorld;
using UnityEngine;
using System.Collections.Generic;
using System.Text;

namespace MyRPGMod
{
    // 装備中の武器の威力を計算に取り入れる Worker
    public class RPGAbilityStatWorker_WeaponDamage : RPGAbilityStatWorker
    {
        public override float Calculate(float baseVal, float perLevel, int level, Pawn caster, RPGAbilityDef abilityDef)
        {
            // 1. アビリティ自体の基本威力（基礎値 + レベルボーナス）
            float abilityPower = baseVal + (perLevel * Mathf.Max(0, level - 1));

            // 2. 武器の威力を取得
            // 修正前: StatDefOf.MeleeWeapon_AverageDamage (これは存在しないよ)
            // 修正後: StatDef.Named("MeleeWeapon_AverageDPS") 

            float weaponPower = 0f;
            if (caster?.equipment?.Primary != null)
            {
                // 「平均DPS」のステータスを取得するよ。これならバニラに確実に存在するんだ！
                weaponPower = caster.equipment.Primary.GetStatValue(StatDef.Named("MeleeWeapon_AverageDPS"));
            }
            else
            {
                weaponPower = 5f; // 素手の時のベース
            }

            // 3. 計算式：(アビリティ威力) × (武器威力 / 10)
            // これなら、武器威力が10の時にアビリティ威力がそのまま出て、
            // 威力が20の業物なら技のダメージも2倍になるよ！
            // ★ここでは魔力倍率（GetMagicMultiplier）は一切使わないよ！
            float finalDamage = abilityPower * (weaponPower / 10f);

            return finalDamage;
        }
    }


    // 治せる病気の実名をずらっと表示するためのWorker
    public class RPGAbilityStatWorker_CureList : RPGAbilityStatWorker
    {
        public override float Calculate(float baseVal, float perLevel, int level, Pawn caster, RPGAbilityDef abilityDef) => 0f;

        public override string GetDisplayString(AbilityStatEntry entry, int level, Pawn caster, RPGAbilityDef abilityDef)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("【現在のレベルで治せる病気】");

            for (int i = 1; i <= level; i++)
            {
                // ★修正：Utilを使う
                HashSet<string> names = RPGCalculationUtil.GetCurableDefNames(i);

                // (注意：GetCurableDefNamesは「そのレベル以下全部」を返す仕様なので、
                // ランクごとの表示にするなら Util に「特定のランクだけ返すメソッド」を作るか、
                // ここで差分を取るロジックが必要になります。
                // 簡易的に表示するなら、このままでも動作はします)

                if (names.Count == 0) continue;

                sb.AppendLine(GetTierHeader(i));
                foreach (string name in names)
                {
                    // そのランクのものだけ表示するフィルタリング（簡易実装）
                    if (RPGCalculationUtil.GetDiseaseTier(name) != i) continue;

                    HediffDef def = DefDatabase<HediffDef>.GetNamedSilentFail(name);
                    string label = (def != null) ? (string)def.LabelCap : name;
                    sb.AppendLine($"  ・{label}");
                }
            }
            return sb.ToString().TrimEnd();
        }

        private string GetTierHeader(int tier)
        {
            switch (tier)
            {
                case 1: return "<color=white>ランク1</color>";
                case 2: return "<color=yellow>ランク2</color>";
                case 3: return "<color=orange>ランク3</color>";
                case 4: return "<color=red>ランク4</color>";
                default: return "不明なランク";
            }
        }
    

    // 各ランクにどの病気が含まれるかの定義
    // ※CompAbilityEffect_Cureのロジックと合わせておくと管理が楽だよ
    private HashSet<string> GetNamesForTier(int tier)
        {
            switch (tier)
            {
                case 1: return new HashSet<string> { "WoundInfection", "Flu", "Animal_Flu", "FoodPoisoning" };
                case 2: return new HashSet<string> { "GutWorms", "MuscleParasites" };
                case 3: return new HashSet<string> { "Malaria", "Plague", "Animal_Plague", "SleepingSickness", "LungRot", "LungRotExposure" };
                case 4: return new HashSet<string> { "FibrousMechanites", "SensoryMechanites", "ScariaInfection" };
                default: return new HashSet<string>();
            }
        }
    }

    public class RPGAbilityStatWorker_CureChance : RPGAbilityStatWorker
    {
        public override float Calculate(float baseVal, float perLevel, int level, Pawn caster, RPGAbilityDef abilityDef) => 0f;

        public override string GetDisplayString(AbilityStatEntry entry, int level, Pawn caster, RPGAbilityDef abilityDef)
        {
            if (caster == null) return "Success Chance: ---";

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Success Chance per Tier:");

            for (int tier = 1; tier <= 4; tier++)
            {
                // ★修正：Utilを使って一発計算
                float chance = RPGCalculationUtil.GetCureSuccessChance(caster, tier);

                string tierName = GetTierName(tier);
                sb.AppendLine($"  {tierName}: {chance.ToStringPercent()}");
            }
            return sb.ToString().TrimEnd();
        }

        private string GetTierName(int tier)
        {
            switch (tier)
            {
                case 1: return "Basic";
                case 2: return "Chronic";
                case 3: return "Deadly";
                case 4: return "Advanced";
                default: return "Unknown";
            }
        }
    }
}
