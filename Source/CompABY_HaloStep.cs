using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    public class CompABY_HaloStep : ThingComp
    {
        private int nextStepTick = -1;
        private float recentDamageWindow;
        private int recentDamageExpireTick = -1;

        public CompProperties_ABY_HaloStep Props => (CompProperties_ABY_HaloStep)props;

        private Pawn PawnParent => parent as Pawn;

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref nextStepTick, "nextStepTick", -1);
            Scribe_Values.Look(ref recentDamageWindow, "recentDamageWindow", 0f);
            Scribe_Values.Look(ref recentDamageExpireTick, "recentDamageExpireTick", -1);
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            if (!respawningAfterLoad)
            {
                ScheduleNextStep(initial: true);
            }
        }

        public override void PostPostApplyDamage(DamageInfo dinfo, float totalDamageDealt)
        {
            base.PostPostApplyDamage(dinfo, totalDamageDealt);

            Pawn pawn = PawnParent;
            if (!ShouldOperateNow(pawn) || totalDamageDealt <= 0.01f)
            {
                return;
            }

            Thing instigator = dinfo.Instigator;
            if (instigator != null && !IsHostileInstigator(pawn, instigator))
            {
                return;
            }

            int ticksGame = Find.TickManager != null ? Find.TickManager.TicksGame : 0;
            recentDamageWindow += totalDamageDealt;
            recentDamageExpireTick = ticksGame + Mathf.Max(30, Props.damageWindowTicks);
        }

        public override void CompTick()
        {
            base.CompTick();

            Pawn pawn = PawnParent;
            if (!ShouldOperateNow(pawn) || !parent.IsHashIntervalTick(Mathf.Max(10, Props.scanIntervalTicks)))
            {
                return;
            }

            int ticksGame = Find.TickManager != null ? Find.TickManager.TicksGame : 0;
            if (nextStepTick < 0)
            {
                ScheduleNextStep(initial: true);
            }

            if (recentDamageExpireTick >= 0 && ticksGame > recentDamageExpireTick)
            {
                recentDamageWindow = 0f;
                recentDamageExpireTick = -1;
            }

            if (ticksGame < nextStepTick)
            {
                return;
            }

            bool threatenedByProximity = HasNearbyThreat(pawn);
            bool threatenedByDamage = recentDamageWindow >= Props.damageThreshold;
            if (!threatenedByProximity && !threatenedByDamage)
            {
                return;
            }

            if (TryHaloStep(pawn))
            {
                recentDamageWindow = 0f;
                recentDamageExpireTick = -1;
                ScheduleNextStep(initial: false);
            }
        }

        private static bool ShouldOperateNow(Pawn pawn)
        {
            return pawn != null && pawn.Spawned && pawn.MapHeld != null && !pawn.Dead && !pawn.Downed;
        }

        private void ScheduleNextStep(bool initial)
        {
            int now = Find.TickManager != null ? Find.TickManager.TicksGame : 0;
            int baseDelay = initial ? Mathf.RoundToInt(Props.cooldownTicks * 0.55f) : Props.cooldownTicks;
            int variance = initial ? 0 : Mathf.Max(0, Props.cooldownVarianceTicks);
            int offset = variance > 0 ? Rand.RangeInclusive(-variance, variance) : 0;
            nextStepTick = now + Mathf.Max(90, baseDelay + offset);
        }

        private bool HasNearbyThreat(Pawn pawn)
        {
            IReadOnlyList<Pawn> pawns = pawn.MapHeld.mapPawns?.AllPawnsSpawned;
            if (pawns == null)
            {
                return false;
            }

            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn other = pawns[i];
                if (!IsValidHostilePawn(pawn, other))
                {
                    continue;
                }

                if (other.PositionHeld.InHorDistOf(pawn.PositionHeld, Props.triggerEnemyRange))
                {
                    return true;
                }
            }

            return false;
        }

        private bool TryHaloStep(Pawn pawn)
        {
            Map map = pawn.MapHeld;
            if (map == null)
            {
                return false;
            }

            IntVec3 origin = pawn.PositionHeld;
            List<Pawn> hostilePawns = CollectHostilePawns(pawn);
            if (hostilePawns.Count == 0)
            {
                return false;
            }

            IntVec3 bestCell = IntVec3.Invalid;
            float bestScore = float.MinValue;

            foreach (IntVec3 cell in GenRadial.RadialCellsAround(origin, Props.maxStepDistance, true))
            {
                if (!cell.IsValid || cell == origin || !cell.InBounds(map) || !cell.Walkable(map) || cell.Fogged(map))
                {
                    continue;
                }

                float stepDistance = origin.DistanceTo(cell);
                if (stepDistance < Props.minStepDistance)
                {
                    continue;
                }

                if (CellHasBlockingPawn(cell, map, pawn))
                {
                    continue;
                }

                float nearestHostileDist = FindNearestHostileDistance(hostilePawns, cell);
                if (nearestHostileDist < Props.avoidEnemyRadius)
                {
                    continue;
                }

                float score = (nearestHostileDist * 4f) + stepDistance + Rand.Value * 0.15f;
                if (score > bestScore)
                {
                    bestScore = score;
                    bestCell = cell;
                }
            }

            if (!bestCell.IsValid)
            {
                return false;
            }

            Vector3 oldDrawPos = pawn.DrawPos;
            pawn.pather?.StopDead();
            pawn.Position = bestCell;
            pawn.Notify_Teleported();

            FleckMaker.ThrowLightningGlow(oldDrawPos, map, Props.visualScale);
            FleckMaker.Static(origin, map, FleckDefOf.ExplosionFlash, Props.visualScale * 0.32f);
            FleckMaker.ThrowLightningGlow(pawn.DrawPos, map, Props.visualScale);
            FleckMaker.Static(bestCell, map, FleckDefOf.ExplosionFlash, Props.visualScale * 0.42f);
            return true;
        }

        private static List<Pawn> CollectHostilePawns(Pawn pawn)
        {
            List<Pawn> hostiles = new List<Pawn>();
            IReadOnlyList<Pawn> pawns = pawn.MapHeld.mapPawns?.AllPawnsSpawned;
            if (pawns == null)
            {
                return hostiles;
            }

            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn other = pawns[i];
                if (IsValidHostilePawn(pawn, other))
                {
                    hostiles.Add(other);
                }
            }

            return hostiles;
        }

        private static bool IsValidHostilePawn(Pawn pawn, Pawn other)
        {
            if (pawn == null || other == null || other == pawn || !other.Spawned || other.MapHeld != pawn.MapHeld || other.Dead || other.Downed)
            {
                return false;
            }

            if (pawn.Faction != null && other.Faction != null)
            {
                return pawn.Faction.HostileTo(other.Faction);
            }

            return other.HostileTo(pawn);
        }

        private static bool IsHostileInstigator(Pawn pawn, Thing instigator)
        {
            if (pawn == null || instigator == null)
            {
                return false;
            }

            Pawn instigatorPawn = instigator as Pawn;
            if (instigatorPawn != null)
            {
                return IsValidHostilePawn(pawn, instigatorPawn);
            }

            if (pawn.Faction != null && instigator.Faction != null)
            {
                return pawn.Faction.HostileTo(instigator.Faction);
            }

            return false;
        }

        private static bool CellHasBlockingPawn(IntVec3 cell, Map map, Pawn pawn)
        {
            List<Thing> thingList = cell.GetThingList(map);
            for (int i = 0; i < thingList.Count; i++)
            {
                Pawn otherPawn = thingList[i] as Pawn;
                if (otherPawn != null && otherPawn != pawn && !otherPawn.Dead)
                {
                    return true;
                }
            }

            return false;
        }

        private static float FindNearestHostileDistance(List<Pawn> hostilePawns, IntVec3 cell)
        {
            float num = 999f;
            for (int i = 0; i < hostilePawns.Count; i++)
            {
                Pawn hostile = hostilePawns[i];
                if (hostile == null || !hostile.Spawned || hostile.Dead)
                {
                    continue;
                }

                float dist = hostile.PositionHeld.DistanceTo(cell);
                if (dist < num)
                {
                    num = dist;
                }
            }

            return num;
        }
    }
}
