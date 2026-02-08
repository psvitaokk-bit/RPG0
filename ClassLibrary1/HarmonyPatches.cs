using HarmonyLib;
using Verse;
using RimWorld;
using UnityEngine; // ColorやMoteMakerのために追加
using System.Linq;
using System.Collections.Generic;

namespace MyRPGMod
{
    [StaticConstructorOnStartup]
    public static class HarmonyInit
    {
        static HarmonyInit()
        {
            var harmony = new Harmony("com.example.myrpgmod");
            harmony.PatchAll();
        }
    }

    [HarmonyPatch(typeof(Pawn), "Kill")]
    public static class Patch_Pawn_Kill
    {
        public static void Prefix(Pawn __instance, DamageInfo? dinfo)
        {
            // 1. 犯人が入植者であることを確認
            if (dinfo.HasValue && dinfo.Value.Instigator is Pawn killer && killer.IsColonist)
            {
                // 2. 倒された相手が味方じゃないことを確認（味方殺しで経験値稼ぎを防止！）
                if (__instance.Faction != killer.Faction)
                {
                    var rpgComp = killer.GetComp<CompRPG>();
                    if (rpgComp != null)
                    {
                        // 3. 相手の「戦闘力 (combatPower)」を取得
                        // kindDefが無い場合に備えて、デフォルト値（10fなど）を設定しておくと安全だよ
                        float victimPower = __instance.kindDef?.combatPower ?? 10f;

                        // 4. 経験値を計算（例：戦闘力 × 10倍）
                        // お兄ちゃんの好みに合わせて「× 5」とかに調整してみてね！
                        float gainXp = victimPower * 10f;

                        // 経験値を加算
                        rpgComp.GainXp(gainXp);

                        // ★おまけ：経験値獲得を画面にパッと表示させると「RPG感」が出るよ！
                        if (killer.Map != null)
                        {
                            MoteMaker.ThrowText(killer.DrawPos, killer.Map, $"+{gainXp:F0} XP", Color.yellow);
                        }
                    }
                }
            }
        }
    }

    [HarmonyPatch(typeof(Thing), "TakeDamage")]
    public static class Patch_Thing_TakeDamage
    {
        // Postfix（事後処理）を使うことで、「実際に与えたダメージ量」が分かるよ
        public static void Postfix(Thing __instance, DamageInfo dinfo, DamageWorker.DamageResult __result)
        {
            // 1. 攻撃した人(Instigator)がいて、かつポーンである場合
            if (dinfo.Instigator is Pawn attacker && __instance is Pawn victim)
            {
                // 2. 自分自身へのダメージや、味方への誤射では経験値を入れない
                if (attacker == victim) return;

                // 敵対していない相手（壁殴りや味方撃ち）での稼ぎ防止
                // ※もし「スパーリングで稼ぎたい」ならこの行は外してもいいよ！
                if (!victim.HostileTo(attacker)) return;

                // 3. RPGコンポーネントを持っているか確認
                var rpgComp = attacker.GetComp<CompRPG>();
                if (rpgComp != null)
                {
                    // 4. 実際に通ったダメージ量を取得
                    // (__result.totalDamageDealt はアーマー軽減後の最終ダメージだよ)
                    float damageDealt = __result.totalDamageDealt;

                    // ダメージが0（回避や無効化）なら何もしない
                    if (damageDealt <= 0) return;

                    // 5. 経験値の計算
                    // 例：ダメージ1点につき 0.5 XP (10ダメージで5XP)
                    // バランスに合わせて係数 (0.5f) を調整してね
                    float xpGain = damageDealt * 1.0f;

                    // 6. 経験値を加算
                    rpgComp.GainXp(xpGain);

                    // ヒットのたびに文字が出ると画面がうるさいかもしれないから、
                    // ここではMote（ポップアップ）は出さないでおくね。
                    // もし出したかったら、Patch_Pawn_Killと同じようにMoteMakerを使ってね！
                }
            }
        }
    }

    // 手当て（Tend）をした瞬間に割り込むパッチ
    [HarmonyPatch(typeof(TendUtility), "DoTend")]
    public static class Patch_TendUtility_DoTend
    {
        // ★ヘルパーメソッド：アビリティ定義とレベルを取得する
        // 毎回DefDatabaseを検索するのは重いから、静的変数にキャッシュしてもいいけど、
        // ここでは分かりやすさ優先で直接書くね。
        private static bool TryGetHealAbility(CompRPG rpgComp, out RPGAbilityDef def, out int level)
        {
            def = DefDatabase<RPGAbilityDef>.GetNamedSilentFail("RPG_MedicalTouch");
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