using UnityEngine;
using Verse;
using RimWorld;
using System.Collections.Generic;

namespace MyRPGMod
{
    public class CompRPG : ThingComp
    {
        // 各ロジックを Handler に委譲
        public RPG_LevelHandler levelHandler = new RPG_LevelHandler();
        public RPG_ManaHandler manaHandler = new RPG_ManaHandler();
        public RPG_AbilityHandler abilityHandler = new RPG_AbilityHandler();

        // 共通設定や状態
        public RPGClassDef currentClass;
        public bool autoHealEnabled = true;

        // ITabや他のクラスから参照されるためのプロパティ（ショートカット）
        // --- 修正箇所：CompRPG.cs のショートカットプロパティ群 ---

        // levelHandler へのショートカット
        public int level
        {
            get => levelHandler.level;
            set => levelHandler.level = value;
        }
        public float currentXp
        {
            get => levelHandler.currentXp;
            set => levelHandler.currentXp = value;
        }
        public int skillPoints
        {
            get => levelHandler.skillPoints;
            set => levelHandler.skillPoints = value; // これで CS0200 エラーが消えます！
        }

        // manaHandler へのショートカット
        public float currentMP
        {
            get => manaHandler.currentMP;
            set => manaHandler.currentMP = value;
        }

        // 計算値は読み取り専用のままでOK
        public float XpToNextLevel => levelHandler.XpToNextLevel;
        public float MaxMP => manaHandler.GetMaxMP(level);

        public void GainXp(float amount) => levelHandler.GainXp(amount, parent as Pawn);
        public bool TryConsumeMP(float cost) => manaHandler.TryConsumeMP(cost);
        public int GetAbilityLevel(AbilityDef def) => abilityHandler.GetAbilityLevel(def);
        public void UpgradeAbility(RPGAbilityDef def) => abilityHandler.UpgradeAbility(def, levelHandler, parent as Pawn);

        public float MagicPower => RPGMagicCalculator.GetMagicPower(parent as Pawn);

        public float GetMagicMultiplier(RPGAbilityDef def)
        {
            Pawn pawn = parent as Pawn;
            // UtilからMagicCalculatorへ変更
            return RPGMagicCalculator.GetMagicMultiplier(pawn, def);
        }

        public override void CompTickRare()
        {
            base.CompTickRare();
            Pawn p = parent as Pawn;
            if (p == null || p.Dead) return;

            // マナの自然回復
            if (p.Map != null) manaHandler.RegenMP(level);

            // パッシブ Hediff の維持
            if (currentClass?.classPassiveHediff != null)
            {
                if (p.health.hediffSet.GetFirstHediffOfDef(currentClass.classPassiveHediff) == null)
                {
                    p.health.AddHediff(HediffMaker.MakeHediff(currentClass.classPassiveHediff, p));
                }
            }
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            levelHandler.ExposeData();
            manaHandler.ExposeData();
            abilityHandler.ExposeData();

            Scribe_Defs.Look(ref currentClass, "currentClass");
            Scribe_Values.Look(ref autoHealEnabled, "autoHealEnabled", true);
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            if (parent is Pawn p && p.IsColonistPlayerControlled)
            {
                yield return new Gizmo_ManaBar(this);

                // ★変更：アビリティを持っている時だけ表示
                RPGAbilityDef healDef = DefDatabase<RPGAbilityDef>.GetNamedSilentFail("RPG_MedicalTouch");

                if (healDef != null && this.GetAbilityLevel(healDef) > 0)
                {
                    yield return new Command_Toggle
                    {
                        defaultLabel = "Auto Heal",
                        defaultDesc = $"ONにすると、手当て時にMPを消費して追加回復を行います。\n(Cost: {healDef.manaCost})",
                        icon = ContentFinder<Texture2D>.Get("UI/Designators/Tame"),
                        isActive = () => autoHealEnabled,
                        toggleAction = () => autoHealEnabled = !autoHealEnabled
                    };
                }
            }
        }

        // クラス変更ロジック
        public void SetClass(RPGClassDef newClass)
        {
            Pawn pawn = parent as Pawn;
            if (pawn == null) return;

            if (currentClass?.classPassiveHediff != null)
            {
                Hediff old = pawn.health.hediffSet.GetFirstHediffOfDef(currentClass.classPassiveHediff);
                if (old != null) pawn.health.RemoveHediff(old);
            }

            currentClass = newClass;

            if (currentClass?.classPassiveHediff != null)
            {
                pawn.health.AddHediff(HediffMaker.MakeHediff(currentClass.classPassiveHediff, pawn));
            }
        }
    }
    public class CompProperties_RPG : CompProperties
    {
        public CompProperties_RPG()
        {
            this.compClass = typeof(CompRPG);
        }
    }
}