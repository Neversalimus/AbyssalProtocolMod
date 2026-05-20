using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace AbyssalProtocol
{
    public class Projectile_ABY_TurretAbyssalHarpoonBolt : Bullet
    {
        private const int NormalTetherTicks = 228;
        private const int ReducedTetherTicks = 150;
        private const int MicroYankMaxSteps = 2;
        private const float HugeBodySizeThreshold = 2.65f;

        private static HediffDef tetherHediffDef;
        private static HediffDef reducedTetherHediffDef;

        private int ticksAlive;
        private bool launchVfxSpawned;

        private static HediffDef TetherHediffDef => tetherHediffDef ?? (tetherHediffDef = DefDatabase<HediffDef>.GetNamedSilentFail("ABY_AbyssalHarpoonTether"));
        private static HediffDef ReducedTetherHediffDef => reducedTetherHediffDef ?? (reducedTetherHediffDef = DefDatabase<HediffDef>.GetNamedSilentFail("ABY_AbyssalHarpoonTether_Reduced"));

        protected override void Tick()
        {
            Vector3 previousPosition = ExactPosition;
            base.Tick();

            if (!Spawned || Map == null)
            {
                return;
            }

            ticksAlive++;
            if (!launchVfxSpawned)
            {
                launchVfxSpawned = true;
                AbyssalHarpoonVfxUtility.SpawnLaunchSpark(previousPosition, destination, Map);
            }

            if (ticksAlive % 5 == 0 && Rand.Chance(0.45f))
            {
                FleckMaker.ThrowDustPuffThick(ExactPosition, Map, 0.18f, new Color(0.42f, 0.04f, 0.07f, 0.34f));
            }
        }

        protected override void Impact(Thing hitThing, bool blockedByShield = false)
        {
            Map impactMap = Map;
            Vector3 impactPosition = ExactPosition;
            Thing instigator = Launcher;
            Pawn targetPawn = ResolveTargetPawn(hitThing);

            if (!ABY_ProjectileImpactSafetyUtility.TryRunBaseImpact(this, "Projectile_ABY_TurretAbyssalHarpoonBolt", () => base.Impact(hitThing, blockedByShield)))
            {
                return;
            }

            if (impactMap == null)
            {
                return;
            }

            AbyssalHarpoonVfxUtility.SpawnImpact(impactPosition, impactMap, blockedByShield);

            if (blockedByShield || targetPawn == null || targetPawn.Destroyed || targetPawn.Dead)
            {
                return;
            }

            bool reduced = IsBossOrHuge(targetPawn);
            ApplyTether(targetPawn, instigator, impactPosition, reduced);
            if (!reduced)
            {
                TryMicroYank(targetPawn, instigator, impactMap);
            }
        }

        private Pawn ResolveTargetPawn(Thing hitThing)
        {
            Pawn pawn = hitThing as Pawn;
            if (pawn != null && !pawn.Destroyed && !pawn.Dead)
            {
                return pawn;
            }

            if (Map == null || !Position.IsValid || !Position.InBounds(Map))
            {
                return null;
            }

            List<Thing> things = Position.GetThingList(Map);
            for (int i = 0; i < things.Count; i++)
            {
                pawn = things[i] as Pawn;
                if (pawn != null && !pawn.Destroyed && !pawn.Dead && IsHostileTarget(pawn, Launcher?.Faction))
                {
                    return pawn;
                }
            }

            return null;
        }

        private static void ApplyTether(Pawn pawn, Thing anchor, Vector3 impactPosition, bool reduced)
        {
            if (pawn?.health == null)
            {
                return;
            }

            HediffDef def = reduced ? ReducedTetherHediffDef : TetherHediffDef;
            if (def == null)
            {
                return;
            }

            HediffDef otherDef = reduced ? TetherHediffDef : ReducedTetherHediffDef;
            Hediff other = otherDef != null ? pawn.health.hediffSet.GetFirstHediffOfDef(otherDef) : null;
            if (other != null)
            {
                pawn.health.RemoveHediff(other);
            }

            Hediff hediff = pawn.health.hediffSet.GetFirstHediffOfDef(def);
            if (hediff == null)
            {
                hediff = HediffMaker.MakeHediff(def, pawn);
                if (hediff == null)
                {
                    return;
                }
                hediff.Severity = 0.50f;
                pawn.health.AddHediff(hediff);
            }
            else
            {
                hediff.Severity = Mathf.Max(hediff.Severity, 0.50f);
            }

            Hediff_ABY_AbyssalHarpoonTether tether = hediff as Hediff_ABY_AbyssalHarpoonTether;
            if (tether != null)
            {
                Vector3 fallback = anchor != null && !anchor.Destroyed ? anchor.DrawPos : impactPosition;
                tether.ConfigureAnchor(anchor, fallback, reduced, reduced ? ReducedTetherTicks : NormalTetherTicks);
            }

            pawn.health.hediffSet.DirtyCache();
            AbyssalHarpoonVfxUtility.SpawnMarker(pawn.DrawPos, pawn.Map, reduced);
            if (anchor != null && !anchor.Destroyed && anchor.Spawned && anchor.Map == pawn.Map)
            {
                AbyssalHarpoonVfxUtility.SpawnTether(anchor.DrawPos, pawn.DrawPos, pawn.Map, reduced);
            }
        }

        private static bool TryMicroYank(Pawn pawn, Thing anchor, Map map)
        {
            if (pawn == null || anchor == null || map == null || !pawn.Spawned || pawn.Dead || pawn.Downed)
            {
                return false;
            }

            if (pawn.Faction == anchor.Faction || IsBossOrHuge(pawn))
            {
                return false;
            }

            IntVec3 destination = ResolveMicroYankDestination(pawn, anchor.Position, map, MicroYankMaxSteps);
            if (!destination.IsValid || destination == pawn.Position)
            {
                return false;
            }

            try
            {
                pawn.pather?.StopDead();
                pawn.jobs?.EndCurrentJob(JobCondition.InterruptForced, true, true);
                pawn.DeSpawn(DestroyMode.Vanish);
                GenSpawn.Spawn(pawn, destination, map, WipeMode.Vanish);
                pawn.Drawer?.tweener?.ResetTweenedPosToRoot();
                FleckMaker.ThrowDustPuffThick(destination.ToVector3Shifted(), map, 0.42f, new Color(0.48f, 0.04f, 0.08f, 0.45f));
                return true;
            }
            catch (System.Exception ex)
            {
                if (!AbyssalProtocolMod.Settings.suppressRepeatedWarnings)
                {
                    Log.Warning("[Abyssal Protocol] Abyssal Harpoon micro-yank failed and was skipped: " + ex.GetType().Name + " " + ex.Message);
                }
                return false;
            }
        }

        private static IntVec3 ResolveMicroYankDestination(Pawn pawn, IntVec3 anchorCell, Map map, int maxSteps)
        {
            IntVec3 current = pawn.Position;
            IntVec3 original = current;
            int steps = Mathf.Max(1, maxSteps);

            for (int i = 0; i < steps; i++)
            {
                IntVec3 next = BestAdjacentStepToward(current, anchorCell, map, pawn);
                if (!next.IsValid || next == current)
                {
                    break;
                }
                current = next;
            }

            return current == original ? IntVec3.Invalid : current;
        }

        private static IntVec3 BestAdjacentStepToward(IntVec3 from, IntVec3 anchorCell, Map map, Pawn pawn)
        {
            float currentDistance = from.DistanceToSquared(anchorCell);
            float bestDistance = currentDistance;
            IntVec3 best = IntVec3.Invalid;

            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dz = -1; dz <= 1; dz++)
                {
                    if (dx == 0 && dz == 0)
                    {
                        continue;
                    }

                    IntVec3 cell = new IntVec3(from.x + dx, from.y, from.z + dz);
                    if (!CanYankInto(cell, from, map, pawn))
                    {
                        continue;
                    }

                    float distance = cell.DistanceToSquared(anchorCell);
                    if (distance < bestDistance)
                    {
                        bestDistance = distance;
                        best = cell;
                    }
                }
            }

            return best;
        }

        private static bool CanYankInto(IntVec3 cell, IntVec3 from, Map map, Pawn pawn)
        {
            if (!cell.IsValid || map == null || !cell.InBounds(map) || cell.Fogged(map) || !cell.Standable(map))
            {
                return false;
            }

            if (!GenSight.LineOfSight(from, cell, map, true))
            {
                return false;
            }

            List<Thing> things = cell.GetThingList(map);
            for (int i = 0; i < things.Count; i++)
            {
                Thing thing = things[i];
                if (thing == null || thing == pawn)
                {
                    continue;
                }

                if (thing is Pawn || thing.def?.passability == Traversability.Impassable)
                {
                    return false;
                }
            }

            return true;
        }

        private static bool IsHostileTarget(Pawn pawn, Faction launcherFaction)
        {
            if (pawn == null || pawn.Destroyed || pawn.Dead || pawn.Downed || launcherFaction == null || pawn.Faction == null)
            {
                return false;
            }

            return ABY_FactionHostilityUtility.SafeHostileTo(pawn.Faction, launcherFaction);
        }

        private static bool IsBossOrHuge(Pawn pawn)
        {
            if (pawn == null)
            {
                return false;
            }

            if (pawn.TryGetComp<CompABY_BossTrueDeath>() != null || pawn.TryGetComp<CompABY_BossNoDowned>() != null)
            {
                return true;
            }

            if (pawn.RaceProps != null && pawn.RaceProps.baseBodySize >= HugeBodySizeThreshold)
            {
                return true;
            }

            return pawn.def != null && (pawn.def.size.x > 1 || pawn.def.size.z > 1);
        }
    }
}
