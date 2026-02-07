using HarmonyLib;
using Verse;

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

    // Pawnが死んだ時の処理に割り込む
    [HarmonyPatch(typeof(Pawn), "Kill")]
    public static class Patch_Pawn_Kill
    {
        // Prefix: 元の処理の前に実行
        // __instance: 死んだPawn
        // dinfo: ダメージ情報（誰がやったか？）
        public static void Prefix(Pawn __instance, DamageInfo? dinfo)
        {
            // 犯人が存在し、かつそれが「入植者」である場合
            if (dinfo.HasValue && dinfo.Value.Instigator is Pawn killer && killer.IsColonist)
            {
                // その入植者が持っているRPGコンポーネントを取得
                var rpgComp = killer.GetComp<CompRPG>();
                if (rpgComp != null)
                {
                    rpgComp.GainXp(500f); // 500経験値ゲット！
                }
            }
        }
    }
}