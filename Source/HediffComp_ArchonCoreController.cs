using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace AbyssalProtocol
{
    public class HediffCompProperties_ArchonCoreController : HediffCompProperties
    {
        public float phase2HealthPct = 0.70f;
        public float phase3HealthPct = 0.35f;

        public float phase1AuraRadius = 6f;
        public float phase2AuraRadius = 7f;
        public float phase3AuraRadius = 8f;

        public float phase1HeatstrokeSeverity = 0.0035f;
        public float phase2HeatstrokeSeverity = 0.0060f;
        public float phase3HeatstrokeSeverity = 0.0090f;

        public int auraIntervalTicks = 60;

        public int dashPhase = 3;
        public int dashCooldownTicks = 780;
        public int dashSearchIntervalTicks = 30;
        public float dashMinRange = 7f;
        public float dashMaxRange = 24f;
        public float dashLandingRadius = 1.9f;

        public int bloodStabilizeIntervalTicks = 30;
        public float bloodLossReductionPerPulse = 0.025f;
        public float heatstrokeReductionPerPulse = 0.020f;

        public int downedRecoveryIntervalTicks = 15;
        public float emergencyBloodLossClamp = 0.12f;
        public float emergencyHeatstrokeClamp = 0.10f;
        public float emergencyHealInjurySeverity = 0.35f;

        public float dashInfernalRadius = 1.7f;
        public float dashInfernalFireChance = 0.32f;
        public int dashInfernalAshCountDeparture = 4;
        public int dashInfernalAshCountArrival = 6;

        public int dashTrailSteps = 5;
        public float dashEntryScale = 1.9f;
        public float dashExitScale = 2.4f;
        public float dashTrailScale = 1.3f;

        public int combatReengageIntervalTicks = 90;

        public HediffCompProperties_ArchonCoreController()
        {
            compClass = typeof(HediffComp_ArchonCoreController);
        }
    }

    public class HediffComp_ArchonCoreController : HediffComp
    {
        private int currentPhase = 1;
        private int lastDashTick = -999999;
        private int lastReengageTick = -999999;
        private bool deathVfxTriggered;

        public HediffCompProperties_ArchonCoreController Props =>
            (HediffCompProperties_ArchonCoreController)props;

        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_Values.Look(ref currentPhase, "currentPhase", 1);
            Scribe_Values.Look(ref lastDashTick, "lastDashTick", -999999);
            Scribe_Values.Look(ref lastReengageTick, "lastReengageTick", -999999);
            Scribe_Values.Look(ref deathVfxTriggered, "deathVfxTriggered", false);
        }

        public override void Notify_PawnDied(DamageInfo? dinfo, Hediff culprit = null)
        {
            base.Notify_PawnDied(dinfo, culprit);
            TryTriggerDeathVFX();
        }

        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);

            Pawn pawn = Pawn;
            if (pawn == null)
                return;

            if (pawn.Dead)
                return;

            if (!pawn.Spawned || pawn.MapHeld == null)
                return;

            UpdatePhase();

            if (pawn.IsHashIntervalTick(Props.auraIntervalTicks))
            {
                ApplyHeatAura();
            }

            if (pawn.IsHashIntervalTick(Props.bloodStabilizeIntervalTicks))
            {
                StabilizeCriticalHediffs();
            }

            if (pawn.Downed)
            {
                if (pawn.IsHashIntervalTick(Props.downedRecoveryIntervalTicks))
                {
                    RecoverFromDowned();
                }

                return;
            }

            if (pawn.IsHashIntervalTick(Props.combatReengageIntervalTicks) && NeedsCombatReengage(pawn))
            {
                TryForceReengageCombat(pawn);
            }

            if (!CanUseDash(pawn))
                return;

            if (currentPhase >= Props.dashPhase && pawn.IsHashIntervalTick(Props.dashSearchIntervalTicks))
            {
                TryDash();
            }
        }

        private bool CanUseDash(Pawn pawn)
        {
            if (pawn == null || pawn.Dead || !pawn.Spawned || pawn.MapHeld == null)
                return false;

            if (pawn.Downed)
                return false;

            if (!pawn.Awake())
                return false;

            if (pawn.health == null || pawn.health.capacities == null)
                return false;

            if (!pawn.health.capacities.CapableOf(PawnCapacityDefOf.Consciousness))
                return false;

            if (!pawn.health.capacities.CapableOf(PawnCapacityDefOf.Moving))
                return false;

            return true;
        }

        private void UpdatePhase()
        {
            Pawn pawn = Pawn;
            if (pawn == null)
                return;

            float hpPct = pawn.health.summaryHealth.SummaryHealthPercent;

            int newPhase;
            if (hpPct <= Props.phase3HealthPct)
            {
                newPhase = 3;
            }
            else if (hpPct <= Props.phase2HealthPct)
            {
                newPhase = 2;
            }
            else
            {
                newPhase = 1;
            }

            currentPhase = newPhase;

            switch (currentPhase)
            {
                case 1:
                    parent.Severity = 0.10f;
                    break;
                case 2:
                    parent.Severity = 1.10f;
                    break;
                default:
                    parent.Severity = 2.10f;
                    break;
            }
        }

        private void ApplyHeatAura()
        {
            Pawn source = Pawn;
            if (source == null || source.MapHeld == null)
                return;

            float radius;
            float severityPerPulse;

            switch (currentPhase)
            {
                case 1:
                    radius = Props.phase1AuraRadius;
                    severityPerPulse = Props.phase1HeatstrokeSeverity;
                    break;
                case 2:
                    radius = Props.phase2AuraRadius;
                    severityPerPulse = Props.phase2HeatstrokeSeverity;
                    break;
                default:
                    radius = Props.phase3AuraRadius;
                    severityPerPulse = Props.phase3HeatstrokeSeverity;
                    break;
            }

            Map map = source.MapHeld;

            foreach (IntVec3 cell in GenRadial.RadialCellsAround(source.Position, radius, true))
            {
                if (!cell.InBounds(map))
                    continue;

                foreach (Thing thing in cell.GetThingList(map))
                {
                    Pawn target = thing as Pawn;
                    if (!IsValidAuraTarget(source, target))
                        continue;

                    ApplyHeatstroke(target, severityPerPulse);
                }
            }
        }

        private void StabilizeCriticalHediffs()
        {
            Pawn pawn = Pawn;
            if (pawn == null || pawn.health == null)
                return;

            ReduceHediffSeverity(pawn, HediffDefOf.BloodLoss, Props.bloodLossReductionPerPulse);
            ReduceHediffSeverity(pawn, HediffDefOf.Heatstroke, Props.heatstrokeReductionPerPulse);
        }

        private void RecoverFromDowned()
        {
            Pawn pawn = Pawn;
            if (pawn == null || pawn.health == null)
                return;

            StabilizeCriticalHediffs();

            ClampHediffSeverity(pawn, HediffDefOf.BloodLoss, Props.emergencyBloodLossClamp);
            ClampHediffSeverity(pawn, HediffDefOf.Heatstroke, Props.emergencyHeatstrokeClamp);
            HealWorstInjury(pawn, Props.emergencyHealInjurySeverity);

            pawn.health.hediffSet.DirtyCache();
            pawn.health.CheckForStateChange(null, null);

            if (!pawn.Downed)
            {
                TryForceReengageCombat(pawn, true);
            }
        }

        private static void ReduceHediffSeverity(Pawn pawn, HediffDef def, float amount)
        {
            if (pawn == null || pawn.health == null || def == null || amount <= 0f)
                return;

            Hediff hediff = pawn.health.hediffSet.GetFirstHediffOfDef(def);
            if (hediff == null)
                return;

            hediff.Severity = Mathf.Max(0f, hediff.Severity - amount);
        }

        private static void ClampHediffSeverity(Pawn pawn, HediffDef def, float maxSeverity)
        {
            if (pawn == null || pawn.health == null || def == null)
                return;

            Hediff hediff = pawn.health.hediffSet.GetFirstHediffOfDef(def);
            if (hediff == null)
                return;

            hediff.Severity = Mathf.Min(hediff.Severity, maxSeverity);
        }

        private static void HealWorstInjury(Pawn pawn, float amount)
        {
            if (pawn == null || pawn.health == null || amount <= 0f)
                return;

            Hediff_Injury worstInjury = null;
            float worstSeverity = 0f;

            foreach (Hediff hediff in pawn.health.hediffSet.hediffs)
            {
                Hediff_Injury injury = hediff as Hediff_Injury;
                if (injury == null || injury.IsPermanent())
                    continue;

                if (injury.Severity > worstSeverity)
                {
                    worstSeverity = injury.Severity;
                    worstInjury = injury;
                }
            }

            if (worstInjury != null)
            {
                worstInjury.Heal(amount);
            }
        }

        private void TryDash()
        {
            Pawn source = Pawn;
            if (!CanUseDash(source))
                return;

            int ticksGame = Find.TickManager.TicksGame;
            if (ticksGame - lastDashTick < Props.dashCooldownTicks)
                return;

            Pawn target = FindDashTarget(source);
            if (target == null)
                return;

            if (!TryFindDashCellNearTarget(source, target, out IntVec3 dashCell))
                return;

            lastDashTick = ticksGame;
            DoDash(source, target, dashCell);
        }

        private Pawn FindDashTarget(Pawn source)
        {
            Pawn bestTarget = null;
            float bestScore = float.MinValue;
            Map map = source.MapHeld;

            foreach (Pawn candidate in map.mapPawns.AllPawnsSpawned)
            {
                if (!IsValidDashTarget(source, candidate))
                    continue;

                float distance = source.Position.DistanceTo(candidate.Position);
                if (distance < Props.dashMinRange || distance > Props.dashMaxRange)
                    continue;

                float score = distance;

                if (candidate.equipment?.Primary != null && candidate.equipment.Primary.def.IsRangedWeapon)
                {
                    score += 12f;
                }

                if (candidate.health != null && candidate.health.summaryHealth.SummaryHealthPercent < 0.50f)
                {
                    score += 2f;
                }

                if (score > bestScore)
                {
                    bestScore = score;
                    bestTarget = candidate;
                }
            }

            return bestTarget;
        }

        private bool TryFindDashCellNearTarget(Pawn source, Pawn target, out IntVec3 dashCell)
        {
            Map map = source.MapHeld;
            IntVec3 bestCell = IntVec3.Invalid;
            float bestScore = float.MinValue;

            foreach (IntVec3 cell in GenRadial.RadialCellsAround(target.Position, Props.dashLandingRadius, true))
            {
                if (!cell.InBounds(map))
                    continue;

                if (!cell.Standable(map))
                    continue;

                Pawn occupant = cell.GetFirstPawn(map);
                if (occupant != null && occupant != source)
                    continue;

                float distFromSource = source.Position.DistanceTo(cell);
                if (distFromSource < 4f)
                    continue;

                float distToTarget = cell.DistanceTo(target.Position);
                float score = -distToTarget;

                if (score > bestScore)
                {
                    bestScore = score;
                    bestCell = cell;
                }
            }

            dashCell = bestCell;
            return bestCell.IsValid;
        }

        private void DoDash(Pawn source, Pawn target, IntVec3 dashCell)
        {
            Map map = source.MapHeld;
            if (map == null || !dashCell.IsValid)
                return;

            IntVec3 origin = source.Position;

            if (source.pather != null)
            {
                source.pather.StopDead();
            }

            if (source.jobs != null && source.CurJob != null)
            {
                source.jobs.EndCurrentJob(JobCondition.InterruptForced);
            }

            SpawnDashDepartureEffect(map, origin);
            SpawnDashTrail(map, origin, dashCell);

            if (source.Spawned)
            {
                source.DeSpawn();
            }

            GenSpawn.Spawn(source, dashCell, map, source.Rotation);

            if (source.pather != null)
            {
                source.pather.StopDead();
            }

            SpawnDashArrivalEffect(map, dashCell);

            if (target != null && target.Spawned && !target.Dead && target.MapHeld == map)
            {
                ForceMeleeAttackImmediate(source, target);
            }
            else
            {
                TryForceReengageCombat(source, true);
            }
        }

        private void SpawnDashDepartureEffect(Map map, IntVec3 center)
        {
            if (map == null || !center.IsValid)
                return;

            if (Props.dashInfernalAshCountDeparture > 0)
            {
                FilthMaker.TryMakeFilth(center, map, ThingDefOf.Filth_Ash, Props.dashInfernalAshCountDeparture);
            }

            TrySpawnDashMote(map, center.ToVector3Shifted(), "ABY_Mote_ArchonDashEntry", Props.dashEntryScale);

            foreach (IntVec3 cell in GenRadial.RadialCellsAround(center, Props.dashInfernalRadius, true))
            {
                if (!cell.InBounds(map) || !cell.Standable(map))
                    continue;

                if (Rand.Chance(Props.dashInfernalFireChance * 0.55f))
                {
                    FireUtility.TryStartFireIn(cell, map, 0.22f, null, null);
                }
            }
        }

        private void SpawnDashArrivalEffect(Map map, IntVec3 center)
        {
            if (map == null || !center.IsValid)
                return;

            if (Props.dashInfernalAshCountArrival > 0)
            {
                FilthMaker.TryMakeFilth(center, map, ThingDefOf.Filth_Ash, Props.dashInfernalAshCountArrival);
            }

            TrySpawnDashMote(map, center.ToVector3Shifted(), "ABY_Mote_ArchonDashExit", Props.dashExitScale);
            TrySpawnDashMote(
                map,
                center.ToVector3Shifted() + new Vector3(0f, 0f, 0.15f),
                "ABY_Mote_ArchonDashEntry",
                Props.dashEntryScale * 0.7f
            );

            foreach (IntVec3 cell in GenRadial.RadialCellsAround(center, Props.dashInfernalRadius, true))
            {
                if (!cell.InBounds(map) || !cell.Standable(map))
                    continue;

                if (Rand.Chance(Props.dashInfernalFireChance))
                {
                    FireUtility.TryStartFireIn(cell, map, 0.28f, null, null);
                }
            }
        }

        private void SpawnDashTrail(Map map, IntVec3 from, IntVec3 to)
        {
            if (map == null || !from.IsValid || !to.IsValid || Props.dashTrailSteps <= 0)
                return;

            Vector3 start = from.ToVector3Shifted();
            Vector3 end = to.ToVector3Shifted();

            for (int i = 1; i <= Props.dashTrailSteps; i++)
            {
                float t = (float)i / (Props.dashTrailSteps + 1);
                Vector3 pos = Vector3.Lerp(start, end, t);
                float scale = Mathf.Lerp(Props.dashTrailScale, Props.dashTrailScale * 0.55f, t);
                TrySpawnDashMote(map, pos, "ABY_Mote_ArchonDashTrail", scale);
            }
        }

        private static void TrySpawnDashMote(Map map, Vector3 pos, string defName, float scale)
        {
            if (map == null || string.IsNullOrEmpty(defName))
                return;

            ThingDef moteDef = DefDatabase<ThingDef>.GetNamedSilentFail(defName);
            if (moteDef == null)
                return;

            MoteMaker.MakeStaticMote(pos, map, moteDef, scale);
        }

        private void TryTriggerDeathVFX()
        {
            if (deathVfxTriggered)
                return;

            deathVfxTriggered = true;

            Pawn pawn = Pawn;
            if (pawn == null)
                return;

            if (pawn.Corpse != null && pawn.Corpse.Spawned && pawn.Corpse.Map != null)
            {
                ArchonInfernalVFXUtility.DoDeathVFX(pawn.Corpse.Map, pawn.Corpse.Position);
                return;
            }

            if (pawn.MapHeld != null && pawn.PositionHeld.IsValid)
            {
                ArchonInfernalVFXUtility.DoDeathVFX(pawn.MapHeld, pawn.PositionHeld);
            }
        }

        private bool NeedsCombatReengage(Pawn pawn)
        {
            if (pawn == null || pawn.Dead || pawn.Downed || pawn.jobs == null)
                return false;

            Job curJob = pawn.CurJob;
            if (curJob == null || curJob.def == null)
                return true;

            string defName = curJob.def.defName;
            if (defName == "GotoWander" || defName == "Wait_Wander" || defName == "Wait")
                return true;

            return false;
        }

        private void TryForceReengageCombat(Pawn pawn, bool forceNow = false)
        {
            if (pawn == null || pawn.Dead || pawn.Downed || pawn.MapHeld == null || pawn.jobs == null)
                return;

            int ticksGame = Find.TickManager.TicksGame;
            if (!forceNow && ticksGame - lastReengageTick < 45)
                return;

            Pawn target = FindNearestHostilePawn(pawn);
            if (target == null)
                return;

            ForceMeleeAttack(pawn, target);
            lastReengageTick = ticksGame;
        }

        private static Pawn FindNearestHostilePawn(Pawn source)
        {
            if (source == null || source.MapHeld == null)
                return null;

            Pawn bestTarget = null;
            float bestDist = float.MaxValue;

            foreach (Pawn candidate in source.MapHeld.mapPawns.AllPawnsSpawned)
            {
                if (!IsValidDashTarget(source, candidate))
                    continue;

                float dist = source.Position.DistanceToSquared(candidate.Position);
                if (dist < bestDist)
                {
                    bestDist = dist;
                    bestTarget = candidate;
                }
            }

            return bestTarget;
        }

        private static void ForceMeleeAttack(Pawn attacker, Pawn target)
        {
            if (attacker == null || target == null || attacker.jobs == null)
                return;

            Job job = JobMaker.MakeJob(JobDefOf.AttackMelee, target);
            job.expiryInterval = 300;
            job.checkOverrideOnExpire = true;
            attacker.jobs.TryTakeOrderedJob(job, JobTag.Misc);
        }

        private static void ForceMeleeAttackImmediate(Pawn attacker, Pawn target)
        {
            if (attacker == null || target == null || attacker.jobs == null)
                return;

            Job job = JobMaker.MakeJob(JobDefOf.AttackMelee, target);
            job.expiryInterval = 500;
            job.checkOverrideOnExpire = true;
            job.collideWithPawns = true;

            attacker.jobs.TryTakeOrderedJob(job, JobTag.Misc);
        }

        private static bool IsValidAuraTarget(Pawn source, Pawn target)
        {
            if (source == null || target == null)
                return false;

            if (target == source)
                return false;

            if (target.Dead || !target.Spawned)
                return false;

            if (!target.RaceProps.IsFlesh)
                return false;

            if (!target.HostileTo(source))
                return false;

            return true;
        }

        private static bool IsValidDashTarget(Pawn source, Pawn target)
        {
            if (source == null || target == null)
                return false;

            if (target == source)
                return false;

            if (target.Dead || !target.Spawned)
                return false;

            if (!target.HostileTo(source))
                return false;

            return true;
        }

        private static void ApplyHeatstroke(Pawn target, float severityAmount)
        {
            if (severityAmount <= 0f)
                return;

            Hediff heatstroke = target.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.Heatstroke);
            if (heatstroke == null)
            {
                heatstroke = HediffMaker.MakeHediff(HediffDefOf.Heatstroke, target);
                heatstroke.Severity = 0f;
                target.health.AddHediff(heatstroke);
            }

            heatstroke.Severity += severityAmount;
        }
    }
}
