using RimWorld;
using Verse;

namespace MyRPGMod
{
    public class RPGAbilityDef : AbilityDef
    {
        public float manaCost = 10f;
    }

    public class RPGAbility : Ability
    {
        // ★★★ これを追加！ ★★★
        // RimWorldがアビリティを生成する時に、この形（引数2つ）を絶対に探すんだ。
        public RPGAbility(Pawn pawn) : base(pawn)
        {
        }

        public RPGAbility(Pawn pawn, AbilityDef def) : base(pawn, def)
        {
        }
        // ★★★★★★★★★★★★★★★

        // 既存の GizmoDisabled や Activate はそのまま残してね！
        public override bool GizmoDisabled(out string reason)
        {
            if (base.GizmoDisabled(out reason)) return true;

            Pawn pawn = this.pawn;
            CompRPG comp = pawn.GetComp<CompRPG>();

            RPGAbilityDef myDef = this.def as RPGAbilityDef;
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
                CompRPG comp = this.pawn.GetComp<CompRPG>();
                RPGAbilityDef myDef = this.def as RPGAbilityDef;

                if (comp != null && myDef != null)
                {
                    comp.TryConsumeMP(myDef.manaCost);
                }
            }
            return result;
        }
    }
}