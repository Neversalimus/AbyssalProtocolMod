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
        public int ticksPerFrame = 8;
        public float drawScale = 1f;
        public float layerOffset = 0.006f;
        public bool disableWhenDead = true;
        public bool disableWhenDowned = false;
        public bool mirrorWestFromEast = true;

        public CompProperties_ABY_AnimatedPawnBody()
        {
            compClass = typeof(CompABY_AnimatedPawnBody);
        }
    }

    public class CompABY_AnimatedPawnBody : ThingComp
    {
        public CompProperties_ABY_AnimatedPawnBody Props => (CompProperties_ABY_AnimatedPawnBody)props;
    }

    [StaticConstructorOnStartup]
    public static class ABY_AnimatedPawnBodyRenderer
    {
        private static readonly Dictionary<string, Material[]> MaterialCache = new Dictionary<string, Material[]>();

        public static void DrawAnimatedBody(Pawn pawn, Vector3 drawLoc)
        {
            if (pawn == null || pawn.def == null || !pawn.Spawned)
            {
                return;
            }

            CompABY_AnimatedPawnBody comp = pawn.TryGetComp<CompABY_AnimatedPawnBody>();
            if (comp == null || comp.Props == null)
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

        private static Material GetCurrentMaterial(Pawn pawn, CompProperties_ABY_AnimatedPawnBody props)
        {
            string texPath = GetDirectionalTexPath(pawn, props);
            if (texPath.NullOrEmpty())
            {
                return null;
            }

            Material[] materials = GetMaterialsFor(texPath, Mathf.Max(1, props.frameCount));
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

        private static Material[] GetMaterialsFor(string baseTexPath, int frameCount)
        {
            if (baseTexPath.NullOrEmpty() || frameCount <= 0)
            {
                return null;
            }

            string key = baseTexPath + "|" + frameCount;
            if (MaterialCache.TryGetValue(key, out Material[] cached))
            {
                return cached;
            }

            Material[] materials = new Material[frameCount];
            for (int i = 0; i < frameCount; i++)
            {
                materials[i] = MaterialPool.MatFrom(baseTexPath + "_" + i, ShaderDatabase.Cutout);
            }

            MaterialCache[key] = materials;
            return materials;
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
