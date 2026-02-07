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
            Widgets.DrawBoxSolid(barRect, new Color(0.2f, 0.2f, 0.2f));

            float fillPercent = comp.currentMP / comp.MaxMP;
            Rect fillRect = barRect.ContractedBy(2f);
            fillRect.width *= fillPercent;
            Widgets.DrawBoxSolid(fillRect, new Color(0f, 0.4f, 0.8f));

            Text.Anchor = TextAnchor.MiddleCenter;
            Text.Font = GameFont.Small;
            Widgets.Label(barRect, $"{comp.currentMP:F0} / {comp.MaxMP:F0}");
            Text.Anchor = TextAnchor.UpperLeft;

            return new GizmoResult(GizmoState.Clear);
        }
    }
}