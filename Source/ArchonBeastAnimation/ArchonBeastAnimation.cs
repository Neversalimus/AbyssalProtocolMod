using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    public class CompProperties_ArchonAnimatedBody : CompProperties
    {
        public string southTexPath = "Pawn/ArchonBeast/ArchonBeastAnim_south";
        public string northTexPath = "Pawn/ArchonBeast/ArchonBeastAnim_north";
        public string eastTexPath = "Pawn/ArchonBeast/ArchonBeastAnim_east";

        public int frameCount = 4;
        public int ticksPerFrame = 7;

        public float southWidth = 7.2f;
        public float southHeight = 7.2f;
        public float northWidth = 7.2f;
        public float northHeight = 7.2f;
        public float eastWidth = 7.2f;
        public float eastHeight = 7.2f;

        public float southOffsetX = 0f;
        public float southOffsetZ = 0f;
        public float northOffsetX = 0f;
        public float northOffsetZ = 0f;
        public float eastOffsetX = 0f;
        public float eastOffsetZ = 0f;

        public bool disableWhenDead = true;
        public bool disableWhenDowned = false;

        public CompProperties_ArchonAnimatedBody()
        {
            compClass = typeof(CompArchonAnimatedBody);
        }
    }

    public class CompArchonAnimatedBody : ThingComp
    {
        private static readonly Dictionary<string, Material[]> MaterialCache = new Dictionary<string, Material[]>();

        private Pawn Pawn => parent as Pawn;
        public CompProperties_ArchonAnimatedBody Props => (CompProperties_ArchonAnimatedBody)props;

        public override void PostDraw()
        {
            base.PostDraw();

            Pawn pawn = Pawn;
            if (pawn == null || pawn.MapHeld == null || !pawn.Spawned)
            {
                return;
            }

            if (!ShouldBeActive(pawn))
            {
                return;
            }

            string baseTexPath = GetBaseTexPath(pawn, out bool mirrorEastForWest);
            if (string.IsNullOrEmpty(baseTexPath))
            {
                return;
            }

            Material[] materials = GetMaterialsFor(baseTexPath);
            if (materials == null || materials.Length == 0)
            {
                return;
            }

            int ticksGame = Find.TickManager?.TicksGame ?? 0;
            int ticksPerFrame = Mathf.Max(1, Props.ticksPerFrame);
            int frameIndex = Mathf.Abs((ticksGame / ticksPerFrame) + pawn.thingIDNumber * 7) % materials.Length;
            Material material = materials[frameIndex];
            if (material == null)
            {
                return;
            }

            Vector2 drawSize = GetDrawSize(pawn, mirrorEastForWest);
            Vector3 drawPos = GetDrawPos(pawn, mirrorEastForWest);

            float scaleX = mirrorEastForWest ? -drawSize.x : drawSize.x;
            Matrix4x4 matrix = Matrix4x4.TRS(
                drawPos,
                Quaternion.identity,
                new Vector3(scaleX, 1f, drawSize.y));

            Graphics.DrawMesh(MeshPool.plane10, matrix, material, 0);
        }

        private bool ShouldBeActive(Pawn pawn)
        {
            if (Props.disableWhenDead && pawn.Dead)
            {
                return false;
            }

            if (Props.disableWhenDowned && pawn.Downed)
            {
                return false;
            }

            return true;
        }

        private string GetBaseTexPath(Pawn pawn, out bool mirrorEastForWest)
        {
            mirrorEastForWest = false;
            switch (pawn.Rotation.AsInt)
            {
                case 0:
                    return Props.northTexPath;
                case 1:
                    return Props.eastTexPath;
                case 2:
                    return Props.southTexPath;
                case 3:
                    mirrorEastForWest = true;
                    return Props.eastTexPath;
                default:
                    return Props.southTexPath;
            }
        }

        private Vector2 GetDrawSize(Pawn pawn, bool mirrorEastForWest)
        {
            switch (pawn.Rotation.AsInt)
            {
                case 0:
                    return new Vector2(Props.northWidth, Props.northHeight);
                case 1:
                case 3:
                    return new Vector2(Props.eastWidth, Props.eastHeight);
                default:
                    return new Vector2(Props.southWidth, Props.southHeight);
            }
        }

        private Vector3 GetDrawPos(Pawn pawn, bool mirrorEastForWest)
        {
            Vector3 drawPos = pawn.DrawPos;
            drawPos.y = Altitudes.AltitudeFor(AltitudeLayer.MetaOverlays) - 0.02f;

            switch (pawn.Rotation.AsInt)
            {
                case 0:
                    drawPos.x += Props.northOffsetX;
                    drawPos.z += Props.northOffsetZ;
                    break;
                case 1:
                    drawPos.x += Props.eastOffsetX;
                    drawPos.z += Props.eastOffsetZ;
                    break;
                case 2:
                    drawPos.x += Props.southOffsetX;
                    drawPos.z += Props.southOffsetZ;
                    break;
                case 3:
                    drawPos.x -= Props.eastOffsetX;
                    drawPos.z += Props.eastOffsetZ;
                    break;
            }

            return drawPos;
        }

        private Material[] GetMaterialsFor(string baseTexPath)
        {
            if (string.IsNullOrEmpty(baseTexPath))
            {
                return null;
            }

            if (MaterialCache.TryGetValue(baseTexPath, out Material[] cached))
            {
                return cached;
            }

            int frameCount = Mathf.Max(1, Props.frameCount);
            Material[] materials = new Material[frameCount];
            for (int i = 0; i < frameCount; i++)
            {
                string texPath = baseTexPath + "_" + i;
                materials[i] = MaterialPool.MatFrom(texPath, ShaderDatabase.Transparent);
            }

            MaterialCache[baseTexPath] = materials;
            return materials;
        }
    }
}
