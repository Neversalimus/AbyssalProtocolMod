using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    public class CompABY_ChoirEngineRelay : ThingComp
    {
        private const int DisruptionDurationTicks = 240;
        private const float DisruptionDamageThreshold = 18f;

        private int nextPulseTick = -1;
        private int nextTurretTick = -1;
        private int disruptedUntilTick = -1;
        private int nextDisruptionFxTick = -1;

        public CompProperties_ABY_ChoirEngineRelay Props => (CompProperties_ABY_ChoirEngineRelay)props;
        private Pawn PawnParent => parent as Pawn;

        public bool IsDisrupted
        {
            get
            {
                int ticksGame = Find.TickManager != null ? Find.TickManager.TicksGame : 0;
                return ticksGame < disruptedUntilTick;
            }
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            if (!respawningAfterLoad)
            {
                SchedulePulse(initial: true);
                ScheduleTurretSuppression(initial: true);
            }
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref nextPulseTick, "nextPulseTick", -1);
            Scribe_Values.Look(ref nextTurretTick, "nextTurretTick", -1);
            Scribe_Values.Look(ref disruptedUntilTick, "disruptedUntilTick", -1);
            Scribe_Values.Look(ref nextDisruptionFxTick, "nextDisruptionFxTick", -1);
        }

        public override void CompTick()
        {
            base.CompTick();

            Pawn pawn = PawnParent;
            if (!ShouldOperateNow(pawn))
            {
                return;
            }

            int ticksGame = Find.TickManager != null ? Find.TickManager.TicksGame : 0;
            if (IsDisrupted)
            {
                if (ticksGame >= nextDisruptionFxTick)
                {
                    ShowDisruptionFX(pawn, false);
                    nextDisruptionFxTick = ticksGame + 45;
                }

                return;
            }

            if (nextPulseTick < 0)
            {
                SchedulePulse(initial: true);
            }
            if (nextTurretTick < 0)
            {
                ScheduleTurretSuppression(initial: true);
            }

            if (ticksGame >= nextTurretTick)
            {
                ExecuteTurretSuppression(pawn);
                ScheduleTurretSuppression(initial: false);
            }

            if (ticksGame >= nextPulseTick)
            {
                if (HasHostileTargetInRadius(pawn, Props.pulseRadius))
                {
                    ExecuteChoirPulse(pawn);
                }
                SchedulePulse(initial: false);
            }
        }

        public override void PostPostApplyDamage(DamageInfo dinfo, float totalDamageDealt)
        {
            base.PostPostApplyDamage(dinfo, totalDamageDealt);

            Pawn pawn = PawnParent;
            if (!ShouldOperateNow(pawn))
            {
                return;
            }

            if (dinfo.Def == DamageDefOf.EMP || totalDamageDealt >= DisruptionDamageThreshold)
            {
                TriggerDisruption(pawn);
            }
        }

        public override string CompInspectStringExtra()
        {
            if (!IsDisrupted || Find.TickManager == null)
            {
                return null;
            }

            int ticksRemaining = Math.Max(0, disruptedUntilTick - Find.TickManager.TicksGame);
            return "Choir relay disrupted: " + (ticksRemaining / 60f).ToString("0.0") + "s";
        }

        private void TriggerDisruption(Pawn pawn)
        {
            int ticksGame = Find.TickManager != null ? Find.TickManager.TicksGame : 0;
            disruptedUntilTick = Math.Max(disruptedUntilTick, ticksGame + DisruptionDurationTicks);
            nextDisruptionFxTick = ticksGame + 30;
            ShowDisruptionFX(pawn, true);
        }

        private void ShowDisruptionFX(Pawn pawn, bool strong)
        {
            if (pawn?.MapHeld == null)
            {
                return;
            }

            FleckMaker.ThrowLightningGlow(pawn.DrawPos, pawn.MapHeld, strong ? 2.4f : 1.25f);
            FleckMaker.Static(pawn.PositionHeld, pawn.MapHeld, FleckDefOf.ExplosionFlash, strong ? 1.8f : 0.9f);
            Current.Game?.GetComponent<AbyssalBossScreenFXGameComponent>()?.RegisterRitualPulse(pawn.MapHeld, strong ? 0.10f : 0.03f);
            if (strong && !Props.pulseSoundDefName.NullOrEmpty())
            {
                ABY_SoundUtility.PlayAt(Props.pulseSoundDefName, pawn.PositionHeld, pawn.MapHeld);
            }
        }

        private void SchedulePulse(bool initial)
        {
            int variance = Math.Max(0, Props.pulseIntervalVariance);
            int offset = variance > 0 ? Rand.RangeInclusive(-variance, variance) : 0;
            int baseDelay = initial ? Props.warmupTicks : Props.pulseIntervalTicks;
            nextPulseTick = (Find.TickManager != null ? Find.TickManager.TicksGame : 0) + Math.Max(90, baseDelay + offset);
        }

        private void ScheduleTurretSuppression(bool initial)
        {
            int variance = Math.Max(0, Props.turretSuppressionVariance);
            int offset = variance > 0 ? Rand.RangeInclusive(-variance, variance) : 0;
            int baseDelay = initial ? Math.Max(90, Props.warmupTicks / 2) : Props.turretSuppressionIntervalTicks;
            nextTurretTick = (Find.TickManager != null ? Find.TickManager.TicksGame : 0) + Math.Max(60, baseDelay + offset);
        }

        private void ExecuteTurretSuppression(Pawn pawn)
        {
            if (pawn?.MapHeld == null || IsDisrupted)
            {
                return;
            }

            Thing target = FindBestInfrastructureTarget(pawn, Props.turretSuppressionRadius, true);
            if (target == null)
            {
                return;
            }

            if (target is Pawn mech && mech.RaceProps != null && mech.RaceProps.IsMechanoid)
            {
                mech.TakeDamage(new DamageInfo(DamageDefOf.EMP, Props.turretEmpDamage, 0f, -1f, pawn));
            }
            else
            {
                target.TakeDamage(new DamageInfo(DamageDefOf.EMP, Props.turretEmpDamage, 0f, -1f, pawn));
            }

            FleckMaker.ThrowLightningGlow(target.DrawPos, pawn.MapHeld, 1.6f);
            if (!Props.pulseSoundDefName.NullOrEmpty())
            {
                ABY_SoundUtility.PlayAt(Props.pulseSoundDefName, target.PositionHeld, pawn.MapHeld);
            }
        }

        private void ExecuteChoirPulse(Pawn pawn)
        {
            Map map = pawn.MapHeld;
            if (map == null || IsDisrupted)
            {
                return;
            }

            IntVec3 center = pawn.PositionHeld;
            FleckMaker.ThrowLightningGlow(pawn.DrawPos, map, Props.pulseVisualScale);
            FleckMaker.Static(center, map, FleckDefOf.ExplosionFlash, Mathf.Max(1.1f, Props.pulseVisualScale * 0.72f));
            if (!Props.pulseSoundDefName.NullOrEmpty())
            {
                ABY_SoundUtility.PlayAt(Props.pulseSoundDefName, center, map);
            }

            CompABY_ChoirEngineAura aura = pawn.TryGetComp<CompABY_ChoirEngineAura>();
            aura?.ApplyPulseAura(Props.pulseRadius, Mathf.Max(6.8f, Props.pulseRadius - 1.5f), Props.pulseAllySeverity, Props.pulseEnemySeverity);
            AffectInfrastructure(pawn, Props.pulseRadius, Props.maxPulseInfrastructureTargets, Props.infrastructureEmpDamage);
        }

        private void AffectInfrastructure(Pawn pawn, float radius, int maxTargets, float empDamage)
        {
            Map map = pawn.MapHeld;
            if (map == null)
            {
                return;
            }

            int affected = 0;
            foreach (Thing target in GetNearbyInfrastructureTargets(pawn, radius, true))
            {
                if (target is Pawn mech && mech.RaceProps != null && mech.RaceProps.IsMechanoid)
                {
                    mech.TakeDamage(new DamageInfo(DamageDefOf.EMP, empDamage, 0f, -1f, pawn));
                }
                else
                {
                    target.TakeDamage(new DamageInfo(DamageDefOf.EMP, empDamage, 0f, -1f, pawn));
                }

                affected++;
                if (affected >= Math.Max(1, maxTargets))
                {
                    break;
                }
            }
        }

        private Thing FindBestInfrastructureTarget(Pawn pawn, float radius, bool preferTurrets)
        {
            foreach (Thing target in GetNearbyInfrastructureTargets(pawn, radius, preferTurrets))
            {
                return target;
            }

            return null;
        }

        private IEnumerable<Thing> GetNearbyInfrastructureTargets(Pawn pawn, float radius, bool preferTurrets)
        {
            List<Thing> turrets = new List<Thing>();
            List<Thing> other = new List<Thing>();
            Map map = pawn.MapHeld;
            if (map == null)
            {
                yield break;
            }

            foreach (Thing thing in GenRadial.RadialDistinctThingsAround(pawn.PositionHeld, map, radius, true))
            {
                if (!IsValidInfrastructureTarget(pawn, thing))
                {
                    continue;
                }

                if (thing is Building_Turret)
                {
                    turrets.Add(thing);
                }
                else
                {
                    other.Add(thing);
                }
            }

            if (preferTurrets)
            {
                turrets.Sort((a, b) => pawn.PositionHeld.DistanceToSquared(a.PositionHeld).CompareTo(pawn.PositionHeld.DistanceToSquared(b.PositionHeld)));
                other.Sort((a, b) => pawn.PositionHeld.DistanceToSquared(a.PositionHeld).CompareTo(pawn.PositionHeld.DistanceToSquared(b.PositionHeld)));
                foreach (Thing t in turrets) yield return t;
                foreach (Thing t in other) yield return t;
            }
            else
            {
                other.Sort((a, b) => pawn.PositionHeld.DistanceToSquared(a.PositionHeld).CompareTo(pawn.PositionHeld.DistanceToSquared(b.PositionHeld)));
                turrets.Sort((a, b) => pawn.PositionHeld.DistanceToSquared(a.PositionHeld).CompareTo(pawn.PositionHeld.DistanceToSquared(b.PositionHeld)));
                foreach (Thing t in other) yield return t;
                foreach (Thing t in turrets) yield return t;
            }
        }

        private bool HasHostileTargetInRadius(Pawn pawn, float radius)
        {
            if (pawn?.MapHeld?.mapPawns?.AllPawnsSpawned == null)
            {
                return false;
            }

            foreach (Pawn other in pawn.MapHeld.mapPawns.AllPawnsSpawned)
            {
                if (other == null || other == pawn || other.Dead || other.Downed || !other.Spawned)
                {
                    continue;
                }

                if (pawn.HostileTo(other) && pawn.PositionHeld.DistanceTo(other.PositionHeld) <= radius)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsValidInfrastructureTarget(Pawn owner, Thing thing)
        {
            if (owner?.Faction == null || thing == null || thing.Destroyed || !thing.Spawned || thing.MapHeld != owner.MapHeld)
            {
                return false;
            }

            if (thing is Building_Turret turret)
            {
                return turret.Faction != null && owner.Faction.HostileTo(turret.Faction);
            }

            if (thing is Pawn mech && mech.RaceProps != null && mech.RaceProps.IsMechanoid)
            {
                return mech.Faction != null && owner.Faction.HostileTo(mech.Faction);
            }

            if (thing is Building building)
            {
                bool powered = building.GetComp<CompPowerTrader>() != null || building.GetComp<CompPowerBattery>() != null;
                return powered && building.Faction != null && owner.Faction.HostileTo(building.Faction);
            }

            return false;
        }

        private static bool ShouldOperateNow(Pawn pawn)
        {
            return pawn != null && pawn.Spawned && pawn.MapHeld != null && !pawn.Dead && !pawn.Downed;
        }
    }
}
