using RimWorld;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    public static class ReactorChoirMinigunVfxUtility
    {
        private const string MuzzleMoteDefName = "ABY_Mote_ReactorChoirMuzzleFlash";
        private const string VentBurstMoteDefName = "ABY_Mote_ReactorChoirVentBurst";
        private const string MuzzleFramePrefix = "Things/VFX/ReactorChoirMinigun/ABY_ReactorChoirMuzzleFlash_";
        private const string VentBurstFramePrefix = "Things/VFX/ReactorChoirMinigun/ABY_ReactorChoirVentBurst_";

        private static ThingDef muzzleMoteDef;
        private static ThingDef ventBurstMoteDef;

        private static ThingDef MuzzleMoteDef => muzzleMoteDef ?? (muzzleMoteDef = DefDatabase<ThingDef>.GetNamedSilentFail(MuzzleMoteDefName));
        private static ThingDef VentBurstMoteDef => ventBurstMoteDef ?? (ventBurstMoteDef = DefDatabase<ThingDef>.GetNamedSilentFail(VentBurstMoteDefName));

        public static void SpawnMuzzle(Vector3 source, Vector3 target, Map map)
        {
            if (map == null)
            {
                return;
            }

            Vector3 direction = target - source;
            direction.y = 0f;
            if (direction.sqrMagnitude <= 0.0001f)
            {
                direction = Vector3.right;
            }
            direction.Normalize();

            Vector3 muzzlePosition = source + direction * 0.48f;
            float angle = DirectionAngle(direction);

            SpawnStatic(MuzzleMoteDef, muzzlePosition, map, MuzzleFramePrefix, 4, 0.54f, 0.32f, angle - 90f);
            if (ABY_VfxBudget.TrySpend(map, ABY_VfxBudgetCategory.CombatLight, 1))
            {
                FleckMaker.ThrowLightningGlow(muzzlePosition, map, 0.18f);
            }
        }

        public static void SpawnTravelSpark(Vector3 position, Map map)
        {
            if (map == null || !ABY_VfxBudget.TrySpend(map, ABY_VfxBudgetCategory.CombatLight, 1))
            {
                return;
            }

            FleckMaker.ThrowLightningGlow(position, map, 0.07f);
            if (Rand.Chance(0.08f))
            {
                FleckMaker.ThrowMicroSparks(position, map);
            }
        }

        public static void SpawnImpact(Vector3 position, Vector3 travelDirection, Map map, bool blockedByShield)
        {
            if (map == null || !ABY_VfxBudget.TrySpend(map, ABY_VfxBudgetCategory.CombatLight, 1))
            {
                return;
            }

            FleckMaker.ThrowLightningGlow(position, map, blockedByShield ? 0.24f : 0.34f);
            if (Rand.Chance(blockedByShield ? 0.10f : 0.20f))
            {
                FleckMaker.ThrowMicroSparks(position, map);
            }
        }

        public static void SpawnVentBurst(Vector3 position, Vector3 travelDirection, Map map)
        {
            if (map == null)
            {
                return;
            }

            float angle = DirectionAngle(travelDirection) - 90f;
            SpawnStatic(VentBurstMoteDef, position, map, VentBurstFramePrefix, 8, 0.76f, 0.52f, angle);
            if (ABY_VfxBudget.TrySpend(map, ABY_VfxBudgetCategory.CombatHeavy, 1))
            {
                FleckMaker.ThrowLightningGlow(position, map, 0.90f);
                FleckMaker.ThrowMicroSparks(position, map);
            }
        }

        private static void SpawnStatic(
            ThingDef moteDef,
            Vector3 position,
            Map map,
            string framePathPrefix,
            int lifetimeTicks,
            float drawSizeX,
            float drawSizeZ,
            float rotation)
        {
            if (moteDef == null || map == null)
            {
                return;
            }

            IntVec3 cell = position.ToIntVec3();
            if (!cell.InBounds(map))
            {
                return;
            }

            Mote_ABY_RiftNeedlerAnimated mote = ThingMaker.MakeThing(moteDef) as Mote_ABY_RiftNeedlerAnimated;
            if (mote == null)
            {
                return;
            }

            mote.framePathPrefix = framePathPrefix;
            mote.frameCount = 1;
            mote.ticksPerFrame = 1;
            mote.ticksLeft = Mathf.Max(1, lifetimeTicks);
            mote.startingTicks = mote.ticksLeft;
            mote.drawSizeX = drawSizeX;
            mote.drawSizeZ = drawSizeZ;
            mote.rotation = rotation;
            mote.exactPosition = position;

            GenSpawn.Spawn(mote, cell, map);
        }

        private static float DirectionAngle(Vector3 direction)
        {
            direction.y = 0f;
            if (direction.sqrMagnitude <= 0.0001f)
            {
                direction = Vector3.right;
            }
            direction.Normalize();
            return Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
        }
    }
}
