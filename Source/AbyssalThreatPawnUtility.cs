using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI.Group;

namespace AbyssalProtocol
{
    public static class AbyssalThreatPawnUtility
    {
        private const string HexgunThrallDefName = "ABY_HexgunThrall";
        private const string HexgunWeaponDefName = "ABY_Hexgun";
        private const string ChainZealotDefName = "ABY_ChainZealot";

        public static void PrepareThreatPawn(Pawn pawn)
        {
            if (pawn == null)
            {
                return;
            }

            CompProperties_AbyssalPawnController controller = GetControllerProperties(pawn);
            EnsureLoadout(pawn, controller);
            EnsureCombatSkills(pawn, controller);
        }

        public static CompProperties_AbyssalPawnController GetControllerProperties(Pawn pawn)
        {
            if (pawn?.AllComps == null)
            {
                return null;
            }

            for (int i = 0; i < pawn.AllComps.Count; i++)
            {
                if (pawn.AllComps[i] is CompAbyssalPawnController controller)
                {
                    return controller.Props;
                }
            }

            return null;
        }

        public static bool CanOperateHostilePawn(Pawn pawn)
        {
            if (pawn == null || pawn.Map == null || pawn.Dead || !pawn.Spawned || pawn.Downed)
            {
                return false;
            }

            if (pawn.Faction == null || Faction.OfPlayer == null || !pawn.Faction.HostileTo(Faction.OfPlayer))
            {
                return false;
            }

            return true;
        }

        public static bool IsValidHostileTarget(Pawn owner, Pawn target)
        {
            if (owner == null || target == null || target == owner || target.Map != owner.Map || !target.Spawned || target.Dead || target.Downed)
            {
                return false;
            }

            if (target.Faction == null || owner.Faction == null)
            {
                return false;
            }

            return owner.Faction.HostileTo(target.Faction);
        }

        public static bool HasRangedWeapon(Pawn pawn)
        {
            return pawn?.equipment?.Primary?.def != null && pawn.equipment.Primary.def.IsRangedWeapon;
        }

        public static Pawn FindBestHostilePawnTarget(
            Pawn pawn,
            float minRange,
            float maxRange,
            bool requireRanged,
            bool preferRangedTargets,
            bool preferLowHealthTargets,
            bool preferFarthestTargets,
            float rangedTargetBonus,
            float lowHealthWeight)
        {
            if (pawn?.Map?.mapPawns == null)
            {
                return null;
            }

            IReadOnlyList<Pawn> pawns = pawn.Map.mapPawns.AllPawnsSpawned;
            Pawn bestTarget = null;
            float bestScore = preferFarthestTargets ? float.MinValue : float.MaxValue;

            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn candidate = pawns[i];
                if (!IsValidHostileTarget(pawn, candidate))
                {
                    continue;
                }

                bool hasRangedWeapon = HasRangedWeapon(candidate);
                if (requireRanged && !hasRangedWeapon)
                {
                    continue;
                }

                float distance = pawn.Position.DistanceTo(candidate.Position);
                if (distance < minRange || distance > maxRange)
                {
                    continue;
                }

                if (!GenSight.LineOfSight(pawn.Position, candidate.Position, pawn.Map))
                {
                    continue;
                }

                float score = distance;
                if (preferRangedTargets && hasRangedWeapon)
                {
                    score += preferFarthestTargets ? rangedTargetBonus : -rangedTargetBonus;
                }

                if (preferLowHealthTargets)
                {
                    float healthFactor = candidate.health != null ? candidate.health.summaryHealth.SummaryHealthPercent : 1f;
                    score += preferFarthestTargets ? (1f - healthFactor) * lowHealthWeight : healthFactor * lowHealthWeight;
                }

                if (preferFarthestTargets)
                {
                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestTarget = candidate;
                    }
                }
                else if (score < bestScore)
                {
                    bestScore = score;
                    bestTarget = candidate;
                }
            }

