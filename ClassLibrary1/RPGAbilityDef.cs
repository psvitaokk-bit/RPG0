using RimWorld;
using Verse;
using System.Collections.Generic;
using Verse.Sound;

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
        public RPGCategory rpgCategory = RPGCategory.Offense;
        public List<AbilityStatEntry> stats = new List<AbilityStatEntry>();
        public float magicPowerFactor = 0f; // デフォルトは0（影響なし）にしておくと安全だよ

        // ★追加：詠唱開始音と発動音★
        public SoundDef soundImpact; // 命中・効果発生時の音
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
       


        public override bool Activate(LocalTargetInfo target, LocalTargetInfo dest)
        {
            bool result = base.Activate(target, dest);
            if (result)
            {
                RPGAbilityDef rpgDef = def as RPGAbilityDef;


                // ★追加：発動成功（インパクト）の音を鳴らす★
                if (rpgDef?.soundImpact != null)
                {
                    // ターゲットの位置（または弾着地点）で鳴らす
                    rpgDef.soundImpact.PlayOneShot(new TargetInfo(target.Cell, pawn.Map));
                }

                CompRPG comp = pawn.GetComp<CompRPG>();
                if (comp != null && rpgDef != null) comp.TryConsumeMP(rpgDef.manaCost);
            }
            return result;
        }

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
    }
}