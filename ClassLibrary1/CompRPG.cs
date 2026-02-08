using UnityEngine;
using Verse;
using RimWorld;
using System.Collections.Generic;

namespace MyRPGMod
{
    public class CompRPG : ThingComp
    {
        public int level = 1;
        public float currentXp = 0;
        public int skillPoints = 0;
        public float currentMP = 0;
        public float MaxMP => level * 20f;
        public float XpToNextLevel => level * 1000f;

        // レベル管理用の辞書
        public Dictionary<string, int> abilityLevels = new Dictionary<string, int>();

        public int GetAbilityLevel(AbilityDef def)
        {
            if (abilityLevels == null) abilityLevels = new Dictionary<string, int>();
            if (def == null) return 0;
            return abilityLevels.ContainsKey(def.defName) ? abilityLevels[def.defName] : 0;
        }

        public void UpgradeAbility(RPGAbilityDef def)
        {
            if (abilityLevels == null) abilityLevels = new Dictionary<string, int>();
            int curLv = GetAbilityLevel(def);

            if (curLv < def.maxLevel && skillPoints >= def.upgradeCost)
            {
                skillPoints -= def.upgradeCost;
                if (curLv == 0)
                {
                    abilityLevels[def.defName] = 1;
                    (parent as Pawn)?.abilities.GainAbility(def);
                }
                else
                {
                    abilityLevels[def.defName]++;
                }
                Messages.Message($"{def.label} Upgraded!", MessageTypeDefOf.PositiveEvent);
            }
        }

        public void GainXp(float amount)
        {
            currentXp += amount;
            while (currentXp >= XpToNextLevel)
            {
                currentXp -= XpToNextLevel;
                level++;
                skillPoints++;
                Messages.Message($"{parent.LabelShort} Leveled Up!", MessageTypeDefOf.PositiveEvent);
            }
        }

        public bool TryConsumeMP(float cost)
        {
            if (currentMP >= cost) { currentMP -= cost; return true; }
            return false;
        }

        public override void CompTick()
        {
            if (parent is Pawn p && p.IsHashIntervalTick(60) && currentMP < MaxMP)
            {
                currentMP = Mathf.Min(currentMP + 1.0f, MaxMP);
            }
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref level, "level", 1);
            Scribe_Values.Look(ref currentXp, "currentXp", 0);
            Scribe_Values.Look(ref skillPoints, "skillPoints", 0);
            Scribe_Values.Look(ref currentMP, "currentMP", 0);
            Scribe_Collections.Look(ref abilityLevels, "abilityLevels", LookMode.Value, LookMode.Value);
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            if (parent is Pawn p && p.IsColonistPlayerControlled) yield return new Gizmo_ManaBar(this);
        }
    }

// プロパティ定義（XMLで設定できるようにする）
public class CompProperties_RPG : CompProperties
    {
        public CompProperties_RPG()
        {
            this.compClass = typeof(CompRPG);
        }
    }
}