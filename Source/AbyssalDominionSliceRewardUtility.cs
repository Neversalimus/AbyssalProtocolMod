using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    public static class AbyssalDominionSliceRewardUtility
    {
        private const string DominionShardThingDefName = "ABY_DominionCrownShard";
        private const string ResidueThingDefName = "ABY_AbyssalResidue";
        private const string DominionSigilThingDefName = "ABY_DominionSigil";
        private const string CrownedCoreFragmentThingDefName = "ABY_CrownedCoreFragment";
        private const string CrownedGateSigilThingDefName = "ABY_CrownedGateSigil";

        public sealed class RewardProfile
        {
            public int DominionShards;
            public int Residue;
            public int DominionSigils;
            public int CrownedCoreFragments;
            public int CrownedGateSigils;
        }

        public static string GetRewardForecastText(MapComponent_DominionCrisis crisis)
        {
            if (crisis == null)
            {
                return null;
            }

            if (crisis.TryGetActivePocketSession(out ABY_DominionPocketSession session))
            {
                Map pocketMap = AbyssalDominionPocketUtility.ResolveMap(session.pocketMapId);
                MapComponent_DominionSliceEncounter encounter = pocketMap != null ? pocketMap.GetComponent<MapComponent_DominionSliceEncounter>() : null;
                RewardProfile profile = BuildRewardProfile(encounter, session);
                return FormatRewardProfile(profile, session.victoryAchieved);
            }

            if (crisis.IsGateEntryReady())
            {
                RewardProfile profile = BuildRewardProfile(null, null);
                return FormatRewardProfile(profile, false);
            }

            return null;
        }

        public static bool TryAwardVictoryRewards(ABY_DominionPocketSession session, Map pocketMap, Map sourceMap, IntVec3 nearCell, out string summary)
        {
            summary = null;
            if (session == null || sourceMap == null)
            {
                return false;
            }

            RewardProfile profile = BuildRewardProfile(pocketMap != null ? pocketMap.GetComponent<MapComponent_DominionSliceEncounter>() : null, session);
            IntVec3 dropCell = ResolveDropCell(sourceMap, nearCell);
            SpawnRewardProfile(sourceMap, dropCell, profile);

            summary = FormatRewardProfile(profile, true);
            session.rewardsGranted = true;
            session.rewardSummary = summary;

            if (dropCell.IsValid)
            {
                FleckMaker.ThrowLightningGlow(dropCell.ToVector3Shifted(), sourceMap, 2.1f);
                FleckMaker.ThrowMicroSparks(dropCell.ToVector3Shifted(), sourceMap);
                ABY_SoundUtility.PlayAt("ABY_SigilSpawnImpulse", dropCell, sourceMap);
            }

            return true;
        }

        public static RewardProfile BuildRewardProfile(MapComponent_DominionSliceEncounter encounter, ABY_DominionPocketSession session)
        {
            int waves = encounter != null ? Mathf.Max(0, encounter.WavesTriggeredCount) : 2;
            RewardProfile profile = new RewardProfile
            {
                DominionShards = Mathf.Clamp(2 + waves / 2, 2, 4),
                Residue = 24 + waves * 6,
                DominionSigils = waves >= 3 ? 1 : 0,
                CrownedCoreFragments = 1,
                CrownedGateSigils = 1
            };

            profile.Residue = Mathf.Max(profile.Residue, Mathf.RoundToInt(profile.Residue * AbyssalDifficultyUtility.GetResidueRewardMultiplier()));
            profile.Residue = ABY_BestiaryRewardUtility.ApplyExtractionBonus(profile.Residue);
            profile.DominionShards = Mathf.Max(profile.DominionShards, Mathf.RoundToInt(profile.DominionShards * AbyssalDifficultyUtility.GetBonusLootMultiplier()));
            profile.DominionShards = ABY_BestiaryRewardUtility.ApplyExtractionBonus(profile.DominionShards);
            profile.DominionSigils = profile.DominionSigils > 0 ? ABY_BestiaryRewardUtility.ApplyExtractionBonus(profile.DominionSigils) : 0;
            return profile;
        }

        public static string FormatRewardProfile(RewardProfile profile, bool extractionReady)
        {
            List<string> parts = new List<string>();
            AddResourceLabel(parts, CrownedGateSigilThingDefName, profile.CrownedGateSigils);
            AddResourceLabel(parts, CrownedCoreFragmentThingDefName, profile.CrownedCoreFragments);
            AddResourceLabel(parts, DominionShardThingDefName, profile.DominionShards);
            AddResourceLabel(parts, DominionSigilThingDefName, profile.DominionSigils);
            AddResourceLabel(parts, ResidueThingDefName, profile.Residue);
            string payload = parts.Count > 0 ? string.Join(", ", parts) : "ABY_DominionSliceRewardForecast_None".Translate();
            return extractionReady
                ? "ABY_DominionSliceRewardForecast_ExtractionReady".Translate(payload)
                : "ABY_DominionSliceRewardForecast_Standard".Translate(payload);
        }

        private static void AddResourceLabel(List<string> parts, string defName, int count)
        {
            if (count <= 0)
            {
                return;
            }

            ThingDef def = DefDatabase<ThingDef>.GetNamedSilentFail(defName);
            string label = def != null ? def.label : defName;
            parts.Add(count + " " + label);
        }

        private static void SpawnRewardProfile(Map map, IntVec3 dropCell, RewardProfile profile)
        {
            if (map == null || !dropCell.IsValid)
            {
                return;
            }

            TrySpawnRewardStack(map, dropCell, CrownedGateSigilThingDefName, profile.CrownedGateSigils);
            TrySpawnRewardStack(map, dropCell, CrownedCoreFragmentThingDefName, profile.CrownedCoreFragments);
            TrySpawnRewardStack(map, dropCell, DominionShardThingDefName, profile.DominionShards);
            TrySpawnRewardStack(map, dropCell, DominionSigilThingDefName, profile.DominionSigils);
            TrySpawnRewardStack(map, dropCell, ResidueThingDefName, profile.Residue);
        }

        private static void TrySpawnRewardStack(Map map, IntVec3 nearCell, string defName, int count)
        {
            if (map == null || !nearCell.IsValid || count <= 0)
            {
                return;
            }

            ThingDef def = DefDatabase<ThingDef>.GetNamedSilentFail(defName);
            if (def == null)
            {
                return;
            }

            Thing thing = ThingMaker.MakeThing(def);
            thing.stackCount = Mathf.Clamp(count, 1, def.stackLimit > 0 ? def.stackLimit : count);
            IntVec3 dropCell = ResolveDropCell(map, nearCell);
            GenPlace.TryPlaceThing(thing, dropCell, map, ThingPlaceMode.Near);
        }

        private static IntVec3 ResolveDropCell(Map map, IntVec3 nearCell)
        {
            if (map == null)
            {
                return IntVec3.Invalid;
            }

            IntVec3 cell = nearCell.IsValid ? nearCell : map.Center;
            if (!cell.InBounds(map) || !cell.Standable(map))
            {
                if (!CellFinder.TryFindRandomCellNear(map.Center, map, 8, c => c.Standable(map), out cell))
                {
                    cell = map.Center;
                }
            }

            return cell;
        }
    }
}