            return bestTarget;
        }

        public static bool TryFindAdjacentLandingCell(Pawn pawn, Thing target, out IntVec3 landingCell)
        {
            landingCell = IntVec3.Invalid;
            if (pawn?.Map == null || target == null)
            {
                return false;
            }

            Map map = pawn.Map;
            IntVec3 center = target.Position;
            float bestDistance = float.MaxValue;

            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dz = -1; dz <= 1; dz++)
                {
                    if (dx == 0 && dz == 0)
                    {
                        continue;
                    }

                    IntVec3 cell = center + new IntVec3(dx, 0, dz);
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

        public static bool TryMaintainSpacing(Pawn pawn, Pawn currentTarget, float maxRange, float preferredMinRange, int retreatSearchRadius, bool holdPositionWhenTargeting)
        {
            if (pawn == null || pawn.Map == null)
            {
                return false;
            }

            if (preferredMinRange <= 0f)
            {
                if (holdPositionWhenTargeting && CanAttackTargetAtRange(pawn, currentTarget, maxRange))
                {
                    pawn.pather?.StopDead();
                }

                return false;
            }

            Pawn nearestThreat = FindClosestThreatWithin(pawn, preferredMinRange);
            if (nearestThreat == null)
            {
                if (holdPositionWhenTargeting && CanAttackTargetAtRange(pawn, currentTarget, maxRange))
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

        public static void ApplyOrRefreshHediff(Pawn target, string hediffDefName, float minimumSeverity)
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
                if (minimumSeverity > 0f)
                {
                    existing.Severity = existing.Severity < minimumSeverity ? minimumSeverity : existing.Severity;
                }

                return;
            }

            Hediff added = HediffMaker.MakeHediff(hediffDef, target);
            if (minimumSeverity > 0f)
            {
                added.Severity = minimumSeverity;
            }

            target.health.AddHediff(added);
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

        private static void EnsureLoadout(Pawn pawn, CompProperties_AbyssalPawnController controller)
        {
            string weaponDefName = controller != null ? controller.forcedWeaponDefName : null;
            if (weaponDefName.NullOrEmpty() && IsHexgunThrall(pawn))
            {
                weaponDefName = HexgunWeaponDefName;
            }

            if (weaponDefName.NullOrEmpty() || pawn.equipment == null || pawn.equipment.Primary != null)
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

        private static void EnsureCombatSkills(Pawn pawn, CompProperties_AbyssalPawnController controller)
        {
            if (pawn?.skills == null)
            {
                return;
            }

            int minimumShootingSkill = controller != null ? controller.minimumShootingSkill : -1;
            int minimumMeleeSkill = controller != null ? controller.minimumMeleeSkill : -1;

            if (minimumShootingSkill < 0 && IsHexgunThrall(pawn))
            {
                minimumShootingSkill = 10;
            }

            if (minimumMeleeSkill < 0 && IsChainZealot(pawn))
            {
                minimumMeleeSkill = 11;
            }

            if (minimumShootingSkill > 0)
            {
                SkillRecord shooting = pawn.skills.GetSkill(SkillDefOf.Shooting);
                if (shooting != null && shooting.Level < minimumShootingSkill)
                {
                    shooting.Level = minimumShootingSkill;
                }
            }

            if (minimumMeleeSkill > 0)
            {
                SkillRecord melee = pawn.skills.GetSkill(SkillDefOf.Melee);
                if (melee != null && melee.Level < minimumMeleeSkill)
                {
                    melee.Level = minimumMeleeSkill;
                }
            }
        }

        private static bool CanAttackTargetAtRange(Pawn pawn, Pawn target, float maxRange)
        {
            return IsValidHostileTarget(pawn, target)
                && pawn.Position.DistanceTo(target.Position) <= maxRange
                && GenSight.LineOfSight(pawn.Position, target.Position, pawn.Map);
        }

        private static Pawn FindClosestThreatWithin(Pawn pawn, float maxDistance)
        {
            if (pawn?.Map?.mapPawns == null)
            {
                return null;
            }

            IReadOnlyList<Pawn> pawns = pawn.Map.mapPawns.AllPawnsSpawned;
            Pawn bestTarget = null;
            float bestDistance = maxDistance;

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
                bestTarget = candidate;
            }

            return bestTarget;
        }

        private static bool TryFindRetreatCell(Pawn pawn, Pawn threat, float preferredMinRange, int retreatSearchRadius, out IntVec3 retreatCell)
        {
            retreatCell = IntVec3.Invalid;
            Map map = pawn.Map;
            float currentDistance = pawn.Position.DistanceTo(threat.Position);
            float bestScore = float.MinValue;
            int radius = retreatSearchRadius > 0 ? retreatSearchRadius : 9;

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

        private static bool CellHasOtherPawn(IntVec3 cell, Map map, Pawn ignore)
        {
            List<Thing> things = cell.GetThingList(map);
            for (int i = 0; i < things.Count; i++)
            {
                if (things[i] is Pawn pawn && pawn != ignore)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsHexgunThrall(Pawn pawn)
        {
            return HasDefName(pawn, HexgunThrallDefName);
        }

        private static bool IsChainZealot(Pawn pawn)
        {
            return HasDefName(pawn, ChainZealotDefName);
        }

        private static bool HasDefName(Pawn pawn, string defName)
        {
            if (pawn == null || defName.NullOrEmpty())
            {
                return false;
            }

            return pawn.def?.defName == defName || pawn.kindDef?.defName == defName;
        }
    }
}
