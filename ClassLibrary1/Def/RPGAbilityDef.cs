using RimWorld;
using Verse;
using System.Collections.Generic;
using Verse.Sound;
using UnityEngine;
using System.Linq;

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
        public List<int> upgradeCosts = new List<int>();
        public RPGCategory rpgCategory = RPGCategory.Offense;
        public List<AbilityStatEntry> stats = new List<AbilityStatEntry>();
        public float magicPowerFactor = 0f; // デフォルトは0（影響なし）にしておくと安全だよ
        // XMLで指定しなければ null になる（＝誰でも使える汎用スキル扱い）
        public RPGClassDef requiredClass;
        // ★追加：詠唱開始音と発動音★
        public SoundDef soundImpact; // 命中・効果発生時の音
        public int GetUpgradeCost(int currentLevel)
        {
            // リストが設定されていない場合は、従来の upgradeCost フィールドの値を返す
            if (upgradeCosts == null || upgradeCosts.Count == 0) return upgradeCost;

            // 現在のレベルがリストの範囲内ならその値を、範囲外（最大レベル以上など）なら最後の値を返す
            if (currentLevel < upgradeCosts.Count) return upgradeCosts[currentLevel];
            return upgradeCosts.Last();
        }


    }
    public class Command_RPGAbility : Command_Ability
    {
        public Command_RPGAbility(Ability ability, Pawn pawn) : base(ability, pawn)
        {
        }

        public override string Desc
        {
            get
            {
                string text = base.Desc;

                if (this.Ability is RPGAbility rpgAbility && rpgAbility.def is RPGAbilityDef rpgDef)
                {
                    text += $"\n\n<color=cyan>MP Cost: {rpgDef.manaCost}</color>";

                    CompRPG comp = this.Ability.pawn.GetComp<CompRPG>();
                    if (comp != null && comp.currentMP < rpgDef.manaCost)
                    {
                        text += $"\n<color=red>Not enough MP (Current: {comp.currentMP:F0})</color>";
                    }
                }
                return text;
            }
        }
    }
    // RPGAbilityDef.cs の AbilityStatEntry を修正
    public class AbilityStatEntry
    {
        public string label;
        public float baseValue;
        public float valuePerLevel;
        public string unit = "";

        // 表示フォーマット (例: "F1" なら小数第1位まで, "P0" ならパーセント)
        public string formatString = "F1";

        // ★ここで計算係を指定！デフォルトは標準型にしておく
        public System.Type workerClass = typeof(RPGAbilityStatWorker_Standard);

        // インスタンスをキャッシュしておく変数
        private RPGAbilityStatWorker workerInt;

        public RPGAbilityStatWorker Worker
        {
            get
            {
                if (workerInt == null)
                {
                    workerInt = (RPGAbilityStatWorker)System.Activator.CreateInstance(workerClass);
                }
                return workerInt;
            }
        }
    }

    // --- ここから下は前回の RPGAbility ロジック ---
    public class RPGAbility : Ability
    {
        public RPGAbility(Pawn pawn) : base(pawn) { }
        public RPGAbility(Pawn pawn, AbilityDef def) : base(pawn, def) { }

        public override IEnumerable<Command> GetGizmos()
        {
            // バニラの処理で生成されたギズモをチェック
            foreach (Command cmd in base.GetGizmos())
            {
                if (cmd is Command_Ability)
                {
                    // ★修正2: コンストラクタに this.pawn を渡す
                    Command_RPGAbility newCmd = new Command_RPGAbility(this, this.pawn);

                    // ★修正3: disabled などのプロパティコピーを削除
                    // Command_Abilityのコンストラクタが自動的に Ability.GizmoDisabled() をチェックして
                    // アイコン、無効状態、ラベルなどを正しくセットしてくれるため、手動コピーは不要です。
                    // これにより CS0122 エラーも解消されます。

                    // 表示順序だけは維持しておく
                    newCmd.Order = cmd.Order;

                    yield return newCmd;
                }
                else
                {
                    yield return cmd;
                }
            }
        }

        public override bool Activate(LocalTargetInfo target, LocalTargetInfo dest)
        {
            bool result = base.Activate(target, dest);

            // 発動に成功した場合
            if (result)
            {
                RPGAbilityDef rpgDef = def as RPGAbilityDef;
                CompRPG comp = pawn.GetComp<CompRPG>();

                if (comp != null && rpgDef != null)
                {
                    // 1. MP消費 (既存)
                    comp.TryConsumeMP(rpgDef.manaCost);

                    // 2. 音再生 (既存)
                    if (rpgDef.soundImpact != null)
                    {
                        rpgDef.soundImpact.PlayOneShot(new TargetInfo(target.Cell, pawn.Map));
                    }

                    // --- ★追加: アビリティ使用経験値 ---
                    // マナコストが高い技ほど、経験値が多く入るようにする
                    // 例: マナ10消費 → 20XP
                    float abilityXp = rpgDef.manaCost * 2.0f;

                    // 最低保証
                    if (abilityXp < 10f) abilityXp = 10f;

                    comp.GainXp(abilityXp);

                    if (pawn.Map != null)
                    {
                        MoteMaker.ThrowText(pawn.DrawPos, pawn.Map, $"+{abilityXp:F0} XP", Color.cyan);
                    }
                }
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