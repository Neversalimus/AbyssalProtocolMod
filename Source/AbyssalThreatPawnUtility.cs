using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI.Group;

namespace AbyssalProtocol
{
    public static class AbyssalThreatPawnUtility
    {
        private const string HexgunThrallDefName = "ABY_HexgunThrall";
        private const string HexgunWeaponDefName = "ABY_Hexgun";
        private const string RiftSniperDefName = "ABY_RiftSniper";
        private const string ChainZealotDefName = "ABY_ChainZealot";

        public static void PrepareThreatPawn(Pawn pawn)
        {
            PrepareThreatPawn(pawn, CompAbyssalPawnController.GetFor(pawn)?.Props);
        }

        public static void PrepareThreatPawn(Pawn pawn, CompProperties_AbyssalPawnController controllerProps)
        {
            if (pawn == null)
            {
                return;
            }

            EnsureLoadout(pawn, controllerProps);
            EnsureCombatSkills(pawn, controllerProps);
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

        public static AbyssalPawnArchetype GetArchetype(Pawn pawn, AbyssalPawnArchetype fallback = AbyssalPawnArchetype.None)
        {
            CompAbyssalPawnController controller = CompAbyssalPawnController.GetFor(pawn);
            if (controller?.Props != null && controller.Props.archetype != AbyssalPawnArchetype.None)
            {
                return controller.Props.archetype;
            }

            return fallback;
        }

        public static bool CanOperateAbyssalPawn(Pawn pawn)
        {
            if (pawn == null || pawn.MapHeld == null || pawn.Dead || pawn.Downed || !pawn.Spawned)
            {
                return false;
            }

            Faction playerFaction = Faction.OfPlayer;
            if (playerFaction == null)
            {
                return false;
            }

            return pawn.Faction != null && pawn.Faction.HostileTo(playerFaction);
        }

        public static void EnsureHostilityAndLord(
            Pawn pawn,
            bool ensureAssaultLord,
            bool sappers,
            int spawnTick,
            ref int lastLordEnsureTick,
            int spawnGraceTicks,
            int lordRetryTicks)
        {
            if (pawn == null || pawn.Map == null || pawn.Dead)
            {
                return;
            }

            EnsureHostility(pawn);

            Faction playerFaction = Faction.OfPlayer;
            if (!ensureAssaultLord || playerFaction == null || pawn.Faction == null || !pawn.Faction.HostileTo(playerFaction))
            {
                return;
            }

            if (!pawn.Spawned || !pawn.Map.IsPlayerHome)
            {
                return;
            }

            int ticksGame = Find.TickManager != null ? Find.TickManager.TicksGame : 0;
            if (spawnTick >= 0 && ticksGame - spawnTick < spawnGraceTicks)
            {
                return;
            }

            if (GetCurrentLord(pawn) != null)
            {
                return;
            }

            if (ticksGame - lastLordEnsureTick < lordRetryTicks)
            {
                return;
            }

            lastLordEnsureTick = ticksGame;
            AbyssalLordUtility.EnsureAssaultLord(pawn, sappers);
        }

        public static void EnsureHostility(Pawn pawn)
        {
            if (pawn == null)
            {
                return;
            }

            Faction playerFaction = Faction.OfPlayer;
            if (playerFaction == null)
            {
                return;
            }

            if (pawn.Faction != null && pawn.Faction.HostileTo(playerFaction))
            {
                return;
            }

            Faction hostileFaction = AbyssalBossSummonUtility.ResolveHostileFaction();
            if (hostileFaction != null && pawn.Faction != hostileFaction)
            {
                pawn.SetFaction(hostileFaction);
            }
        }

        public static bool IsValidHostileTarget(Pawn source, Pawn target)
        {
            if (source == null || target == null || target == source || target.MapHeld != source.MapHeld || !target.Spawned || target.Dead || target.Downed)
            {
                return false;
            }

            return source.HostileTo(target);
        }

        public static bool HasRangedWeapon(Pawn pawn)
        {
            return pawn?.equipment?.Primary?.def != null && pawn.equipment.Primary.def.IsRangedWeapon;
        }

        public static Pawn FindBestTarget(
            Pawn pawn,
            float minRange,
            float maxRange,
            AbyssalPawnArchetype archetype,
            bool preferRangedTargets,
            bool requireLineOfSight = true)
        {
            if (pawn?.Map == null)
            {
                return null;
            }

            List<Pawn> pawns = pawn.Map.mapPawns?.AllPawnsSpawned;
            if (pawns == null)
            {
                return null;
            }

            Pawn bestTarget = null;
            float bestScore = float.MinValue;
            float effectiveMaxRange = maxRange <= 0f ? 9999f : maxRange;
            float effectiveMinRange = minRange < 0f ? 0f : minRange;

            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn candidate = pawns[i];
                if (!IsValidHostileTarget(pawn, candidate))
                {
                    continue;
                }

                float distance = pawn.Position.DistanceTo(candidate.Position);
                if (distance < effectiveMinRange || distance > effectiveMaxRange)
                {
                    continue;
                }

                if (requireLineOfSight && !GenSight.LineOfSight(pawn.Position, candidate.Position, pawn.Map))
                {
                    continue;
                }

                float score = ScoreTarget(pawn, candidate, distance, archetype, preferRangedTargets);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestTarget = candidate;
                }
            }

