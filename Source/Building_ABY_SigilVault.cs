using System;
using System.Collections.Generic;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    [StaticConstructorOnStartup]
    public class Building_ABY_SigilVault : Building, IThingHolder
    {
        public const int MaxSigilCapacity = 20;

        private ThingOwner<Thing> innerContainer;

        private static readonly Vector3[] OverlayOffsets =
        {
            new Vector3(-0.36f, 0f, 0.24f),
            new Vector3(0.36f, 0f, 0.24f),
            new Vector3(-0.36f, 0f, -0.24f),
            new Vector3(0.36f, 0f, -0.24f)
        };

        private static readonly Dictionary<string, Material> OverlayMaterialCache = new Dictionary<string, Material>(StringComparer.Ordinal);
        private static List<ThingDef> cachedAcceptedSigilDefs;

        public Building_ABY_SigilVault()
        {
            innerContainer = new ThingOwner<Thing>(this, false);
        }

        public ThingOwner GetDirectlyHeldThings()
        {
            if (innerContainer == null)
            {
                innerContainer = new ThingOwner<Thing>(this, false);
            }

            return innerContainer;
        }

        public void GetChildHolders(List<IThingHolder> outChildren)
        {
            ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, GetDirectlyHeldThings());
        }

        public int StoredSigilCount
        {
            get
            {
                if (innerContainer == null)
                {
                    return 0;
                }

                int total = 0;
                for (int i = 0; i < innerContainer.Count; i++)
                {
                    Thing thing = innerContainer[i];
                    if (thing != null)
                    {
                        total += thing.stackCount;
                    }
                }

                return total;
            }
        }

        public int FreeSigilSlots => Math.Max(0, MaxSigilCapacity - StoredSigilCount);

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Deep.Look(ref innerContainer, "innerContainer", this);
            if (Scribe.mode == LoadSaveMode.PostLoadInit && innerContainer == null)
            {
                innerContainer = new ThingOwner<Thing>(this, false);
            }
        }

        public override string GetInspectString()
        {
            StringBuilder sb = new StringBuilder();
            string baseText = base.GetInspectString();
            if (!string.IsNullOrEmpty(baseText))
            {
                sb.Append(baseText.TrimEnd());
            }

            AppendLine(sb, "ABY_SigilVault_Stored".Translate(StoredSigilCount, MaxSigilCapacity));
            AppendLine(sb, "ABY_SigilVault_Accepts".Translate());

            if (StoredSigilCount > 0)
            {
                foreach (ThingDef def in AcceptedSigilDefs)
                {
                    int count = CountSigilsOfDef(def);
                    if (count > 0)
                    {
                        AppendLine(sb, "ABY_SigilVault_CountLine".Translate(def.LabelCap, count));
                    }
                }
            }

            return sb.ToString().TrimEnd();
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (Gizmo gizmo in base.GetGizmos())
            {
                yield return gizmo;
            }

            yield return new Command_Action
            {
                defaultLabel = "ABY_SigilVault_EjectOneLabel".Translate(),
                defaultDesc = "ABY_SigilVault_EjectOneDesc".Translate(),
                icon = ContentFinder<Texture2D>.Get("UI/Commands/Drop", true),
                action = TryEjectOne
            };

            yield return new Command_Action
            {
                defaultLabel = "ABY_SigilVault_EjectAllLabel".Translate(),
                defaultDesc = "ABY_SigilVault_EjectAllDesc".Translate(),
                icon = ContentFinder<Texture2D>.Get("UI/Commands/Drop", true),
                action = EjectAllContents
            };
        }

        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            if (mode != DestroyMode.Vanish)
            {
                EjectAllContents();
            }

            base.Destroy(mode);
        }

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            base.DrawAt(drawLoc, flip);
            DrawStoredSigils(drawLoc);
        }

        public bool CanAccept(Thing thing)
        {
            if (thing == null || thing.def == null)
            {
                return false;
            }

            if (!IsAcceptedSigilDef(thing.def))
            {
                return false;
            }

            return StoredSigilCount + Math.Max(1, thing.stackCount) <= MaxSigilCapacity;
        }

        public bool TryAbsorbThing(Thing thing)
        {
            if (!CanAccept(thing))
            {
                return false;
            }

            bool wasSpawned = thing.Spawned;
            Map map = MapHeld;
            IntVec3 fallbackCell = GetDropCell();

            if (wasSpawned)
            {
                thing.DeSpawn();
            }

            if (innerContainer.TryAdd(thing))
            {
                return true;
            }

            if (wasSpawned && map != null)
            {
                GenPlace.TryPlaceThing(thing, fallbackCell, map, ThingPlaceMode.Near);
            }

            return false;
        }

        public static bool IsAcceptedSigilDef(ThingDef def)
        {
            return def != null
                && def.EverStorable(false)
                && !string.IsNullOrEmpty(def.defName)
                && def.defName.StartsWith("ABY_", StringComparison.OrdinalIgnoreCase)
                && def.defName.IndexOf("Sigil", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        public static List<ThingDef> AcceptedSigilDefs
        {
            get
            {
                if (cachedAcceptedSigilDefs == null)
                {
                    cachedAcceptedSigilDefs = new List<ThingDef>();
                    List<ThingDef> allDefs = DefDatabase<ThingDef>.AllDefsListForReading;
                    for (int i = 0; i < allDefs.Count; i++)
                    {
                        ThingDef def = allDefs[i];
                        if (IsAcceptedSigilDef(def))
                        {
                            cachedAcceptedSigilDefs.Add(def);
                        }
                    }
                }

                return cachedAcceptedSigilDefs;
            }
        }

        private void TryEjectOne()
        {
            if (innerContainer == null || innerContainer.Count == 0 || MapHeld == null)
            {
                return;
            }

            Thing first = innerContainer[0];
            innerContainer.TryDrop(first, GetDropCell(), MapHeld, ThingPlaceMode.Near, out _);
        }

        private void EjectAllContents()
        {
            if (innerContainer == null || innerContainer.Count == 0 || MapHeld == null)
            {
                return;
            }

            for (int i = innerContainer.Count - 1; i >= 0; i--)
            {
                Thing thing = innerContainer[i];
                if (thing == null)
                {
                    continue;
                }

                innerContainer.TryDrop(thing, GetDropCell(), MapHeld, ThingPlaceMode.Near, out _);
            }
        }

        private IntVec3 GetDropCell()
        {
            if (!Spawned || Map == null)
            {
                return PositionHeld;
            }

            CellRect rect = OccupiedRect();
            IntVec3 best = Position;
            int bestDist = int.MaxValue;

            foreach (IntVec3 cell in rect.ExpandedBy(1))
            {
                if (!cell.InBounds(Map) || !cell.Walkable(Map) || rect.Contains(cell))
                {
                    continue;
                }

                int dist = cell.DistanceToSquared(rect.CenterCell);
                if (dist < bestDist)
                {
                    best = cell;
                    bestDist = dist;
                }
            }

            return best;
        }

        private void DrawStoredSigils(Vector3 drawLoc)
        {
            int stored = StoredSigilCount;
            if (stored <= 0 || innerContainer == null || innerContainer.Count == 0)
            {
                return;
            }

            int visible = Mathf.Clamp(Mathf.CeilToInt(stored / 5f), 1, 4);
            float angle = Rotation.AsAngle;

            for (int i = 0; i < visible; i++)
            {
                ThingDef def = GetDisplayedDefForIndex(i, visible);
                Material material = GetOverlayMaterial(def);
                if (material == null)
                {
                    continue;
                }

                Vector3 pos = drawLoc + RotateOffset(OverlayOffsets[i], Rotation);
                pos.y = Altitudes.AltitudeFor(AltitudeLayer.Item) + 0.026f + (i * 0.001f);

                Matrix4x4 matrix = Matrix4x4.identity;
                matrix.SetTRS(pos, Quaternion.AngleAxis(angle, Vector3.up), new Vector3(0.30f, 1f, 0.30f));
                Graphics.DrawMesh(MeshPool.plane10, matrix, material, 0);
            }
        }

        private ThingDef GetDisplayedDefForIndex(int overlayIndex, int overlayCount)
        {
            if (innerContainer == null || innerContainer.Count == 0)
            {
                return DefDatabase<ThingDef>.GetNamedSilentFail("ABY_ArchonSigil");
            }

            int index = Mathf.Clamp(Mathf.FloorToInt(((overlayIndex + 0.5f) / overlayCount) * innerContainer.Count), 0, innerContainer.Count - 1);
            Thing thing = innerContainer[index];
            return thing?.def ?? DefDatabase<ThingDef>.GetNamedSilentFail("ABY_ArchonSigil");
        }

        private Material GetOverlayMaterial(ThingDef def)
        {
            string texPath = def?.graphicData?.texPath;
            if (string.IsNullOrEmpty(texPath))
            {
                return null;
            }

            if (!OverlayMaterialCache.TryGetValue(texPath, out Material material))
            {
                material = MaterialPool.MatFrom(texPath, ShaderDatabase.Cutout);
                OverlayMaterialCache[texPath] = material;
            }

            return material;
        }

        private int CountSigilsOfDef(ThingDef def)
        {
            if (def == null || innerContainer == null)
            {
                return 0;
            }

            int total = 0;
            for (int i = 0; i < innerContainer.Count; i++)
            {
                Thing thing = innerContainer[i];
                if (thing != null && thing.def == def)
                {
                    total += thing.stackCount;
                }
            }

            return total;
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

        private static void AppendLine(StringBuilder sb, string line)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                return;
            }

            if (sb.Length > 0)
            {
                sb.AppendLine();
            }

            sb.Append(line);
        }
    }
}
