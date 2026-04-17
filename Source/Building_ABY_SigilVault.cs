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

        private const string CircleDefName = "ABY_SummoningCircle";
        private const string CarrySigilJobDefName = "ABY_CarrySigilToCircle";

        private static readonly string[] AcceptedSigilDefNames =
        {
            "ABY_DominionSigil",
            "ABY_ReactorSaintSigil",
            "ABY_ChoirEngineSigil",
            "ABY_ArchonSigil",
            "ABY_WardenOfAshSigil",
            "ABY_UnstableBreachSigil",
            "ABY_EmberHoundSigil",
            "ABY_HexgunRelaySigil"
        };

        private static readonly Dictionary<string, int> AcceptedSigilPriority = new Dictionary<string, int>(StringComparer.Ordinal)
        {
            { "ABY_DominionSigil", 0 },
            { "ABY_ReactorSaintSigil", 1 },
            { "ABY_ChoirEngineSigil", 2 },
            { "ABY_ArchonSigil", 3 },
            { "ABY_WardenOfAshSigil", 4 },
            { "ABY_HexgunRelaySigil", 5 },
            { "ABY_EmberHoundSigil", 6 },
            { "ABY_UnstableBreachSigil", 7 }
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
        private static readonly Texture2D DefaultCommandIcon = ContentFinder<Texture2D>.Get("UI/ABY/Commands/ABY_SigilVault_Stage", false)
            ?? ContentFinder<Texture2D>.Get("UI/Commands/Drop", false)
            ?? BaseContent.BadTex;
        private static readonly Texture2D LinkCommandIcon = ContentFinder<Texture2D>.Get("UI/ABY/Commands/ABY_SigilVault_Link", false)
            ?? ContentFinder<Texture2D>.Get("UI/Commands/SetTarget", false)
            ?? DefaultCommandIcon;
        private static readonly Texture2D JumpCommandIcon = ContentFinder<Texture2D>.Get("UI/ABY/Commands/ABY_SigilVault_Jump", false)
            ?? ContentFinder<Texture2D>.Get("UI/Commands/JumpToLocation", false)
            ?? DefaultCommandIcon;
        private static readonly Texture2D UnlinkCommandIcon = ContentFinder<Texture2D>.Get("UI/ABY/Commands/ABY_SigilVault_Unlink", false)
            ?? ContentFinder<Texture2D>.Get("UI/Commands/Cancel", false)
            ?? DefaultCommandIcon;
        private static readonly Texture2D EjectOneCommandIcon = ContentFinder<Texture2D>.Get("UI/ABY/Commands/ABY_SigilVault_EjectOne", false)
            ?? DefaultCommandIcon;
        private static readonly Texture2D EjectAllCommandIcon = ContentFinder<Texture2D>.Get("UI/ABY/Commands/ABY_SigilVault_EjectAll", false)
            ?? DefaultCommandIcon;
        private static readonly Material LinkedOverlayMaterial = MaterialPool.MatFrom("Things/Building/ABY_SigilVault_LinkedOverlay", ShaderDatabase.Cutout);
        private static readonly Material LinkedPulseMaterial = MaterialPool.MatFrom("Things/Building/ABY_SigilVault_LinkPulse", ShaderDatabase.Cutout);
        private static List<ThingDef> cachedAcceptedSigilDefs;

        private ThingOwner<Thing> innerContainer;
        private Building_AbyssalSummoningCircle linkedCircle;
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

        public int ConvertStoredSigils(ThingDef fromDef, ThingDef toDef)
        {
            if (innerContainer == null || fromDef == null || toDef == null || fromDef == toDef)
            {
                return 0;
            }

            int converted = 0;
            for (int i = innerContainer.Count - 1; i >= 0; i--)
            {
                Thing thing = innerContainer[i];
                if (thing == null || thing.def != fromDef)
                {
                    continue;
                }

                int count = Math.Max(1, thing.stackCount);
                innerContainer.Remove(thing);
                thing.Destroy(DestroyMode.Vanish);
                AddSigilsToContainer(toDef, count);
                converted += count;
            }

            if (converted > 0)
            {
                MarkCacheDirty();
            }

            return converted;
        }

        public bool HasLinkedCircle => ResolveLinkedCircle() != null;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Deep.Look(ref innerContainer, "innerContainer", this);
            Scribe_References.Look(ref linkedCircle, "linkedCircle");

            if (Scribe.mode == LoadSaveMode.PostLoadInit && innerContainer == null)
            {
                innerContainer = new ThingOwner<Thing>(this, false);
            }

            MarkCacheDirty();
            PruneInvalidLinkedCircle();
        }

        public override string GetInspectString()
        {
            EnsureCache();
            PruneInvalidLinkedCircle();

            StringBuilder sb = new StringBuilder();
            string baseText = base.GetInspectString();
            if (!string.IsNullOrEmpty(baseText))
            {
                sb.Append(baseText.TrimEnd());
            }

            AppendLine(sb, "ABY_SigilVault_Stored".Translate(StoredSigilCount, MaxSigilCapacity));
            AppendLine(sb, "ABY_SigilVault_AcceptsConfigured".Translate(GetAcceptedSigilLabelList()));

            Building_AbyssalSummoningCircle resolvedLink = ResolveLinkedCircle();
            if (resolvedLink != null)
            {
                int dist = Mathf.RoundToInt(PositionHeld.DistanceTo(resolvedLink.PositionHeld));
                AppendLine(sb, "ABY_SigilVault_LinkedCircle".Translate(resolvedLink.LabelCap, dist));

                if (resolvedLink.IsReadyForSigil(out string linkedFailReason))
                {
                    AppendLine(sb, "ABY_SigilVault_LinkStateReady".Translate());
                }
                else
                {
                    string reason = linkedFailReason.NullOrEmpty()
                        ? "ABY_SigilVault_LinkStateBlocked".Translate().ToString()
                        : "ABY_SigilVault_LinkStateBlockedReason".Translate(linkedFailReason).ToString();
                    AppendLine(sb, reason);
                }
            }
            else if (TryFindNearestReadyCircle(out Building_AbyssalSummoningCircle circle, out string failReason))
            {
                int dist = Mathf.RoundToInt(PositionHeld.DistanceTo(circle.PositionHeld));
                AppendLine(sb, "ABY_SigilVault_NearestReadyCircle".Translate(circle.LabelCap, dist));
                AppendLine(sb, "ABY_SigilVault_LinkHint".Translate());
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

            Command_Action linkCommand = new Command_Action
            {
                defaultLabel = HasLinkedCircle
                    ? "ABY_SigilVault_ChangeLinkLabel".Translate()
                    : "ABY_SigilVault_LinkLabel".Translate(),
                defaultDesc = "ABY_SigilVault_LinkDesc".Translate(),
                icon = LinkCommandIcon,
                action = OpenLinkMenu
            };
            yield return linkCommand;

            Command_Action unlinkCommand = new Command_Action
            {
                defaultLabel = "ABY_SigilVault_UnlinkLabel".Translate(),
                defaultDesc = "ABY_SigilVault_UnlinkDesc".Translate(),
                icon = UnlinkCommandIcon,
                action = UnlinkCircle
            };
            if (!HasLinkedCircle)
            {
                unlinkCommand.Disable("ABY_SigilVault_Disabled_NoLink".Translate());
            }
            yield return unlinkCommand;

            Command_Action jumpCommand = new Command_Action
            {
                defaultLabel = "ABY_SigilVault_JumpToLinkedCircleLabel".Translate(),
                defaultDesc = "ABY_SigilVault_JumpToLinkedCircleDesc".Translate(),
                icon = JumpCommandIcon,
                action = JumpToLinkedCircle
            };
            if (!HasLinkedCircle)
            {
                jumpCommand.Disable("ABY_SigilVault_Disabled_NoLink".Translate());
            }
            yield return jumpCommand;

            Command_Action stageCommand = new Command_Action
            {
                defaultLabel = "ABY_SigilVault_StageToLinkedCircleLabel".Translate(),
                defaultDesc = "ABY_SigilVault_StageToLinkedCircleDesc".Translate(),
                icon = DefaultCommandIcon,
                action = OpenStageMenu
            };

            if (StoredSigilCount <= 0)
            {
                stageCommand.Disable("ABY_SigilVault_Disabled_NoSigils".Translate());
            }
            else if (!HasLinkedCircle)
            {
                stageCommand.Disable("ABY_SigilVault_Disabled_NoLink".Translate());
            }

            yield return stageCommand;

            Command_Action ejectOneCommand = new Command_Action
            {
                defaultLabel = "ABY_SigilVault_EjectOneLabel".Translate(),
                defaultDesc = "ABY_SigilVault_EjectOneDesc".Translate(),
                icon = EjectOneCommandIcon,
                action = OpenEjectOneMenu
            };

            if (StoredSigilCount <= 0)
            {
                ejectOneCommand.Disable("ABY_SigilVault_Disabled_NoSigils".Translate());
            }

            yield return ejectOneCommand;

            Command_Action ejectAllCommand = new Command_Action
            {
                defaultLabel = "ABY_SigilVault_EjectAllLabel".Translate(),
                defaultDesc = "ABY_SigilVault_EjectAllDesc".Translate(),
                icon = EjectAllCommandIcon,
                action = EjectAllContents
            };
            if (StoredSigilCount <= 0)
            {
                ejectAllCommand.Disable("ABY_SigilVault_Disabled_NoSigils".Translate());
            }
            yield return ejectAllCommand;
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
            DrawLinkState(drawLoc);
            DrawStoredSigils(drawLoc);
        }

        public override void DrawExtraSelectionOverlays()
        {
            base.DrawExtraSelectionOverlays();

            Building_AbyssalSummoningCircle resolvedLink = ResolveLinkedCircle();
            if (resolvedLink == null)
            {
                return;
            }

            GenDraw.DrawLineBetween(this.TrueCenter(), resolvedLink.TrueCenter());
        }

        public bool IsLinkedTo(Building_AbyssalSummoningCircle circle)
        {
            return circle != null && ResolveLinkedCircle() == circle;
        }

        public int CountStoredSigilsOfDef(ThingDef sigilDef)
        {
            if (sigilDef == null || innerContainer == null || innerContainer.Count == 0)
            {
                return 0;
            }

            int count = 0;
            for (int i = 0; i < innerContainer.Count; i++)
            {
                Thing thing = innerContainer[i];
                if (thing != null && thing.def == sigilDef)
                {
                    count += Math.Max(1, thing.stackCount);
                }
            }

            return count;
        }

        public bool TryStageOneSigilToLinkedCircleFromConsole(ThingDef sigilDef, out Pawn carrier, out string failReason)
        {
            carrier = null;
            failReason = null;

            if (sigilDef == null)
            {
                failReason = "ABY_SigilVault_Fail_NoStoredSigils".Translate().ToString();
                return false;
            }

            if (StoredSigilCount <= 0)
            {
                failReason = "ABY_SigilVault_Fail_NoStoredSigils".Translate().ToString();
                return false;
            }

            Building_AbyssalSummoningCircle circle = ResolveLinkedCircle();
            if (circle == null)
            {
                failReason = "ABY_SigilVault_Fail_NoLink".Translate().ToString();
                return false;
            }

            JobDef carryJobDef = DefDatabase<JobDef>.GetNamedSilentFail(CarrySigilJobDefName);
            if (carryJobDef == null)
            {
                failReason = "ABY_SigilVault_Fail_MissingCarryJob".Translate().ToString();
                return false;
            }

            if (!circle.IsReadyForSigil(out string readyFailReason))
            {
                failReason = readyFailReason.NullOrEmpty()
                    ? "ABY_SigilVault_Fail_LinkBlocked".Translate().ToString()
                    : "ABY_SigilVault_Fail_LinkBlockedReason".Translate(readyFailReason).ToString();
                return false;
            }

            Thing droppedSigil = TryDropOneOfDef(sigilDef);
            if (droppedSigil == null)
            {
                failReason = "ABY_SigilVault_Fail_NoStoredSigils".Translate().ToString();
                return false;
            }

            if (!TryFindOperatorForCircle(droppedSigil, circle, out carrier, out failReason))
            {
                TryReabsorbOrPlace(droppedSigil);
                if (failReason.NullOrEmpty())
                {
                    failReason = "ABY_SigilVault_Fail_NoOperator".Translate().ToString();
                }

                return false;
            }

            Job job = JobMaker.MakeJob(carryJobDef, droppedSigil, circle);
            job.count = 1;
            carrier.jobs.TryTakeOrderedJob(job);
            return true;
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

        private void OpenLinkMenu()
        {
            if (MapHeld == null)
            {
                Messages.Message("ABY_SigilVault_Fail_NoCircle".Translate(), MessageTypeDefOf.RejectInput, false);
                return;
            }

            List<Building_AbyssalSummoningCircle> circles = GetAllCirclesOnMap();
            if (circles.Count == 0)
            {
                Messages.Message("ABY_SigilVault_Fail_NoCircle".Translate(), MessageTypeDefOf.RejectInput, false);
                return;
            }

            circles.Sort((a, b) => PositionHeld.DistanceToSquared(a.PositionHeld).CompareTo(PositionHeld.DistanceToSquared(b.PositionHeld)));

            List<FloatMenuOption> options = new List<FloatMenuOption>(circles.Count);
            for (int i = 0; i < circles.Count; i++)
            {
                Building_AbyssalSummoningCircle circle = circles[i];
                int dist = Mathf.RoundToInt(PositionHeld.DistanceTo(circle.PositionHeld));
                bool isCurrent = circle == linkedCircle;
                string readiness = circle.IsReadyForSigil(out string failReason)
                    ? "ABY_SigilVault_LinkOptionReady".Translate().ToString()
                    : (failReason.NullOrEmpty() ? "ABY_SigilVault_LinkOptionBlocked".Translate().ToString() : failReason);
                string label = (isCurrent
                        ? "ABY_SigilVault_LinkOptionCurrent".Translate(circle.LabelCap, dist, readiness)
                        : "ABY_SigilVault_LinkOption".Translate(circle.LabelCap, dist, readiness))
                    .ToString();

                options.Add(new FloatMenuOption(label, delegate
                {
                    LinkToCircle(circle);
                }));
            }

            Find.WindowStack.Add(new FloatMenu(options));
        }

        private void LinkToCircle(Building_AbyssalSummoningCircle circle)
        {
            if (circle == null || circle.Destroyed || !circle.Spawned || circle.MapHeld != MapHeld)
            {
                Messages.Message("ABY_SigilVault_Fail_LinkTargetInvalid".Translate(), MessageTypeDefOf.RejectInput, false);
                return;
            }

            linkedCircle = circle;
            Messages.Message("ABY_SigilVault_LinkSuccess".Translate(LabelCap, circle.LabelCap), this, MessageTypeDefOf.TaskCompletion, false);
        }

        private void UnlinkCircle()
        {
            Building_AbyssalSummoningCircle oldLink = ResolveLinkedCircle();
            if (oldLink == null)
            {
                linkedCircle = null;
                Messages.Message("ABY_SigilVault_Disabled_NoLink".Translate(), MessageTypeDefOf.RejectInput, false);
                return;
            }

            linkedCircle = null;
            Messages.Message("ABY_SigilVault_UnlinkSuccess".Translate(oldLink.LabelCap), this, MessageTypeDefOf.TaskCompletion, false);
        }

        private void JumpToLinkedCircle()
        {
            Building_AbyssalSummoningCircle circle = ResolveLinkedCircle();
            if (circle == null)
            {
                Messages.Message("ABY_SigilVault_Disabled_NoLink".Translate(), MessageTypeDefOf.RejectInput, false);
                return;
            }

            CameraJumper.TryJumpAndSelect(circle);
        }

        private void OpenStageMenu()
        {
            EnsureCache();

            if (StoredSigilCount <= 0)
            {
                Messages.Message("ABY_SigilVault_Fail_NoStoredSigils".Translate(), MessageTypeDefOf.RejectInput, false);
                return;
            }

            if (ResolveLinkedCircle() == null)
            {
                Messages.Message("ABY_SigilVault_Fail_NoLink".Translate(), MessageTypeDefOf.RejectInput, false);
                return;
            }

            List<FloatMenuOption> options = BuildSigilTypeMenuOptions("ABY_SigilVault_StageOption", TryStageOneSigilToLinkedCircle);
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

        private void TryStageOneSigilToLinkedCircle(ThingDef sigilDef)
        {
            if (TryStageOneSigilToLinkedCircleFromConsole(sigilDef, out Pawn carrier, out string failReason))
            {
                Building_AbyssalSummoningCircle circle = ResolveLinkedCircle();
                Messages.Message("ABY_SigilVault_StageSuccess".Translate(sigilDef.LabelCap, carrier.LabelShortCap, circle?.LabelCap ?? "summoning circle"), MessageTypeDefOf.TaskCompletion, false);
                return;
            }

            if (!failReason.NullOrEmpty())
            {
                Messages.Message(failReason, MessageTypeDefOf.RejectInput, false);
            }
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

        private bool TryFindOperatorForCircle(Thing sigil, Building_AbyssalSummoningCircle circle, out Pawn bestCarrier, out string failReason)
        {
            bestCarrier = null;
            failReason = null;

            if (sigil == null || !sigil.Spawned || sigil.Map == null)
            {
                failReason = "ABY_SigilVault_Fail_NoStoredSigils".Translate();
                return false;
            }

            if (circle == null || circle.Destroyed || !circle.Spawned || circle.MapHeld != sigil.MapHeld)
            {
                failReason = "ABY_SigilVault_Fail_LinkTargetInvalid".Translate();
                return false;
            }

            List<Pawn> pawns = sigil.Map.mapPawns?.FreeColonistsSpawned;
            if (pawns == null || pawns.Count == 0)
            {
                failReason = "ABY_SigilVault_Fail_NoOperator".Translate();
                return false;
            }

            float bestScore = float.MaxValue;
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn pawn = pawns[i];
                if (pawn == null || pawn.Dead || pawn.Downed || pawn.jobs == null || pawn.InMentalState)
                {
                    continue;
                }

                if (!pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation))
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

                if (score < bestScore)
                {
                    bestScore = score;
                    bestCarrier = pawn;
                }
            }

            if (bestCarrier != null)
            {
                return true;
            }

            failReason = "ABY_SigilVault_Fail_NoOperator".Translate();
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

            List<Building_AbyssalSummoningCircle> circles = GetAllCirclesOnMap();
            if (circles.Count == 0)
            {
                failReason = "ABY_SigilVault_Fail_NoCircle".Translate();
                return false;
            }

            float bestDist = float.MaxValue;
            string lastReason = null;

            for (int i = 0; i < circles.Count; i++)
            {
                Building_AbyssalSummoningCircle circle = circles[i];
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

        private List<Building_AbyssalSummoningCircle> GetAllCirclesOnMap()
        {
            List<Building_AbyssalSummoningCircle> result = new List<Building_AbyssalSummoningCircle>();
            if (MapHeld == null)
            {
                return result;
            }

            ThingDef circleDef = DefDatabase<ThingDef>.GetNamedSilentFail(CircleDefName);
            if (circleDef == null)
            {
                return result;
            }

            List<Thing> circles = MapHeld.listerThings.ThingsOfDef(circleDef);
            for (int i = 0; i < circles.Count; i++)
            {
                Building_AbyssalSummoningCircle circle = circles[i] as Building_AbyssalSummoningCircle;
                if (circle != null && circle.Spawned && !circle.Destroyed)
                {
                    result.Add(circle);
                }
            }

            return result;
        }

        private void PruneInvalidLinkedCircle()
        {
            if (linkedCircle == null)
            {
                return;
            }

            if (linkedCircle.Destroyed || !linkedCircle.Spawned || linkedCircle.MapHeld != MapHeld)
            {
                linkedCircle = null;
            }
        }

        private Building_AbyssalSummoningCircle ResolveLinkedCircle()
        {
            PruneInvalidLinkedCircle();
            return linkedCircle;
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

            CellRect rect = GenAdj.OccupiedRect(Position, Rotation, def.Size);
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

        private void AddSigilsToContainer(ThingDef def, int count)
        {
            int remaining = Math.Max(0, count);
            while (remaining > 0)
            {
                Thing replacement = ThingMaker.MakeThing(def);
                replacement.stackCount = Math.Min(def.stackLimit, remaining);
                innerContainer.TryAdd(replacement);
                remaining -= replacement.stackCount;
            }
        }

        private void DrawLinkState(Vector3 drawLoc)
        {
            Building_AbyssalSummoningCircle circle = ResolveLinkedCircle();
            if (circle == null)
            {
                return;
            }

            Vector3 overlayPos = drawLoc;
            overlayPos.y = Altitudes.AltitudeFor(AltitudeLayer.BuildingOnTop) + 0.023f;

            Matrix4x4 overlayMatrix = Matrix4x4.identity;
            overlayMatrix.SetTRS(overlayPos, Quaternion.identity, new Vector3(1.94f, 1f, 1.94f));
            Graphics.DrawMesh(MeshPool.plane10, overlayMatrix, LinkedOverlayMaterial, 0);

            bool ready = circle.IsReadyForSigil(out _);
            float pulse = 1f + Mathf.Sin(Find.TickManager.TicksGame * 0.085f) * (ready ? 0.035f : 0.012f);
            Matrix4x4 pulseMatrix = Matrix4x4.identity;
            pulseMatrix.SetTRS(overlayPos + new Vector3(0f, 0.0008f, 0f), Quaternion.identity, new Vector3(1.30f * pulse, 1f, 1.30f * pulse));
            Graphics.DrawMesh(MeshPool.plane10, pulseMatrix, LinkedPulseMaterial, 0);
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
