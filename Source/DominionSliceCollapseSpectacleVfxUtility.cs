using RimWorld;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    public static class DominionSliceCollapseSpectacleVfxUtility
    {
        private const string ShockwaveMoteDefName = "ABY_Mote_DominionSliceCollapseShockwave";
        private const string ExtractionBeaconMoteDefName = "ABY_Mote_DominionSliceExtractionBeacon";
        private const string RewardBeaconMoteDefName = "ABY_Mote_DominionSliceRewardBeacon";
        private const string EdgeInstabilityMoteDefName = "ABY_Mote_DominionSliceEdgeInstability";
        private const string WarningPulseMoteDefName = "ABY_Mote_DominionSliceCollapseWarningPulse";

        private static ThingDef shockwaveMoteDef;
        private static ThingDef extractionBeaconMoteDef;
        private static ThingDef rewardBeaconMoteDef;
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

        private static ThingDef RewardBeaconMoteDef
        {
            get { return rewardBeaconMoteDef ?? (rewardBeaconMoteDef = DefDatabase<ThingDef>.GetNamedSilentFail(RewardBeaconMoteDefName)); }
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

            Vector3 pos = extractionCell.ToVector3Shifted();
            SpawnStaticMote(pos, map, ExtractionBeaconMoteDef, Mathf.Lerp(1.55f, 2.45f, Mathf.Clamp01(urgency)));
            if (Rand.Chance(0.25f + urgency * 0.28f))
            {
                FleckMaker.ThrowLightningGlow(pos, map, 1.25f + urgency);
            }
        }

        public static void SpawnRewardBeacon(IntVec3 rewardCell, Map map, float urgency)
        {
            if (!IsValid(rewardCell, map))
            {
                return;
            }

            Vector3 pos = rewardCell.ToVector3Shifted();
            SpawnStaticMote(pos, map, RewardBeaconMoteDef, Mathf.Lerp(1.25f, 2.05f, Mathf.Clamp01(urgency)));
            if (Rand.Chance(0.20f + urgency * 0.20f))
            {
                FleckMaker.ThrowMicroSparks(pos, map);
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
