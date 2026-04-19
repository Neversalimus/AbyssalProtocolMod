using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI.Group;

namespace AbyssalProtocol
{
    public static class AbyssalHostileSummonUtility
    {
        public sealed class HostilePackEntry
        {
            public PawnKindDef KindDef;
            public int Count;
        }

        public static bool TrySpawnHostilePack(
            Map map,
            PawnKindDef kindDef,
            Faction faction,
            IntVec3 requestedArrivalCell,
            int count,
            string packLabel,
            string letterLabel,
            string letterDesc,
            out IntVec3 arrivalCell,
            out string failReason)
        {
            return TrySpawnHostilePack(
                map,
                new List<HostilePackEntry>
                {
                    new HostilePackEntry
                    {
                        KindDef = kindDef,
                        Count = count
                    }
                },
                faction,
                requestedArrivalCell,
                packLabel,
                letterLabel,
                letterDesc,
                true,
                out arrivalCell,
                out failReason);
        }

        public static bool TrySpawnHostilePack(
            Map map,
            List<HostilePackEntry> entries,
            Faction faction,
            IntVec3 requestedArrivalCell,
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
                failReason = "No map available for hostile pack spawn.";
                return false;
            }

            if (entries == null || entries.Count == 0)
            {
                failReason = "Missing hostile pack entries for summon spawn.";
                return false;
            }

            if (faction == null)
            {
                failReason = "No hostile faction available for hostile pack spawn.";
                return false;
            }

            arrivalCell = requestedArrivalCell;
            if (!arrivalCell.IsValid && !AbyssalBossSummonUtility.TryFindBossArrivalCell(map, out arrivalCell))
            {
                failReason = "ABY_CircleFail_NoBossArrival".Translate();
                return false;
            }

            List<Pawn> generated = new List<Pawn>();
            for (int entryIndex = 0; entryIndex < entries.Count; entryIndex++)
            {
                HostilePackEntry entry = entries[entryIndex];
                if (entry == null || entry.KindDef == null)
                {
                    failReason = "Missing PawnKindDef for hostile pack spawn.";
                    CleanupGeneratedPawns(generated);
                    return false;
                }

                int count = Mathf.Max(0, entry.Count);
                for (int i = 0; i < count; i++)
                {
                    if (!TryGenerateHostilePawn(map, entry.KindDef, faction, out Pawn pawn, out failReason))
                    {
                        CleanupGeneratedPawns(generated);
                        return false;
                    }

                    generated.Add(pawn);
                }
            }

            if (generated.Count <= 0)
            {
                failReason = "Failed to generate any hostile pack pawns.";
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

            ArchonInfernalVFXUtility.DoSummonVFX(map, arrivalCell);
            ABY_SoundUtility.PlayAt("ABY_SigilSpawnImpulse", arrivalCell, map);

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
                string finalLabel = letterLabel.NullOrEmpty()
                    ? "ABY_BossSummonSuccessLabel".Translate()
                    : letterLabel;
                string fallbackLabel = packLabel ?? (entries[0]?.KindDef?.label ?? "breach");
                string finalDesc = letterDesc.NullOrEmpty()
                    ? "ABY_BossSummonSuccessDesc".Translate(fallbackLabel)
                    : letterDesc;

                Find.LetterStack.ReceiveLetter(
                    finalLabel,
                    finalDesc,
                    LetterDefOf.ThreatSmall,
                    new TargetInfo(arrivalCell, map));
            }

            return true;
        }

        public static bool TrySpawnHostilePackAroundAnchor(
            Map map,
            List<HostilePackEntry> entries,
            Faction faction,
            IntVec3 anchorCell,
            string packLabel,
            out string failReason)
        {
            failReason = null;

            if (map == null)
            {
                failReason = "No map available for hostile pack spawn.";
                return false;
            }

            if (entries == null || entries.Count == 0)
            {
                failReason = "Missing hostile pack entries for summon spawn.";
                return false;
            }

            if (faction == null)
            {
                failReason = "No hostile faction available for hostile pack spawn.";
                return false;
            }

            if (!anchorCell.IsValid || !anchorCell.InBounds(map))
            {
                failReason = "Missing valid anchor cell for local hostile pack spawn.";
                return false;
            }

            List<Pawn> generated = new List<Pawn>();
            for (int entryIndex = 0; entryIndex < entries.Count; entryIndex++)
            {
                HostilePackEntry entry = entries[entryIndex];
                if (entry == null || entry.KindDef == null)
                {
                    failReason = "Missing PawnKindDef for hostile pack spawn.";
                    CleanupGeneratedPawns(generated);
                    return false;
                }

                int count = Mathf.Max(0, entry.Count);
                for (int i = 0; i < count; i++)
                {
                    if (!TryGenerateHostilePawn(map, entry.KindDef, faction, out Pawn pawn, out failReason))
                    {
                        CleanupGeneratedPawns(generated);
                        return false;
                    }

                    generated.Add(pawn);
                }
            }

            if (generated.Count <= 0)
            {
                failReason = "Failed to generate any hostile pack pawns.";
                return false;
            }

            List<Pawn> spawned = new List<Pawn>();
            for (int i = 0; i < generated.Count; i++)
            {
                Pawn pawn = generated[i];
                IntVec3 spawnCell = FindLocalEscortSpawnCell(anchorCell, map, spawned);
                GenSpawn.Spawn(pawn, spawnCell, map, Rot4.Random);
                spawned.Add(pawn);
            }

            ArchonInfernalVFXUtility.DoSummonVFX(map, anchorCell);
            ABY_SoundUtility.PlayAt("ABY_SigilSpawnImpulse", anchorCell, map);

            LordJob lordJob = new LordJob_AssaultColony(
                faction,
                canKidnap: false,
                canTimeoutOrFlee: false,
                sappers: false,
                useAvoidGridSmart: true,
                canSteal: false);

            LordMaker.MakeNewLord(faction, lordJob, map, spawned);
            return true;
        }