            return bestTarget;
        }

        public static Pawn FindClosestThreatWithin(Pawn pawn, float maxDistance)
        {
            if (pawn?.Map == null)
            {
                return null;
            }

            List<Pawn> pawns = pawn.Map.mapPawns?.AllPawnsSpawned;
            if (pawns == null)
            {
                return null;
            }

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

        public static bool TryFindRetreatCell(Pawn pawn, Pawn threat, float preferredMinRange, int retreatSearchRadius, out IntVec3 retreatCell)
        {
            retreatCell = IntVec3.Invalid;
            if (pawn?.Map == null || threat == null)
            {
                return false;
            }

            Map map = pawn.Map;
            float currentDistance = pawn.Position.DistanceTo(threat.Position);
            float bestScore = float.MinValue;
            int radius = retreatSearchRadius < 1 ? 1 : retreatSearchRadius;

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
            if (pawn?.Map == null || target == null)
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

        public static void RefreshOrApplyHediff(Pawn target, string hediffDefName, float minimumSeverity = 0f)
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

        private static float ScoreTarget(Pawn source, Pawn candidate, float distance, AbyssalPawnArchetype archetype, bool preferRangedTargets)
        {
            bool targetIsRanged = HasRangedWeapon(candidate);
            float healthPct = 1f;
            if (candidate.health != null)
            {
                healthPct = candidate.health.summaryHealth.SummaryHealthPercent;
            }

            float score;
            switch (archetype)
            {
                case AbyssalPawnArchetype.SwarmRusher:
                    score = 18f - distance;
                    break;
                case AbyssalPawnArchetype.Pouncer:
                    score = 10f - Mathf.Abs(distance - 7.5f);
                    break;
                case AbyssalPawnArchetype.HookBruiser:
                    score = 9f - Mathf.Abs(distance - 6.5f);
                    break;
                case AbyssalPawnArchetype.RangedSkirmisher:
                    score = distance * 1.15f;
                    break;
                case AbyssalPawnArchetype.LongRangeMarksman:
                    score = distance * 1.45f + ((1f - healthPct) * 2.2f);
                    break;
                case AbyssalPawnArchetype.BossJuggernaut:
                    score = 13f - distance;
                    break;
                case AbyssalPawnArchetype.ArchonPredator:
                    score = 12f - Mathf.Abs(distance - 5.5f);
                    break;
                default:
                    score = 10f - distance;
                    break;
            }

            if (preferRangedTargets && targetIsRanged)
            {
                score += 4.5f;
            }

            if (candidate.Faction == Faction.OfPlayer)
            {
                score += 1.5f;
            }

            if (candidate.Drafted)
            {
                score += 0.8f;
            }

            return score;
        }

        private static void EnsureLoadout(Pawn pawn, CompProperties_AbyssalPawnController controllerProps)
        {
            if (pawn?.equipment == null || pawn.equipment.Primary != null)
            {
                return;
            }

            string weaponDefName = controllerProps?.forcedPrimaryDefName;
            if (weaponDefName.NullOrEmpty())
            {
                if (IsHexgunThrall(pawn))
                {
                    weaponDefName = HexgunWeaponDefName;
                }
                else if (IsRiftSniper(pawn))
                {
                    weaponDefName = null;
                }
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

        private static void EnsureCombatSkills(Pawn pawn, CompProperties_AbyssalPawnController controllerProps)
        {
            if (pawn?.skills == null)
            {
                return;
            }

            int minShooting = controllerProps != null ? controllerProps.minimumShootingSkill : -1;
            int minMelee = controllerProps != null ? controllerProps.minimumMeleeSkill : -1;

            if (minShooting < 0 && IsHexgunThrall(pawn))
            {
                minShooting = 10;
            }

            if (minMelee < 0 && IsChainZealot(pawn))
            {
                minMelee = 11;
            }

            if (minShooting >= 0)
            {
                SkillRecord shooting = pawn.skills.GetSkill(SkillDefOf.Shooting);
                if (shooting != null && shooting.Level < minShooting)
                {
                    shooting.Level = minShooting;
                }
            }

            if (minMelee >= 0)
            {
                SkillRecord melee = pawn.skills.GetSkill(SkillDefOf.Melee);
                if (melee != null && melee.Level < minMelee)
                {
                    melee.Level = minMelee;
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
    }
}
