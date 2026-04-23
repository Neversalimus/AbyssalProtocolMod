using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace AbyssalProtocol
{
    public static class CrownshardStormVfxUtility
    {
        private static SoundDef cachedImpactSound;

        public static void SpawnSeedImpact(Map map, Vector3 drawPos, bool shieldDampened)
        {
            if (map == null)
            {
                return;
            }

            FleckMaker.ThrowLightningGlow(drawPos, map, shieldDampened ? 1.25f : 1.65f);
            PlayImpactSound(drawPos.ToIntVec3(), map);
        }

        public static void SpawnStormOpen(IntVec3 cell, Map map, bool shieldDampened)
        {
            if (map == null)
            {
                return;
            }

            Vector3 loc = cell.ToVector3Shifted();
            FleckMaker.ThrowLightningGlow(loc, map, shieldDampened ? 1.55f : 2.05f);
        }

        public static void SpawnIdlePulse(IntVec3 cell, Map map, bool shieldDampened)
        {
            if (map == null)
            {
                return;
            }

            if (Rand.Chance(0.45f))
            {
                FleckMaker.ThrowLightningGlow(cell.ToVector3Shifted(), map, shieldDampened ? 0.55f : 0.75f);
            }
        }

        public static void SpawnPulseCore(IntVec3 cell, Map map, bool shieldDampened)
        {
            if (map == null)
            {
                return;
            }

            FleckMaker.ThrowLightningGlow(cell.ToVector3Shifted(), map, shieldDampened ? 0.85f : 1.15f);
        }

        public static void SpawnShardImpact(IntVec3 originCell, Thing target, bool denseTarget)
        {
            if (target == null || target.Map == null)
            {
                return;
            }

            Vector3 targetPos = target.DrawPos;
            FleckMaker.ThrowLightningGlow(targetPos, target.Map, denseTarget ? 1.05f : 0.72f);

            if (denseTarget)
            {
                FleckMaker.ThrowLightningGlow(originCell.ToVector3Shifted(), target.Map, 0.55f);
            }
        }

        public static void SpawnExecutionFlare(IntVec3 cell, Map map)
        {
            if (map == null)
            {
                return;
            }

            Vector3 loc = cell.ToVector3Shifted();
            FleckMaker.ThrowLightningGlow(loc, map, 1.55f);
            FleckMaker.ThrowLightningGlow(loc, map, 0.95f);
        }

        private static void PlayImpactSound(IntVec3 cell, Map map)
        {
            if (map == null)
            {
                return;
            }

            if (cachedImpactSound == null)
            {
                cachedImpactSound = DefDatabase<SoundDef>.GetNamedSilentFail("ABY_CrownshardStormcasterImpact");
            }

            if (cachedImpactSound != null)
            {
                cachedImpactSound.PlayOneShot(new TargetInfo(cell, map));
            }
        }
    }
}
