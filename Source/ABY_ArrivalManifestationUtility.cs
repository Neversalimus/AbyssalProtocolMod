using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI.Group;

namespace AbyssalProtocol
{
    public enum ABY_ArrivalManifestationType
    {
        SigilBloom,
        StaticPhaseIn,
        SeamBreach
    }

    public static class ABY_ArrivalManifestationUtility
    {
        private const string SigilBloomDefName = "ABY_Manifestation_SigilBloom";
        private const string StaticPhaseInDefName = "ABY_Manifestation_StaticPhaseIn";
        private const string SeamBreachDefName = "ABY_Manifestation_SeamBreach";

        public static bool TrySpawnSigilBloom(
            Map map,
            List<ABY_HostileManifestEntry> entries,
            Faction faction,
            IntVec3 requestedCell,
            int warmupTicks,
            out Thing manifestation,
            out string failReason,
            string packLabel = null,
            string letterLabel = null,
            string letterDesc = null)
        {
            return TrySpawnManifestation(
                map,
                ABY_ArrivalManifestationType.SigilBloom,
                entries,
                faction,
                requestedCell,
                warmupTicks,
                out manifestation,
                out failReason,
                packLabel,
                letterLabel,
                letterDesc);
        }

        public static bool TrySpawnStaticPhaseIn(
            Map map,
            List<ABY_HostileManifestEntry> entries,
            Faction faction,
            IntVec3 requestedCell,
            int warmupTicks,
            out Thing manifestation,
            out string failReason,
            string packLabel = null,
            string letterLabel = null,
            string letterDesc = null)
        {
            return TrySpawnManifestation(
                map,
                ABY_ArrivalManifestationType.StaticPhaseIn,
                entries,
                faction,
                requestedCell,
                warmupTicks,
                out manifestation,
                out failReason,
                packLabel,
                letterLabel,
                letterDesc);
        }

        public static bool TrySpawnSeamBreach(
            Map map,
            List<ABY_HostileManifestEntry> entries,
            Faction faction,
            IntVec3 requestedCell,
            int warmupTicks,
            out Thing manifestation,
            out string failReason,
            string packLabel = null,
            string letterLabel = null,
            string letterDesc = null)
        {
            return TrySpawnManifestation(
                map,
                ABY_ArrivalManifestationType.SeamBreach,
                entries,
                faction,
                requestedCell,
                warmupTicks,
                out manifestation,
                out failReason,
                packLabel,
                letterLabel,
                letterDesc);
        }

