using UnityEngine;
using Verse;
using RimWorld;

namespace MyRPGMod
{
    public class Gizmo_ManaBar : Gizmo
    {
        public CompRPG comp;

        public Gizmo_ManaBar(CompRPG comp)
        {
            this.comp = comp;
            this.Order = -100f;
        }

        public override float GetWidth(float maxWidth) => 140f;

        public override GizmoResult GizmoOnGUI(Vector2 topLeft, float maxWidth, GizmoRenderParms parms)
        {
            Rect rect = new Rect(topLeft.x, topLeft.y, GetWidth(maxWidth), 75f);
            Widgets.DrawWindowBackground(rect);

            Rect innerRect = rect.ContractedBy(6f);

            Text.Font = GameFont.Tiny;
            Widgets.Label(innerRect, "Mana (MP)");

            Rect barRect = new Rect(innerRect.x, innerRect.y + 20f, innerRect.width, 24f);

            // 1. 背景（空のゲージ）を描画
            Widgets.DrawBoxSolid(barRect, new Color(0.2f, 0.2f, 0.2f));

            float currentMP = comp.currentMP;
            float maxMP = comp.MaxMP;

            // --- 消費予測の計算 ---
            // --- 消費予測の計算 ---
            float predictedConsumption = 0f;

            // 修正箇所: targetingVerb ではなく targetingSource を使う
            if (Find.Targeter.IsTargeting && Find.Targeter.targetingSource != null)
            {
                // ターゲットソースから Verb (実行しようとしているアクション) を取得
                Verb verb = Find.Targeter.targetingSource.GetVerb;

                // Verbが存在し、かつその使用者がこのバーの持ち主であるか確認
                if (verb != null && verb.CasterPawn == comp.parent)
                {
                    // そのVerbが「アビリティの使用 (Verb_CastAbility)」であるか確認してキャスト
                    if (verb is Verb_CastAbility castAbilityVerb)
                    {
                        // アビリティ情報を取得
                        Ability ability = castAbilityVerb.ability;

                        // それがRPG用のアビリティ定義を持っていればコストを取得
                        if (ability != null && ability.def is RPGAbilityDef rpgDef)
                        {
                            predictedConsumption = rpgDef.manaCost;
                        }
                    }
                }
            }

            // --- ゲージの描画 ---

            // A. 消費後の残量（確定している青色部分）
            float remainingMP = Mathf.Max(0f, currentMP - predictedConsumption);
            float remainingPercent = remainingMP / maxMP;

            Rect fillRect = barRect.ContractedBy(2f);
            Rect safeRect = new Rect(fillRect);
            safeRect.width *= remainingPercent;

            Widgets.DrawBoxSolid(safeRect, new Color(0f, 0.4f, 0.8f)); // 通常の青色

            // B. 消費予定分（点滅させる黄色/赤色部分）
            if (predictedConsumption > 0f)
            {
                float costPercent = Mathf.Min(predictedConsumption, currentMP) / maxMP;
                Rect costRect = new Rect(fillRect);
                costRect.x += safeRect.width; // 青色の直後から開始
                costRect.width *= costPercent;

                // 点滅エフェクト (Alpha値を時間で変動させる)
                float alpha = 0.4f + Mathf.PingPong(Time.time * 3f, 0.6f);
                Color blinkColor = new Color(1f, 0.8f, 0f, alpha); // 黄色で点滅

                // もし足りないなら赤く点滅
                if (currentMP < predictedConsumption)
                {
                    blinkColor = new Color(1f, 0f, 0f, alpha);
                }

                Widgets.DrawBoxSolid(costRect, blinkColor);
            }

            // テキスト描画
            Text.Anchor = TextAnchor.MiddleCenter;
            Text.Font = GameFont.Small;

            string label = $"{currentMP:F0} / {maxMP:F0}";

            // 消費中は「50 -> 30」のように表示するとより親切
            if (predictedConsumption > 0f)
            {
                float afterMP = currentMP - predictedConsumption;
                Color textColor = (afterMP >= 0) ? Color.yellow : Color.red;
                string arrow = (afterMP >= 0) ? "->" : "(!)";
                // ラベルの上書き
                label = $"{currentMP:F0} {arrow} {afterMP:F0}";

                GUI.color = textColor; // 文字色変更
            }

            Widgets.Label(barRect, label);

            // 設定を戻す
            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperLeft;

            return new GizmoResult(GizmoState.Clear);
        }
    }
}