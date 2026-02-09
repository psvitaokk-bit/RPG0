using System.Collections.Generic;
using Verse;
using RimWorld;

namespace MyRPGMod
{
    public class RPG_AbilityHandler
    {
        public Dictionary<string, int> abilityLevels = new Dictionary<string, int>();

        public int GetAbilityLevel(AbilityDef def)
        {
            if (def == null) return 0;
            return abilityLevels.TryGetValue(def.defName, out int level) ? level : 0;
        }

        public void UpgradeAbility(RPGAbilityDef def, RPG_LevelHandler levelHandler, Pawn pawn)
        {
            int curLv = GetAbilityLevel(def);

            if (curLv < def.maxLevel && levelHandler.skillPoints >= def.upgradeCost)
            {
                levelHandler.skillPoints -= def.upgradeCost;

                if (curLv == 0)
                {
                    abilityLevels[def.defName] = 1;
                    if (def.rpgCategory != RPGCategory.Passive)
                    {
                        pawn.abilities.GainAbility(def);
                    }
                }
                else
                {
                    abilityLevels[def.defName]++;
                }
                Messages.Message($"{def.label} Upgraded!", MessageTypeDefOf.PositiveEvent);
            }
        }

        public void ExposeData()
        {
            Scribe_Collections.Look(ref abilityLevels, "abilityLevels", LookMode.Value, LookMode.Value);
            if (abilityLevels == null) abilityLevels = new Dictionary<string, int>();
        }
    }
}