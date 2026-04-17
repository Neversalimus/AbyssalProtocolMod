using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace AbyssalProtocol
{
    public static class AbyssalThreatPawnUtility
    {
        private const string HexgunThrallDefName = "ABY_HexgunThrall";
        private const string HexgunWeaponDefName = "ABY_Hexgun";
        private const string ChainZealotDefName = "ABY_ChainZealot";

        public static void PrepareThreatPawn(Pawn pawn, CompProperties_AbyssalPawnController controllerProps = null)
        {
            if (pawn == null)
            {
                return;
            }

            CompProperties_AbyssalPawnController resolvedProps = controllerProps ?? GetControllerProps(pawn);
            RemoveSpawnDiseases(pawn);
            EnsureLoadout(pawn, resolvedProps);
            EnsureCombatSkills(pawn, resolvedProps);
        }

        public static CompProperties_AbyssalPawnController GetControllerProps(Pawn pawn)
        {
            return pawn?.TryGetComp<CompAbyssalPawnController>()?.Props;
        }

        public static Lord GetCurrentLord(Pawn pawn)
        {
            if (pawn?.Map?.lordManager?.lords == null)
            {
                return null;
            }

            List<Lord> lords = pawn.Map.lordManager.lords;
            for (int i = 0; i < lords.Count; i++)
            {
                Lord lord = lords[i];
                if (lord?.ownedPawns != null && lord.ownedPawns.Contains(pawn))
                {
                    return lord;
                }
            }

            return null;
        }

        public static bool TryEnsureHostileAggression(
            Pawn pawn,
            bool sappers,
            int spawnGraceTicks,
            int lordRetryTicks,
            ref int spawnTick,
            ref int lastAggroTick)
        {
            if (pawn == null || pawn.Map == null || pawn.Dead)
            {
                return false;
            }

            EnsureHostileFaction(pawn);

            Faction playerFaction = Faction.OfPlayer;
            if (pawn.Faction == null || playerFaction == null || !pawn.Faction.HostileTo(playerFaction))
            {
                return false;
            }

            if (!pawn.Spawned || !pawn.Map.IsPlayerHome)
            {
                return false;
            }

            int ticksGame = Find.TickManager != null ? Find.TickManager.TicksGame : 0;
            if (spawnTick < 0)
            {
                spawnTick = ticksGame;
            }

            if (ticksGame - spawnTick < Mathf.Max(0, spawnGraceTicks))
            {
                return false;
            }

            if (AbyssalLordUtility.FindLordFor(pawn) != null)
            {
                return false;
            }

            if (ticksGame - lastAggroTick < Mathf.Max(30, lordRetryTicks))
            {
                return false;
            }

            lastAggroTick = ticksGame;
            AbyssalLordUtility.EnsureAssaultLord(pawn, sappers);
            return true;
        }

        public static void EnsureHostileFaction(Pawn pawn)
        {
            if (pawn == null || pawn.Faction != null)
            {
                return;
            }

            Faction hostileFaction = AbyssalBossSummonUtility.ResolveHostileFaction();
            if (hostileFaction != null)
            {
                pawn.SetFaction(hostileFaction);
            }
        }

        public static Pawn FindBestTarget(
            Pawn pawn,
            float minRange,
            float maxRange,
            bool preferFarthestTargets,
            bool preferRangedTargets,
            bool requireRangedTargets,
            float rangedTargetBias = 0f,
            float healthWeight = 0f)
        {
            if (pawn?.Map == null)
            {
                return null;
            }

            var pawns = pawn.Map.mapPawns?.AllPawnsSpawned;
            if (pawns == null)
            {
                return null;
            }

            Pawn best = null;
            float bestScore = preferFarthestTargets ? float.MinValue : float.MaxValue;
            float resolvedMinRange = Mathf.Max(0f, minRange);
            float resolvedMaxRange = maxRange > 0f ? maxRange : float.MaxValue;

            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn candidate = pawns[i];
                if (!IsValidHostileTarget(pawn, candidate))
                {
                    continue;
                }

                float distance = pawn.Position.DistanceTo(candidate.Position);
                if (distance < resolvedMinRange || distance > resolvedMaxRange)
                {
                    continue;
                }

                if (!GenSight.LineOfSight(pawn.Position, candidate.Position, pawn.Map))
                {
                    continue;
                }

                bool hasRangedWeapon = HasRangedWeapon(candidate);
                if (requireRangedTargets && !hasRangedWeapon)
                {
                    continue;
                }

                float score = distance;
                if (preferRangedTargets && hasRangedWeapon)
                {
                    score += preferFarthestTargets ? Mathf.Abs(rangedTargetBias) : -Mathf.Abs(rangedTargetBias);
                }

                if (healthWeight > 0f && candidate.health != null)
                {
                    score += candidate.health.summaryHealth.SummaryHealthPercent * healthWeight;
                }

                if (preferFarthestTargets)
                {
                    if (score > bestScore)
                    {
                        bestScore = score;
                        best = candidate;
                    }
                }
                else if (score < bestScore)
                {
                    bestScore = score;
                    best = candidate;
                }
            }

            return best;
        }

        public static Pawn FindClosestThreatWithin(Pawn pawn, float maxDistance)
        {
            if (pawn?.Map == null)
            {
                return null;
            }

            Pawn best = null;
            float bestDistance = maxDistance;
            var pawns = pawn.Map.mapPawns?.AllPawnsSpawned;
            if (pawns == null)
            {
                return null;
            }

            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn candidate = pawns[i];
                if (!IsValidHostileTarget(pawn, candidate))
                {
                    continue;
                }

                float distance = pawn.Position.DistanceTo(candidate.Position);
                if (distance > bestDistance)
                {
                    continue;
                }

                bestDistance = distance;
                best = candidate;
            }

            return best;
        }

        public static bool TryFindRetreatCell(Pawn pawn, Pawn threat, float preferredMinRange, int searchRadius, out IntVec3 retreatCell)
        {
            retreatCell = IntVec3.Invalid;
            Map map = pawn?.Map;
            if (map == null || threat == null)
            {
                return false;
            }

            float currentDistance = pawn.Position.DistanceTo(threat.Position);
            float bestScore = float.MinValue;
            int radius = Mathf.Max(4, searchRadius);

            foreach (IntVec3 cell in GenRadial.RadialCellsAround(pawn.Position, radius, true))
            {
                if (!cell.InBounds(map) || !cell.Standable(map) || CellHasOtherPawn(cell, map, pawn))
                {
                    continue;
                }

                float threatDistance = cell.DistanceTo(threat.Position);
                if (threatDistance <= currentDistance + 1.9f || threatDistance < preferredMinRange + 1.2f)
                {
                    continue;
                }

                float moveCost = pawn.Position.DistanceTo(cell);
                float score = (threatDistance * 2.8f) - (moveCost * 0.75f);
                if (GenSight.LineOfSight(cell, threat.Position, map))
                {
                    score += 1.6f;
                }

                if (score > bestScore)
                {
                    bestScore = score;
                    retreatCell = cell;
                }
            }

            return retreatCell.IsValid;
        }

        public static bool TryFindAdjacentLandingCell(Pawn pawn, Pawn target, out IntVec3 landingCell)
        {
            landingCell = IntVec3.Invalid;
            Map map = pawn?.Map;
            if (map == null || target == null)
            {
                return false;
            }

            float bestDistance = float.MaxValue;
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dz = -1; dz <= 1; dz++)
                {
                    if (dx == 0 && dz == 0)
                    {
                        continue;
                    }

                    IntVec3 cell = target.Position + new IntVec3(dx, 0, dz);
                    if (!cell.InBounds(map) || !cell.Standable(map) || CellHasOtherPawn(cell, map, pawn))
                    {
                        continue;
                    }

                    float distance = pawn.Position.DistanceToSquared(cell);
                    if (distance < bestDistance)
                    {
                        bestDistance = distance;
                        landingCell = cell;
                    }
                }
            }

            return landingCell.IsValid;
        }

        public static void ApplyOrRefreshHediff(Pawn target, string hediffDefName, float severity = -1f)
        {
            if (target?.health == null || hediffDefName.NullOrEmpty())
            {
                return;
            }

            HediffDef hediffDef = DefDatabase<HediffDef>.GetNamedSilentFail(hediffDefName);
            if (hediffDef == null)
            {
                return;
            }

            Hediff existing = target.health.hediffSet.GetFirstHediffOfDef(hediffDef);
            if (existing != null)
            {
                if (severity > 0f)
                {
                    existing.Severity = Mathf.Max(existing.Severity, severity);
                }

                return;
            }

            Hediff added = HediffMaker.MakeHediff(hediffDef, target);
            if (added == null)
            {
                return;
            }

            if (severity > 0f)
            {
                added.Severity = severity;
            }

            target.health.AddHediff(added);
        }

        public static bool IsValidHostileTarget(Pawn actor, Pawn candidate)
        {
            if (actor == null || candidate == null || candidate == actor)
            {
                return false;
            }

            if (candidate.Map != actor.Map || !candidate.Spawned || candidate.Dead || candidate.Downed)
            {
                return false;
            }

            if (candidate.Faction == null || actor.Faction == null)
            {
                return false;
            }

            return actor.Faction.HostileTo(candidate.Faction);
        }

        public static bool HasRangedWeapon(Pawn pawn)
        {
            return pawn?.equipment?.Primary?.def != null && pawn.equipment.Primary.def.IsRangedWeapon;
        }

        public static bool CellHasOtherPawn(IntVec3 cell, Map map, Pawn ignore)
        {
            var things = cell.GetThingList(map);
            for (int i = 0; i < things.Count; i++)
            {
                if (things[i] is Pawn pawn && pawn != ignore)
                {
                    return true;
                }
            }

            return false;
        }

        public static bool TryMaintainSpacing(Pawn pawn, Thing currentTarget, float preferredMinRange, int retreatSearchRadius, bool holdPositionWhenTargeting)
        {
            if (pawn == null)
            {
                return false;
            }

            if (preferredMinRange <= 0f)
            {
                if (holdPositionWhenTargeting && currentTarget is Pawn targetPawn && CanFireAt(pawn, targetPawn))
                {
                    pawn.pather?.StopDead();
                }

                return false;
            }

            Pawn nearestThreat = FindClosestThreatWithin(pawn, preferredMinRange);
            if (nearestThreat == null)
            {
                if (holdPositionWhenTargeting && currentTarget is Pawn targetPawn && CanFireAt(pawn, targetPawn))
                {
                    pawn.pather?.StopDead();
                }

                return false;
            }

            if (!TryFindRetreatCell(pawn, nearestThreat, preferredMinRange, retreatSearchRadius, out IntVec3 retreatCell))
            {
                return false;
            }

            if (retreatCell == pawn.Position)
            {
                return false;
            }

            pawn.pather?.StartPath(retreatCell, PathEndMode.OnCell);
            pawn.rotationTracker?.FaceCell(nearestThreat.Position);
            return true;
        }

        public static bool CanFireAt(Pawn shooter, Pawn target)
        {
            if (!IsValidHostileTarget(shooter, target))
            {
                return false;
            }

            return GenSight.LineOfSight(shooter.Position, target.Position, shooter.Map);
        }

        private static void RemoveSpawnDiseases(Pawn pawn)
        {
            if (pawn?.health?.hediffSet?.hediffs == null)
            {
                return;
            }

            List<Hediff> toRemove = null;
            List<Hediff> hediffs = pawn.health.hediffSet.hediffs;
            for (int i = 0; i < hediffs.Count; i++)
            {
                Hediff hediff = hediffs[i];
                if (hediff == null || hediff.def == null || !hediff.def.isBad)
                {
                    continue;
                }

                if (!IsSpawnDisease(hediff.def))
                {
                    continue;
                }

                if (toRemove == null)
                {
                    toRemove = new List<Hediff>();
                }

                toRemove.Add(hediff);
            }

            if (toRemove == null)
            {
                return;
            }

            for (int i = 0; i < toRemove.Count; i++)
            {
                pawn.health.RemoveHediff(toRemove[i]);
            }
        }

        private static bool IsSpawnDisease(HediffDef hediffDef)
        {
            if (hediffDef == null || hediffDef.comps == null)
            {
                return false;
            }

            for (int i = 0; i < hediffDef.comps.Count; i++)
            {
                if (hediffDef.comps[i] is HediffCompProperties_Immunizable)
                {
                    return true;
                }
            }

            return false;
        }

        private static void EnsureLoadout(Pawn pawn, CompProperties_AbyssalPawnController controllerProps)
        {
            string weaponDefName = ResolveForcedPrimaryDefName(pawn, controllerProps);
            if (weaponDefName.NullOrEmpty())
            {
                return;
            }

            if (pawn.equipment == null || pawn.equipment.Primary != null)
            {
                return;
            }

            ThingDef weaponDef = DefDatabase<ThingDef>.GetNamedSilentFail(weaponDefName);
            if (weaponDef == null)
            {
                return;
            }

            Thing weapon = ThingMaker.MakeThing(weaponDef);
            if (weapon is ThingWithComps thingWithComps)
            {
                pawn.equipment.AddEquipment(thingWithComps);
            }
        }

        private static void EnsureCombatSkills(Pawn pawn, CompProperties_AbyssalPawnController controllerProps)
        {
            if (pawn.skills == null)
            {
                return;
            }

            int minShoot = ResolveMinShootingSkill(pawn, controllerProps);
            int minMelee = ResolveMinMeleeSkill(pawn, controllerProps);

            if (minShoot > 0)
            {
                SkillRecord shooting = pawn.skills.GetSkill(SkillDefOf.Shooting);
                if (shooting != null && shooting.Level < minShoot)
                {
                    shooting.Level = minShoot;
                }
            }

            if (minMelee > 0)
            {
                SkillRecord melee = pawn.skills.GetSkill(SkillDefOf.Melee);
                if (melee != null && melee.Level < minMelee)
                {
                    melee.Level = minMelee;
                }
            }
        }

        private static string ResolveForcedPrimaryDefName(Pawn pawn, CompProperties_AbyssalPawnController controllerProps)
        {
            if (controllerProps != null && !controllerProps.forcedPrimaryDefName.NullOrEmpty())
            {
                return controllerProps.forcedPrimaryDefName;
            }

            if (IsHexgunThrall(pawn))
            {
                return HexgunWeaponDefName;
            }

            if (controllerProps != null)
            {
                switch (controllerProps.archetype)
                {
                    case AbyssalPawnArchetype.RangedSkirmisher:
                        return HexgunWeaponDefName;
                }
            }

            return null;
        }

        private static int ResolveMinShootingSkill(Pawn pawn, CompProperties_AbyssalPawnController controllerProps)
        {
            if (controllerProps != null && controllerProps.minShootingSkill >= 0)
            {
                return controllerProps.minShootingSkill;
            }

            if (IsHexgunThrall(pawn))
            {
                return 10;
            }

            switch (controllerProps?.archetype)
            {
                case AbyssalPawnArchetype.RangedSkirmisher:
                    return 10;
                case AbyssalPawnArchetype.LongRangeMarksman:
                    return 12;
                case AbyssalPawnArchetype.SupportCaster:
                    return 8;
                case AbyssalPawnArchetype.SiegeNode:
                    return 11;
                default:
                    return -1;
            }
        }

        private static int ResolveMinMeleeSkill(Pawn pawn, CompProperties_AbyssalPawnController controllerProps)
        {
            if (controllerProps != null && controllerProps.minMeleeSkill >= 0)
            {
                return controllerProps.minMeleeSkill;
            }

            if (IsChainZealot(pawn))
            {
                return 11;
            }

            switch (controllerProps?.archetype)
            {
                case AbyssalPawnArchetype.SwarmRusher:
                    return 8;
                case AbyssalPawnArchetype.Pouncer:
                    return 10;
                case AbyssalPawnArchetype.HookBruiser:
                    return 11;
                case AbyssalPawnArchetype.BossJuggernaut:
                    return 12;
                case AbyssalPawnArchetype.ArchonPredator:
                    return 14;
                default:
                    return -1;
            }
        }

        private static bool IsHexgunThrall(Pawn pawn)
        {
            return HasDefName(pawn, HexgunThrallDefName);
        }

        private static bool IsChainZealot(Pawn pawn)
        {
            return HasDefName(pawn, ChainZealotDefName);
        }

        public static bool HasDefName(Pawn pawn, string defName)
        {
            if (pawn == null || defName.NullOrEmpty())
            {
                return false;
            }

            return pawn.def?.defName == defName || pawn.kindDef?.defName == defName;
        }
    }
}
