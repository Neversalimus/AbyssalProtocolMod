using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    [StaticConstructorOnStartup]
    public class Building_ABY_SigilVault : Building_Storage
    {
        private const int MaxSigils = 20;

        private static readonly Vector3[] OverlayOffsets =
        {
            new Vector3(-0.34f, 0f, 0.24f),
            new Vector3(0.34f, 0f, 0.24f),
            new Vector3(-0.34f, 0f, -0.24f),
            new Vector3(0.34f, 0f, -0.24f)
        };

        public override IEnumerable<IntVec3> AllSlotCells()
        {
            if (!Spawned)
            {
                yield break;
            }

            CellRect rect = OccupiedRect();
            yield return new IntVec3(rect.minX, 0, rect.minZ);
            yield return new IntVec3(rect.maxX, 0, rect.minZ);
        }

        public override string GetInspectString()
        {
            string baseText = base.GetInspectString();
            string countText = "ABY_SigilVault_Stored".Translate(StoredSigilCount, MaxSigils).Resolve();
            string acceptText = "ABY_SigilVault_Accepts".Translate().Resolve();

            if (string.IsNullOrEmpty(baseText))
            {
                return countText + "\n" + acceptText;
            }

            return baseText + "\n" + countText + "\n" + acceptText;
        }

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            base.DrawAt(drawLoc, flip);
            DrawSigilOverlays(drawLoc);
        }

        private int StoredSigilCount
        {
            get
            {
                if (!Spawned || Map == null)
                {
                    return 0;
                }

                int total = 0;
                foreach (IntVec3 cell in AllSlotCells())
                {
                    List<Thing> things = Map.thingGrid.ThingsListAtFast(cell);
                    for (int i = 0; i < things.Count; i++)
                    {
                        Thing thing = things[i];
                        if (thing != null && IsSigilThing(thing))
                        {
                            total += thing.stackCount;
                        }
                    }
                }

                return total;
            }
        }

        private ThingDef GetDisplaySigilDef()
        {
            if (!Spawned || Map == null)
            {
                return DefDatabase<ThingDef>.GetNamedSilentFail("ABY_ArchonSigil");
            }

            foreach (IntVec3 cell in AllSlotCells())
            {
                List<Thing> things = Map.thingGrid.ThingsListAtFast(cell);
                for (int i = things.Count - 1; i >= 0; i--)
                {
                    Thing thing = things[i];
                    if (thing != null && IsSigilThing(thing))
                    {
                        return thing.def;
                    }
                }
            }

            return DefDatabase<ThingDef>.GetNamedSilentFail("ABY_ArchonSigil");
        }

        private void DrawSigilOverlays(Vector3 drawLoc)
        {
            int total = StoredSigilCount;
            if (total <= 0)
            {
                return;
            }

            ThingDef displayDef = GetDisplaySigilDef();
            string texPath = displayDef?.graphicData?.texPath;
            if (string.IsNullOrEmpty(texPath))
            {
                return;
            }

            int visibleSigils = Mathf.Clamp(Mathf.CeilToInt(total / 5f), 1, 4);
            float angle = Rotation.AsAngle;
            Material material = MaterialPool.MatFrom(texPath, ShaderDatabase.Cutout);

            for (int i = 0; i < visibleSigils; i++)
            {
                Vector3 pos = drawLoc + RotateOffset(OverlayOffsets[i], Rotation);
                pos.y = Altitudes.AltitudeFor(AltitudeLayer.Item) + 0.024f + (i * 0.001f);

                float scale = 0.34f;
                Matrix4x4 matrix = Matrix4x4.identity;
                matrix.SetTRS(pos, Quaternion.AngleAxis(angle, Vector3.up), new Vector3(scale, 1f, scale));
                Graphics.DrawMesh(MeshPool.plane10, matrix, material, 0);
            }
        }

        private static bool IsSigilThing(Thing thing)
        {
            return thing != null &&
                   thing.def != null &&
                   thing.def.defName != null &&
                   thing.def.EverStorable(false) &&
                   thing.def.defName.IndexOf("Sigil", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static Vector3 RotateOffset(Vector3 offset, Rot4 rotation)
        {
            switch (rotation.AsInt)
            {
                case 1:
                    return new Vector3(offset.z, offset.y, -offset.x);
                case 2:
                    return new Vector3(-offset.x, offset.y, -offset.z);
                case 3:
                    return new Vector3(-offset.z, offset.y, offset.x);
                default:
                    return offset;
            }
        }
    }
}
