using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace MyRPGMod
{
    // これを定義しておくと、自動的にXMLのDefとリンクしてくれるの！
    [DefOf]
    public static class RPGDefOf
    {
        public static AbilityDef RPG_MedicalTouch;
        public static HediffDef RPG_ConsciousnessBuff;
        public static HediffDef RPG_RegenerateBuff;

        // これを書くお約束
        static RPGDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(RPGDefOf));
        }
    }
}