        public static bool TrySpawnManifestation(
            Map map,
            ABY_ArrivalManifestationType manifestationType,
            List<ABY_HostileManifestEntry> entries,
            Faction faction,
            IntVec3 requestedCell,
            int warmupTicks,
            out Thing manifestation,
            out string failReason,
            string packLabel = null,
            string letterLabel = null,
            string letterDesc = null)
        {
            manifestation = null;
            failReason = null;

            if (map == null)
            {
                failReason = "No map available for hostile manifestation spawn.";
                return false;
            }

            if (entries == null || entries.Count == 0)
            {
                failReason = "No hostile manifestation entries were supplied.";
                return false;
            }

            if (faction == null)
            {
                failReason = "No hostile faction available for hostile manifestation spawn.";
                return false;
            }

            ThingDef manifestationDef = DefDatabase<ThingDef>.GetNamedSilentFail(GetDefName(manifestationType));
            if (manifestationDef == null)
            {
                failReason = "Missing manifestation ThingDef: " + GetDefName(manifestationType);
                return false;
            }

            IntVec3 spawnCell;
            Rot4 seamSide = Rot4.South;

            if (manifestationType == ABY_ArrivalManifestationType.SeamBreach)
            {
                if (!TryFindSeamBreachCell(map, requestedCell, out spawnCell, out seamSide))
                {
                    failReason = "Could not find a suitable seam breach cell.";
                    return false;
                }
            }
            else if (!TryResolveManifestationCell(map, requestedCell, out spawnCell))
            {
                failReason = "Could not find a suitable manifestation cell.";
                return false;
            }

            Thing thing = GenSpawn.Spawn(manifestationDef, spawnCell, map, WipeMode.Vanish);

            switch (manifestationType)
            {
                case ABY_ArrivalManifestationType.SigilBloom:
                    if (!(thing is Building_ABY_SigilBloomManifestation sigil))
                    {
                        thing.Destroy(DestroyMode.Vanish);
                        failReason = "Spawned manifestation was not a Sigil Bloom building.";
                        return false;
                    }

                    sigil.Initialize(faction, entries, warmupTicks, packLabel, letterLabel, letterDesc);
                    manifestation = sigil;
                    return true;

                case ABY_ArrivalManifestationType.StaticPhaseIn:
                    if (!(thing is Building_ABY_StaticPhaseInManifestation phaseIn))
                    {
                        thing.Destroy(DestroyMode.Vanish);
                        failReason = "Spawned manifestation was not a Static Phase-In building.";
                        return false;
                    }

                    phaseIn.Initialize(faction, entries, warmupTicks, packLabel, letterLabel, letterDesc);
                    manifestation = phaseIn;
                    return true;

                case ABY_ArrivalManifestationType.SeamBreach:
                    if (!(thing is Building_ABY_SeamBreachManifestation breach))
                    {
                        thing.Destroy(DestroyMode.Vanish);
                        failReason = "Spawned manifestation was not a Seam Breach building.";
                        return false;
                    }

                    breach.Initialize(faction, entries, warmupTicks, seamSide, packLabel, letterLabel, letterDesc);
                    manifestation = breach;
                    return true;

                default:
                    thing.Destroy(DestroyMode.Vanish);
                    failReason = "Unknown manifestation type.";
                    return false;
            }
        }

        public static bool TrySpawnManifestedHostiles(
            Map map,
            IntVec3 requestedArrivalCell,
            Faction faction,
            List<ABY_HostileManifestEntry> entries,
            string packLabel,
            string letterLabel,
            string letterDesc,
            bool sendLetter,
            out IntVec3 arrivalCell,
            out string failReason)
        {
            arrivalCell = IntVec3.Invalid;
            failReason = null;

            if (map == null)
            {
                failReason = "No map available for manifested hostile pack spawn.";
                return false;
            }

            if (entries == null || entries.Count == 0)
            {
                failReason = "Missing manifested hostile entries.";
                return false;
            }

            if (faction == null)
            {
                failReason = "Missing faction for manifested hostile pack spawn.";
                return false;
            }

            if (!TryResolveManifestationCell(map, requestedArrivalCell, out arrivalCell))
            {
                failReason = "Could not resolve manifested hostile arrival cell.";
                return false;
            }

            List<Pawn> generated = new List<Pawn>();
            for (int entryIndex = 0; entryIndex < entries.Count; entryIndex++)
            {
                ABY_HostileManifestEntry entry = entries[entryIndex];
                if (entry?.KindDef == null)
                {
                    failReason = "Manifested hostile entry is missing a PawnKindDef.";
                    DestroyGeneratedPawns(generated);
                    return false;
                }

                int count = Mathf.Max(0, entry.Count);
                for (int i = 0; i < count; i++)
                {
                    if (!TryGenerateHostilePawn(map, entry.KindDef, faction, out Pawn pawn, out failReason))
                    {
                        DestroyGeneratedPawns(generated);
                        return false;
                    }

                    generated.Add(pawn);
                }
            }

            if (generated.Count <= 0)
            {
                failReason = "No manifested hostile pawns were generated.";
                return false;
            }

            List<Pawn> spawned = new List<Pawn>();
            for (int i = 0; i < generated.Count; i++)
            {
                Pawn pawn = generated[i];
                IntVec3 spawnCell = FindSpawnCellNear(arrivalCell, map, i);
                GenSpawn.Spawn(pawn, spawnCell, map, Rot4.Random);
                spawned.Add(pawn);
            }

            LordJob lordJob = new LordJob_AssaultColony(
                faction,
                canKidnap: false,
                canTimeoutOrFlee: false,
                sappers: false,
                useAvoidGridSmart: true,
                canSteal: false);
            LordMaker.MakeNewLord(faction, lordJob, map, spawned);

            if (sendLetter)
            {
                string finalLabel = !string.IsNullOrEmpty(letterLabel) ? letterLabel : "Abyssal manifestation";
                string finalDesc = !string.IsNullOrEmpty(letterDesc)
                    ? letterDesc
                    : "Abyssal entities have completed their manifestation.";
                Find.LetterStack.ReceiveLetter(
                    finalLabel,
                    finalDesc,
                    LetterDefOf.ThreatSmall,
                    new TargetInfo(arrivalCell, map));
            }

            return true;
        }

