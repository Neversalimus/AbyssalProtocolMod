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
        public int ticksPerFrame = 10;
        public float drawScale = 1f;
        public float layerOffset = 0.0035f;
        public bool disableWhenDead = true;
        public bool disableWhenDowned = true;

        public CompProperties_ArchonAnimatedBody()
        {
            compClass = typeof(CompArchonAnimatedBody);
        }
    }

    public class CompArchonAnimatedBody : ThingComp
    {
        private static readonly Dictionary<string, Material[]> MaterialCache = new Dictionary<string, Material[]>();

        public CompProperties_ArchonAnimatedBody Props => (CompProperties_ArchonAnimatedBody)props;

        private Pawn Pawn => parent as Pawn;

        public override void PostDraw()
        {
            base.PostDraw();

            Pawn pawn = Pawn;
            if (pawn == null || !pawn.Spawned || pawn.MapHeld == null)
            {
                return;
            }

            if (!ShouldBeActive(pawn))
            {
                return;
            }

            Material material = GetCurrentMaterial(pawn);
            if (material == null)
            {
                return;
            }

            Vector2 drawSize = pawn.def?.graphicData?.drawSize ?? Vector2.one;
            float width = drawSize.x * Mathf.Max(0.01f, Props.drawScale);
            float height = drawSize.y * Mathf.Max(0.01f, Props.drawScale);

            Vector3 drawPos = pawn.DrawPos;
            drawPos.y = Altitudes.AltitudeFor(AltitudeLayer.Pawn) + Props.layerOffset;

            if (pawn.Rotation == Rot4.West)
            {
                width = -width;
            }

            Matrix4x4 matrix = Matrix4x4.TRS(
                drawPos,
                Quaternion.identity,
                new Vector3(width, 1f, height));

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

        private Material GetCurrentMaterial(Pawn pawn)
        {
            string baseTexPath;
            switch (pawn.Rotation.AsInt)
            {
                case 0:
                    baseTexPath = Props.northTexPath;
                    break;
                case 1:
                case 3:
                    baseTexPath = Props.eastTexPath;
                    break;
                default:
                    baseTexPath = Props.southTexPath;
                    break;
            }

            Material[] materials = GetMaterialsFor(baseTexPath);
            if (materials == null || materials.Length == 0)
            {
                return null;
            }

            int ticksGame = Find.TickManager?.TicksGame ?? 0;
            int ticksPerFrame = Mathf.Max(1, Props.ticksPerFrame);
            int frame = Mathf.Abs((ticksGame / ticksPerFrame) + pawn.thingIDNumber) % materials.Length;
            return materials[frame];
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
                materials[i] = MaterialPool.MatFrom(baseTexPath + "_" + i, ShaderDatabase.Cutout);
            }

            MaterialCache[baseTexPath] = materials;
            return materials;
        }
    }
}
