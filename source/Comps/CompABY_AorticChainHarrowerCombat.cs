using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace AbyssalProtocol
{
    public class CompProperties_ABY_AorticChainHarrowerCombat : CompProperties
    {
        public string heartDefName = "ABY_DominionSliceHeart";
        public int scanIntervalTicks = 18;
        public float defendRadius = 14.0f;
        public float minInterceptRange = 4.2f;
        public float maxInterceptRange = 11.8f;
        public float maxDistanceFromHeartToIntercept = 17.5f;
        public int interceptCooldownTicks = 390;
        public int interceptCooldownJitterTicks = 90;
        public int dashDurationTicks = 12;
        public float dashMoteScale = 1.18f;
        public string dashMoteDefName = "ABY_Mote_ArchonDashTrail";
        public string dashSoundDefName = "ABY_SigilChargePulse";
        public string snareHediffDefName = "ABY_AorticSnare";
        public float snareSeverity = 0.42f;
        public bool preferRangedTargets = true;
        public bool preferHeartAttackers = true;

        public int cagePulseCooldownTicks = 840;
        public int cagePulseCooldownJitterTicks = 180;
        public float cagePulseRadius = 4.6f;
        public float cagePulseDamage = 9.0f;
        public float cagePulseArmorPenetration = 0.22f;
        public int cagePulseMaxTargets = 8;
        public string cagePulseMoteDefName = "ABY_Mote_DominionSliceAmbientPressurePulse";
        public string cagePulseSparkMoteDefName = "ABY_Mote_DominionSliceStaticPressureSpark";
        public string cagePulseSoundDefName = "ABY_SigilChargePulse";

        public CompProperties_ABY_AorticChainHarrowerCombat()
        {
            compClass = typeof(CompABY_AorticChainHarrowerCombat);
        }
    }

    public class CompABY_AorticChainHarrowerCombat : ThingComp
    {
        private int nextInterceptTick;
        private int nextCagePulseTick;
        private Thing cachedHeart;
        private MapComponent_DominionSliceEncounter cachedEncounter;
        private int nextEncounterResolveTick;
        private readonly List<Pawn> pulseTargets = new List<Pawn>();

        public CompProperties_ABY_AorticChainHarrowerCombat Props => (CompProperties_ABY_AorticChainHarrowerCombat)props;

        private Pawn PawnParent => parent as Pawn;

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref nextInterceptTick, "nextInterceptTick", 0);
            Scribe_Values.Look(ref nextCagePulseTick, "nextCagePulseTick", 0);
            Scribe_References.Look(ref cachedHeart, "cachedHeart");
        }

        public override void CompTick()
        {
            base.CompTick();

            try
            {
                TickCombatSafe();
            }
            catch (System.Exception ex)
            {
                ABY_LogThrottleUtility.Warning(
                    "aortic-combat-tick-failed",
                    "[Abyssal Protocol] Aortic Chain Harrower combat tick failed and was skipped: " + ex.GetType().Name + ": " + ex.Message,
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

            cachedHeart = ResolveHeart(pawn);
            if (cachedHeart == null)
            {
                return;
            }

            int ticks = Find.TickManager != null ? Find.TickManager.TicksGame : 0;
            if (ticks >= nextCagePulseTick)
            {
                if (TryDoCagePulse(pawn))
                {
                    nextCagePulseTick = ticks + Mathf.Max(180, Props.cagePulseCooldownTicks) + Rand.RangeInclusive(-Mathf.Max(0, Props.cagePulseCooldownJitterTicks), Mathf.Max(0, Props.cagePulseCooldownJitterTicks));
                }
                else if (nextCagePulseTick <= 0)
                {
                    nextCagePulseTick = ticks + Rand.RangeInclusive(120, 240);
                }
            }

            if (ticks >= nextInterceptTick)
            {
                if (TryDoChainIntercept(pawn, cachedHeart))
                {
                    nextInterceptTick = ticks + Mathf.Max(120, Props.interceptCooldownTicks) + Rand.RangeInclusive(-Mathf.Max(0, Props.interceptCooldownJitterTicks), Mathf.Max(0, Props.interceptCooldownJitterTicks));
                }
                else if (nextInterceptTick <= 0)
                {
                    nextInterceptTick = ticks + Rand.RangeInclusive(90, 180);
                }
            }
        }

        private bool TryDoChainIntercept(Pawn pawn, Thing heart)
        {
            if (pawn == null || heart == null || ABY_AbyssalDashRuntime.IsDashing(pawn))
            {
                return false;
            }

            if (pawn.PositionHeld.DistanceTo(heart.PositionHeld) > Props.maxDistanceFromHeartToIntercept)
            {
                return false;
            }

            Pawn target = FindInterceptTarget(pawn, heart);
            if (target == null)
            {
                return false;
            }

            if (!AbyssalThreatPawnUtility.TryFindAdjacentLandingCell(pawn, target, out IntVec3 landingCell))
            {
                return false;
            }

            bool started = ABY_AbyssalDashRuntime.TryStartDash(
                pawn,
                target,
                landingCell,
                Props.snareHediffDefName,
                Props.dashDurationTicks,
                Props.dashMoteDefName,
                Props.dashMoteScale,
                Props.dashSoundDefName,
                "aortic_chain_intercept");

            if (started)
            {
                TrySpawnInterceptCue(pawn, target);
            }

            return started;
        }

        private Pawn FindInterceptTarget(Pawn pawn, Thing heart)
        {
            Map map = pawn?.MapHeld;
            if (map == null || heart == null)
            {
                return null;
            }

            IReadOnlyList<Pawn> pawns = ABY_RuntimeTargetCache.SpawnedLivingPawnsFor(map);
            Pawn best = null;
            float bestScore = float.MinValue;
            IntVec3 heartCell = heart.PositionHeld;

            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn candidate = pawns[i];
                if (!AbyssalThreatPawnUtility.IsValidHostileTarget(pawn, candidate))
                {
                    continue;
                }

                float pawnDistance = pawn.PositionHeld.DistanceTo(candidate.PositionHeld);
                if (pawnDistance < Props.minInterceptRange || pawnDistance > Props.maxInterceptRange)
                {
                    continue;
                }

                if (!GenSight.LineOfSight(pawn.PositionHeld, candidate.PositionHeld, map))
                {
                    continue;
                }

                float heartDistance = heartCell.DistanceTo(candidate.PositionHeld);
                if (heartDistance > Props.defendRadius)
                {
                    continue;
                }

                float score = (Props.defendRadius - heartDistance) * 3.2f;
                score += pawnDistance * 0.28f;

                if (Props.preferRangedTargets && AbyssalThreatPawnUtility.HasRangedWeapon(candidate))
                {
                    score += 5.5f;
                }

                if (Props.preferHeartAttackers && IsTargetingHeart(candidate, heart))
                {
                    score += 8.0f;
                }

                if (candidate.health != null)
                {
                    score += (1f - candidate.health.summaryHealth.SummaryHealthPercent) * 1.8f;
                }

                score += Rand.Value * 0.4f;
                if (score > bestScore)
                {
                    bestScore = score;
                    best = candidate;
                }
            }

            return best;
        }

        private bool TryDoCagePulse(Pawn pawn)
        {
            if (pawn?.MapHeld == null)
            {
                return false;
            }

            pulseTargets.Clear();
            Map map = pawn.MapHeld;
            foreach (Thing thing in GenRadial.RadialDistinctThingsAround(pawn.PositionHeld, map, Props.cagePulseRadius, true))
            {
                if (!(thing is Pawn target) || !AbyssalThreatPawnUtility.IsValidHostileTarget(pawn, target))
                {
                    continue;
                }

                if (!GenSight.LineOfSight(pawn.PositionHeld, target.PositionHeld, map))
                {
                    continue;
                }

                pulseTargets.Add(target);
                if (pulseTargets.Count >= Mathf.Max(1, Props.cagePulseMaxTargets))
                {
                    break;
                }
            }

            if (pulseTargets.Count <= 0)
            {
                return false;
            }

            TrySpawnCagePulseCue(pawn);
            for (int i = 0; i < pulseTargets.Count; i++)
            {
                ApplyCagePulseTo(pawn, pulseTargets[i]);
            }

            pulseTargets.Clear();
            return true;
        }

        private void ApplyCagePulseTo(Pawn source, Pawn target)
        {
            if (!AbyssalThreatPawnUtility.IsValidHostileTarget(source, target))
            {
                return;
            }

            DamageInfo damageInfo = new DamageInfo(
                DamageDefOf.Burn,
                Mathf.Max(0f, Props.cagePulseDamage),
                Mathf.Max(0f, Props.cagePulseArmorPenetration),
                -1f,
                source,
                null,
                source.def,
                DamageInfo.SourceCategory.ThingOrUnknown);
            target.TakeDamage(damageInfo);
            AbyssalThreatPawnUtility.ApplyOrRefreshHediff(target, Props.snareHediffDefName, Mathf.Max(0.01f, Props.snareSeverity));
        }

        private Thing ResolveHeart(Pawn pawn)
        {
            Map map = pawn?.MapHeld;
            if (map == null)
            {
                return null;
            }

            MapComponent_DominionSliceEncounter encounter = ABY_DominionSliceEncounterResolveUtility.Resolve(map, ref cachedEncounter, ref nextEncounterResolveTick);
            Thing heart = encounter?.HeartBuilding;
            if (IsValidHeart(pawn, heart))
            {
                return heart;
            }

            if (IsValidHeart(pawn, cachedHeart))
            {
                return cachedHeart;
            }

            if (Props.heartDefName.NullOrEmpty())
            {
                return null;
            }

            ThingDef heartDef = ABY_DefCache.ThingDefNamed(Props.heartDefName);
            if (heartDef == null || map.listerThings == null)
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
                if (!IsValidHeart(pawn, candidate))
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

        private bool IsTargetingHeart(Pawn candidate, Thing heart)
        {
            if (candidate == null || heart == null)
            {
                return false;
            }

            Job currentJob = candidate.CurJob;
            if (currentJob != null)
            {
                if (currentJob.targetA.Thing == heart || currentJob.targetB.Thing == heart || currentJob.targetC.Thing == heart)
                {
                    return true;
                }
            }

            Stance_Busy busyStance = candidate.stances?.curStance as Stance_Busy;
            LocalTargetInfo focus = busyStance?.focusTarg ?? LocalTargetInfo.Invalid;
            return focus.Thing == heart || (focus.Cell.IsValid && focus.Cell == heart.PositionHeld);
        }

        private void TrySpawnInterceptCue(Pawn pawn, Pawn target)
        {
            if (pawn?.MapHeld == null || target == null)
            {
                return;
            }

            Map map = pawn.MapHeld;
            ABY_AbyssalDashRuntime.SpawnTrailMote(map, pawn.PositionHeld, Props.cagePulseSparkMoteDefName, 0.82f);
            ABY_AbyssalDashRuntime.SpawnTrailMote(map, target.PositionHeld, Props.cagePulseSparkMoteDefName, 0.92f);
            FleckMaker.ThrowMicroSparks(SafePawnDrawPos(target), map);
        }

        private void TrySpawnCagePulseCue(Pawn pawn)
        {
            if (pawn?.MapHeld == null)
            {
                return;
            }

            Map map = pawn.MapHeld;
            if (!Props.cagePulseMoteDefName.NullOrEmpty())
            {
                ThingDef moteDef = DefDatabase<ThingDef>.GetNamedSilentFail(Props.cagePulseMoteDefName);
                if (moteDef != null)
                {
                    MoteMaker.MakeStaticMote(SafePawnDrawPos(pawn), map, moteDef, Mathf.Max(1.0f, Props.cagePulseRadius * 0.56f));
                }
            }

            if (!Props.cagePulseSparkMoteDefName.NullOrEmpty())
            {
                ThingDef sparkDef = DefDatabase<ThingDef>.GetNamedSilentFail(Props.cagePulseSparkMoteDefName);
                if (sparkDef != null)
                {
                    for (int i = 0; i < 3; i++)
                    {
                        Vector3 loc = SafePawnDrawPos(pawn) + Gen.RandomHorizontalVector(Props.cagePulseRadius * 0.35f);
                        MoteMaker.MakeStaticMote(loc, map, sparkDef, Rand.Range(0.62f, 0.92f));
                    }
                }
            }

            FleckMaker.ThrowLightningGlow(SafePawnDrawPos(pawn), map, Mathf.Max(1.3f, Props.cagePulseRadius * 0.34f));
            ABY_SoundUtility.PlayAt(Props.cagePulseSoundDefName, pawn.PositionHeld, map);
        }

        private static bool IsValidHeart(Pawn pawn, Thing heart)
        {
            if (pawn == null || heart == null || heart.Destroyed || !heart.Spawned || heart.MapHeld != pawn.MapHeld)
            {
                return false;
            }

            return heart.def != null && heart.def.defName == "ABY_DominionSliceHeart";
        }

        private static bool ShouldOperate(Pawn pawn)
        {
            return pawn != null
                && pawn.Spawned
                && pawn.MapHeld != null
                && !pawn.Dead
                && !pawn.Downed
                && pawn.Faction != null;
        }
        private static Vector3 SafePawnDrawPos(Pawn pawn)
        {
            if (pawn == null)
            {
                return Vector3.zero;
            }

            try
            {
                return pawn.DrawPos;
            }
            catch
            {
                return pawn.PositionHeld.IsValid ? pawn.PositionHeld.ToVector3Shifted() : Vector3.zero;
            }
        }

    }
}
