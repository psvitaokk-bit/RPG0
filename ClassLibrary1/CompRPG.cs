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
        public bool autoHealEnabled = true; // デフォルトはON
        public float MaxMP => level * 20f;
        public float XpToNextLevel => level * 1000f;
        // クラスを変更する時の処理
        public void SetClass(RPGClassDef newClass)
        {
            Pawn pawn = parent as Pawn;
            if (pawn == null) return;

            // 1. 古いクラスのパッシブ効果を消す
            if (this.currentClass != null && this.currentClass.classPassiveHediff != null)
            {
                Hediff oldHediff = pawn.health.hediffSet.GetFirstHediffOfDef(this.currentClass.classPassiveHediff);
                if (oldHediff != null)
                {
                    pawn.health.RemoveHediff(oldHediff);
                }
            }

            // 2. クラス情報を更新
            this.currentClass = newClass;

            // 3. 新しいクラスのパッシブ効果を付ける
            if (this.currentClass != null && this.currentClass.classPassiveHediff != null)
            {
                // 既に持っていないか確認してから追加
                if (pawn.health.hediffSet.GetFirstHediffOfDef(this.currentClass.classPassiveHediff) == null)
                {
                    Hediff newHediff = HediffMaker.MakeHediff(this.currentClass.classPassiveHediff, pawn);
                    // 部位指定なし（全身）で追加
                    pawn.health.AddHediff(newHediff);
                }
            }
        }


        // レベル管理用の辞書
        public Dictionary<string, int> abilityLevels = new Dictionary<string, int>();

        public int GetAbilityLevel(AbilityDef def)
        {
            if (abilityLevels == null) abilityLevels = new Dictionary<string, int>();
            if (def == null) return 0;
            return abilityLevels.ContainsKey(def.defName) ? abilityLevels[def.defName] : 0;
        }
        // CompRPG.cs の MaxMP とかがあるあたりに追加してね
        public float GetMagicMultiplier(RPGAbilityDef def)
        {
            Pawn pawn = parent as Pawn;
            // 自分では計算せず、新しく作った便利屋さんに丸投げする！
            return RPGCalculationUtil.GetMagicMultiplier(pawn, def);
        }

        // もし "MagicPower" のエラーも出ていたら、これも足しておいてね
        public float MagicPower
        {
            get
            {
                Pawn pawn = parent as Pawn;
                return RPGCalculationUtil.GetMagicPower(pawn);
            }
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

                    // ★修正点：文字列 "Passive" ではなく、列挙型の RPGCategory.Passive を使う
                    if (def.rpgCategory != RPGCategory.Passive)
                    {
                        (parent as Pawn)?.abilities.GainAbility(def);
                    }
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

    
        public override void CompTickRare()
        {
            base.CompTickRare(); // 親クラスの処理

            Pawn p = parent as Pawn;
            if (p == null || p.Dead) return;

            // 1. MPの自然回復 (マップにいる時のみ)
            if (p.Map != null)
            {
                if (currentMP < MaxMP)
                {
                    // 250Tick(約4秒)ごとの回復量
                    currentMP = Mathf.Min(currentMP + 4.0f, MaxMP);
                }
            }

            // 2. パッシブ効果の維持チェック
            // ★重要：currentClass が null じゃないか必ずチェックする！
            if (currentClass != null && currentClass.classPassiveHediff != null)
            {
                // まだバフを持っていないなら付与する
                if (p.health.hediffSet.GetFirstHediffOfDef(currentClass.classPassiveHediff) == null)
                {
                    p.health.AddHediff(HediffMaker.MakeHediff(currentClass.classPassiveHediff, p));
                }
            }
        }
        public RPGClassDef currentClass;
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref level, "level", 1);
            Scribe_Values.Look(ref currentXp, "currentXp", 0);
            Scribe_Values.Look(ref skillPoints, "skillPoints", 0);
            Scribe_Values.Look(ref currentMP, "currentMP", 0);
            Scribe_Collections.Look(ref abilityLevels, "abilityLevels", LookMode.Value, LookMode.Value);
            Scribe_Values.Look(ref autoHealEnabled, "autoHealEnabled", true);
            Scribe_Defs.Look(ref currentClass, "currentClass");
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