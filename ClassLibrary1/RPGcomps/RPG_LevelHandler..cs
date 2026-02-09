using Verse;
using RimWorld;

namespace MyRPGMod
{
    public class RPG_LevelHandler
    {
        public int level = 1;
        public float currentXp = 0;
        public int skillPoints = 0;

        public float XpToNextLevel => level * 1000f;

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