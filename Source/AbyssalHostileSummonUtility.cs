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
                    for (int j = 0; j < generated.Count; j++)
                    {
                        generated[j]?.Destroy(DestroyMode.Vanish);
                    }

                    return false;
                }

                int count = Mathf.Max(0, entry.Count);
                for (int i = 0; i < count; i++)
                {
                    if (!TryGenerateHostilePawn(map, entry.KindDef, faction, out Pawn pawn, out failReason))
                    {
                        for (int j = 0; j < generated.Count; j++)
                        {
                            generated[j]?.Destroy(DestroyMode.Vanish);
                        }

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
            return true;
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
