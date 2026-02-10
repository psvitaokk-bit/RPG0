using Verse;
using RimWorld;

namespace MyRPGMod
{
    public class RPG_LevelHandler
    {
        public int level = 1;
        public float currentXp = 0;
        public int skillPoints = 0;

        // 次のレベルまでに必要な経験値を段階的に計算
        public float XpToNextLevel
        {
            get
            {
                // 1. レベル5まで：超低コスト（序盤をサクサク進める設定）
                // Lv1: 500, Lv5: 2500
                if (level <= 5)
                {
                    return level * 500f;
                }
                // 2. レベル20まで：標準的なコスト
                // Lv6: 6000, Lv20: 20,000
                else if (level <= 20)
                {
                    return level * 1000f;
                }
                // 3. レベル30まで：成長が鈍化（コスト増）
                // Lv21: 63,000, Lv30: 90,000
                else if (level <= 30)
                {
                    return level * 3000f;
                }
                // 4. 31以降：エンドコンテンツ級
                // Lv31: 217,000
                else
                {
                    return level * 7000f;
                }
            }
        }

        public void GainXp(float amount, Pawn pawn)
        {
            currentXp += amount;
            while (currentXp >= XpToNextLevel)
            {
                currentXp -= XpToNextLevel;
                level++;
                skillPoints++;
                Messages.Message($"{pawn.LabelShort} Leveled Up to {level}!", MessageTypeDefOf.PositiveEvent);
            }
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref level, "level", 1);
            Scribe_Values.Look(ref currentXp, "currentXp", 0);
            Scribe_Values.Look(ref skillPoints, "skillPoints", 0);
        }
    }
}