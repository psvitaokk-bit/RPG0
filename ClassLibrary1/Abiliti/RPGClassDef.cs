using Verse;
using RimWorld;

namespace MyRPGMod
{
    // XMLで <MyRPGMod.RPGClassDef> と書けるようにする定義
    public class RPGClassDef : Def
    {
        public HediffDef classPassiveHediff;
    }
}