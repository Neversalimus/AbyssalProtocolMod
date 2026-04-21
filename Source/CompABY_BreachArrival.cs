using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace AbyssalProtocol
{
    public class CompProperties_ABY_BreachArrival : CompProperties
    {
        public int lockTicks = 56;
        public int manifestationWarmupTicks = 62;
        public int manifestationOffsetCells = 1;
        public bool onlyWhenHostileToPlayer = true;
        public string manifestationDefName = "ABY_Manifestation_BreachBruteArrival";

        public CompProperties_ABY_BreachArrival()
        {
            compClass = typeof(CompABY_BreachArrival);
        }
    }

    public class CompABY_BreachArrival : ThingComp
    {
        private int arrivalReleaseTick = -1;
        private bool triggered;
        private Thing activeManifestation;
        private Rot4 seamSide = Rot4.South;

        private CompProperties_ABY_BreachArrival Props => (CompProperties_ABY_BreachArrival)props;
        private Pawn PawnParent => parent as Pawn;

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref arrivalReleaseTick, "arrivalReleaseTick", -1);
            Scribe_Values.Look(ref triggered, "triggered", false);
            Scribe_Values.Look(ref seamSide, "seamSide", Rot4.South);
            Scribe_References.Look(ref activeManifestation, "activeManifestation");
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);

            if (respawningAfterLoad)
            {
                return;
            }

            Pawn pawn = PawnParent;
            if (!ShouldTriggerOnSpawn(pawn))
            {
                return;
            }

            triggered = true;
            int now = CurrentTicks();
            arrivalReleaseTick = now + Mathf.Max(20, Props.lockTicks);
            seamSide = ResolveApproachSide(pawn);
            TrySpawnArrivalManifestation(pawn, seamSide);
            ForceArrivalHold(pawn, now);
        }

        public override void CompTick()
        {
            base.CompTick();

            Pawn pawn = PawnParent;
            if (pawn == null || pawn.Dead || pawn.Downed || !pawn.Spawned)
            {
                return;
            }

            int now = CurrentTicks();
            if (!IsArrivalLocked(now))
            {
                return;
            }

            if (now % 12 == 0)
            {
                ForceArrivalHold(pawn, now);
            }
        }

        public override string CompInspectStringExtra()
        {
            if (!IsArrivalLocked(CurrentTicks()))
            {
                return null;
            }

            int remaining = Mathf.Max(0, arrivalReleaseTick - CurrentTicks());
            return "Manifesting: " + remaining.ToStringTicksToPeriod();
        }

        private bool ShouldTriggerOnSpawn(Pawn pawn)
        {
            if (pawn == null || !pawn.Spawned || pawn.MapHeld == null || pawn.Dead || pawn.Downed || pawn.Faction == null)
            {
                return false;
            }

            if (Props.onlyWhenHostileToPlayer && Faction.OfPlayer != null && !pawn.Faction.HostileTo(Faction.OfPlayer))
            {
                return false;
            }

            return true;
        }

        private bool IsArrivalLocked(int now)
        {
            return triggered && arrivalReleaseTick > now;
        }

        private void ForceArrivalHold(Pawn pawn, int now)
        {
            if (pawn == null || !pawn.Spawned || pawn.Dead || pawn.Downed)
            {
                return;
            }

            pawn.pather?.StopDead();

            Job currentJob = pawn.jobs?.curJob;
            int remaining = Mathf.Max(20, arrivalReleaseTick - now);
            if (currentJob != null && currentJob.def == JobDefOf.Wait_Combat && currentJob.expiryInterval >= remaining - 8)
            {
                return;
            }

            Job holdJob = JobMaker.MakeJob(JobDefOf.Wait_Combat);
            holdJob.expiryInterval = remaining;
            pawn.jobs?.TryTakeOrderedJob(holdJob);
        }

        private void TrySpawnArrivalManifestation(Pawn pawn, Rot4 side)
        {
            if (pawn?.MapHeld == null)
            {
                return;
            }

            ThingDef manifestationDef = DefDatabase<ThingDef>.GetNamedSilentFail(Props.manifestationDefName);
            if (manifestationDef == null)
            {
                return;
            }

            IntVec3 spawnCell = ResolveManifestationCell(pawn, side);
            if (!spawnCell.IsValid)
            {
                return;
            }

            Thing spawned = GenSpawn.Spawn(manifestationDef, spawnCell, pawn.MapHeld, WipeMode.Vanish);
            if (spawned is Building_ABY_BreachBruteArrivalManifestation breachManifestation)
            {
                breachManifestation.Initialize(Mathf.Max(30, Props.manifestationWarmupTicks), side);
                activeManifestation = breachManifestation;
                return;
            }

            spawned.Destroy(DestroyMode.Vanish);
        }

        private IntVec3 ResolveManifestationCell(Pawn pawn, Rot4 side)
        {
            if (pawn == null || pawn.MapHeld == null)
            {
                return IntVec3.Invalid;
            }

            int offset = Mathf.Clamp(Props.manifestationOffsetCells, 0, 2);
            IntVec3 preferred = pawn.Position + (side.FacingCell * offset);
            if (preferred.IsValid && preferred.InBounds(pawn.MapHeld) && preferred.Walkable(pawn.MapHeld))
            {
                return preferred;
            }

            if (CellFinder.TryFindRandomCellNear(
                pawn.Position,
                pawn.MapHeld,
                2,
                c => c.InBounds(pawn.MapHeld) && c.Walkable(pawn.MapHeld),
                out IntVec3 fallback))
            {
                return fallback;
            }

            return pawn.Position;
        }

        private Rot4 ResolveApproachSide(Pawn pawn)
        {
            if (pawn?.MapHeld == null)
            {
                return Rot4.South;
            }

            Building bestBuilding = null;
            float bestDistance = float.MaxValue;
            foreach (Building building in pawn.MapHeld.listerBuildings.allBuildingsColonist)
            {
                if (building == null || building.Destroyed)
                {
                    continue;
                }

                float distance = pawn.Position.DistanceToSquared(building.Position);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestBuilding = building;
                }
            }

            IntVec3 targetCell = bestBuilding != null ? bestBuilding.Position : pawn.MapHeld.Center;
            IntVec3 delta = targetCell - pawn.Position;
            if (Mathf.Abs(delta.x) > Mathf.Abs(delta.z))
            {
                return delta.x >= 0 ? Rot4.East : Rot4.West;
            }

            return delta.z >= 0 ? Rot4.North : Rot4.South;
        }

        private int CurrentTicks()
        {
            return Find.TickManager != null ? Find.TickManager.TicksGame : 0;
        }
    }
}
