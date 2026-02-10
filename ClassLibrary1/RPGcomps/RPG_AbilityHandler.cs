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

            // ★変更：現在のレベルに基づいたコストを取得
            int requiredCost = def.GetUpgradeCost(curLv);

            if (curLv < def.maxLevel && levelHandler.skillPoints >= requiredCost)
            {
                levelHandler.skillPoints -= requiredCost;

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
            else if (levelHandler.skillPoints < requiredCost)
            {
                // ポイント不足時の通知（任意）
                Messages.Message("Not enough skill points.", MessageTypeDefOf.RejectInput, false);
            }
        }

        public void ExposeData()
        {
            Scribe_Collections.Look(ref abilityLevels, "abilityLevels", LookMode.Value, LookMode.Value);
            if (abilityLevels == null) abilityLevels = new Dictionary<string, int>();
        }
    }
}