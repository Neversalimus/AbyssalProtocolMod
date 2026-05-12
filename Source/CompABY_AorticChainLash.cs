using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    public class CompProperties_ABY_AorticChainLash : CompProperties
    {
        public string heartDefName = "ABY_DominionSliceHeart";
        public List<string> anchorDefNames = new List<string>
        {
            "ABY_DominionSliceAnchor_Seal",
            "ABY_DominionSliceAnchor_Choir",
            "ABY_DominionSliceAnchor_Law"
        };

        public int scanIntervalTicks = 30;
        public int cooldownTicks = 540;
        public int cooldownJitterTicks = 150;
        public float minRange = 3.2f;
        public float maxRange = 16.0f;
        public float targetFocusRadius = 28.0f;
        public float maxGuardianDistanceFromFocus = 34.0f;
        public float damageAmount = 12.0f;
        public float armorPenetration = 0.26f;
        public float snareSeverity = 0.46f;
        public string snareHediffDefName = "ABY_AorticSnare";
        public bool preferRangedTargets = true;
        public bool preferFocusAttackers = true;
        public bool defendNearestAnchorBeforeHeartExposed = true;

        public string beamMoteDefName = "ABY_Mote_AorticChainLashBeam";
        public string baseTetherTexturePath = "Things/VFX/AorticChainHarrower/ABY_AorticChainLash_BaseTether";
        public string chainTetherTexturePath = "Things/VFX/AorticChainHarrower/ABY_AorticChainLash_ChainTether";
        public string launchTexturePath = "Things/VFX/AorticChainHarrower/ABY_AorticChainLash_Launch";
        public string impactTexturePath = "Things/VFX/AorticChainHarrower/ABY_AorticChainLash_Impact";
        public string snapTexturePath = "Things/VFX/AorticChainHarrower/ABY_AorticChainLash_Snap";
        public string residualTexturePath = "Things/VFX/AorticChainHarrower/ABY_AorticChainLash_Residual";
        public string fireSoundDefName = "ABY_SpecterLashFire";
        public string impactSoundDefName = "ABY_SigilChargePulse";

        public int beamTicks = 11;
        public int snapTicks = 15;
        public int burstTicks = 10;
        public float baseBeamWidth = 0.20f;
        public float chainBeamWidth = 0.38f;
        public float snapBeamWidth = 0.30f;
        public float launchLength = 2.8f;
        public float launchWidth = 1.05f;
        public float impactLength = 2.25f;
        public float impactWidth = 1.18f;
        public float residualLength = 2.10f;
        public float residualWidth = 0.74f;

        public CompProperties_ABY_AorticChainLash()
        {
            compClass = typeof(CompABY_AorticChainLash);
        }
    }

    public class CompABY_AorticChainLash : ThingComp
    {
        private int nextAttackTick;
        private Thing cachedFocus;

        public CompProperties_ABY_AorticChainLash Props => (CompProperties_ABY_AorticChainLash)props;

        private Pawn PawnParent => parent as Pawn;

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref nextAttackTick, "nextAorticChainLashTick", 0);
            Scribe_References.Look(ref cachedFocus, "cachedAorticChainLashFocus");
        }

        public override void CompTick()
        {
            base.CompTick();

            try
            {
                TickLashSafe();
            }
            catch (System.Exception ex)
            {
                ABY_LogThrottleUtility.Warning(
                    "aortic-chain-lash-tick-failed",
                    "[Abyssal Protocol] Aortic Chain Harrower lash tick failed and was skipped: " + ex.GetType().Name + ": " + ex.Message,
                    1200);
            }
        }

        private void TickLashSafe()
        {
            Pawn pawn = PawnParent;
            if (!ShouldOperate(pawn))
            {
                return;
            }

            int interval = Mathf.Max(12, Props.scanIntervalTicks);
            if (!parent.IsHashIntervalTick(interval))
            {
                return;
            }

            int now = CurrentTicks;
            if (now < nextAttackTick)
            {
                return;
            }

            Thing focus = ResolveDefendFocus(pawn);
            if (!IsValidFocus(pawn, focus))
            {
                return;
            }

            if (pawn.PositionHeld.DistanceTo(focus.PositionHeld) > Mathf.Max(Props.maxRange, Props.maxGuardianDistanceFromFocus))
            {
                return;
            }

            Pawn target = FindBestLashTarget(pawn, focus);
            if (target == null)
            {
                return;
            }

            FireLash(pawn, target, focus);
            ScheduleNextAttack(now);
        }

        private void FireLash(Pawn pawn, Pawn target, Thing focus)
        {
            if (pawn == null || target == null || pawn.MapHeld == null)
            {
                return;
            }

            Map map = pawn.MapHeld;
            IntVec3 sourceCell = pawn.PositionHeld;
            IntVec3 targetCell = target.PositionHeld;
            Vector3 source = sourceCell.ToVector3Shifted();
            Vector3 targetPos = targetCell.ToVector3Shifted();
            source.y = AltitudeLayer.MoteOverhead.AltitudeFor();
            targetPos.y = AltitudeLayer.MoteOverhead.AltitudeFor();

            Vector3 delta = targetPos - source;
            delta.y = 0f;
            float distance = delta.magnitude;
            if (distance <= 0.05f)
            {
                return;
            }

            Vector3 direction = delta / distance;

            ABY_SoundUtility.PlayAt(Props.fireSoundDefName, sourceCell, map);
            ABY_SoundUtility.PlayAt(Props.impactSoundDefName, targetCell, map);

            SpawnBeam(map, source, targetPos, Props.baseBeamWidth, Props.beamTicks, Props.baseTetherTexturePath, true);
            SpawnBeam(map, source, targetPos, Props.chainBeamWidth, Props.beamTicks, Props.chainTetherTexturePath, false);
            SpawnBeam(map, source, targetPos, Props.snapBeamWidth, Props.snapTicks, Props.snapTexturePath, true);

            Vector3 launchEnd = source + direction * Mathf.Min(Props.launchLength, distance * 0.45f);
            SpawnBeam(map, source, launchEnd, Props.launchWidth, Props.burstTicks, Props.launchTexturePath, false);

            Vector3 impactStart = targetPos - direction * (Props.impactLength * 0.50f);
            Vector3 impactEnd = targetPos + direction * (Props.impactLength * 0.50f);
            SpawnBeam(map, impactStart, impactEnd, Props.impactWidth, Props.burstTicks, Props.impactTexturePath, false);

            Vector3 residualStart = targetPos - direction * (Props.residualLength * 0.62f);
            Vector3 residualEnd = targetPos + direction * (Props.residualLength * 0.38f);
            SpawnBeam(map, residualStart, residualEnd, Props.residualWidth, Props.burstTicks + 3, Props.residualTexturePath, true);

            ApplyLashDamageAndSnare(pawn, target);
            pawn.rotationTracker?.FaceCell(targetCell);
        }

        private void ApplyLashDamageAndSnare(Pawn pawn, Pawn target)
        {
            if (target == null || target.Destroyed || target.Dead)
            {
                return;
            }

            if (Props.damageAmount > 0f)
            {
                DamageInfo damageInfo = new DamageInfo(
                    DamageDefOf.Cut,
                    Props.damageAmount,
                    Props.armorPenetration,
                    -1f,
                    pawn);

                target.TakeDamage(damageInfo);
            }

            AbyssalThreatPawnUtility.ApplyOrRefreshHediff(target, Props.snareHediffDefName, Props.snareSeverity);
        }

        private Pawn FindBestLashTarget(Pawn pawn, Thing focus)
        {
            Map map = pawn?.MapHeld;
            if (map?.mapPawns?.AllPawnsSpawned == null || focus == null)
            {
                return null;
            }

            IReadOnlyList<Pawn> pawns = map.mapPawns.AllPawnsSpawned;
            Pawn best = null;
            float bestScore = float.MinValue;
            float minRange = Mathf.Max(0f, Props.minRange);
            float maxRange = Mathf.Max(minRange + 0.1f, Props.maxRange);
            float focusRadius = Mathf.Max(1f, Props.targetFocusRadius);

            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn candidate = pawns[i];
                if (!AbyssalThreatPawnUtility.IsValidHostileTarget(pawn, candidate))
                {
                    continue;
                }

                float pawnDistance = pawn.PositionHeld.DistanceTo(candidate.PositionHeld);
                if (pawnDistance < minRange || pawnDistance > maxRange)
                {
                    continue;
                }

                if (!GenSight.LineOfSight(pawn.PositionHeld, candidate.PositionHeld, map, true))
                {
                    continue;
                }

                float focusDistance = focus.PositionHeld.DistanceTo(candidate.PositionHeld);
                if (focusDistance > focusRadius)
                {
                    continue;
                }

                float score = (maxRange - pawnDistance) * 0.35f;
                score += (focusRadius - focusDistance) * 0.42f;

                if (Props.preferRangedTargets && AbyssalThreatPawnUtility.HasRangedWeapon(candidate))
                {
                    score += 5.0f;
                }

                if (Props.preferFocusAttackers && IsTargetingFocus(candidate, focus))
                {
                    score += 8.5f;
                }

                if (candidate.health != null)
                {
                    score += (1f - candidate.health.summaryHealth.SummaryHealthPercent) * 1.5f;
                }

                score += Rand.Value * 0.35f;
                if (score > bestScore)
                {
                    bestScore = score;
                    best = candidate;
                }
            }

            return best;
        }

        private Thing ResolveDefendFocus(Pawn pawn)
        {
            Map map = pawn?.MapHeld;
            if (map == null)
            {
                return null;
            }

            MapComponent_DominionSliceEncounter encounter = map.GetComponent<MapComponent_DominionSliceEncounter>();
            if (Props.defendNearestAnchorBeforeHeartExposed && encounter != null && encounter.IsActiveEncounter && !encounter.IsHeartExposed)
            {
                Thing anchor = ResolveNearestLiveAnchor(pawn, map);
                if (IsValidFocus(pawn, anchor))
                {
                    cachedFocus = anchor;
                    return anchor;
                }
            }

            Thing heart = encounter != null ? encounter.HeartBuilding : null;
            if (IsValidFocus(pawn, heart))
            {
                cachedFocus = heart;
                return heart;
            }

            if (IsValidFocus(pawn, cachedFocus))
            {
                return cachedFocus;
            }

            Thing nearestHeart = ResolveNearestHeart(pawn, map);
            if (IsValidFocus(pawn, nearestHeart))
            {
                cachedFocus = nearestHeart;
                return nearestHeart;
            }

            return null;
        }

        private Thing ResolveNearestLiveAnchor(Pawn pawn, Map map)
        {
            if (pawn == null || map?.listerThings == null || Props.anchorDefNames == null)
            {
                return null;
            }

            Thing best = null;
            float bestDistance = float.MaxValue;
            for (int i = 0; i < Props.anchorDefNames.Count; i++)
            {
                string defName = Props.anchorDefNames[i];
                if (defName.NullOrEmpty())
                {
                    continue;
                }

                ThingDef def = DefDatabase<ThingDef>.GetNamedSilentFail(defName);
                if (def == null)
                {
                    continue;
                }

                List<Thing> candidates = map.listerThings.ThingsOfDef(def);
                if (candidates == null)
                {
                    continue;
                }

                for (int j = 0; j < candidates.Count; j++)
                {
                    Thing candidate = candidates[j];
                    if (!IsValidFocus(pawn, candidate))
                    {
                        continue;
                    }

                    float distance = pawn.PositionHeld.DistanceTo(candidate.PositionHeld);
                    if (distance < bestDistance)
                    {
                        bestDistance = distance;
                        best = candidate;
                    }
                }
            }

            return best;
        }

        private Thing ResolveNearestHeart(Pawn pawn, Map map)
        {
            if (pawn == null || map?.listerThings == null || Props.heartDefName.NullOrEmpty())
            {
                return null;
            }

            ThingDef heartDef = DefDatabase<ThingDef>.GetNamedSilentFail(Props.heartDefName);
            if (heartDef == null)
            {
                return null;
            }

            List<Thing> hearts = map.listerThings.ThingsOfDef(heartDef);
            if (hearts == null)
            {
                return null;
            }

            Thing best = null;
            float bestDistance = float.MaxValue;
            for (int i = 0; i < hearts.Count; i++)
            {
                Thing candidate = hearts[i];
                if (!IsValidFocus(pawn, candidate))
                {
                    continue;
                }

                float distance = pawn.PositionHeld.DistanceTo(candidate.PositionHeld);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = candidate;
                }
            }

            return best;
        }

        private bool IsValidFocus(Pawn pawn, Thing focus)
        {
            return pawn != null
                && focus != null
                && !focus.Destroyed
                && focus.Spawned
                && focus.Map == pawn.MapHeld
                && focus.PositionHeld.IsValid;
        }

        private bool IsTargetingFocus(Pawn candidate, Thing focus)
        {
            if (candidate?.CurJob == null || focus == null)
            {
                return false;
            }

            LocalTargetInfo targetA = candidate.CurJob.targetA;
            LocalTargetInfo targetB = candidate.CurJob.targetB;
            LocalTargetInfo targetC = candidate.CurJob.targetC;

            return TargetMatchesFocus(targetA, focus)
                || TargetMatchesFocus(targetB, focus)
                || TargetMatchesFocus(targetC, focus);
        }

        private bool TargetMatchesFocus(LocalTargetInfo target, Thing focus)
        {
            if (!target.IsValid || focus == null)
            {
                return false;
            }

            if (target.HasThing)
            {
                return target.Thing == focus;
            }

            return target.Cell.IsValid && target.Cell.DistanceTo(focus.PositionHeld) <= 2.8f;
        }

        private void SpawnBeam(Map map, Vector3 start, Vector3 end, float width, int ticks, string texturePath, bool pulse)
        {
            if (map == null || ticks <= 0 || texturePath.NullOrEmpty())
            {
                return;
            }

            ThingDef beamDef = DefDatabase<ThingDef>.GetNamedSilentFail(Props.beamMoteDefName);
            if (beamDef == null)
            {
                return;
            }

            Mote_CrownspikeRailBeam beam = ThingMaker.MakeThing(beamDef) as Mote_CrownspikeRailBeam;
            if (beam == null)
            {
                return;
            }

            start.y = AltitudeLayer.MoteOverhead.AltitudeFor();
            end.y = AltitudeLayer.MoteOverhead.AltitudeFor();

            beam.start = start;
            beam.end = end;
            beam.width = Mathf.Max(0.04f, width);
            beam.ticksLeft = Mathf.Max(1, ticks);
            beam.startingTicks = Mathf.Max(1, ticks);
            beam.texturePath = texturePath;
            beam.additivePulse = pulse;

            IntVec3 spawnCell = ((start + end) * 0.5f).ToIntVec3();
            if (!spawnCell.InBounds(map))
            {
                spawnCell = start.ToIntVec3();
            }
            if (!spawnCell.InBounds(map))
            {
                spawnCell = end.ToIntVec3();
            }
            if (!spawnCell.InBounds(map))
            {
                return;
            }

            GenSpawn.Spawn(beam, spawnCell, map);
        }

        private void ScheduleNextAttack(int now)
        {
            int cooldown = Mathf.Max(60, Props.cooldownTicks);
            int jitter = Mathf.Max(0, Props.cooldownJitterTicks);
            nextAttackTick = now + cooldown + (jitter > 0 ? Rand.Range(0, jitter + 1) : 0);
        }

        private bool ShouldOperate(Pawn pawn)
        {
            return pawn != null
                && pawn.Spawned
                && !pawn.Destroyed
                && !pawn.Dead
                && !pawn.Downed
                && pawn.MapHeld != null
                && pawn.Faction != null;
        }

        private int CurrentTicks => Find.TickManager != null ? Find.TickManager.TicksGame : 0;
    }
}
