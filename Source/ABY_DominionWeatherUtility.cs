using System;
using RimWorld;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    public enum ABY_DominionWeatherState
    {
        Ashfall = 0,
        StaticVeil = 1,
        FurnaceDrift = 2
    }

    public static class ABY_DominionWeatherUtility
    {
        private const string AshMoteDefName = "ABY_Mote_DominionWeatherAsh";
        private const string StaticVeilMoteDefName = "ABY_Mote_DominionWeatherStaticVeil";
        private const string FurnaceDriftMoteDefName = "ABY_Mote_DominionWeatherFurnaceDrift";
        private const string PressurePulseDefName = "ABY_Mote_DominionSliceAmbientPressurePulse";

        private static ThingDef ashMoteDef;
        private static ThingDef staticVeilMoteDef;
        private static ThingDef furnaceDriftMoteDef;
        private static ThingDef pressurePulseDef;

        private static ThingDef AshMoteDef => ashMoteDef ?? (ashMoteDef = DefDatabase<ThingDef>.GetNamedSilentFail(AshMoteDefName));
        private static ThingDef StaticVeilMoteDef => staticVeilMoteDef ?? (staticVeilMoteDef = DefDatabase<ThingDef>.GetNamedSilentFail(StaticVeilMoteDefName));
        private static ThingDef FurnaceDriftMoteDef => furnaceDriftMoteDef ?? (furnaceDriftMoteDef = DefDatabase<ThingDef>.GetNamedSilentFail(FurnaceDriftMoteDefName));
        private static ThingDef PressurePulseDef => pressurePulseDef ?? (pressurePulseDef = DefDatabase<ThingDef>.GetNamedSilentFail(PressurePulseDefName));

        public static void EmitWeatherBurst(Map map, ABY_DominionWeatherState state, float intensity, bool reducedMotion)
        {
            if (map == null || Find.TickManager == null)
            {
                return;
            }

            float safeIntensity = Mathf.Clamp(intensity, 0.15f, 2.0f);
            switch (state)
            {
                case ABY_DominionWeatherState.StaticVeil:
                    EmitStaticVeil(map, safeIntensity, reducedMotion);
                    break;
                case ABY_DominionWeatherState.FurnaceDrift:
                    EmitFurnaceDrift(map, safeIntensity, reducedMotion);
                    break;
                default:
                    EmitAshfall(map, safeIntensity, reducedMotion);
                    break;
            }
        }

        private static void EmitAshfall(Map map, float intensity, bool reducedMotion)
        {
            int count = reducedMotion ? 1 : Mathf.Clamp(Mathf.RoundToInt(Rand.Range(2.2f, 4.8f) * intensity), 1, 7);
            for (int i = 0; i < count; i++)
            {
                if (!TryFindWeatherCell(map, out IntVec3 cell))
                {
                    continue;
                }

                Vector3 pos = CellToMotePos(cell, Rand.Range(0.070f, 0.105f));
                pos.x += Rand.Range(-0.42f, 0.42f);
                pos.z += Rand.Range(-0.42f, 0.42f);
                SpawnStaticMote(AshMoteDef, pos, map, Rand.Range(0.28f, 0.58f) * Mathf.Lerp(0.85f, 1.22f, Mathf.Clamp01(intensity - 0.5f)));

                if (!reducedMotion && Rand.Chance(0.20f * intensity))
                {
                    FleckMaker.ThrowDustPuff(pos, map, Rand.Range(0.24f, 0.46f));
                }
            }
        }

        private static void EmitStaticVeil(Map map, float intensity, bool reducedMotion)
        {
            IntVec3 focus;
            if (!ABY_DominionAtmosphereUtility.TryFindFocusCell(map, out focus))
            {
                focus = map.Center;
            }

            int count = reducedMotion ? 1 : Mathf.Clamp(Mathf.RoundToInt(Rand.Range(1.0f, 2.2f) * intensity), 1, 4);
            for (int i = 0; i < count; i++)
            {
                if (!ABY_DominionAtmosphereUtility.TryFindAtmosphereCellNear(map, focus, 10, 48, out IntVec3 cell))
                {
                    continue;
                }

                Vector3 pos = CellToMotePos(cell, Rand.Range(0.055f, 0.095f));
                SpawnStaticMote(StaticVeilMoteDef, pos, map, Rand.Range(1.10f, 2.35f) * Mathf.Lerp(0.85f, 1.25f, intensity * 0.5f));

                if (!reducedMotion && Rand.Chance(0.12f * intensity))
                {
                    FleckMaker.ThrowLightningGlow(pos, map, Rand.Range(0.10f, 0.22f));
                }
            }
        }

        private static void EmitFurnaceDrift(Map map, float intensity, bool reducedMotion)
        {
            int count = reducedMotion ? 1 : Mathf.Clamp(Mathf.RoundToInt(Rand.Range(1.2f, 2.8f) * intensity), 1, 5);
            for (int i = 0; i < count; i++)
            {
                IntVec3 cell;
                if (!TryFindPeripheralWeatherCell(map, out cell) && !TryFindWeatherCell(map, out cell))
                {
                    continue;
                }

                Vector3 pos = CellToMotePos(cell, Rand.Range(0.060f, 0.100f));
                SpawnStaticMote(FurnaceDriftMoteDef, pos, map, Rand.Range(0.62f, 1.18f) * Mathf.Lerp(0.80f, 1.20f, intensity * 0.5f));

                if (!reducedMotion && Rand.Chance(0.28f * intensity))
                {
                    FleckMaker.ThrowHeatGlow(cell, map, Rand.Range(0.18f, 0.42f));
                }
            }

            if (!reducedMotion && Rand.Chance(0.055f * intensity))
            {
                IntVec3 focus;
                if (ABY_DominionAtmosphereUtility.TryFindFocusCell(map, out focus))
                {
                    Vector3 pulsePos = CellToMotePos(focus, 0.045f);
                    SpawnStaticMote(PressurePulseDef, pulsePos, map, Rand.Range(2.8f, 4.2f));
                }
            }
        }

        private static bool TryFindWeatherCell(Map map, out IntVec3 cell)
        {
            cell = IntVec3.Invalid;
            if (map == null)
            {
                return false;
            }

            IntVec3 focus;
            if (!ABY_DominionAtmosphereUtility.TryFindFocusCell(map, out focus))
            {
                focus = map.Center;
            }

            for (int i = 0; i < 20; i++)
            {
                IntVec3 candidate;
                if (!ABY_DominionAtmosphereUtility.TryFindAtmosphereCellNear(map, focus, 6, 54, out candidate))
                {
                    continue;
                }

                if (IsValidWeatherCell(map, candidate))
                {
                    cell = candidate;
                    return true;
                }
            }

            return CellFinder.TryFindRandomCell(map, c => IsValidWeatherCell(map, c), out cell);
        }

        private static bool TryFindPeripheralWeatherCell(Map map, out IntVec3 cell)
        {
            cell = IntVec3.Invalid;
            if (map == null || map.Size.x <= 20 || map.Size.z <= 20)
            {
                return false;
            }

            IntVec3 center = map.Center;
            for (int i = 0; i < 24; i++)
            {
                float angle = Rand.Range(0f, 360f) * Mathf.Deg2Rad;
                float radius = Rand.Range(Mathf.Min(map.Size.x, map.Size.z) * 0.32f, Mathf.Min(map.Size.x, map.Size.z) * 0.47f);
                int x = Mathf.Clamp(center.x + GenMath.RoundRandom(Mathf.Cos(angle) * radius), 8, map.Size.x - 9);
                int z = Mathf.Clamp(center.z + GenMath.RoundRandom(Mathf.Sin(angle) * radius), 8, map.Size.z - 9);
                IntVec3 candidate = new IntVec3(x, 0, z);
                if (IsValidWeatherCell(map, candidate))
                {
                    cell = candidate;
                    return true;
                }
            }

            return false;
        }

        private static bool IsValidWeatherCell(Map map, IntVec3 cell)
        {
            if (map == null || !cell.InBounds(map) || cell.Fogged(map))
            {
                return false;
            }

            if (cell.x < 6 || cell.z < 6 || cell.x > map.Size.x - 7 || cell.z > map.Size.z - 7)
            {
                return false;
            }

            TerrainDef terrain = map.terrainGrid?.TerrainAt(cell);
            if (terrain == null || terrain.IsWater)
            {
                return false;
            }

            Building building = cell.GetEdifice(map);
            return building == null || building.def == null || building.def.passability != Traversability.Impassable;
        }

        private static void SpawnStaticMote(ThingDef moteDef, Vector3 pos, Map map, float scale)
        {
            if (moteDef == null || map == null)
            {
                return;
            }

            MoteMaker.MakeStaticMote(pos, map, moteDef, Mathf.Max(0.05f, scale));
        }

        private static Vector3 CellToMotePos(IntVec3 cell, float altitudeOffset)
        {
            return new Vector3(cell.x + 0.5f, AltitudeLayer.MoteOverhead.AltitudeFor() + altitudeOffset, cell.z + 0.5f);
        }
    }
}