        private static bool TryGenerateHostilePawn(
            Map map,
            PawnKindDef kindDef,
            Faction faction,
            out Pawn pawn,
            out string failReason)
        {
            pawn = null;
            failReason = null;

            PawnGenerationRequest request = new PawnGenerationRequest(
                kindDef,
                faction,
                PawnGenerationContext.NonPlayer,
                map.Tile,
                forceGenerateNewPawn: true,
                allowDead: false,
                allowDowned: false,
                canGeneratePawnRelations: false,
                mustBeCapableOfViolence: true,
                colonistRelationChanceFactor: 0f,
                forceAddFreeWarmLayerIfNeeded: false,
                allowGay: false,
                allowPregnant: false,
                allowFood: false,
                inhabitant: false,
                certainlyBeenInCryptosleep: false,
                forceRedressWorldPawnIfFormerColonist: false,
                worldPawnFactionDoesntMatter: false,
                biocodeWeaponChance: 0f,
                biocodeApparelChance: 0f,
                extraPawnForExtraRelationChance: null,
                relationWithExtraPawnChanceFactor: 0f,
                validatorPreGear: null,
                validatorPostGear: null,
                fixedBirthName: null,
                fixedLastName: null,
                fixedGender: null,
                fixedIdeo: null,
                forceNoIdeo: true,
                developmentalStages: DevelopmentalStage.Adult);

            pawn = PawnGenerator.GeneratePawn(request);
            if (pawn == null)
            {
                failReason = "Failed to generate a manifested hostile pawn.";
                return false;
            }

            PrepareThreatPawn(pawn);
            return true;
        }

