using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    public class CompProperties_ABY_AnimatedPawnBody : CompProperties
    {
        public string southTexPath = "Pawn/AorticChainHarrower/Anim/ABY_AorticChainHarrower_south";
        public string eastTexPath = "Pawn/AorticChainHarrower/Anim/ABY_AorticChainHarrower_east";
        public string northTexPath = "Pawn/AorticChainHarrower/Anim/ABY_AorticChainHarrower_north";
        public int frameCount = 4;
        public int ticksPerFrame = 12;
        public float drawScale = 1f;
        public float layerOffset = 0.006f;
        public bool disableWhenDead = true;
        public bool disableWhenDowned = false;
        public bool mirrorWestFromEast = true;
        public float overlayAlpha = 0.52f;
        public bool enableAnimation = true;
        public bool respectReducedMotion = true;

        public CompProperties_ABY_AnimatedPawnBody()
        {
            compClass = typeof(CompABY_AnimatedPawnBody);
        }
    }

    public class CompABY_AnimatedPawnBody : ThingComp
    {
        public CompProperties_ABY_AnimatedPawnBody Props => (CompProperties_ABY_AnimatedPawnBody)props;

        public override string CompInspectStringExtra()
        {
            if (AbyssalProtocolMod.Settings == null || !AbyssalProtocolMod.Settings.showDebugInspectStrings)
            {
                return null;
            }

            bool enabled = ABY_AnimatedPawnBodyRenderer.IsAnimationEnabledFor(this);
            return enabled
                ? AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_HarrowerAnimation_DebugEnabled", "Aortic body animation: enabled")
                : AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_HarrowerAnimation_DebugDisabled", "Aortic body animation: disabled");
        }
    }

    [StaticConstructorOnStartup]
    public static class ABY_AnimatedPawnBodyRenderer
    {
        private static readonly Dictionary<string, Material[]> MaterialCache = new Dictionary<string, Material[]>();
        private static bool runtimeDisabled;
        private static int runtimeDisabledTick = -1;
        private static string runtimeDisableReason;

        public static bool IsRuntimeDisabled => runtimeDisabled;
        public static string RuntimeDisableReason => runtimeDisableReason;

        public static bool IsAnimationEnabledFor(CompABY_AnimatedPawnBody comp)
        {
            try
            {
                if (comp == null || comp.Props == null)
                {
                    return false;
                }

                if (!comp.Props.enableAnimation || runtimeDisabled)
                {
                    return false;
                }

                AbyssalProtocolModSettings settings = AbyssalProtocolMod.Settings;
                if (settings == null || !settings.enableAorticHarrowerBodyAnimation)
                {
                    return false;
                }

                if (comp.Props.respectReducedMotion && settings.reducedMotion)
                {
                    return false;
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        public static void DrawAnimatedBody(Pawn pawn, Vector3 drawLoc)
        {
            try
            {
                if (pawn == null || pawn.def == null || !pawn.Spawned || pawn.def.defName != "ABY_AorticChainHarrower")
                {
                    return;
                }

                CompABY_AnimatedPawnBody comp = pawn.TryGetComp<CompABY_AnimatedPawnBody>();
                if (!IsAnimationEnabledFor(comp))
                {
                    return;
                }

                CompProperties_ABY_AnimatedPawnBody props = comp.Props;
                if (props.disableWhenDead && pawn.Dead)
                {
                    return;
                }

                if (props.disableWhenDowned && pawn.Downed)
                {
                    return;
                }

                Material material = GetCurrentMaterial(pawn, props);
                if (material == null)
                {
                    return;
                }

                Vector2 drawSize = pawn.def.graphicData != null ? pawn.def.graphicData.drawSize : Vector2.one;
                float width = Mathf.Max(0.01f, drawSize.x * Mathf.Max(0.01f, props.drawScale));
                float height = Mathf.Max(0.01f, drawSize.y * Mathf.Max(0.01f, props.drawScale));

                if (pawn.Rotation == Rot4.West && props.mirrorWestFromEast)
                {
                    width = -width;
                }

                Vector3 loc = drawLoc;
                loc.y += props.layerOffset;

                Matrix4x4 matrix = Matrix4x4.TRS(loc, Quaternion.identity, new Vector3(width, 1f, height));
                Graphics.DrawMesh(MeshPool.plane10, matrix, material, 0);
            }
            catch (Exception ex)
            {
                DisableForSession("render exception: " + ex.GetType().Name + ": " + ex.Message, true);
            }
        }

        public static void ResetRuntimeDisableForDevTest()
        {
            runtimeDisabled = false;
            runtimeDisabledTick = -1;
            runtimeDisableReason = null;
        }

        private static void DisableForSession(string reason, bool alsoDisableSetting)
        {
            if (runtimeDisabled)
            {
                return;
            }

            runtimeDisabled = true;
            runtimeDisabledTick = SafeTicks();
            runtimeDisableReason = reason ?? "unknown";

            try
            {
                if (alsoDisableSetting && AbyssalProtocolMod.Settings != null)
                {
                    AbyssalProtocolMod.Settings.enableAorticHarrowerBodyAnimation = false;
                    AbyssalProtocolMod.SaveNow();
                }
            }
            catch
            {
                // Settings writes are best-effort. Rendering must remain disabled for this session either way.
            }

            ABY_LogThrottleUtility.Warning(
                "aortic-animated-body-autodisable",
                "[Abyssal Protocol] Aortic Chain Harrower body animation was auto-disabled for safety (" + runtimeDisableReason + "). Re-enable it from Mod Settings after testing.",
                5000);
        }

        private static Material GetCurrentMaterial(Pawn pawn, CompProperties_ABY_AnimatedPawnBody props)
        {
            string texPath = GetDirectionalTexPath(pawn, props);
            if (texPath.NullOrEmpty())
            {
                return null;
            }

            Material[] materials = GetMaterialsFor(texPath, Mathf.Max(1, props.frameCount), Mathf.Clamp01(props.overlayAlpha));
            if (materials == null || materials.Length == 0)
            {
                return null;
            }

            int ticksGame = Find.TickManager != null ? Find.TickManager.TicksGame : 0;
            int ticksPerFrame = Mathf.Max(1, props.ticksPerFrame);
            int seed = pawn.thingIDNumber % Mathf.Max(1, materials.Length);
            int frame = Mathf.Abs((ticksGame / ticksPerFrame) + seed) % materials.Length;
            return materials[frame];
        }

        private static string GetDirectionalTexPath(Pawn pawn, CompProperties_ABY_AnimatedPawnBody props)
        {
            if (pawn == null)
            {
                return null;
            }

            if (pawn.Rotation == Rot4.North)
            {
                return props.northTexPath;
            }

            if (pawn.Rotation == Rot4.East || pawn.Rotation == Rot4.West)
            {
                return props.eastTexPath;
            }

            return props.southTexPath;
        }

        private static Material[] GetMaterialsFor(string baseTexPath, int frameCount, float overlayAlpha)
        {
            if (baseTexPath.NullOrEmpty() || frameCount <= 0)
            {
                return null;
            }

            string key = baseTexPath + "|" + frameCount + "|" + overlayAlpha.ToString("0.###");
            if (MaterialCache.TryGetValue(key, out Material[] cached))
            {
                return cached;
            }

            Material[] materials = new Material[frameCount];
            Color color = new Color(1f, 1f, 1f, Mathf.Clamp01(overlayAlpha));
            for (int i = 0; i < frameCount; i++)
            {
                string texPath = baseTexPath + "_" + i;
                Texture2D texture = ContentFinder<Texture2D>.Get(texPath, false);
                if (texture == null)
                {
                    ABY_LogThrottleUtility.Warning(
                        "aortic-animated-body-missing-tex-" + texPath,
                        "[Abyssal Protocol] Missing Aortic Chain Harrower animation texture: " + texPath + ". Body animation layer will skip this pawn until the asset is present.",
                        5000);
                    return null;
                }

                materials[i] = MaterialPool.MatFrom(texPath, ShaderDatabase.TransparentPostLight, color);
            }

            MaterialCache[key] = materials;
            return materials;
        }

        private static int SafeTicks()
        {
            try
            {
                if (Find.TickManager != null)
                {
                    return Find.TickManager.TicksGame;
                }
            }
            catch
            {
                // Ignore early startup access issues.
            }

            return Environment.TickCount & int.MaxValue;
        }
    }

    [HarmonyPatch(typeof(PawnRenderer), nameof(PawnRenderer.RenderPawnAt))]
    public static class HarmonyPatch_ABY_AnimatedPawnBody_RenderPawnAt
    {
        public static void Postfix(Pawn ___pawn, Vector3 drawLoc)
        {
            ABY_AnimatedPawnBodyRenderer.DrawAnimatedBody(___pawn, drawLoc);
        }
    }
}
