using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    public class CompABY_WardenOfAshPulse : ThingComp
    {
        private int nextPulseTick = -1;

        public CompProperties_ABY_WardenOfAshPulse Props => (CompProperties_ABY_WardenOfAshPulse)props;

        private Pawn PawnParent => parent as Pawn;

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);

            if (!respawningAfterLoad)
            {
                ScheduleNextPulse(initial: true);
            }
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref nextPulseTick, "nextPulseTick", -1);
        }

        public override void CompTick()
        {
            base.CompTick();

            Pawn pawn = PawnParent;
            if (!ShouldOperateNow(pawn))
            {
                return;
            }

            if (nextPulseTick < 0)
            {
                ScheduleNextPulse(initial: true);
            }

            if (Find.TickManager.TicksGame < nextPulseTick)
            {
                return;
            }

            if (HasHostileTargetInRange(pawn))
            {
                EmitPulse(pawn);
            }

            ScheduleNextPulse(initial: false);
        }

        private bool ShouldOperateNow(Pawn pawn)
        {
            return pawn != null && pawn.Spawned && pawn.MapHeld != null && !pawn.Dead && !pawn.Downed;
        }

        private void ScheduleNextPulse(bool initial)
        {
            int variance = Math.Max(0, Props.pulseIntervalVariance);
            int offset = variance > 0 ? Rand.RangeInclusive(-variance, variance) : 0;
            int baseDelay = initial ? Props.warmupTicks : Props.pulseIntervalTicks;
            nextPulseTick = Find.TickManager.TicksGame + Math.Max(90, baseDelay + offset);
        }

        private bool HasHostileTargetInRange(Pawn pawn)
        {
            foreach (IntVec3 cell in GenRadial.RadialCellsAround(pawn.PositionHeld, Props.pulseRadius, useCenter: true))
            {
                if (!cell.InBounds(pawn.MapHeld))
                {
                    continue;
                }

                List<Thing> things = cell.GetThingList(pawn.MapHeld);
                for (int i = 0; i < things.Count; i++)
                {
                    if (things[i] is Pawn other && other != pawn && !other.Dead && pawn.HostileTo(other))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private void EmitPulse(Pawn pawn)
        {
            Map map = pawn.MapHeld;
            if (map == null)
            {
                return;
            }

            IntVec3 center = pawn.PositionHeld;
            FleckMaker.Static(center, map, FleckDefOf.ExplosionFlash, Props.visualScale);

            HashSet<Pawn> damagedPawns = new HashSet<Pawn>();
            List<IntVec3> candidateFireCells = new List<IntVec3>();

            foreach (IntVec3 cell in GenRadial.RadialCellsAround(center, Props.pulseRadius, useCenter: true))
            {
                if (!cell.InBounds(map))
                {
                    continue;
                }

                float distance = Mathf.Max(0.6f, center.DistanceTo(cell));
                float falloff = Mathf.Clamp01(1f - ((distance - 0.6f) / Mathf.Max(0.5f, Props.pulseRadius)));
                if (falloff <= 0f)
                {
                    continue;
                }

                List<Thing> things = cell.GetThingList(map);
                for (int i = 0; i < things.Count; i++)
                {
                    if (things[i] is Pawn victim && victim != pawn && !victim.Dead && pawn.HostileTo(victim) && damagedPawns.Add(victim))
                    {
                        int damage = Math.Max(1, GenMath.RoundRandom(Props.pulseDamage * falloff));
                        DamageInfo dinfo = new DamageInfo(
                            DamageDefOf.Flame,
                            damage,
                            Props.pulseArmorPenetration,
                            angle: -1f,
                            instigator: pawn);
                        victim.TakeDamage(dinfo);
                    }
                }

                if (cell.Walkable(map) && Rand.Chance(Props.igniteChancePerCell * falloff))
                {
                    candidateFireCells.Add(cell);
                }
            }

            int ignited = 0;
            for (int i = 0; i < candidateFireCells.Count && ignited < Props.maxCellsToIgnite; i++)
            {
                IntVec3 fireCell = candidateFireCells[i];
                if (fireCell.GetFirstThing<Fire>(map) != null)
                {
                    continue;
                }

                FireUtility.TryStartFireIn(fireCell, map, Rand.Range(0.35f, 0.7f));
                ignited++;
            }
        }
    }
}
