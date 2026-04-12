using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    public class Thing_AbyssalAnimatedItem : ThingWithComps
    {
        private const float GlowBaseScale = 1.75f;
        private const float OuterRingScale = 1.42f;
        private const float InnerRingScale = 1.12f;
        private const float HoverAmplitude = 0.018f;
        private const float HoverSpeed = 0.045f;
        private const float PulseSpeed = 0.060f;
        private const float OuterRotationPerTick = 1.10f;
        private const float InnerRotationPerTick = -0.70f;
        private const float OverlayAltitude = 0.036f;

        public override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            base.DrawAt(drawLoc, flip);

            if (!Spawned || Map == null || def?.graphicData == null || string.IsNullOrEmpty(def.graphicData.texPath))
            {
                return;
            }

            DrawAnimatedLayers(drawLoc);
        }

        private void DrawAnimatedLayers(Vector3 drawLoc)
        {
            int ticks = Find.TickManager.TicksGame + (thingIDNumber % 251);
            float pulse01 = (Mathf.Sin(ticks * PulseSpeed) + 1f) * 0.5f;
            float hover = Mathf.Sin(ticks * HoverSpeed) * HoverAmplitude;

            Vector3 overlayLoc = drawLoc;
            overlayLoc.y += OverlayAltitude + hover;

            float glowScale = GlowBaseScale * Mathf.Lerp(0.96f, 1.08f, pulse01);
            float glowAlpha = Mathf.Lerp(0.30f, 0.62f, pulse01);
            float outerAlpha = Mathf.Lerp(0.42f, 0.88f, pulse01);
            float innerAlpha = Mathf.Lerp(0.22f, 0.55f, pulse01);

            DrawPlane(GetTexturePath("_Glow"), overlayLoc, glowScale, 0f, MakeColor(1f, 0.30f, 0.14f, glowAlpha), true);
            DrawPlane(GetTexturePath("_Ring"), overlayLoc, OuterRingScale, ticks * OuterRotationPerTick, MakeColor(1f, 0.38f, 0.20f, outerAlpha), true);
            DrawPlane(GetTexturePath("_Ring"), overlayLoc, InnerRingScale, ticks * InnerRotationPerTick, MakeColor(1f, 0.16f, 0.10f, innerAlpha), true);
        }

        private string GetTexturePath(string suffix)
        {
            return def.graphicData.texPath + suffix;
        }

        private static Color MakeColor(float r, float g, float b, float a)
        {
            return new Color(r, g, b, QuantizeAlpha(a));
        }

        private static float QuantizeAlpha(float value)
        {
            return Mathf.Clamp01(Mathf.Round(value * 48f) / 48f);
        }

        private static void DrawPlane(string texPath, Vector3 loc, float scale, float angle, Color color, bool postLight)
        {
            if (ContentFinder<Texture2D>.Get(texPath, false) == null)
            {
                return;
            }

            Material material = MaterialPool.MatFrom(
                texPath,
                postLight ? ShaderDatabase.TransparentPostLight : ShaderDatabase.Transparent,
                color);

            Matrix4x4 matrix = Matrix4x4.identity;
            matrix.SetTRS(loc, Quaternion.AngleAxis(angle, Vector3.up), new Vector3(scale, 1f, scale));
            Graphics.DrawMesh(MeshPool.plane10, matrix, material, 0);
        }
    }
}
