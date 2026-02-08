using HarmonyLib;
using Verse;
using RimWorld;
using UnityEngine; // ColorやMoteMakerのために追加

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
}