        public static bool TrySpawnHostilePackThroughPortal(
            Map map,
            List<HostilePackEntry> entries,
            Faction faction,
            IntVec3 portalCell,
            string packLabel,
            out string failReason)
        {
            failReason = null;
            if (!TrySpawnHostilePackAroundAnchor(map, entries, faction, portalCell, packLabel, out failReason))
            {
                return false;
            }

            if (map != null && portalCell.IsValid && portalCell.InBounds(map))
            {
                ABY_SoundUtility.PlayAt("ABY_RupturePortalOpen", portalCell, map);
                Current.Game?.GetComponent<AbyssalBossScreenFXGameComponent>()?.RegisterRitualPulse(map, 0.16f);
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
                failReason = "Failed to generate the hostile pack pawn.";
                return false;
            }

            AbyssalThreatPawnUtility.PrepareThreatPawn(pawn);
            AbyssalDifficultyUtility.ApplyDifficultyScaling(pawn);
            return true;
        }

        private static void CleanupGeneratedPawns(List<Pawn> generated)
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

        private static IntVec3 FindLocalEscortSpawnCell(IntVec3 root, Map map, List<Pawn> alreadySpawned)
        {
            if (map == null)
            {
                return root;
            }

            float[] searchRadii = { 5.9f, 8.9f, 12.9f, 16.9f };
            for (int radiusIndex = 0; radiusIndex < searchRadii.Length; radiusIndex++)
            {
                int maxCells = Mathf.Min(GenRadial.RadialPattern.Length, GenRadial.NumCellsInRadius(searchRadii[radiusIndex]));
                for (int i = 0; i < maxCells; i++)
                {
                    IntVec3 candidate = root + GenRadial.RadialPattern[i];
                    if (!candidate.InBounds(map) || !candidate.Standable(map) || candidate.Fogged(map))
                    {
                        continue;
                    }

                    if (CellHasPawn(candidate, map) || CellOccupiedBySpawnList(candidate, alreadySpawned))
                    {
                        continue;
                    }

                    return candidate;
                }
            }

            IntVec3 alternateRoot = root;
            if (AbyssalBossSummonUtility.TryFindBossArrivalCell(map, out IntVec3 arrivalCell) && arrivalCell.IsValid)
            {
                alternateRoot = arrivalCell;
            }

            if (CellFinder.TryFindRandomCellNear(
                alternateRoot,
                map,
                12,
                cell => cell.InBounds(map) && cell.Standable(map) && !cell.Fogged(map) && !CellHasPawn(cell, map) && !CellOccupiedBySpawnList(cell, alreadySpawned),
                out IntVec3 result))
            {
                return result;
            }

            return FindSpawnCellNear(alternateRoot, map, alreadySpawned != null ? alreadySpawned.Count : 0);
        }

        private static bool CellOccupiedBySpawnList(IntVec3 cell, List<Pawn> alreadySpawned)
        {
            if (alreadySpawned == null)
            {
                return false;
            }

            for (int i = 0; i < alreadySpawned.Count; i++)
            {
                Pawn pawn = alreadySpawned[i];
                if (pawn != null && pawn.Spawned && pawn.Position == cell)
                {
                    return true;
                }
            }

            return false;
        }

        private static IntVec3 FindSpawnCellNear(IntVec3 root, Map map, int index)
        {
            if (CellFinder.TryFindRandomCellNear(
                root,
                map,
                3 + Mathf.Min(index, 2),
                cell => cell.InBounds(map) && cell.Standable(map) && !cell.Fogged(map) && !CellHasPawn(cell, map),
                out IntVec3 result))
            {
                return result;
            }

            return root;
        }

        private static bool CellHasPawn(IntVec3 cell, Map map)
        {
            List<Thing> things = cell.GetThingList(map);
            for (int i = 0; i < things.Count; i++)
            {
                if (things[i] is Pawn)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
