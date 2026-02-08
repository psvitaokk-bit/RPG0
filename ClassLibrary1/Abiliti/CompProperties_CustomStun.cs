using RimWorld;
using Verse;


using System.Linq;
using UnityEngine;

namespace MyRPGMod
{
    // --- 1. 設定用クラス ---
    // XML側の stats を使うので、このクラス独自の変数は不要になるよ！
    public class CompProperties_CustomStun : CompProperties_AbilityEffect
    {
        public CompProperties_CustomStun()
        {
            this.compClass = typeof(CompAbilityEffect_CustomStun);
        }
    }

    // --- 2. 実行用クラス ---
    public class CompAbilityEffect_CustomStun : CompAbilityEffect
    {
        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);

            Pawn victim = target.Pawn;
            Pawn caster = parent.pawn;

            if (victim != null && caster != null)
            {
                CompRPG rpgComp = caster.GetComp<CompRPG>();
                int currentLevel = rpgComp?.GetAbilityLevel(parent.def) ?? 1;
                if (currentLevel < 1) currentLevel = 1;

                // ★ここがポイント：XMLの <stats> から "Stun Duration" というラベルの数値を探す
                float durationSeconds = 3f; // 見つからなかった時のデフォルト値

                RPGAbilityDef rpgDef = parent.def as RPGAbilityDef;
                if (rpgDef != null && rpgDef.stats != null)
                {
                    // ラベルが "Stun Duration" に一致する設定を取得
                    var stat = rpgDef.stats.FirstOrDefault(s => s.label == "Stun Duration");
                    if (stat != null)
                    {
                        // XMLの数値から計算：基本値 + (上昇量 * (Lv-1))
                        durationSeconds = stat.baseValue + (stat.valuePerLevel * (currentLevel - 1));
                    }
                }

                // 秒をTicksに変換 (1秒 = 60Ticks)
                int finalTicks = Mathf.RoundToInt(durationSeconds * 60f);

                // 実行
                victim.stances.stunner.StunFor(finalTicks, caster, true, true);

                // エフェクト表示
                MoteMaker.ThrowText(victim.DrawPos, victim.Map, $"STUN! ({durationSeconds:F1}s)", 2f);
            }
        }
    }
}