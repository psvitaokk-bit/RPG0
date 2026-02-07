using System;
using System.Collections.Generic;
using Verse;
using RimWorld;
using System.Runtime.Remoting.Messaging;

namespace MyRPGMod
{
    // 入植者にくっつけるデータ（レベル、経験値、スキルポイント）
    public class CompRPG : ThingComp
    {
        public int level = 1;
        public float currentXp = 0;
        public int skillPoints = 0;
        public float currentMP = 0;

        // 最大MP：レベル × 20 (Lv10なら200)
        public float MaxMP => level * 20f;
        // 次のレベルに必要な経験値（とりあえず固定式）
        public List<string> unlockedAbilities = new List<string>();

        public float XpToNextLevel => level * 1000f;
        public override void CompTick()
        {
            base.CompTick();
            // 死んでたりマップにいなかったら処理しない
            Pawn pawn = parent as Pawn;
            if (pawn == null || pawn.Dead || !pawn.Spawned) return;

            // 60Tickに1回だけ計算（重くならないように）
            if (pawn.IsHashIntervalTick(60))
            {
                if (currentMP < MaxMP)
                {
                    currentMP += 1.0f; // 毎秒1回復（調整してね）
                    if (currentMP > MaxMP) currentMP = MaxMP;
                }
            }
        }
        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            // 親クラスのギズモも一応出す
            foreach (var g in base.CompGetGizmosExtra()) yield return g;

            // 自分の入植者だけMPバーを出す
            Pawn pawn = parent as Pawn;
            if (pawn != null && pawn.IsColonistPlayerControlled)
            {
                yield return new Gizmo_ManaBar(this);
            }
        }
        public bool TryConsumeMP(float cost)
        {
            if (currentMP >= cost)
            {
                currentMP -= cost;
                return true;
            }
            return false;
        }
        // 経験値を獲得するメソッド
        public void GainXp(float amount)
        {
            currentXp += amount;
            if (currentXp >= XpToNextLevel)
            {
                LevelUp();
            }
        }
        // ★追加：アビリティをアンロックする処理★
        public void UnlockAbility(AbilityDef abilityDef)
        {
            if (!unlockedAbilities.Contains(abilityDef.defName))
            {
                // リストに記録
                unlockedAbilities.Add(abilityDef.defName);

                // 実際にポーンにアビリティ（魔法）を使えるように付与する
                Pawn pawn = parent as Pawn;
                if (pawn != null)
                {
                    pawn.abilities.GainAbility(abilityDef);
                }

                Messages.Message($"{pawn.LabelShort} learned {abilityDef.label}!", MessageTypeDefOf.PositiveEvent);
            }
        }
        // レベルアップ処理
        public void LevelUp()
        {
            currentXp -= XpToNextLevel;
            level++;
            skillPoints++; // スキルポイント付与

            // 派手にメッセージを出す
            Messages.Message($"{parent.LabelShort} is now Level {level}!", MessageTypeDefOf.PositiveEvent);
        }

        // セーブデータに保存・読み込みするための必須処理
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref level, "level", 1);
            Scribe_Values.Look(ref currentXp, "currentXp", 0);
            Scribe_Values.Look(ref skillPoints, "skillPoints", 0);

            // ★追加：リストの保存★
            Scribe_Collections.Look(ref unlockedAbilities, "unlockedAbilities", LookMode.Value);
            Scribe_Values.Look(ref currentMP, "currentMP", 0);
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