using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using RimWorld;
using Verse;

namespace MyRPGMod
{
    public class RPGAbilityStatWorker_StunDuration : RPGAbilityStatWorker_Standard
    {
        public override string GetDisplayString(AbilityStatEntry entry, int level, Pawn caster, RPGAbilityDef abilityDef)
        {
            float val = Calculate(entry.baseValue, entry.valuePerLevel, level, caster, abilityDef);
            return $"Stun: {val:F1} s";
        }
    }
}