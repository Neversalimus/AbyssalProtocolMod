using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    public class CompABY_NullPriestBreach : ThingComp
    {
        private const string RiftImpPawnKindDefName = "ABY_RiftImp";
        private const string EmberHoundPawnKindDefName = "ABY_EmberHound";
        private const string NullExposureHediffDefName = "ABY_NullExposure";

        private int nextBreachTick = -1;
        private int totalLifetimeSummons;
        private List<Thing> activeManifestations = new List<Thing>();

        public CompProperties_ABY_NullPriestBreach Props => (CompProperties_ABY_NullPriestBreach)props;

        private Pawn PawnParent => parent as Pawn;

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);

            if (!respawningAfterLoad)
            {
                ScheduleNextBreach(initial: true);
            }
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref nextBreachTick, "nextBreachTick", -1);
            Scribe_Values.Look(ref totalLifetimeSummons, "totalLifetimeSummons", 0);
            Scribe_Collections.Look(ref activeManifestations, "activeManifestations", LookMode.Reference);

            if (Scribe.mode == LoadSaveMode.PostLoadInit && activeManifestations == null)
            {
                activeManifestations = new List<Thing>();
            }
        }

        public override void CompTick()
        {
            base.CompTick();

            Pawn pawn = PawnParent;
            if (!ShouldOperateNow(pawn))
            {
                return;
            }

            CleanupManifestationRefs();

            if (nextBreachTick < 0)
            {
                ScheduleNextBreach(initial: true);
            }

            int ticksGame = Find.TickManager != null ? Find.TickManager.TicksGame : 0;
            if (ticksGame < nextBreachTick)
            {
                return;
            }

            Pawn target = AbyssalThreatPawnUtility.FindBestTarget(
                pawn,
                Props.minTargetRange,
                Props.maxTargetRange,
                preferFarthestTargets: false,
                preferRangedTargets: true,
                requireRangedTargets: false,
                rangedTargetBias: 2.8f,
                healthWeight: 0f);

            if (target != null)
            {
                bool canManifest = activeManifestations.Count < Math.Max(1, Props.maxActiveManifestations)
                    && totalLifetimeSummons < Math.Max(1, Props.maxLifetimeSummons);

                if (!canManifest || !TryOpenMicroRift(pawn, target))
                {
                    EmitNullPulse(pawn, target.PositionHeld);
                }
            }

            ScheduleNextBreach(initial: false);
        }

        private void CleanupManifestationRefs()
        {
            if (activeManifestations == null)
            {
                activeManifestations = new List<Thing>();
                return;
            }

            activeManifestations.RemoveAll(thing => thing == null || thing.Destroyed);
        }

        private void ScheduleNextBreach(bool initial)
        {
            int variance = Math.Max(0, Props.breachIntervalVariance);
            int offset = variance > 0 ? Rand.RangeInclusive(-variance, variance) : 0;
            int baseDelay = initial ? Props.warmupTicks : Props.breachIntervalTicks;
            int now = Find.TickManager != null ? Find.TickManager.TicksGame : 0;
            nextBreachTick = now + Math.Max(120, baseDelay + offset);
        }

        private static bool ShouldOperateNow(Pawn pawn)
        {
            return pawn != null
                && pawn.Spawned
                && pawn.MapHeld != null
                && !pawn.Dead
                && !pawn.Downed
                && pawn.Faction != null;
        }

        private bool TryOpenMicroRift(Pawn pawn, Pawn target)
        {
            Map map = pawn.MapHeld;
            if (map == null || target == null)
            {
                return false;
            }

            if (!TryFindManifestationCellNearTarget(pawn, target, out IntVec3 cell))
            {
                return false;
            }

            PawnKindDef kindDef = ChooseManifestPawnKind();
            if (kindDef == null)
            {
                return false;
            }

            List<ABY_HostileManifestEntry> entries = new List<ABY_HostileManifestEntry>
            {
                new ABY_HostileManifestEntry(kindDef, 1)
            };

            int warmup = Rand.RangeInclusive(
                Math.Max(30, Props.manifestationWarmupMinTicks),
                Math.Max(Props.manifestationWarmupMinTicks, Props.manifestationWarmupMaxTicks));

            bool useSeam = Rand.Chance(Props.seamBreachChance);
            Thing manifestation;
            bool spawned = useSeam
                ? ABY_ArrivalManifestationUtility.TrySpawnSeamBreach(map, entries, pawn.Faction, cell, warmup, out manifestation, out string _)
                : ABY_ArrivalManifestationUtility.TrySpawnStaticPhaseIn(map, entries, pawn.Faction, cell, warmup, out manifestation, out string _);

            if (!spawned || manifestation == null)
            {
                return false;
            }

            activeManifestations.Add(manifestation);
            totalLifetimeSummons++;
            ABY_SoundUtility.PlayAt(Props.pulseSoundDefName, cell, map);
            Current.Game?.GetComponent<AbyssalBossScreenFXGameComponent>()?.RegisterRitualPulse(map, 0.05f);
            return true;
        }

        private PawnKindDef ChooseManifestPawnKind()
        {
            PawnKindDef impKind = DefDatabase<PawnKindDef>.GetNamedSilentFail(RiftImpPawnKindDefName);
            PawnKindDef houndKind = DefDatabase<PawnKindDef>.GetNamedSilentFail(EmberHoundPawnKindDefName);

            if (houndKind != null && Rand.Chance(Props.emberHoundChance))
            {
                return houndKind;
            }

            return impKind ?? houndKind;
        }

        private bool TryFindManifestationCellNearTarget(Pawn pawn, Pawn target, out IntVec3 bestCell)
        {
            bestCell = IntVec3.Invalid;
            Map map = pawn.MapHeld;
            if (map == null)
            {
                return false;
            }

            Vector3 targetVector = new Vector3(target.PositionHeld.x - pawn.PositionHeld.x, 0f, target.PositionHeld.z - pawn.PositionHeld.z);
            Vector3 targetDir = targetVector.sqrMagnitude > 0.001f ? targetVector.normalized : Vector3.forward;
            float bestScore = float.MinValue;

            foreach (IntVec3 cell in GenRadial.RadialCellsAround(target.PositionHeld, 6.9f, useCenter: false))
            {
                if (!cell.InBounds(map) || !cell.Standable(map))
                {
                    continue;
                }

                float distanceToTarget = cell.DistanceTo(target.PositionHeld);
                if (distanceToTarget < 2.2f || distanceToTarget > 6.7f)
                {
                    continue;
                }

                if (cell.DistanceTo(pawn.PositionHeld) < 4.8f)
                {
                    continue;
                }

                if (AbyssalThreatPawnUtility.CellHasOtherPawn(cell, map, pawn))
                {
                    continue;
                }

                Vector3 cellVector = new Vector3(cell.x - target.PositionHeld.x, 0f, cell.z - target.PositionHeld.z);
                Vector3 cellDir = cellVector.sqrMagnitude > 0.001f ? cellVector.normalized : Vector3.zero;
                float sideScore = 1f - Mathf.Abs(Vector3.Dot(cellDir, targetDir));
                float flankScore = Mathf.Max(0f, Vector3.Dot(cellDir, -targetDir));
                float coverScore = CountAdjacentCover(cell, map) * 0.85f;
                float losBreakScore = GenSight.LineOfSight(cell, pawn.PositionHeld, map) ? 0f : 0.65f;
                float distanceScore = Mathf.Min(3.5f, cell.DistanceTo(pawn.PositionHeld) * 0.10f);
                float score = sideScore * 2.2f + flankScore * 1.8f + coverScore + losBreakScore + distanceScore + Rand.Value * 0.25f;

                if (score > bestScore)
                {
                    bestScore = score;
                    bestCell = cell;
                }
            }

            return bestCell.IsValid;
        }

        private static int CountAdjacentCover(IntVec3 root, Map map)
        {
            int score = 0;
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dz = -1; dz <= 1; dz++)
                {
                    if (dx == 0 && dz == 0)
                    {
                        continue;
                    }

                    IntVec3 cell = root + new IntVec3(dx, 0, dz);
                    if (!cell.InBounds(map))
                    {
                        continue;
                    }

                    if (cell.GetEdifice(map) != null)
                    {
                        score++;
                    }
                }
            }

            return score;
        }

        private void EmitNullPulse(Pawn pawn, IntVec3 center)
        {
            Map map = pawn.MapHeld;
            if (map == null || !center.IsValid)
            {
                return;
            }

            FleckMaker.Static(center, map, FleckDefOf.ExplosionFlash, Props.pulseVisualScale);
            FleckMaker.ThrowLightningGlow(center.ToVector3Shifted(), map, Props.pulseVisualScale * 0.9f);
            if (!Props.pulseSoundDefName.NullOrEmpty())
            {
                ABY_SoundUtility.PlayAt(Props.pulseSoundDefName, center, map);
            }

            HediffDef exposureDef = DefDatabase<HediffDef>.GetNamedSilentFail(NullExposureHediffDefName);
            if (exposureDef == null)
            {
                return;
            }

            HashSet<Pawn> affected = new HashSet<Pawn>();
            foreach (IntVec3 cell in GenRadial.RadialCellsAround(center, Props.pulseRadius, useCenter: true))
            {
                if (!cell.InBounds(map))
                {
                    continue;
                }

                List<Thing> things = cell.GetThingList(map);
                for (int i = 0; i < things.Count; i++)
                {
                    if (!(things[i] is Pawn victim) || victim == pawn || victim.Dead || victim.Downed || !pawn.HostileTo(victim) || !affected.Add(victim))
                    {
                        continue;
                    }

                    ApplyExposure(victim, exposureDef, Props.pulseSeverity, 320);
                }
            }
        }

        private static void ApplyExposure(Pawn victim, HediffDef exposureDef, float severityGain, int disappearTicks)
        {
            if (victim?.health == null || exposureDef == null)
            {
                return;
            }

            Hediff hediff = victim.health.hediffSet.GetFirstHediffOfDef(exposureDef);
            if (hediff == null)
            {
                hediff = HediffMaker.MakeHediff(exposureDef, victim);
                victim.health.AddHediff(hediff);
            }

            hediff.Severity = Mathf.Clamp(hediff.Severity + severityGain, 0.01f, 0.99f);
            HediffComp_Disappears disappears = hediff.TryGetComp<HediffComp_Disappears>();
            if (disappears != null)
            {
                disappears.ticksToDisappear = disappearTicks;
            }

            victim.health.hediffSet.DirtyCache();
        }
    }
}
