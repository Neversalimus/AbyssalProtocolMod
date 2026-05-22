using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    [StaticConstructorOnStartup]
    public static class ABY_ReactorSaintAegisOverlayRenderer
    {
        private static readonly Dictionary<string, Material> MaterialCache = new Dictionary<string, Material>();

        public static void DrawFor(Pawn pawn, Vector3 drawLoc)
        {
            try
            {
                if (pawn == null || pawn.def == null || !pawn.Spawned || pawn.MapHeld == null || pawn.Dead)
                {
                    return;
                }

                if (pawn.def.defName != "ABY_ReactorSaint")
                {
                    return;
                }

                CompABY_ReactorAegis aegis = pawn.TryGetComp<CompABY_ReactorAegis>();
                if (aegis == null || aegis.Props == null || !aegis.Props.drawPawnAegisOverlay)
                {
                    return;
                }

                bool collapse = aegis.CollapseWindowActive;
                bool active = aegis.AegisActive;
                if (!collapse && !active)
                {
                    return;
                }

                CompProperties_ABY_ReactorAegis props = aegis.Props;
                string basePath = collapse ? props.breakOverlayTexPath : props.activeOverlayTexPath;
                string texPath = GetDirectionalTexPath(pawn, basePath);
                if (texPath.NullOrEmpty())
                {
                    return;
                }

                float alpha = ComputeAlpha(aegis, collapse, props);
                if (alpha <= 0.01f)
                {
                    return;
                }

                Material material = GetMaterial(texPath, alpha);
                if (material == null)
                {
                    return;
                }

                Vector2 drawSize = pawn.def.graphicData != null ? pawn.def.graphicData.drawSize : Vector2.one;
                float scale = Mathf.Max(0.01f, props.overlayDrawScale);
                float width = Mathf.Max(0.01f, drawSize.x * scale);
                float height = Mathf.Max(0.01f, drawSize.y * scale);
                if (pawn.Rotation == Rot4.West)
                {
                    width = -width;
                }

                Vector3 loc = drawLoc;
                loc.y += Mathf.Max(0.001f, props.overlayLayerOffset);

                Matrix4x4 matrix = Matrix4x4.TRS(loc, Quaternion.identity, new Vector3(width, 1f, height));
                Graphics.DrawMesh(MeshPool.plane10, matrix, material, 0);
            }
            catch (Exception ex)
            {
                ABY_LogThrottleUtility.Warning(
                    "reactor-saint-aegis-overlay-render-failed",
                    "[Abyssal Protocol] Reactor Saint Aegis pawn overlay failed and was skipped: " + ex.GetType().Name + ": " + ex.Message,
                    2500);
            }
        }

        private static float ComputeAlpha(CompABY_ReactorAegis aegis, bool collapse, CompProperties_ABY_ReactorAegis props)
        {
            int ticksGame = Find.TickManager != null ? Find.TickManager.TicksGame : 0;
            bool reducedMotion = props.respectReducedMotion && AbyssalProtocolMod.Settings != null && AbyssalProtocolMod.Settings.reducedMotion;

            if (collapse)
            {
                float alpha = Mathf.Clamp01(props.breakOverlayAlpha);
                if (!reducedMotion)
                {
                    alpha *= 0.78f + 0.22f * Mathf.Sin(ticksGame * 0.33f);
                }

                int fadeTicks = Mathf.Max(0, props.collapseOverlayFadeTicks);
                if (fadeTicks > 0)
                {
                    int remaining = aegis.CollapseWindowTicksRemaining;
                    if (remaining < fadeTicks)
                    {
                        alpha *= Mathf.Clamp01(remaining / (float)fadeTicks);
                    }
                }

                return Mathf.Clamp01(alpha);
            }

            float activeAlpha = Mathf.Clamp01(props.activeOverlayAlpha);
            if (props.pulseActiveOverlay && !reducedMotion)
            {
                activeAlpha *= 0.84f + 0.16f * Mathf.Sin(ticksGame * 0.075f + (aegis.parent?.thingIDNumber ?? 0));
            }

            return Mathf.Clamp01(activeAlpha);
        }

        private static string GetDirectionalTexPath(Pawn pawn, string baseTexPath)
        {
            if (baseTexPath.NullOrEmpty() || pawn == null)
            {
                return null;
            }

            if (pawn.Rotation == Rot4.North)
            {
                return baseTexPath + "_north";
            }

            if (pawn.Rotation == Rot4.East || pawn.Rotation == Rot4.West)
            {
                return baseTexPath + "_east";
            }

            return baseTexPath + "_south";
        }

        private static Material GetMaterial(string texPath, float alpha)
        {
            if (texPath.NullOrEmpty())
            {
                return null;
            }

            float quantizedAlpha = Mathf.Clamp01(Mathf.Round(alpha * 20f) / 20f);
            string key = texPath + "|" + quantizedAlpha.ToString("0.00");
            if (MaterialCache.TryGetValue(key, out Material cached))
            {
                return cached;
            }

            Texture2D texture = ContentFinder<Texture2D>.Get(texPath, false);
            if (texture == null)
            {
                ABY_LogThrottleUtility.Warning(
                    "reactor-saint-aegis-overlay-missing-" + texPath,
                    "[Abyssal Protocol] Missing Reactor Saint Aegis overlay texture: " + texPath + ". The pawn-state marker will be skipped for this direction.",
                    5000);
                return null;
            }

            Material material = ABY_MaterialCacheUtility.MatFrom(texPath, ShaderDatabase.TransparentPostLight, new Color(1f, 1f, 1f, quantizedAlpha));
            MaterialCache[key] = material;
            return material;
        }
    }

    [HarmonyPatch(typeof(PawnRenderer), nameof(PawnRenderer.RenderPawnAt))]
    public static class HarmonyPatch_ABY_ReactorSaintAegisOverlay_RenderPawnAt
    {
        public static void Postfix(Pawn ___pawn, Vector3 drawLoc)
        {
            ABY_ReactorSaintAegisOverlayRenderer.DrawFor(___pawn, drawLoc);
        }
    }
}
