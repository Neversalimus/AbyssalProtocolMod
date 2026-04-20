using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace AbyssalProtocol
{
    public class CompProperties_ABY_BreachDirective : CompProperties
    {
        public int retargetIntervalTicks = 24;
        public float searchRadius = 26f;
        public float preserveTargetRange = 34f;
        public float meleeThreatOverrideDistance = 2.3f;
        public int siegeSwingCooldownTicks = 120;
        public float siegeBonusDamage = 18f;
        public float siegeArmorPenetration = 0.28f;
        public int empStumbleTicks = 45;
        public int empReactionCooldownTicks = 180;
        public int thinWallMaxHitPoints = 320;
        public bool prioritizeTurrets = true;
        public bool prioritizeDoors = true;
        public bool prioritizeBarriers = true;

        public CompProperties_ABY_BreachDirective()
        {
            compClass = typeof(CompABY_BreachDirective);
        }
    }

    public class CompABY_BreachDirective : ThingComp
    {
        private int nextRetargetTick = -1;
        private int nextSiegeSwingTick = -1;
        private int nextEmpReactionAllowedTick = -1;
        private Thing currentBreachTarget;

        public CompProperties_ABY_BreachDirective Props => (CompProperties_ABY_BreachDirective)props;

        private Pawn PawnParent => parent as Pawn;

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref nextRetargetTick, "nextRetargetTick", -1);
            Scribe_Values.Look(ref nextSiegeSwingTick, "nextSiegeSwingTick", -1);
            Scribe_Values.Look(ref nextEmpReactionAllowedTick, "nextEmpReactionAllowedTick", -1);
            Scribe_References.Look(ref currentBreachTarget, "currentBreachTarget");
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            int now = Find.TickManager != null ? Find.TickManager.TicksGame : 0;
            nextRetargetTick = now + 30;
            nextSiegeSwingTick = now + 90;
            nextEmpReactionAllowedTick = now;
        }

        public override void CompTick()
        {
            base.CompTick();

            Pawn pawn = PawnParent;
            if (!ShouldOperateNow(pawn))
            {
                return;
            }

            if (currentBreachTarget != null && !IsStillValidBreachTarget(pawn, currentBreachTarget, Props.preserveTargetRange))
            {
                currentBreachTarget = null;
            }

            TryPerformSiegeSwing(pawn);

            int now = Find.TickManager != null ? Find.TickManager.TicksGame : 0;
            if (now < nextRetargetTick)
            {
                return;
            }

            nextRetargetTick = now + Mathf.Max(10, Props.retargetIntervalTicks);

            if (ShouldRespectImmediateMeleeThreat(pawn))
            {
                return;
            }

            if (currentBreachTarget == null)
            {
                currentBreachTarget = FindBestBreachTarget(pawn);
            }

            if (currentBreachTarget == null)
            {
                return;
            }

            EnsureAttackJob(pawn, currentBreachTarget);
        }

        public override void PostPostApplyDamage(DamageInfo dinfo, float totalDamageDealt)
        {
            base.PostPostApplyDamage(dinfo, totalDamageDealt);

            Pawn pawn = PawnParent;
            if (pawn == null || pawn.Dead || pawn.Downed || !pawn.Spawned)
            {
                return;
            }

            if (dinfo.Def != DamageDefOf.EMP)
            {
                return;
            }

            int now = Find.TickManager != null ? Find.TickManager.TicksGame : 0;
            if (now < nextEmpReactionAllowedTick)
            {
                return;
            }

            nextEmpReactionAllowedTick = now + Mathf.Max(60, Props.empReactionCooldownTicks);

            Job stumbleJob = JobMaker.MakeJob(JobDefOf.Wait_Combat);
            stumbleJob.expiryInterval = Mathf.Max(20, Props.empStumbleTicks);
            pawn.jobs?.TryTakeOrderedJob(stumbleJob);
        }

        public override string CompInspectStringExtra()
        {
            if (currentBreachTarget == null || currentBreachTarget.Destroyed)
            {
                return null;
            }

            return "Breach target: " + currentBreachTarget.LabelShortCap;
        }

        private void TryPerformSiegeSwing(Pawn pawn)
        {
            int now = Find.TickManager != null ? Find.TickManager.TicksGame : 0;
            if (now < nextSiegeSwingTick)
            {
                return;
            }

            Building targetBuilding = ResolveAdjacentBuildingTarget(pawn);
            if (targetBuilding == null)
            {
                return;
            }

            nextSiegeSwingTick = now + Mathf.Max(45, Props.siegeSwingCooldownTicks);

            DamageInfo damageInfo = new DamageInfo(
                DamageDefOf.Blunt,
                Mathf.Max(1f, Props.siegeBonusDamage),
                Mathf.Max(0f, Props.siegeArmorPenetration),
                -1f,
                pawn,
                null,
                pawn.equipment?.Primary?.def,
                DamageInfo.SourceCategory.ThingOrUnknown,
                targetBuilding);

            targetBuilding.TakeDamage(damageInfo);

        }

        private Building ResolveAdjacentBuildingTarget(Pawn pawn)
        {
            Thing jobTarget = pawn.jobs?.curJob?.targetA.Thing;
            if (jobTarget is Building currentJobBuilding && IsAdjacentAndAttackable(pawn, currentJobBuilding))
            {
                return currentJobBuilding;
            }

            if (currentBreachTarget is Building rememberedBuilding && IsAdjacentAndAttackable(pawn, rememberedBuilding))
            {
                return rememberedBuilding;
            }

            return null;
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

        private bool ShouldRespectImmediateMeleeThreat(Pawn pawn)
        {
            Thing currentJobTarget = pawn.jobs?.curJob?.targetA.Thing;
            if (currentJobTarget is Pawn hostilePawn
                && AbyssalThreatPawnUtility.IsValidHostileTarget(pawn, hostilePawn)
                && pawn.Position.DistanceTo(hostilePawn.Position) <= Props.meleeThreatOverrideDistance)
            {
                return true;
            }

            Pawn closeThreat = AbyssalThreatPawnUtility.FindClosestThreatWithin(pawn, Props.meleeThreatOverrideDistance);
            return closeThreat != null && currentBreachTarget == null;
        }

        private Thing FindBestBreachTarget(Pawn pawn)
        {
            Map map = pawn.MapHeld;
            if (map == null)
            {
                return null;
            }

            Thing best = null;
            float bestScore = float.MinValue;
            float maxDistance = Mathf.Max(8f, Props.searchRadius);

            foreach (Thing thing in map.listerThings.AllThings)
            {
                if (!(thing is Building building))
                {
                    continue;
                }

                if (!IsValidBreachBuildingTarget(pawn, building, maxDistance))
                {
                    continue;
                }

                float score = ScoreBuildingTarget(pawn, building);
                if (score > bestScore)
                {
                    bestScore = score;
                    best = building;
                }
            }

            return best;
        }

        private bool IsValidBreachBuildingTarget(Pawn pawn, Building building, float maxDistance)
        {
            if (pawn == null || building == null || building.Destroyed || !building.Spawned)
            {
                return false;
            }

            if (building.Map != pawn.MapHeld || building.def == null || !building.def.useHitPoints)
            {
                return false;
            }

            if (building.Position.DistanceTo(pawn.Position) > maxDistance)
            {
                return false;
            }

            if (building == currentBreachTarget)
            {
                return true;
            }

            Faction pawnFaction = pawn.Faction;
            Faction buildingFaction = building.Faction;
            if (pawnFaction != null && buildingFaction != null)
            {
                return pawnFaction.HostileTo(buildingFaction);
            }

            if (buildingFaction == null && pawn.MapHeld != null && pawn.MapHeld.IsPlayerHome)
            {
                if (building.def.building != null && !building.def.building.isNaturalRock)
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsStillValidBreachTarget(Pawn pawn, Thing thing, float preserveRange)
        {
            if (!(thing is Building building))
            {
                return false;
            }

            if (!IsValidBreachBuildingTarget(pawn, building, preserveRange))
            {
                return false;
            }

            return true;
        }

        private float ScoreBuildingTarget(Pawn pawn, Building building)
        {
            float distance = pawn.Position.DistanceTo(building.Position);
            float score = 100f - (distance * 2.9f);

            if (Props.prioritizeTurrets && building is Building_Turret)
            {
                score += 140f;
            }

            if (Props.prioritizeDoors && building is Building_Door)
            {
                score += 105f;
            }

            if (Props.prioritizeBarriers && IsBarrierLike(building))
            {
                score += 82f;
            }

            if (IsThinWallLike(building))
            {
                score += 58f;
            }

            if (building.HitPoints > 0 && building.MaxHitPoints > 0)
            {
                float hpFraction = (float)building.HitPoints / building.MaxHitPoints;
                score += (1f - hpFraction) * 12f;
            }

            if (!GenSight.LineOfSight(pawn.Position, building.Position, pawn.MapHeld))
            {
                score -= 8f;
            }

            if (currentBreachTarget == building)
            {
                score += 20f;
            }

            return score;
        }

        private static bool IsBarrierLike(Building building)
        {
            string defName = building.def?.defName ?? string.Empty;
            return defName.IndexOf("Barricade", System.StringComparison.OrdinalIgnoreCase) >= 0
                || defName.IndexOf("Sandbag", System.StringComparison.OrdinalIgnoreCase) >= 0
                || defName.IndexOf("Barrier", System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private bool IsThinWallLike(Building building)
        {
            if (building == null || building.def == null)
            {
                return false;
            }

            string defName = building.def.defName ?? string.Empty;
            string label = building.def.label ?? string.Empty;
            bool readsAsWall = defName.IndexOf("Wall", System.StringComparison.OrdinalIgnoreCase) >= 0
                || label.IndexOf("wall", System.StringComparison.OrdinalIgnoreCase) >= 0;

            return readsAsWall && building.MaxHitPoints <= Mathf.Max(1, Props.thinWallMaxHitPoints);
        }

        private void EnsureAttackJob(Pawn pawn, Thing target)
        {
            if (target == null || target.Destroyed)
            {
                return;
            }

            Job currentJob = pawn.jobs?.curJob;
            if (currentJob != null && currentJob.def == JobDefOf.AttackMelee && currentJob.targetA.Thing == target)
            {
                return;
            }

            Job attackJob = JobMaker.MakeJob(JobDefOf.AttackMelee, target);
            attackJob.expiryInterval = Mathf.Max(90, Props.retargetIntervalTicks * 6);
            attackJob.canBashDoors = true;
            pawn.jobs?.TryTakeOrderedJob(attackJob);
        }

        private static bool IsAdjacentAndAttackable(Pawn pawn, Building building)
        {
            if (pawn == null || building == null || building.Destroyed || !building.Spawned)
            {
                return false;
            }

            return pawn.Position.AdjacentTo8WayOrInside(building.Position);
        }
    }
}