        private static void PrepareThreatPawn(Pawn pawn)
        {
            if (pawn == null)
            {
                return;
            }

            try
            {
                Type utilityType = GenTypes.GetTypeInAnyAssembly("AbyssalProtocol.AbyssalThreatPawnUtility");
                if (utilityType == null)
                {
                    return;
                }

                System.Reflection.MethodInfo method = utilityType.GetMethod(
                    "PrepareThreatPawn",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

                if (method != null)
                {
                    method.Invoke(null, new object[] { pawn });
                }
            }
            catch
            {
            }
        }

        private static void DestroyGeneratedPawns(List<Pawn> generated)
        {
            if (generated == null)
            {
                return;
            }

            for (int i = 0; i < generated.Count; i++)
            {
                generated[i]?.Destroy(DestroyMode.Vanish);
            }
        }

        private static IntVec3 FindSpawnCellNear(IntVec3 root, Map map, int index)
        {
            int radius = 2 + Mathf.Min(index, 3);

            for (int i = 0; i < 30; i++)
            {
                IntVec3 candidate = root + GenRadial.RadialPattern[Rand.Range(0, Mathf.Min(GenRadial.NumCellsInRadius(radius), GenRadial.RadialPattern.Length))];
                if (IsUsableManifestCell(candidate, map))
                {
                    return candidate;
                }
            }

            return root;
        }

        private static bool TryResolveManifestationCell(Map map, IntVec3 requestedCell, out IntVec3 cell)
        {
            if (requestedCell.IsValid && IsUsableManifestCell(requestedCell, map))
            {
                cell = requestedCell;
                return true;
            }

            IntVec3 center = map.Center;
            int maxRadius = Mathf.Min(Mathf.Min(map.Size.x, map.Size.z) / 2, 40);

            for (int radius = 0; radius <= maxRadius; radius += 3)
            {
                int maxCells = Mathf.Min(GenRadial.NumCellsInRadius(radius), GenRadial.RadialPattern.Length);
                for (int i = 0; i < maxCells; i++)
                {
                    IntVec3 candidate = center + GenRadial.RadialPattern[i];
                    if (IsUsableManifestCell(candidate, map))
                    {
                        cell = candidate;
                        return true;
                    }
                }
            }

            cell = IntVec3.Invalid;
            return false;
        }

        private static bool TryFindSeamBreachCell(Map map, IntVec3 requestedCell, out IntVec3 cell, out Rot4 seamSide)
        {
            seamSide = Rot4.South;

            if (requestedCell.IsValid && TryResolveSeamCellAt(map, requestedCell, out cell, out seamSide))
            {
                return true;
            }

            IntVec3 center = map.Center;
            int maxRadius = Mathf.Min(Mathf.Min(map.Size.x, map.Size.z) / 2, 45);

            for (int radius = 0; radius <= maxRadius; radius += 2)
            {
                int maxCells = Mathf.Min(GenRadial.NumCellsInRadius(radius), GenRadial.RadialPattern.Length);
                for (int i = 0; i < maxCells; i++)
                {
                    IntVec3 candidate = center + GenRadial.RadialPattern[i];
                    if (TryResolveSeamCellAt(map, candidate, out cell, out seamSide))
                    {
                        return true;
                    }
                }
            }

            cell = IntVec3.Invalid;
            return false;
        }

        private static bool TryResolveSeamCellAt(Map map, IntVec3 candidate, out IntVec3 cell, out Rot4 seamSide)
        {
            cell = IntVec3.Invalid;
            seamSide = Rot4.South;

            if (!IsUsableManifestCell(candidate, map))
            {
                return false;
            }

            Rot4[] sides = { Rot4.North, Rot4.East, Rot4.South, Rot4.West };
            for (int i = 0; i < sides.Length; i++)
            {
                Rot4 side = sides[i];
                IntVec3 seamCell = candidate + side.FacingCell;

                bool blocked =
                    !seamCell.InBounds(map) ||
                    !seamCell.Standable(map) ||
                    seamCell.GetEdifice(map) != null;

                if (blocked)
                {
                    cell = candidate;
                    seamSide = side;
                    return true;
                }
            }

            return false;
        }

        private static bool IsUsableManifestCell(IntVec3 cell, Map map)
        {
            if (!cell.IsValid || map == null || !cell.InBounds(map) || !cell.Standable(map) || cell.Fogged(map))
            {
                return false;
            }

            List<Thing> things = cell.GetThingList(map);
            for (int i = 0; i < things.Count; i++)
            {
                if (things[i] is Pawn)
                {
                    return false;
                }
            }

            return true;
        }

        private static string GetDefName(ABY_ArrivalManifestationType manifestationType)
        {
            switch (manifestationType)
            {
                case ABY_ArrivalManifestationType.SigilBloom:
                    return SigilBloomDefName;
                case ABY_ArrivalManifestationType.StaticPhaseIn:
                    return StaticPhaseInDefName;
                case ABY_ArrivalManifestationType.SeamBreach:
                    return SeamBreachDefName;
                default:
                    return SigilBloomDefName;
            }
        }
    }
}
