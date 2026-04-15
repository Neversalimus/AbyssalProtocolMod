using System;
using System.Collections.Generic;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace AbyssalProtocol
{
    [StaticConstructorOnStartup]
    public class Building_ABY_SigilVault : Building, IThingHolder
    {
        public const int MaxSigilCapacity = 20;

        private static readonly string[] AcceptedSigilDefNames =
        {
            "ABY_ArchonSigil",
            "ABY_UnstableBreachSigil",
            "ABY_EmberHoundSigil"
        };

        private static readonly Dictionary<string, int> AcceptedSigilPriority = new Dictionary<string, int>(StringComparer.Ordinal)
        {
            { "ABY_ArchonSigil", 0 },
            { "ABY_EmberHoundSigil", 1 },
            { "ABY_UnstableBreachSigil", 2 }
        };

        private static readonly HashSet<string> AcceptedSigilDefNameSet = new HashSet<string>(AcceptedSigilDefNames, StringComparer.Ordinal);
        private static readonly Vector3[] OverlayOffsets =
        {
            new Vector3(-0.36f, 0f, 0.24f),
            new Vector3(0.36f, 0f, 0.24f),
            new Vector3(-0.36f, 0f, -0.24f),
            new Vector3(0.36f, 0f, -0.24f)
        };

        private static readonly Dictionary<string, Material> OverlayMaterialCache = new Dictionary<string, Material>(StringComparer.Ordinal);
        private static List<ThingDef> cachedAcceptedSigilDefs;

        private ThingOwner<Thing> innerContainer;
        private bool cacheDirty = true;
        private int cachedStoredSigilCount;
        private readonly List<SigilSummary> cachedSummaries = new List<SigilSummary>();

        private struct SigilSummary
        {
            public ThingDef Def;
            public int Count;

            public SigilSummary(ThingDef def, int count)
            {
                Def = def;
                Count = count;
            }
        }

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
                EnsureCache();
                return cachedStoredSigilCount;
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

            MarkCacheDirty();
        }

        public override string GetInspectString()
        {
            EnsureCache();

            StringBuilder sb = new StringBuilder();
            string baseText = base.GetInspectString();
            if (!string.IsNullOrEmpty(baseText))
            {
                sb.Append(baseText.TrimEnd());
            }

            AppendLine(sb, "ABY_SigilVault_Stored".Translate(StoredSigilCount, MaxSigilCapacity));
            AppendLine(sb, "ABY_SigilVault_AcceptsConfigured".Translate(GetAcceptedSigilLabelList()));

            if (TryFindNearestReadyCircle(out Building_AbyssalSummoningCircle circle, out string failReason))
            {
                int dist = Mathf.RoundToInt(PositionHeld.DistanceTo(circle.PositionHeld));
                AppendLine(sb, "ABY_SigilVault_NearestReadyCircle".Translate(circle.LabelCap, dist));
            }
            else
            {
                AppendLine(sb, failReason.NullOrEmpty()
                    ? "ABY_SigilVault_NoReadyCircle".Translate()
                    : "ABY_SigilVault_NoReadyCircleReason".Translate(failReason));
            }

            for (int i = 0; i < cachedSummaries.Count; i++)
            {
                SigilSummary summary = cachedSummaries[i];
                AppendLine(sb, "ABY_SigilVault_CountLine".Translate(summary.Def.LabelCap, summary.Count));
            }

            return sb.ToString().TrimEnd();
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (Gizmo gizmo in base.GetGizmos())
            {
                yield return gizmo;
            }

            Command_Action stageCommand = new Command_Action
            {
                defaultLabel = "ABY_SigilVault_StageToCircleLabel".Translate(),
                defaultDesc = "ABY_SigilVault_StageToCircleDesc".Translate(),
                icon = ContentFinder<Texture2D>.Get("UI/Commands/Drop", true),
                action = OpenStageMenu
            };

            if (StoredSigilCount <= 0)
            {
                stageCommand.Disable("ABY_SigilVault_Disabled_NoSigils".Translate());
            }

            yield return stageCommand;

            Command_Action ejectOneCommand = new Command_Action
            {
                defaultLabel = "ABY_SigilVault_EjectOneLabel".Translate(),
                defaultDesc = "ABY_SigilVault_EjectOneDesc".Translate(),
                icon = ContentFinder<Texture2D>.Get("UI/Commands/Drop", true),
                action = OpenEjectOneMenu
            };

            if (StoredSigilCount <= 0)
            {
                ejectOneCommand.Disable("ABY_SigilVault_Disabled_NoSigils".Translate());
            }

            yield return ejectOneCommand;

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
                MarkCacheDirty();
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
                && AcceptedSigilDefNameSet.Contains(def.defName);
        }

        public static List<ThingDef> AcceptedSigilDefs
        {
            get
            {
                if (cachedAcceptedSigilDefs == null)
                {
                    cachedAcceptedSigilDefs = new List<ThingDef>(AcceptedSigilDefNames.Length);
                    for (int i = 0; i < AcceptedSigilDefNames.Length; i++)
                    {
                        ThingDef def = DefDatabase<ThingDef>.GetNamedSilentFail(AcceptedSigilDefNames[i]);
                        if (def != null)
                        {
                            cachedAcceptedSigilDefs.Add(def);
                        }
                    }
                }

                return cachedAcceptedSigilDefs;
            }
        }

        private void OpenStageMenu()
        {
            EnsureCache();

            if (StoredSigilCount <= 0)
            {
                Messages.Message("ABY_SigilVault_Fail_NoStoredSigils".Translate(), MessageTypeDefOf.RejectInput, false);
                return;
            }

            List<FloatMenuOption> options = BuildSigilTypeMenuOptions("ABY_SigilVault_StageOption", TryStageOneSigilToCircle);
            if (options.Count == 1)
            {
                options[0].action();
                return;
            }

            Find.WindowStack.Add(new FloatMenu(options));
        }

        private void OpenEjectOneMenu()
        {
            EnsureCache();

            if (StoredSigilCount <= 0)
            {
                Messages.Message("ABY_SigilVault_Fail_NoStoredSigils".Translate(), MessageTypeDefOf.RejectInput, false);
                return;
            }

            List<FloatMenuOption> options = BuildSigilTypeMenuOptions("ABY_SigilVault_EjectOption", TryEjectOneOfDefWithMessage);
            if (options.Count == 1)
            {
                options[0].action();
                return;
            }

            Find.WindowStack.Add(new FloatMenu(options));
        }

        private List<FloatMenuOption> BuildSigilTypeMenuOptions(string translateKey, Action<ThingDef> onChosen)
        {
            List<FloatMenuOption> options = new List<FloatMenuOption>();
            for (int i = 0; i < cachedSummaries.Count; i++)
            {
                SigilSummary summary = cachedSummaries[i];
                ThingDef def = summary.Def;
                int count = summary.Count;
                string label = translateKey.Translate(def.LabelCap.Named("SIGIL"), count.Named("COUNT")).ToString();

                options.Add(new FloatMenuOption(label, delegate
                {
                    onChosen(def);
                }));
            }

            return options;
        }

        private void TryStageOneSigilToCircle(ThingDef sigilDef)
        {
            if (sigilDef == null)
            {
                Messages.Message("ABY_SigilVault_Fail_NoStoredSigils".Translate(), MessageTypeDefOf.RejectInput, false);
                return;
            }

            JobDef carryJobDef = DefDatabase<JobDef>.GetNamedSilentFail("ABY_CarrySigilToCircle");
            if (carryJobDef == null)
            {
                Messages.Message("ABY_SigilVault_Fail_MissingCarryJob".Translate(), MessageTypeDefOf.RejectInput, false);
                return;
            }

            Thing droppedSigil = TryDropOneOfDef(sigilDef);
            if (droppedSigil == null)
            {
                Messages.Message("ABY_SigilVault_Fail_NoStoredSigils".Translate(), MessageTypeDefOf.RejectInput, false);
                return;
            }

            if (!TryFindBestStageTarget(droppedSigil, out Building_AbyssalSummoningCircle circle, out Pawn carrier, out string failReason))
            {
                TryReabsorbOrPlace(droppedSigil);
                string message = failReason.NullOrEmpty() ? "ABY_SigilVault_Fail_NoReadyCircle".Translate().ToString() : failReason;
                Messages.Message(message, MessageTypeDefOf.RejectInput, false);
                return;
            }

            Job job = JobMaker.MakeJob(carryJobDef, droppedSigil, circle);
            job.count = 1;
            carrier.jobs.TryTakeOrderedJob(job);

            Messages.Message("ABY_SigilVault_StageSuccess".Translate(sigilDef.LabelCap, carrier.LabelShortCap, circle.LabelCap), MessageTypeDefOf.TaskCompletion, false);
        }

        private void TryEjectOneOfDefWithMessage(ThingDef sigilDef)
        {
            Thing dropped = TryDropOneOfDef(sigilDef);
            if (dropped == null)
            {
                Messages.Message("ABY_SigilVault_Fail_NoStoredSigils".Translate(), MessageTypeDefOf.RejectInput, false);
                return;
            }

            Messages.Message("ABY_SigilVault_EjectSuccess".Translate(sigilDef.LabelCap), dropped, MessageTypeDefOf.TaskCompletion, false);
        }

        private Thing TryDropOneOfDef(ThingDef sigilDef)
        {
            if (sigilDef == null || innerContainer == null || MapHeld == null)
            {
                return null;
            }

            for (int i = 0; i < innerContainer.Count; i++)
            {
                Thing thing = innerContainer[i];
                if (thing == null || thing.def != sigilDef)
                {
                    continue;
                }

                if (innerContainer.TryDrop(thing, GetDropCell(), MapHeld, ThingPlaceMode.Near, out Thing dropped))
                {
                    MarkCacheDirty();
                    return dropped;
                }

                break;
            }

            return null;
        }

        private bool TryFindBestStageTarget(Thing sigil, out Building_AbyssalSummoningCircle bestCircle, out Pawn bestCarrier, out string failReason)
        {
            bestCircle = null;
            bestCarrier = null;
            failReason = null;

            if (sigil == null || !sigil.Spawned || sigil.Map == null)
            {
                failReason = "ABY_SigilVault_Fail_NoStoredSigils".Translate();
                return false;
            }

            ThingDef circleDef = DefDatabase<ThingDef>.GetNamedSilentFail("ABY_SummoningCircle");
            if (circleDef == null)
            {
                failReason = "ABY_SigilVault_Fail_NoCircle".Translate();
                return false;
            }

            List<Thing> circles = sigil.Map.listerThings.ThingsOfDef(circleDef);
            if (circles == null || circles.Count == 0)
            {
                failReason = "ABY_SigilVault_Fail_NoCircle".Translate();
                return false;
            }

            List<Pawn> pawns = sigil.Map.mapPawns?.FreeColonistsSpawned;
            if (pawns == null || pawns.Count == 0)
            {
                failReason = "ABY_SigilVault_Fail_NoOperator".Translate();
                return false;
            }

            float bestScore = float.MaxValue;
            string lastCircleFail = null;
            bool foundReadyCircle = false;

            for (int i = 0; i < circles.Count; i++)
            {
                Building_AbyssalSummoningCircle circle = circles[i] as Building_AbyssalSummoningCircle;
                if (circle == null || circle.Destroyed || !circle.Spawned)
                {
                    continue;
                }

                if (!circle.IsReadyForSigil(out string circleFailReason))
                {
                    if (!circleFailReason.NullOrEmpty())
                    {
                        lastCircleFail = circleFailReason;
                    }

                    continue;
                }

                foundReadyCircle = true;

                for (int j = 0; j < pawns.Count; j++)
                {
                    Pawn pawn = pawns[j];
                    if (pawn == null || pawn.Dead || pawn.Downed || pawn.jobs == null)
                    {
                        continue;
                    }

                    if (!pawn.CanReserveAndReach(sigil, PathEndMode.ClosestTouch, Danger.Deadly))
                    {
                        continue;
                    }

                    if (!pawn.CanReserveAndReach(circle, PathEndMode.InteractionCell, Danger.Deadly))
                    {
                        continue;
                    }

                    float score = pawn.PositionHeld.DistanceToSquared(sigil.PositionHeld);
                    score += sigil.PositionHeld.DistanceToSquared(circle.InteractionCell) * 0.45f;

                    if (pawn.Drafted)
                    {
                        score += 4000f;
                    }

                    if (score >= bestScore)
                    {
                        continue;
                    }

                    bestScore = score;
                    bestCircle = circle;
                    bestCarrier = pawn;
                }
            }

            if (bestCircle != null && bestCarrier != null)
            {
                return true;
            }

            failReason = foundReadyCircle
                ? "ABY_SigilVault_Fail_NoOperator".Translate()
                : (lastCircleFail.NullOrEmpty() ? "ABY_SigilVault_Fail_NoReadyCircle".Translate() : lastCircleFail);

            return false;
        }

        private bool TryFindNearestReadyCircle(out Building_AbyssalSummoningCircle bestCircle, out string failReason)
        {
            bestCircle = null;
            failReason = null;

            if (MapHeld == null)
            {
                failReason = "ABY_SigilVault_NoReadyCircle".Translate();
                return false;
            }

            ThingDef circleDef = DefDatabase<ThingDef>.GetNamedSilentFail("ABY_SummoningCircle");
            if (circleDef == null)
            {
                failReason = "ABY_SigilVault_NoReadyCircle".Translate();
                return false;
            }

            List<Thing> circles = MapHeld.listerThings.ThingsOfDef(circleDef);
            if (circles == null || circles.Count == 0)
            {
                failReason = "ABY_SigilVault_Fail_NoCircle".Translate();
                return false;
            }

            float bestDist = float.MaxValue;
            string lastReason = null;

            for (int i = 0; i < circles.Count; i++)
            {
                Building_AbyssalSummoningCircle circle = circles[i] as Building_AbyssalSummoningCircle;
                if (circle == null || circle.Destroyed || !circle.Spawned)
                {
                    continue;
                }

                if (!circle.IsReadyForSigil(out string circleFailReason))
                {
                    if (!circleFailReason.NullOrEmpty())
                    {
                        lastReason = circleFailReason;
                    }

                    continue;
                }

                float dist = PositionHeld.DistanceToSquared(circle.PositionHeld);
                if (dist >= bestDist)
                {
                    continue;
                }

                bestDist = dist;
                bestCircle = circle;
            }

            if (bestCircle != null)
            {
                return true;
            }

            failReason = lastReason.NullOrEmpty()
                ? "ABY_SigilVault_NoReadyCircle".Translate()
                : lastReason;
            return false;
        }

        private void TryReabsorbOrPlace(Thing thing)
        {
            if (thing == null)
            {
                return;
            }

            if (TryAbsorbThing(thing))
            {
                return;
            }

            if (thing.Spawned || MapHeld == null)
            {
                return;
            }

            GenPlace.TryPlaceThing(thing, GetDropCell(), MapHeld, ThingPlaceMode.Near);
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

            MarkCacheDirty();
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
            EnsureCache();

            if (cachedStoredSigilCount <= 0 || cachedSummaries.Count == 0)
            {
                return;
            }

            int visible = Mathf.Clamp(Mathf.CeilToInt(cachedStoredSigilCount / 5f), 1, 4);
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
            if (cachedSummaries.Count == 0)
            {
                return DefDatabase<ThingDef>.GetNamedSilentFail("ABY_ArchonSigil");
            }

            float normalized = (overlayIndex + 0.5f) / overlayCount;
            int target = Mathf.Clamp(Mathf.CeilToInt(normalized * cachedStoredSigilCount), 1, cachedStoredSigilCount);
            int cumulative = 0;

            for (int i = 0; i < cachedSummaries.Count; i++)
            {
                cumulative += cachedSummaries[i].Count;
                if (target <= cumulative)
                {
                    return cachedSummaries[i].Def;
                }
            }

            return cachedSummaries[cachedSummaries.Count - 1].Def;
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

        private void EnsureCache()
        {
            if (!cacheDirty)
            {
                return;
            }

            cacheDirty = false;
            cachedStoredSigilCount = 0;
            cachedSummaries.Clear();

            if (innerContainer == null)
            {
                return;
            }

            List<ThingDef> defs = AcceptedSigilDefs;
            int[] counts = new int[defs.Count];

            for (int i = 0; i < innerContainer.Count; i++)
            {
                Thing thing = innerContainer[i];
                if (thing == null || thing.def == null)
                {
                    continue;
                }

                cachedStoredSigilCount += Math.Max(1, thing.stackCount);

                for (int j = 0; j < defs.Count; j++)
                {
                    if (thing.def == defs[j])
                    {
                        counts[j] += Math.Max(1, thing.stackCount);
                        break;
                    }
                }
            }

            for (int i = 0; i < defs.Count; i++)
            {
                if (counts[i] > 0)
                {
                    cachedSummaries.Add(new SigilSummary(defs[i], counts[i]));
                }
            }

            cachedSummaries.Sort(CompareSigilSummaries);
        }

        private string GetAcceptedSigilLabelList()
        {
            List<ThingDef> defs = AcceptedSigilDefs;
            if (defs.Count == 0)
            {
                return "—";
            }

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < defs.Count; i++)
            {
                if (i > 0)
                {
                    sb.Append(", ");
                }

                sb.Append(defs[i].label);
            }

            return sb.ToString();
        }

        private int CountSigilsOfDef(ThingDef def)
        {
            EnsureCache();

            for (int i = 0; i < cachedSummaries.Count; i++)
            {
                if (cachedSummaries[i].Def == def)
                {
                    return cachedSummaries[i].Count;
                }
            }

            return 0;
        }

        private void MarkCacheDirty()
        {
            cacheDirty = true;
        }

        private static int CompareSigilSummaries(SigilSummary a, SigilSummary b)
        {
            int countCompare = b.Count.CompareTo(a.Count);
            if (countCompare != 0)
            {
                return countCompare;
            }

            return GetSigilPriority(a.Def).CompareTo(GetSigilPriority(b.Def));
        }

        private static int GetSigilPriority(ThingDef def)
        {
            if (def == null || string.IsNullOrEmpty(def.defName))
            {
                return int.MaxValue;
            }

            if (AcceptedSigilPriority.TryGetValue(def.defName, out int priority))
            {
                return priority;
            }

            return int.MaxValue;
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
