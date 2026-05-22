using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace AbyssalProtocol
{
    public class CompProperties_ABY_RiftButcherCombat : CompProperties
    {
        public int scanIntervalTicks = 15;

        public string startCarapaceHediffDefName = "ABY_RiftButcherCarapace";
        public int startCarapaceTicks = 900;

        public string hookSnareHediffDefName = "ABY_RiftButcherHookSnare";
        public int hookCooldownTicks = 520;
        public int hookCooldownJitterTicks = 120;
        public float hookMinRange = 3.2f;
        public float hookRange = 10.0f;
        public float hookDamage = 12f;
        public float hookArmorPenetration = 0.22f;
        public float hookSnareSeverity = 0.58f;
        public string hookSoundDefName = "ABY_SigilChargePulse";

        public string severanceHediffDefName = "ABY_RiftButcherSeveredLine";
        public int sweepCooldownTicks = 500;
        public int sweepCooldownJitterTicks = 90;
        public float sweepTriggerRadius = 2.4f;
        public int sweepTriggerHostiles = 2;
        public float sweepRadius = 2.75f;
        public float sweepDamage = 17f;
        public float sweepArmorPenetration = 0.30f;
        public int sweepMaxTargets = 9;
        public string sweepSoundDefName = "ABY_SigilChargePulse";
        public string sweepMoteDefName = "ABY_Mote_DominionSliceAmbientPressurePulse";

        public int dashCooldownTicks = 660;
        public int dashCooldownJitterTicks = 130;
        public float dashMinRange = 5.0f;
        public float dashRange = 13.0f;
        public int dashDurationTicks = 14;
        public string dashMoteDefName = "ABY_Mote_ArchonDashTrail";
        public float dashMoteScale = 1.18f;
        public string dashSoundDefName = "ABY_SigilChargePulse";

        public float frenzyHealthPct = 0.35f;
        public string frenzyHediffDefName = "ABY_RiftButcherExecutionFocus";
        public float frenzySeverity = 1f;

        public float firstThresholdHealthPct = 0.70f;
        public float secondThresholdHealthPct = 0.40f;
        public int thresholdImpCount = 2;
        public int thresholdHoundCount = 1;
        public string thresholdImpPawnKindDefName = "ABY_RiftImp";
        public string thresholdHoundPawnKindDefName = "ABY_EmberHound";
        public float thresholdSpawnRadius = 4.2f;
        public string thresholdMoteDefName = "ABY_Mote_ArchonDashTrail";
        public string thresholdSoundDefName = "ABY_SigilChargePulse";

        public CompProperties_ABY_RiftButcherCombat()
        {
            compClass = typeof(CompABY_RiftButcherCombat);
        }
    }

    public class CompABY_RiftButcherCombat : ThingComp
    {
        private int nextHookTick;
        private int nextSweepTick;
        private int nextDashTick;
        private bool firstThresholdSpawned;
        private bool secondThresholdSpawned;
        private readonly List<Pawn> sweepTargets = new List<Pawn>();

        public CompProperties_ABY_RiftButcherCombat Props => (CompProperties_ABY_RiftButcherCombat)props;
        private Pawn PawnParent => parent as Pawn;

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref nextHookTick, "nextHookTick", 0);
            Scribe_Values.Look(ref nextSweepTick, "nextSweepTick", 0);
            Scribe_Values.Look(ref nextDashTick, "nextDashTick", 0);
            Scribe_Values.Look(ref firstThresholdSpawned, "firstThresholdSpawned", false);
            Scribe_Values.Look(ref secondThresholdSpawned, "secondThresholdSpawned", false);
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            if (!respawningAfterLoad)
            {
                Pawn pawn = PawnParent;
                if (ShouldOperate(pawn))
                {
                    ABY_ProjectileProcUtility.ApplyOrRefreshFixedHediff(pawn, Props.startCarapaceHediffDefName, 1f, Props.startCarapaceTicks);
                    int ticks = Find.TickManager?.TicksGame ?? 0;
                    nextHookTick = ticks + 180;
                    nextSweepTick = ticks + 240;
                    nextDashTick = ticks + 260;
                }
            }
        }

        public override string CompInspectStringExtra()
        {
            Pawn pawn = PawnParent;
            if (!ShouldOperate(pawn))
            {
                return base.CompInspectStringExtra();
            }

            int ticks = Find.TickManager?.TicksGame ?? 0;
            string focus = HealthPct(pawn) <= Props.frenzyHealthPct
                ? "ABY_RiftButcherInspect_Frenzy".Translate()
                : "ABY_RiftButcherInspect_Priming".Translate(
                    Mathf.Max(0, nextHookTick - ticks).ToStringTicksToPeriod(),
                    Mathf.Max(0, nextDashTick - ticks).ToStringTicksToPeriod(),
                    Mathf.Max(0, nextSweepTick - ticks).ToStringTicksToPeriod());
            return focus;
        }

        public override void CompTick()
        {
            base.CompTick();

            try
            {
                TickCombatSafe();
            }
            catch (Exception ex)
            {
                ABY_LogThrottleUtility.Warning(
                    "rift-butcher-combat-tick-failed",
                    "[Abyssal Protocol] Rift Butcher combat tick failed and was skipped: " + ex.GetType().Name + ": " + ex.Message,
                    1200);
            }
        }

        private void TickCombatSafe()
        {
            Pawn pawn = PawnParent;
            if (!ShouldOperate(pawn) || !parent.IsHashIntervalTick(Mathf.Max(10, Props.scanIntervalTicks)))
            {
                return;
            }

            int ticks = Find.TickManager?.TicksGame ?? 0;
            float healthPct = HealthPct(pawn);

            if (healthPct <= Props.frenzyHealthPct)
            {
                ABY_ProjectileProcUtility.ApplyOrRefreshFixedHediff(pawn, Props.frenzyHediffDefName, Props.frenzySeverity, 300);
            }

            TryThresholdReinforcements(pawn, healthPct);

            if (ticks >= nextSweepTick && ShouldSweep(pawn))
            {
                if (TryDoSweep(pawn))
                {
                    nextSweepTick = ticks + CooldownWithJitter(Props.sweepCooldownTicks, Props.sweepCooldownJitterTicks);
                    return;
                }
            }

            if (ticks >= nextDashTick && TryDoDash(pawn))
            {
                nextDashTick = ticks + CooldownWithJitter(Props.dashCooldownTicks, Props.dashCooldownJitterTicks);
                return;
            }

            if (ticks >= nextHookTick && TryDoHookSnare(pawn))
            {
                nextHookTick = ticks + CooldownWithJitter(Props.hookCooldownTicks, Props.hookCooldownJitterTicks);
            }
        }

        private bool TryDoHookSnare(Pawn pawn)
        {
            Pawn target = AbyssalThreatPawnUtility.FindBestTarget(
                pawn,
                Props.hookMinRange,
                Props.hookRange,
                true,
                true,
                false,
                8f,
                0f);

            if (target == null)
            {
                return false;
            }

            if (Props.hookDamage > 0f)
            {
                DamageInfo damageInfo = new DamageInfo(
                    DamageDefOf.Cut,
                    Props.hookDamage,
                    Props.hookArmorPenetration,
                    -1f,
                    pawn);
                ABY_ProjectileImpactSafetyUtility.TryApplyDamage(target, damageInfo, "RiftButcherHookSnare");
            }

            ABY_ProjectileProcUtility.ApplyOrRefreshFixedHediff(target, Props.hookSnareHediffDefName, Props.hookSnareSeverity, 360);
            target.pather?.StopDead();
            target.stances?.CancelBusyStanceSoft();
            pawn.rotationTracker?.FaceCell(target.Position);
            ABY_SoundUtility.PlayAt(Props.hookSoundDefName, target.PositionHeld, pawn.Map);
            ABY_AbyssalDashRuntime.SpawnTrailMote(pawn.Map, target.PositionHeld, Props.dashMoteDefName, 0.82f);
            return true;
        }

        private bool TryDoDash(Pawn pawn)
        {
            if (ABY_AbyssalDashRuntime.IsDashing(pawn))
            {
                return false;
            }

            Pawn target = AbyssalThreatPawnUtility.FindBestTarget(
                pawn,
                Props.dashMinRange,
                Props.dashRange,
                true,
                true,
                false,
                12f,
                0f);

            if (target == null || !AbyssalThreatPawnUtility.TryFindAdjacentLandingCell(pawn, target, out IntVec3 landingCell))
            {
                return false;
            }

            bool started = ABY_AbyssalDashRuntime.TryStartDash(
                pawn,
                target,
                landingCell,
                Props.severanceHediffDefName,
                Props.dashDurationTicks,
                Props.dashMoteDefName,
                Props.dashMoteScale,
                Props.dashSoundDefName,
                "rift_butcher_dash");

            if (started)
            {
                ABY_AbyssalDashRuntime.SpawnTrailMote(pawn.Map, pawn.PositionHeld, Props.dashMoteDefName, Props.dashMoteScale);
            }

            return started;
        }

        private bool ShouldSweep(Pawn pawn)
        {
            if (pawn?.Map == null)
            {
                return false;
            }

            int count = 0;
            IReadOnlyList<Pawn> pawns = ABY_RuntimeTargetCache.CombatTargetPawnsFor(pawn.Map);
            if (pawns == null)
            {
                return false;
            }

            float radiusSq = Props.sweepTriggerRadius * Props.sweepTriggerRadius;
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn candidate = pawns[i];
                if (!AbyssalThreatPawnUtility.IsValidHostileTarget(pawn, candidate))
                {
                    continue;
                }

                if (pawn.Position.DistanceToSquared(candidate.Position) <= radiusSq)
                {
                    count++;
                    if (count >= Mathf.Max(1, Props.sweepTriggerHostiles))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private bool TryDoSweep(Pawn pawn)
        {
            if (pawn?.Map == null)
            {
                return false;
            }

            sweepTargets.Clear();
            IReadOnlyList<Pawn> pawns = ABY_RuntimeTargetCache.CombatTargetPawnsFor(pawn.Map);
            if (pawns == null)
            {
                return false;
            }

            float radiusSq = Props.sweepRadius * Props.sweepRadius;
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn candidate = pawns[i];
                if (!AbyssalThreatPawnUtility.IsValidHostileTarget(pawn, candidate))
                {
                    continue;
                }

                if (pawn.Position.DistanceToSquared(candidate.Position) > radiusSq)
                {
                    continue;
                }

                sweepTargets.Add(candidate);
                if (sweepTargets.Count >= Mathf.Max(1, Props.sweepMaxTargets))
                {
                    break;
                }
            }

            if (sweepTargets.Count == 0)
            {
                return false;
            }

            for (int i = 0; i < sweepTargets.Count; i++)
            {
                Pawn target = sweepTargets[i];
                float distance = pawn.Position.DistanceTo(target.PositionHeld);
                float falloff = Mathf.Clamp01(1f - (distance / Mathf.Max(0.1f, Props.sweepRadius)) * 0.35f);
                DamageInfo damageInfo = new DamageInfo(
                    DamageDefOf.Cut,
                    Props.sweepDamage * falloff,
                    Props.sweepArmorPenetration,
                    -1f,
                    pawn);
                ABY_ProjectileImpactSafetyUtility.TryApplyDamage(target, damageInfo, "RiftButcherSeveranceSweep");
                ABY_ProjectileProcUtility.ApplyOrRefreshFixedHediff(target, Props.severanceHediffDefName, 1f, 240);
                target.pather?.StopDead();
            }

            pawn.rotationTracker?.FaceCell(sweepTargets[0].PositionHeld);
            ABY_SoundUtility.PlayAt(Props.sweepSoundDefName, pawn.PositionHeld, pawn.Map);
            ABY_AbyssalDashRuntime.SpawnTrailMote(pawn.Map, pawn.PositionHeld, Props.sweepMoteDefName, 1.55f);
            return true;
        }

        private void TryThresholdReinforcements(Pawn pawn, float healthPct)
        {
            if (!firstThresholdSpawned && healthPct <= Props.firstThresholdHealthPct)
            {
                firstThresholdSpawned = true;
                SpawnThresholdPack(pawn);
            }

            if (!secondThresholdSpawned && healthPct <= Props.secondThresholdHealthPct)
            {
                secondThresholdSpawned = true;
                SpawnThresholdPack(pawn);
            }
        }

        private void SpawnThresholdPack(Pawn pawn)
        {
            if (pawn?.Map == null)
            {
                return;
            }

            SpawnPawnKindPack(pawn, Props.thresholdImpPawnKindDefName, Mathf.Max(0, Props.thresholdImpCount));
            SpawnPawnKindPack(pawn, Props.thresholdHoundPawnKindDefName, Mathf.Max(0, Props.thresholdHoundCount));
            ABY_SoundUtility.PlayAt(Props.thresholdSoundDefName, pawn.PositionHeld, pawn.Map);
            ABY_AbyssalDashRuntime.SpawnTrailMote(pawn.Map, pawn.PositionHeld, Props.thresholdMoteDefName, 1.25f);
        }

        private void SpawnPawnKindPack(Pawn pawn, string pawnKindDefName, int count)
        {
            if (count <= 0 || pawnKindDefName.NullOrEmpty() || pawn?.Map == null)
            {
                return;
            }

            PawnKindDef kindDef = ABY_DefCache.PawnKindDefNamed(pawnKindDefName);
            if (kindDef == null)
            {
                return;
            }

            for (int i = 0; i < count; i++)
            {
                if (!TryFindSpawnCellNear(pawn, out IntVec3 cell))
                {
                    continue;
                }

                Pawn spawned = PawnGenerator.GeneratePawn(kindDef, pawn.Faction);
                if (spawned == null)
                {
                    continue;
                }

                GenSpawn.Spawn(spawned, cell, pawn.Map);
                AbyssalThreatPawnUtility.PrepareThreatPawn(spawned);
                AbyssalLordUtility.EnsureAssaultLord(spawned, false);
                ABY_AbyssalDashRuntime.SpawnTrailMote(pawn.Map, cell, Props.thresholdMoteDefName, 0.72f);
            }
        }

        private bool TryFindSpawnCellNear(Pawn pawn, out IntVec3 result)
        {
            result = IntVec3.Invalid;
            if (pawn?.Map == null)
            {
                return false;
            }

            Map map = pawn.Map;
            int radius = Mathf.CeilToInt(Mathf.Max(2.0f, Props.thresholdSpawnRadius));
            for (int i = 0; i < 36; i++)
            {
                IntVec3 cell = pawn.Position + new IntVec3(Rand.RangeInclusive(-radius, radius), 0, Rand.RangeInclusive(-radius, radius));
                if (!cell.InBounds(map) || !cell.Standable(map) || AbyssalThreatPawnUtility.CellHasOtherPawn(cell, map, pawn))
                {
                    continue;
                }

                result = cell;
                return true;
            }

            return CellFinder.TryFindRandomCellNear(pawn.Position, pawn.Map, radius, c => c.Standable(map) && !AbyssalThreatPawnUtility.CellHasOtherPawn(c, map, pawn), out result);
        }

        private static int CooldownWithJitter(int baseTicks, int jitterTicks)
        {
            int jitter = Mathf.Max(0, jitterTicks);
            return Mathf.Max(60, baseTicks + Rand.RangeInclusive(-jitter, jitter));
        }

        private static float HealthPct(Pawn pawn)
        {
            return Mathf.Clamp01(pawn?.health?.summaryHealth?.SummaryHealthPercent ?? 1f);
        }

        private static bool ShouldOperate(Pawn pawn)
        {
            return pawn != null && pawn.Spawned && pawn.Map != null && !pawn.Dead && !pawn.Downed && pawn.Faction != null;
        }
    }
}
