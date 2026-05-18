using RimWorld;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    public static class DominionSliceCollapseSpectacleVfxUtility
    {
        private const string ShockwaveMoteDefName = "ABY_Mote_DominionSliceCollapseShockwave";
        private const string ExtractionBeaconMoteDefName = "ABY_Mote_DominionSliceExtractionBeacon";
        private const string ExtractionGuideMoteDefName = "ABY_Mote_DominionSliceExtractionGuide";
        private const string ExtractionSafeRingMoteDefName = "ABY_Mote_DominionSliceExtractionSafeRing";
        private const string ExtractionColumnMoteDefName = "ABY_Mote_DominionSliceExtractionColumn";
        private const string RewardBeaconMoteDefName = "ABY_Mote_DominionSliceRewardBeacon";
        private const string RewardGuideMoteDefName = "ABY_Mote_DominionSliceRewardGuide";
        private const string RewardSanctumRingMoteDefName = "ABY_Mote_DominionSliceRewardSanctumRing";
        private const string RewardSanctumColumnMoteDefName = "ABY_Mote_DominionSliceRewardSanctumColumn";
        private const string RewardShardMoteDefName = "ABY_Mote_DominionSliceRewardShard";
        private const string GuidanceTrailMoteDefName = "ABY_Mote_DominionSliceGuidanceTrail";
        private const string EdgeInstabilityMoteDefName = "ABY_Mote_DominionSliceEdgeInstability";
        private const string WarningPulseMoteDefName = "ABY_Mote_DominionSliceCollapseWarningPulse";

        private static ThingDef shockwaveMoteDef;
        private static ThingDef extractionBeaconMoteDef;
        private static ThingDef extractionGuideMoteDef;
        private static ThingDef extractionSafeRingMoteDef;
        private static ThingDef extractionColumnMoteDef;
        private static ThingDef rewardBeaconMoteDef;
        private static ThingDef rewardGuideMoteDef;
        private static ThingDef rewardSanctumRingMoteDef;
        private static ThingDef rewardSanctumColumnMoteDef;
        private static ThingDef rewardShardMoteDef;
        private static ThingDef guidanceTrailMoteDef;
        private static ThingDef edgeInstabilityMoteDef;
        private static ThingDef warningPulseMoteDef;

        private static ThingDef ShockwaveMoteDef
        {
            get { return shockwaveMoteDef ?? (shockwaveMoteDef = DefDatabase<ThingDef>.GetNamedSilentFail(ShockwaveMoteDefName)); }
        }

        private static ThingDef ExtractionBeaconMoteDef
        {
            get { return extractionBeaconMoteDef ?? (extractionBeaconMoteDef = DefDatabase<ThingDef>.GetNamedSilentFail(ExtractionBeaconMoteDefName)); }
        }

        private static ThingDef ExtractionGuideMoteDef
        {
            get { return extractionGuideMoteDef ?? (extractionGuideMoteDef = DefDatabase<ThingDef>.GetNamedSilentFail(ExtractionGuideMoteDefName)); }
        }

        private static ThingDef ExtractionSafeRingMoteDef
        {
            get { return extractionSafeRingMoteDef ?? (extractionSafeRingMoteDef = DefDatabase<ThingDef>.GetNamedSilentFail(ExtractionSafeRingMoteDefName)); }
        }

        private static ThingDef ExtractionColumnMoteDef
        {
            get { return extractionColumnMoteDef ?? (extractionColumnMoteDef = DefDatabase<ThingDef>.GetNamedSilentFail(ExtractionColumnMoteDefName)); }
        }

        private static ThingDef RewardBeaconMoteDef
        {
            get { return rewardBeaconMoteDef ?? (rewardBeaconMoteDef = DefDatabase<ThingDef>.GetNamedSilentFail(RewardBeaconMoteDefName)); }
        }

        private static ThingDef RewardGuideMoteDef
        {
            get { return rewardGuideMoteDef ?? (rewardGuideMoteDef = DefDatabase<ThingDef>.GetNamedSilentFail(RewardGuideMoteDefName)); }
        }

        private static ThingDef RewardSanctumRingMoteDef
        {
            get { return rewardSanctumRingMoteDef ?? (rewardSanctumRingMoteDef = DefDatabase<ThingDef>.GetNamedSilentFail(RewardSanctumRingMoteDefName)); }
        }

        private static ThingDef RewardSanctumColumnMoteDef
        {
            get { return rewardSanctumColumnMoteDef ?? (rewardSanctumColumnMoteDef = DefDatabase<ThingDef>.GetNamedSilentFail(RewardSanctumColumnMoteDefName)); }
        }

        private static ThingDef RewardShardMoteDef
        {
            get { return rewardShardMoteDef ?? (rewardShardMoteDef = DefDatabase<ThingDef>.GetNamedSilentFail(RewardShardMoteDefName)); }
        }

        private static ThingDef GuidanceTrailMoteDef
        {
            get { return guidanceTrailMoteDef ?? (guidanceTrailMoteDef = DefDatabase<ThingDef>.GetNamedSilentFail(GuidanceTrailMoteDefName)); }
        }

        private static ThingDef EdgeInstabilityMoteDef
        {
            get { return edgeInstabilityMoteDef ?? (edgeInstabilityMoteDef = DefDatabase<ThingDef>.GetNamedSilentFail(EdgeInstabilityMoteDefName)); }
        }

        private static ThingDef WarningPulseMoteDef
        {
            get { return warningPulseMoteDef ?? (warningPulseMoteDef = DefDatabase<ThingDef>.GetNamedSilentFail(WarningPulseMoteDefName)); }
        }

        public static void SpawnCollapseStartBurst(IntVec3 heartCell, Map map)
        {
            if (!IsValid(heartCell, map))
            {
                return;
            }

            Vector3 pos = heartCell.ToVector3Shifted();
            SpawnStaticMote(pos, map, ShockwaveMoteDef, 4.25f);
            SpawnStaticMote(pos + new Vector3(0f, 0.003f, 0f), map, WarningPulseMoteDef, 3.15f);
            FleckMaker.ThrowLightningGlow(pos, map, 3.4f);
            FleckMaker.ThrowMicroSparks(pos, map);
            FleckMaker.ThrowMicroSparks(pos, map);
            FleckMaker.ThrowMicroSparks(pos, map);
            ABY_SoundUtility.PlayAt("ABY_SigilChargePulse", heartCell, map);
        }

        public static void SpawnHeartShockwave(IntVec3 heartCell, Map map, float urgency)
        {
            if (!IsValid(heartCell, map))
            {
                return;
            }

            Vector3 pos = heartCell.ToVector3Shifted();
            float scale = Mathf.Lerp(2.75f, 4.65f, Mathf.Clamp01(urgency));
            SpawnStaticMote(pos, map, ShockwaveMoteDef, scale);
            FleckMaker.ThrowLightningGlow(pos, map, Mathf.Lerp(1.35f, 2.45f, Mathf.Clamp01(urgency)));
        }

        public static void SpawnExtractionBeacon(IntVec3 extractionCell, Map map, float urgency)
        {
            if (!IsValid(extractionCell, map))
            {
                return;
            }

            float clampedUrgency = Mathf.Clamp01(urgency);
            Vector3 pos = extractionCell.ToVector3Shifted();
            SpawnStaticMote(pos, map, ExtractionSafeRingMoteDef, Mathf.Lerp(2.45f, 3.75f, clampedUrgency));
            SpawnStaticMote(pos + new Vector3(0f, 0.003f, 0f), map, ExtractionBeaconMoteDef, Mathf.Lerp(2.10f, 3.35f, clampedUrgency));
            SpawnStaticMote(pos + new Vector3(0f, 0.006f, 0f), map, ExtractionColumnMoteDef, Mathf.Lerp(1.65f, 2.85f, clampedUrgency));
            SpawnStaticMote(pos + new Vector3(0f, 0.009f, 0f), map, ExtractionGuideMoteDef, Mathf.Lerp(1.75f, 2.75f, clampedUrgency));

            if (Rand.Chance(0.42f + clampedUrgency * 0.40f))
            {
                FleckMaker.ThrowLightningGlow(pos, map, 1.85f + clampedUrgency * 1.45f);
            }

            if (Rand.Chance(0.18f + clampedUrgency * 0.20f))
            {
                FleckMaker.ThrowMicroSparks(pos, map);
            }
        }

        public static void SpawnRewardBeacon(IntVec3 rewardCell, Map map, float urgency)
        {
            if (!IsValid(rewardCell, map))
            {
                return;
            }

            Vector3 pos = rewardCell.ToVector3Shifted();
            float clampedUrgency = Mathf.Clamp01(urgency);
            SpawnStaticMote(pos, map, RewardSanctumRingMoteDef, Mathf.Lerp(2.20f, 3.60f, clampedUrgency));
            SpawnStaticMote(pos + new Vector3(0f, 0.002f, 0f), map, RewardBeaconMoteDef, Mathf.Lerp(1.80f, 2.85f, clampedUrgency));
            SpawnStaticMote(pos + new Vector3(0f, 0.005f, 0f), map, RewardGuideMoteDef, Mathf.Lerp(1.55f, 2.35f, clampedUrgency));
            SpawnStaticMote(pos + new Vector3(0f, 0.008f, 0f), map, RewardSanctumColumnMoteDef, Mathf.Lerp(1.40f, 2.30f, clampedUrgency));
            if (clampedUrgency >= 0.72f)
            {
                SpawnStaticMote(pos + new Vector3(0f, 0.010f, 0f), map, RewardSanctumRingMoteDef, Mathf.Lerp(1.10f, 1.70f, clampedUrgency));
            }

            SpawnRewardShards(pos, map, clampedUrgency);

            if (Rand.Chance(0.34f + clampedUrgency * 0.32f))
            {
                FleckMaker.ThrowLightningGlow(pos, map, 1.15f + clampedUrgency * 1.20f);
            }

            if (Rand.Chance(0.26f + clampedUrgency * 0.26f))
            {
                FleckMaker.ThrowMicroSparks(pos, map);
            }
        }

        public static void SpawnExtractionGuidance(IntVec3 focusCell, IntVec3 extractionCell, Map map, float urgency)
        {
            if (!IsValid(extractionCell, map))
            {
                return;
            }

            SpawnExtractionBeacon(extractionCell, map, urgency);
            if (IsValid(focusCell, map))
            {
                SpawnGuidanceTrail(focusCell, extractionCell, map, urgency, false);
            }
        }

        public static void SpawnRewardGuidance(IntVec3 rewardCell, IntVec3 extractionCell, Map map, float urgency)
        {
            if (!IsValid(rewardCell, map))
            {
                return;
            }

            SpawnRewardBeacon(rewardCell, map, urgency);
            if (IsValid(extractionCell, map))
            {
                SpawnGuidanceTrail(rewardCell, extractionCell, map, urgency, true);
            }
        }

        public static void SpawnCollapseWarningPulse(IntVec3 heartCell, Map map, float urgency)
        {
            if (!IsValid(heartCell, map))
            {
                return;
            }

            Vector3 pos = heartCell.ToVector3Shifted();
            SpawnStaticMote(pos, map, WarningPulseMoteDef, Mathf.Lerp(2.10f, 3.80f, Mathf.Clamp01(urgency)));
            if (urgency >= 0.80f)
            {
                ABY_SoundUtility.PlayAt("ABY_SigilChargePulse", heartCell, map);
            }
        }

        public static void SpawnEdgeInstability(Map map, float urgency)
        {
            if (map == null)
            {
                return;
            }

            int count = urgency >= 0.75f ? 5 : 3;
            for (int i = 0; i < count; i++)
            {
                IntVec3 cell;
                if (!TryFindEdgeCell(map, out cell))
                {
                    continue;
                }

                Vector3 pos = cell.ToVector3Shifted();
                SpawnStaticMote(pos, map, EdgeInstabilityMoteDef, Rand.Range(0.95f, 1.55f) + urgency * 0.40f);
                if (Rand.Chance(0.20f + urgency * 0.25f))
                {
                    FleckMaker.ThrowMicroSparks(pos, map);
                }
            }
        }

        private static void SpawnGuidanceTrail(IntVec3 from, IntVec3 to, Map map, float urgency, bool rewardToExit)
        {
            if (map == null || !from.IsValid || !to.IsValid)
            {
                return;
            }

            float clampedUrgency = Mathf.Clamp01(urgency);
            int count = rewardToExit ? (clampedUrgency >= 0.75f ? 7 : 5) : (clampedUrgency >= 0.75f ? 10 : 7);
            for (int i = 1; i <= count; i++)
            {
                float t = i / (float)(count + 1);
                IntVec3 cell = new IntVec3(
                    Mathf.RoundToInt(Mathf.Lerp(from.x, to.x, t)),
                    0,
                    Mathf.RoundToInt(Mathf.Lerp(from.z, to.z, t)));
                cell = ClampToMap(cell, map);
                if (!cell.IsValid || !cell.InBounds(map))
                {
                    continue;
                }

                Vector3 pos = cell.ToVector3Shifted();
                float scale = Mathf.Lerp(rewardToExit ? 0.88f : 0.82f, rewardToExit ? 1.28f : 1.36f, clampedUrgency);
                SpawnStaticMote(pos, map, GuidanceTrailMoteDef, scale);
                if (!rewardToExit && i == count)
                {
                    SpawnStaticMote(pos + new Vector3(0f, 0.003f, 0f), map, ExtractionGuideMoteDef, Mathf.Lerp(0.80f, 1.22f, clampedUrgency));
                }

                if (Rand.Chance((rewardToExit ? 0.18f : 0.16f) + clampedUrgency * 0.14f))
                {
                    FleckMaker.ThrowMicroSparks(pos, map);
                }
            }
        }

        private static void SpawnRewardShards(Vector3 center, Map map, float urgency)
        {
            ThingDef shardDef = RewardShardMoteDef;
            if (map == null || shardDef == null)
            {
                return;
            }

            int count = urgency >= 0.75f ? 3 : 2;
            for (int i = 0; i < count; i++)
            {
                float angle = (360f / count) * i + Rand.Range(-22f, 22f);
                float rad = angle * Mathf.Deg2Rad;
                float radius = Rand.Range(0.55f, 1.30f) + urgency * 0.30f;
                Vector3 pos = center + new Vector3(Mathf.Cos(rad) * radius, 0.004f + i * 0.001f, Mathf.Sin(rad) * radius);
                SpawnStaticMote(pos, map, shardDef, Rand.Range(0.68f, 1.08f) + urgency * 0.18f);
            }
        }

        private static bool TryFindEdgeCell(Map map, out IntVec3 cell)
        {
            cell = IntVec3.Invalid;
            if (map == null || map.Size.x <= 16 || map.Size.z <= 16)
            {
                return false;
            }

            for (int i = 0; i < 12; i++)
            {
                int side = Rand.RangeInclusive(0, 3);
                int x;
                int z;
                if (side == 0)
                {
                    x = Rand.RangeInclusive(7, map.Size.x - 8);
                    z = Rand.RangeInclusive(7, 12);
                }
                else if (side == 1)
                {
                    x = Rand.RangeInclusive(7, map.Size.x - 8);
                    z = Rand.RangeInclusive(map.Size.z - 13, map.Size.z - 8);
                }
                else if (side == 2)
                {
                    x = Rand.RangeInclusive(7, 12);
                    z = Rand.RangeInclusive(7, map.Size.z - 8);
                }
                else
                {
                    x = Rand.RangeInclusive(map.Size.x - 13, map.Size.x - 8);
                    z = Rand.RangeInclusive(7, map.Size.z - 8);
                }

                IntVec3 candidate = new IntVec3(x, 0, z);
                if (candidate.InBounds(map))
                {
                    cell = candidate;
                    return true;
                }
            }

            return false;
        }

        private static IntVec3 ClampToMap(IntVec3 cell, Map map)
        {
            if (map == null || !cell.IsValid)
            {
                return IntVec3.Invalid;
            }

            int x = System.Math.Max(6, System.Math.Min(map.Size.x - 7, cell.x));
            int z = System.Math.Max(6, System.Math.Min(map.Size.z - 7, cell.z));
            return new IntVec3(x, 0, z);
        }

        private static void SpawnStaticMote(Vector3 pos, Map map, ThingDef moteDef, float scale)
        {
            if (map == null || moteDef == null)
            {
                return;
            }

            MoteMaker.MakeStaticMote(pos, map, moteDef, scale);
        }

        private static bool IsValid(IntVec3 cell, Map map)
        {
            return map != null && cell.IsValid && cell.InBounds(map);
        }
    }
}
