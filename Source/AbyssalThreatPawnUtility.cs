using System;
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
        private const string RiftSniperDefName = "ABY_RiftSniper";

        public static void PrepareThreatPawn(Pawn pawn, CompAbyssalPawnController controller = null)
        {
            if (pawn == null)
            {
                return;
            }

            controller = controller ?? GetController(pawn);
            EnsureLoadout(pawn, controller);
            EnsureCombatSkills(pawn, controller);
        }

        public static CompAbyssalPawnController GetController(Pawn pawn)
        {
            return CompAbyssalPawnController.GetFor(pawn);
        }

        public static Lord GetCurrentLord(Pawn pawn)
        {
            return AbyssalLordUtility.FindLordFor(pawn);
        }

        public static void EnsureHostileFaction(Pawn pawn)
        {
            if (pawn == null)
            {
                return;
            }

            if (pawn.Faction != null)
            {
                return;
            }

            Faction hostileFaction = AbyssalBossSummonUtility.ResolveHostileFaction();
            if (hostileFaction != null)
            {
                pawn.SetFaction(hostileFaction);
            }
        }

        public static Lord EnsureAssaultLordForPawn(Pawn pawn, bool sappers)
        {
            return AbyssalLordUtility.EnsureAssaultLord(pawn, sappers);
        }

        public static Pawn FindBestTarget(Pawn pawn, float maxRange, bool preferRangedTargets, bool preferFarthestTargets, bool requireRangedTarget)
        {
            if (pawn?.Map?.mapPawns?.AllPawnsSpawned == null)
            {
                return null;
            }

            Pawn bestTarget = null;
            float bestScore = preferFarthestTargets ? float.MinValue : float.MaxValue;
            List<Pawn> pawns = pawn.Map.mapPawns.AllPawnsSpawned;
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn candidate = pawns[i];
                if (!IsValidTarget(pawn, candidate))
                {
                    continue;
                }

                bool hasRangedWeapon = HasRangedWeapon(candidate);
                if (requireRangedTarget && !hasRangedWeapon)
                {
                    continue;
                }

                float distance = pawn.Position.DistanceTo(candidate.Position);
                if (distance > maxRange)
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
                    score += preferFarthestTargets ? 4.5f : -2.8f;
                }

                float healthFactor = 1f;
                if (candidate.health != null)
                {
                    healthFactor = candidate.health.summaryHealth.SummaryHealthPercent;
                }

                score += preferFarthestTargets ? healthFactor * 0.35f : healthFactor * 1.2f;
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

        public static bool CanFireAt(Pawn shooter, Thing target, float maxRange)
        {
            Pawn targetPawn = target as Pawn;
            if (!IsValidTarget(shooter, targetPawn))
            {
                return false;
            }

            if (shooter.Position.DistanceTo(targetPawn.Position) > maxRange)
            {
                return false;
            }

            return GenSight.LineOfSight(shooter.Position, targetPawn.Position, shooter.Map);
        }

        public static bool TryMaintainSpacing(Pawn pawn, float preferredMinRange, int retreatSearchRadius, Thing currentTarget, bool holdPositionWhenTargeting)
        {
            if (pawn == null || pawn.Map == null)
            {
                return false;
            }

            if (preferredMinRange <= 0f)
            {
                if (holdPositionWhenTargeting && CanFireAt(pawn, currentTarget, 999f))
                {
                    pawn.pather?.StopDead();
                }

                return false;
            }

            Pawn nearestThreat = FindClosestThreatWithin(pawn, preferredMinRange);
            if (nearestThreat == null)
            {
                if (holdPositionWhenTargeting && currentTarget != null)
                {
                    pawn.pather?.StopDead();
                }

                return false;
            }

            IntVec3 retreatCell;
            if (!TryFindRetreatCell(pawn, nearestThreat, preferredMinRange, retreatSearchRadius, out retreatCell))
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

        public static Pawn FindClosestThreatWithin(Pawn pawn, float maxDistance)
        {
            if (pawn?.Map?.mapPawns?.AllPawnsSpawned == null)
            {
                return null;
            }

            Pawn bestTarget = null;
            float bestDistance = maxDistance;
            List<Pawn> pawns = pawn.Map.mapPawns.AllPawnsSpawned;
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn candidate = pawns[i];
                if (!IsValidTarget(pawn, candidate))
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

        public static bool TryFindRetreatCell(Pawn pawn, Pawn threat, float preferredMinRange, int retreatSearchRadius, out IntVec3 retreatCell)
        {
            retreatCell = IntVec3.Invalid;
            if (pawn == null || threat == null || pawn.Map == null)
            {
                return false;
            }

            Map map = pawn.Map;
            float currentDistance = pawn.Position.DistanceTo(threat.Position);
            float bestScore = float.MinValue;
            int radius = Math.Max(4, retreatSearchRadius);

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
            if (pawn == null || target == null || pawn.Map == null)
            {
                return false;
            }

            Map map = pawn.Map;
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
                    existing.Severity = Math.Max(existing.Severity, minimumSeverity);
                }

                return;
            }

            Hediff added = HediffMaker.MakeHediff(hediffDef, target);
            if (minimumSeverity > 0f)
            {
                added.Severity = Math.Max(added.Severity, minimumSeverity);
            }

            target.health.AddHediff(added);
        }

        public static bool IsValidTarget(Pawn shooter, Pawn target)
        {
            if (shooter == null || target == null || target == shooter || target.Map != shooter.Map || !target.Spawned || target.Dead || target.Downed)
            {
                return false;
            }

            if (target.Faction == null || shooter.Faction == null)
            {
                return false;
            }

            return shooter.Faction.HostileTo(target.Faction);
        }

        public static bool HasRangedWeapon(Pawn pawn)
        {
            return pawn?.equipment?.Primary?.def != null && pawn.equipment.Primary.def.IsRangedWeapon;
        }

        public static bool CellHasOtherPawn(IntVec3 cell, Map map, Pawn ignore)
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

        private static void EnsureLoadout(Pawn pawn, CompAbyssalPawnController controller)
        {
            if (pawn.equipment == null || pawn.equipment.Primary != null)
            {
                return;
            }

            string weaponDefName = controller?.Props.forcePrimaryWeaponDefName;
            if (weaponDefName.NullOrEmpty() && IsHexgunThrall(pawn))
            {
                weaponDefName = HexgunWeaponDefName;
            }

            if (weaponDefName.NullOrEmpty())
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

        private static void EnsureCombatSkills(Pawn pawn, CompAbyssalPawnController controller)
        {
            if (pawn.skills == null)
            {
                return;
            }

            int minimumShooting = 0;
            int minimumMelee = 0;
            if (controller != null)
            {
                minimumShooting = controller.Props.minimumShootingSkill;
                minimumMelee = controller.Props.minimumMeleeSkill;
            }
            else
            {
                if (IsHexgunThrall(pawn))
                {
                    minimumShooting = 10;
                }
                else if (IsRiftSniper(pawn))
                {
                    minimumShooting = 14;
                }
                else if (IsChainZealot(pawn))
                {
                    minimumMelee = 11;
                }
            }

            if (minimumShooting > 0)
            {
                SkillRecord shooting = pawn.skills.GetSkill(SkillDefOf.Shooting);
                if (shooting != null && shooting.Level < minimumShooting)
                {
                    shooting.Level = minimumShooting;
                }
            }

            if (minimumMelee > 0)
            {
                SkillRecord melee = pawn.skills.GetSkill(SkillDefOf.Melee);
                if (melee != null && melee.Level < minimumMelee)
                {
                    melee.Level = minimumMelee;
                }
            }
        }

        private static bool IsHexgunThrall(Pawn pawn)
        {
            return HasDefName(pawn, HexgunThrallDefName);
        }

        private static bool IsRiftSniper(Pawn pawn)
        {
            return HasDefName(pawn, RiftSniperDefName);
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
