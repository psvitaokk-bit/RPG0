using RimWorld;
using Verse;
using System.Collections.Generic;

namespace MyRPGMod
{
    // 名前が被らないように「RPG」を頭に付けたよ
    public enum RPGCategory
    {
        Offense,
        Support,
        Passive
    }

    public class RPGAbilityDef : AbilityDef
    {
        public float manaCost = 10f;
        public int maxLevel = 1;
        public int upgradeCost = 1;
        public RPGCategory rpgCategory = RPGCategory.Offense; // 名前を変更

        public List<AbilityStatEntry> stats = new List<AbilityStatEntry>();
    }

    public class AbilityStatEntry
    {
        public string label;
        public float baseValue;
        public float valuePerLevel;
        public string unit = "";
    }

    // --- ここから下は前回の RPGAbility ロジック ---
    public class RPGAbility : Ability
    {
        public RPGAbility(Pawn pawn) : base(pawn) { }
        public RPGAbility(Pawn pawn, AbilityDef def) : base(pawn, def) { }

        public override bool GizmoDisabled(out string reason)
        {
            if (base.GizmoDisabled(out reason)) return true;
            CompRPG comp = pawn.GetComp<CompRPG>();
            RPGAbilityDef myDef = def as RPGAbilityDef;
            float cost = myDef != null ? myDef.manaCost : 0f;

            if (comp != null && comp.currentMP < cost)
            {
                reason = "Not enough MP";
                return true;
            }
            return false;
        }

        public override bool Activate(LocalTargetInfo target, LocalTargetInfo dest)
        {
            bool result = base.Activate(target, dest);
            if (result)
            {
                CompRPG comp = pawn.GetComp<CompRPG>();
                RPGAbilityDef myDef = def as RPGAbilityDef;
                if (comp != null && myDef != null) comp.TryConsumeMP(myDef.manaCost);
            }
            return result;
        }
    }
}