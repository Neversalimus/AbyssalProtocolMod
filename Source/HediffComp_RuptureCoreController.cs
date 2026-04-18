using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace AbyssalProtocol
{
    public class HediffCompProperties_RuptureCoreController : HediffCompProperties
    {
        public float phase2HealthPct = 0.75f;
        public float phase3HealthPct = 0.45f;
        public float finalFrenzyHealthPct = 0.20f;
        public int spawnShieldTicks = 360;

        public float phase1AuraRadius = 7f;
        public float phase2AuraRadius = 8.6f;
        public float phase3AuraRadius = 10f;
        public float phase4AuraRadius = 11f;

        public float phase1HeatstrokeSeverity = 0.0045f;
        public float phase2HeatstrokeSeverity = 0.0075f;
        public float phase3HeatstrokeSeverity = 0.0105f;
        public float phase4HeatstrokeSeverity = 0.0135f;
        public int auraIntervalTicks = 60;

        public int dashCooldownTicks = 420;
        public int dashSearchIntervalTicks = 20;
        public float dashMinRange = 6f;
        public float dashMaxRange = 32f;
        public float dashLandingRadius = 2.6f;

        public float dashInfernalRadius = 2.4f;
        public float dashInfernalFireChance = 0.48f;
        public int dashInfernalAshCountDeparture = 5;
        public int dashInfernalAshCountArrival = 8;
        public int dashTrailSteps = 8;
        public float dashEntryScale = 2.8f;
        public float dashExitScale = 3.6f;
        public float dashTrailScale = 1.8f;

        public int portalWarmupTicks = 48;
        public int portalImpSpawnIntervalTicks = 14;
        public int portalLingerTicks = 240;
        public int phase2TransitionImps = 4;
        public int phase3TransitionImps = 6;
        public int rebirthTransitionImps = 8;
        public int recurringPortalIntervalTicks = 900;
        public int recurringPortalImps = 4;

        public int rebirthRecoveryIntervalTicks = 15;
        public float rebirthBloodLossClamp = 0.08f;
        public float rebirthHeatstrokeClamp = 0.06f;
        public float rebirthHealInjurySeverity = 0.85f;

        public HediffCompProperties_RuptureCoreController()
        {
            compClass = typeof(HediffComp_RuptureCoreController);
        }
    }

    public class HediffComp_RuptureCoreController : HediffComp
    {
        private int currentPhase = 1;
        private int spawnTick = -1;
        private int lastDashTick = -999999;
        private int lastPortalTick = -999999;
        private int lastReengageTick = -999999;
        private bool rebirthUsed;
        private bool deathVfxTriggered;
        private bool finalFrenzyTriggered;

        public HediffCompProperties_RuptureCoreController Props => (HediffCompProperties_RuptureCoreController)props;

        public int CurrentPhase => currentPhase < 1 ? 1 : currentPhase;
        public float Phase2HealthPct => Props.phase2HealthPct;
        public float Phase3HealthPct => Props.phase3HealthPct;
        public float FinalFrenzyHealthPct => Props.finalFrenzyHealthPct;
        public bool RebirthUsed => rebirthUsed;
        public bool FinalFrenzyTriggered => finalFrenzyTriggered;
        public bool SpawnShieldActive
        {
            get
            {
                if (spawnTick < 0 || Find.TickManager == null)
                {
                    return false;
                }

                return Find.TickManager.TicksGame - spawnTick < Props.spawnShieldTicks;
            }
        }

        private Pawn ControlledPawn => parent?.pawn;

        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_Values.Look(ref currentPhase, "currentPhase", 1);
            Scribe_Values.Look(ref spawnTick, "spawnTick", -1);
            Scribe_Values.Look(ref lastDashTick, "lastDashTick", -999999);
            Scribe_Values.Look(ref lastPortalTick, "lastPortalTick", -999999);
            Scribe_Values.Look(ref lastReengageTick, "lastReengageTick", -999999);
            Scribe_Values.Look(ref rebirthUsed, "rebirthUsed", false);
            Scribe_Values.Look(ref deathVfxTriggered, "deathVfxTriggered", false);
            Scribe_Values.Look(ref finalFrenzyTriggered, "finalFrenzyTriggered", false);
        }

        public override void Notify_PawnDied(DamageInfo? dinfo, Hediff culprit = null)
        {
            base.Notify_PawnDied(dinfo, culprit);
            TryTriggerDeathVFX();
        }

        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);

            Pawn pawn = ControlledPawn;
            if (pawn == null || pawn.Dead || !pawn.Spawned || pawn.MapHeld == null)
            {
                return;
            }

            if (spawnTick < 0)
            {
                spawnTick = Find.TickManager.TicksGame;
            }

            UpdatePhase();

            if (pawn.IsHashIntervalTick(Props.auraIntervalTicks))
            {
                ApplyHeatAura();
            }

            if (pawn.Downed)
            {
                if (pawn.IsHashIntervalTick(Props.rebirthRecoveryIntervalTicks))
                {
                    TryExecuteRebirth();
                }

                return;
            }

            if (ShouldTriggerRecurringPortals())
            {
                TriggerPortalVolley(Props.recurringPortalImps);
                lastPortalTick = Find.TickManager.TicksGame;
            }

            if (pawn.IsHashIntervalTick(90) && NeedsCombatReengage(pawn))
            {
                TryForceReengageCombat(pawn);
            }

            if (CanUseDash(pawn) && pawn.IsHashIntervalTick(Props.dashSearchIntervalTicks))
            {
                TryDash();
            }
        }

        private void UpdatePhase()
        {
            Pawn pawn = ControlledPawn;
            if (pawn == null)
            {
                return;
            }

            int ticksSinceSpawn = Find.TickManager.TicksGame - spawnTick;
            int previousPhase = currentPhase;

            if (ticksSinceSpawn < Props.spawnShieldTicks)
            {
                currentPhase = 0;
                parent.Severity = 0.10f;
                return;
            }

            if (finalFrenzyTriggered)
            {
                currentPhase = 4;
                parent.Severity = 4.10f;
                return;
            }

            float hpPct = pawn.health.summaryHealth.SummaryHealthPercent;
            if (hpPct <= Props.finalFrenzyHealthPct)
            {
                currentPhase = 3;
                parent.Severity = 3.10f;
            }
            else if (hpPct <= Props.phase3HealthPct)
            {
                currentPhase = 3;
                parent.Severity = 3.10f;
            }
            else if (hpPct <= Props.phase2HealthPct)
            {
                currentPhase = 2;
                parent.Severity = 2.10f;
            }
            else
            {
                currentPhase = 1;
                parent.Severity = 1.10f;
            }

            if (currentPhase > previousPhase)
            {
                ABY_SoundUtility.PlayAt("ABY_RuptureVerdict", pawn.PositionHeld, pawn.MapHeld);
                if (currentPhase == 2)
                {
                    TriggerPortalVolley(Props.phase2TransitionImps);
                }
                else if (currentPhase >= 3)
                {
                    TriggerPortalVolley(Props.phase3TransitionImps);
                }
            }
        }

        private bool ShouldTriggerRecurringPortals()
        {
            Pawn pawn = ControlledPawn;
            if (pawn == null || pawn.MapHeld == null || currentPhase < 2)
            {
                return false;
            }

            int ticksGame = Find.TickManager.TicksGame;
            return ticksGame - lastPortalTick >= Props.recurringPortalIntervalTicks;
        }

        private void TriggerPortalVolley(int impCount)
        {
            Pawn pawn = ControlledPawn;
            if (pawn == null || pawn.MapHeld == null || impCount <= 0)
            {
                return;
            }

            ABY_Phase2PortalUtility.TrySpawnImpPortal(
                pawn.MapHeld,
                pawn.Faction,
                impCount,
                Props.portalWarmupTicks,
                Props.portalImpSpawnIntervalTicks,
                Props.portalLingerTicks,
                out Building_AbyssalImpPortal _);
        }

        private void TryExecuteRebirth()
        {
            Pawn pawn = ControlledPawn;
            if (pawn == null || pawn.health == null)
            {
                return;
            }

            if (!rebirthUsed)
            {
                rebirthUsed = true;
                finalFrenzyTriggered = true;
                ABY_SoundUtility.PlayAt("ABY_RuptureVerdict", pawn.PositionHeld, pawn.MapHeld);
                TriggerPortalVolley(Props.rebirthTransitionImps);
            }

            ClampHediffSeverity(pawn, HediffDefOf.BloodLoss, Props.rebirthBloodLossClamp);
            ClampHediffSeverity(pawn, HediffDefOf.Heatstroke, Props.rebirthHeatstrokeClamp);

            for (int i = 0; i < 4; i++)
            {
                HealWorstInjury(pawn, Props.rebirthHealInjurySeverity);
            }

            pawn.health.hediffSet.DirtyCache();
            pawn.health.CheckForStateChange(null, null);
            parent.Severity = 4.10f;
            currentPhase = 4;

            if (!pawn.Downed)
            {
                TryForceReengageCombat(pawn, true);
            }
        }

        private bool CanUseDash(Pawn pawn)
        {
            if (pawn == null || pawn.Dead || !pawn.Spawned || pawn.MapHeld == null)
                return false;

            if (pawn.Downed || !pawn.Awake())
                return false;

            if (pawn.health == null || pawn.health.capacities == null)
                return false;

            if (!pawn.health.capacities.CapableOf(PawnCapacityDefOf.Consciousness))
                return false;

            if (!pawn.health.capacities.CapableOf(PawnCapacityDefOf.Moving))
                return false;

            return currentPhase >= 1;
        }

        private void TryDash()
        {
            Pawn source = Pawn;
            if (!CanUseDash(source))
                return;

            int ticksGame = Find.TickManager.TicksGame;
            int cooldown = finalFrenzyTriggered ? Mathf.Max(180, Props.dashCooldownTicks / 2) : Props.dashCooldownTicks;
            if (ticksGame - lastDashTick < cooldown)
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
                    score += 18f;
                }

                if (candidate.health != null)
                {
                    score += candidate.health.summaryHealth.SummaryHealthPercent < 0.55f ? 4f : 0f;
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
                if (!cell.InBounds(map) || !cell.Standable(map))
                    continue;

                Pawn occupant = cell.GetFirstPawn(map);
                if (occupant != null && occupant != source)
                    continue;

                float distFromSource = source.Position.DistanceTo(cell);
                if (distFromSource < 4f)
                    continue;

                float score = -cell.DistanceTo(target.Position);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestCell = cell;
                }
            }

            dashCell = bestCell;
            return bestCell.IsValid;
        }

        private static void FaceCellNow(Pawn pawn, IntVec3 cell)
        {
            if (pawn == null || !cell.IsValid || pawn.rotationTracker == null)
                return;

            pawn.rotationTracker.FaceCell(cell);
        }

        private void DoDash(Pawn source, Pawn target, IntVec3 dashCell)
        {
            Map map = source.MapHeld;
            if (map == null || !dashCell.IsValid)
                return;

            IntVec3 origin = source.Position;
            IntVec3 faceCell = (target != null && target.Spawned && target.MapHeld == map)
                ? target.Position
                : dashCell + (dashCell - origin);

            FaceCellNow(source, faceCell);
            source.pather?.StopDead();

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
            FaceCellNow(source, faceCell);
            source.pather?.StopDead();

            SpawnDashArrivalEffect(map, dashCell);
            ABY_SoundUtility.PlayAt("ABY_RuptureImpact", dashCell, map);

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
            TrySpawnDashMote(map, center.ToVector3Shifted() + new Vector3(0f, 0f, 0.12f), "ABY_Mote_ArchonDashEntry", Props.dashEntryScale * 0.75f);

            foreach (IntVec3 cell in GenRadial.RadialCellsAround(center, Props.dashInfernalRadius, true))
            {
                if (!cell.InBounds(map) || !cell.Standable(map))
                    continue;

                if (Rand.Chance(Props.dashInfernalFireChance))
                {
                    FireUtility.TryStartFireIn(cell, map, 0.32f, null, null);
                }

                foreach (Thing thing in cell.GetThingList(map))
                {
                    Pawn target = thing as Pawn;
                    if (!IsValidAuraTarget(Pawn, target))
                        continue;

                    ApplyHeatstroke(target, finalFrenzyTriggered ? 0.035f : 0.025f);
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

        private void ApplyHeatAura()
        {
            Pawn source = Pawn;
            if (source == null || source.MapHeld == null)
                return;

            float radius;
            float severity;

            if (currentPhase >= 4)
            {
                radius = Props.phase4AuraRadius;
                severity = Props.phase4HeatstrokeSeverity;
            }
            else if (currentPhase >= 3)
            {
                radius = Props.phase3AuraRadius;
                severity = Props.phase3HeatstrokeSeverity;
            }
            else if (currentPhase >= 2)
            {
                radius = Props.phase2AuraRadius;
                severity = Props.phase2HeatstrokeSeverity;
            }
            else
            {
                radius = Props.phase1AuraRadius;
                severity = Props.phase1HeatstrokeSeverity;
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

                    ApplyHeatstroke(target, severity);
                }
            }
        }

        private void TryTriggerDeathVFX()
        {
            if (deathVfxTriggered)
                return;

            deathVfxTriggered = true;
            Pawn pawn = ControlledPawn;
            if (pawn == null)
                return;

            if (pawn.Corpse != null && pawn.Corpse.Spawned && pawn.Corpse.Map != null)
            {
                ABY_SoundUtility.PlayAt("ABY_RuptureDeath", pawn.Corpse.Position, pawn.Corpse.Map);
                ArchonInfernalVFXUtility.DoDeathVFX(pawn.Corpse.Map, pawn.Corpse.Position);
                return;
            }

            if (pawn.MapHeld != null && pawn.PositionHeld.IsValid)
            {
                ABY_SoundUtility.PlayAt("ABY_RuptureDeath", pawn.PositionHeld, pawn.MapHeld);
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
            return defName == "GotoWander" || defName == "Wait_Wander" || defName == "Wait";
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

            FaceCellNow(pawn, target.Position);
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

            FaceCellNow(attacker, target.Position);
            Job job = JobMaker.MakeJob(JobDefOf.AttackMelee, target);
            job.expiryInterval = 300;
            job.checkOverrideOnExpire = true;
            attacker.jobs.TryTakeOrderedJob(job, JobTag.Misc);
        }

        private static void ForceMeleeAttackImmediate(Pawn attacker, Pawn target)
        {
            if (attacker == null || target == null || attacker.jobs == null)
                return;

            FaceCellNow(attacker, target.Position);
            Job job = JobMaker.MakeJob(JobDefOf.AttackMelee, target);
            job.expiryInterval = 500;
            job.checkOverrideOnExpire = true;
            job.collideWithPawns = true;
            attacker.jobs.TryTakeOrderedJob(job, JobTag.Misc);
        }

        private static void ClampHediffSeverity(Pawn pawn, HediffDef def, float maxSeverity)
        {
            if (pawn == null || pawn.health == null || def == null)
                return;

            Hediff hediff = pawn.health.hediffSet.GetFirstHediffOfDef(def);
            if (hediff != null)
            {
                hediff.Severity = Mathf.Min(hediff.Severity, maxSeverity);
            }
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

            worstInjury?.Heal(amount);
        }

        private static bool IsValidAuraTarget(Pawn source, Pawn target)
        {
            if (source == null || target == null || target == source)
                return false;

            if (target.Dead || !target.Spawned)
                return false;

            if (!target.RaceProps.IsFlesh)
                return false;

            return target.HostileTo(source);
        }

        private static bool IsValidDashTarget(Pawn source, Pawn target)
        {
            if (source == null || target == null || target == source)
                return false;

            if (target.Dead || !target.Spawned)
                return false;

            return target.HostileTo(source);
        }

        private static void ApplyHeatstroke(Pawn target, float severityAmount)
        {
            if (target == null || target.health == null || severityAmount <= 0f)
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
