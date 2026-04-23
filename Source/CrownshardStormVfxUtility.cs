using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace AbyssalProtocol
{
    public static class CrownshardStormVfxUtility
    {
        private const string CoreMoteDefName = "ABY_Mote_CrownshardStormCore";
        private const string RingMoteDefName = "ABY_Mote_CrownshardStormRing";
        private const string ShardsMoteDefName = "ABY_Mote_CrownshardStormShards";
        private const string PulseMoteDefName = "ABY_Mote_CrownshardStormPulse";
        private const string ImpactMoteDefName = "ABY_Mote_CrownshardStormImpact";
        private const string ExecutionMoteDefName = "ABY_Mote_CrownshardStormExecution";

        private const string ImpactSoundDefName = "ABY_CrownshardStormcasterImpact";
        private const string PulseSoundDefName = "ABY_CrownshardStormcasterPulse";
        private const string ExecutionSoundDefName = "ABY_CrownshardStormcasterExecution";

        private static ThingDef coreMoteDef;
        private static ThingDef ringMoteDef;
        private static ThingDef shardsMoteDef;
        private static ThingDef pulseMoteDef;
        private static ThingDef impactMoteDef;
        private static ThingDef executionMoteDef;

        private static SoundDef impactSoundDef;
        private static SoundDef pulseSoundDef;
        private static SoundDef executionSoundDef;

        private static int lastPulseSoundTick = -99999;
        private static int lastPulseSoundMapId = -1;

        public static void SpawnSeedTrail(Vector3 from, Vector3 to, Map map)
        {
            if (map == null)
            {
                return;
            }

            EnsureDefsLoaded();

            for (int i = 1; i <= 2; i++)
            {
                float t = i / 3f;
                Vector3 point = Vector3.Lerp(from, to, t);
                MakeMote(point, map, shardsMoteDef, 0.34f + Rand.Value * 0.12f);

                if (i == 2 && Rand.Chance(0.45f))
                {
                    FleckMaker.ThrowLightningGlow(point, map, 0.22f);
                }
            }
        }

        public static void SpawnSeedImpact(Map map, Vector3 drawPos, bool shieldDampened)
        {
            if (map == null)
            {
                return;
            }

            EnsureDefsLoaded();

            MakeMote(drawPos, map, impactMoteDef, shieldDampened ? 0.90f : 1.15f);
            MakeMote(drawPos + RandomHorizontalOffset(0.05f), map, ringMoteDef, shieldDampened ? 0.88f : 1.16f);
            FleckMaker.ThrowLightningGlow(drawPos, map, shieldDampened ? 1.25f : 1.65f);
            FleckMaker.ThrowMicroSparks(drawPos, map);
            PlaySound(impactSoundDef, ImpactSoundDefName, drawPos.ToIntVec3(), map);
        }

        public static void SpawnStormOpen(IntVec3 cell, Map map, bool shieldDampened)
        {
            if (map == null)
            {
                return;
            }

            EnsureDefsLoaded();

            Vector3 loc = cell.ToVector3Shifted();
            MakeMote(loc, map, ringMoteDef, shieldDampened ? 1.55f : 2.15f);
            MakeMote(loc + RandomHorizontalOffset(0.07f), map, coreMoteDef, shieldDampened ? 0.80f : 1.08f);
            MakeMote(loc + RandomHorizontalOffset(0.13f), map, pulseMoteDef, shieldDampened ? 1.25f : 1.70f);
            FleckMaker.ThrowLightningGlow(loc, map, shieldDampened ? 1.55f : 2.05f);
            PlayPulseSound(cell, map, true);
        }

        public static void SpawnNodeSustain(IntVec3 cell, Map map, int ageTicks, int durationTicks, bool shieldDampened)
        {
            if (map == null)
            {
                return;
            }

            EnsureDefsLoaded();

            Vector3 loc = cell.ToVector3Shifted();
            float remaining = durationTicks > 0 ? Mathf.Clamp01(1f - (float)ageTicks / durationTicks) : 0.5f;
            float pulse = 0.72f + 0.20f * Mathf.Sin(ageTicks * 0.19f);
            float scale = (shieldDampened ? 0.52f : 0.68f) * Mathf.Lerp(0.75f, 1.06f, remaining) * pulse;

            MakeMote(loc + RandomHorizontalOffset(0.10f), map, coreMoteDef, scale);

            if (Rand.Chance(shieldDampened ? 0.24f : 0.40f))
            {
                MakeMote(loc + RandomHorizontalOffset(0.24f), map, shardsMoteDef, 0.30f + Rand.Value * 0.16f);
            }
        }

        public static void SpawnIdlePulse(IntVec3 cell, Map map, bool shieldDampened)
        {
            if (map == null)
            {
                return;
            }

            EnsureDefsLoaded();

            Vector3 loc = cell.ToVector3Shifted();
            MakeMote(loc, map, pulseMoteDef, shieldDampened ? 0.78f : 1.02f);

            if (Rand.Chance(0.45f))
            {
                FleckMaker.ThrowLightningGlow(loc, map, shieldDampened ? 0.55f : 0.75f);
            }
        }

        public static void SpawnPulseCore(IntVec3 cell, Map map, bool shieldDampened)
        {
            if (map == null)
            {
                return;
            }

            EnsureDefsLoaded();

            Vector3 loc = cell.ToVector3Shifted();
            MakeMote(loc, map, pulseMoteDef, shieldDampened ? 1.08f : 1.42f);
            MakeMote(loc + RandomHorizontalOffset(0.06f), map, ringMoteDef, shieldDampened ? 0.95f : 1.25f);
            FleckMaker.ThrowLightningGlow(loc, map, shieldDampened ? 0.85f : 1.15f);
            PlayPulseSound(cell, map, false);
        }

        public static void SpawnShardImpact(IntVec3 originCell, Thing target, bool denseTarget)
        {
            if (target == null || target.Map == null)
            {
                return;
            }

            EnsureDefsLoaded();

            Map map = target.Map;
            Vector3 origin = originCell.ToVector3Shifted();
            Vector3 targetPos = target.DrawPos;
            Vector3 direction = targetPos - origin;

            MakeMote(Vector3.Lerp(origin, targetPos, 0.42f) + RandomHorizontalOffset(0.05f), map, shardsMoteDef, denseTarget ? 0.58f : 0.44f);
            MakeMote(Vector3.Lerp(origin, targetPos, 0.72f) + RandomHorizontalOffset(0.04f), map, shardsMoteDef, denseTarget ? 0.50f : 0.38f);
            MakeMote(targetPos, map, impactMoteDef, denseTarget ? 0.95f : 0.68f);

            FleckMaker.ThrowLightningGlow(targetPos, map, denseTarget ? 1.05f : 0.72f);

            if (denseTarget)
            {
                MakeMote(targetPos + RandomHorizontalOffset(0.08f), map, ringMoteDef, 0.72f);
                FleckMaker.ThrowMicroSparks(targetPos, map);
                FleckMaker.ThrowLightningGlow(origin, map, 0.55f);
            }
            else if (Rand.Chance(0.35f))
            {
                FleckMaker.ThrowMicroSparks(targetPos, map);
            }
        }

        public static void SpawnExecutionFlare(IntVec3 cell, Map map)
        {
            if (map == null)
            {
                return;
            }

            EnsureDefsLoaded();

            Vector3 loc = cell.ToVector3Shifted();
            MakeMote(loc, map, executionMoteDef, 1.18f);
            MakeMote(loc + RandomHorizontalOffset(0.09f), map, ringMoteDef, 1.04f);
            MakeMote(loc + RandomHorizontalOffset(0.13f), map, shardsMoteDef, 0.84f);
            FleckMaker.ThrowLightningGlow(loc, map, 1.55f);
            FleckMaker.ThrowLightningGlow(loc, map, 0.95f);
            FleckMaker.ThrowMicroSparks(loc, map);
            PlaySound(executionSoundDef, ExecutionSoundDefName, cell, map);
        }

        public static void SpawnStormCollapse(IntVec3 cell, Map map, bool shieldDampened)
        {
            if (map == null)
            {
                return;
            }

            EnsureDefsLoaded();

            Vector3 loc = cell.ToVector3Shifted();
            MakeMote(loc, map, pulseMoteDef, shieldDampened ? 0.72f : 0.92f);
            if (!shieldDampened)
            {
                MakeMote(loc + RandomHorizontalOffset(0.08f), map, shardsMoteDef, 0.52f);
            }
        }

        private static void EnsureDefsLoaded()
        {
            if (coreMoteDef == null)
            {
                coreMoteDef = DefDatabase<ThingDef>.GetNamedSilentFail(CoreMoteDefName);
            }

            if (ringMoteDef == null)
            {
                ringMoteDef = DefDatabase<ThingDef>.GetNamedSilentFail(RingMoteDefName);
            }

            if (shardsMoteDef == null)
            {
                shardsMoteDef = DefDatabase<ThingDef>.GetNamedSilentFail(ShardsMoteDefName);
            }

            if (pulseMoteDef == null)
            {
                pulseMoteDef = DefDatabase<ThingDef>.GetNamedSilentFail(PulseMoteDefName);
            }

            if (impactMoteDef == null)
            {
                impactMoteDef = DefDatabase<ThingDef>.GetNamedSilentFail(ImpactMoteDefName);
            }

            if (executionMoteDef == null)
            {
                executionMoteDef = DefDatabase<ThingDef>.GetNamedSilentFail(ExecutionMoteDefName);
            }

            if (impactSoundDef == null)
            {
                impactSoundDef = DefDatabase<SoundDef>.GetNamedSilentFail(ImpactSoundDefName);
            }

            if (pulseSoundDef == null)
            {
                pulseSoundDef = DefDatabase<SoundDef>.GetNamedSilentFail(PulseSoundDefName);
            }

            if (executionSoundDef == null)
            {
                executionSoundDef = DefDatabase<SoundDef>.GetNamedSilentFail(ExecutionSoundDefName);
            }
        }

        private static void MakeMote(Vector3 position, Map map, ThingDef moteDef, float scale)
        {
            if (map == null || moteDef == null)
            {
                return;
            }

            MoteMaker.MakeStaticMote(position, map, moteDef, Mathf.Max(0.05f, scale));
        }

        private static void PlayPulseSound(IntVec3 cell, Map map, bool force)
        {
            if (map == null)
            {
                return;
            }

            int ticks = Find.TickManager != null ? Find.TickManager.TicksGame : 0;
            if (!force && lastPulseSoundMapId == map.uniqueID && ticks - lastPulseSoundTick < 55)
            {
                return;
            }

            lastPulseSoundMapId = map.uniqueID;
            lastPulseSoundTick = ticks;
            PlaySound(pulseSoundDef, PulseSoundDefName, cell, map);
        }

        private static void PlaySound(SoundDef cachedSoundDef, string defName, IntVec3 cell, Map map)
        {
            if (map == null)
            {
                return;
            }

            SoundDef soundDef = cachedSoundDef ?? DefDatabase<SoundDef>.GetNamedSilentFail(defName);
            if (soundDef != null)
            {
                soundDef.PlayOneShot(new TargetInfo(cell, map));
            }
        }

        private static Vector3 RandomHorizontalOffset(float radius)
        {
            if (radius <= 0f)
            {
                return Vector3.zero;
            }

            return new Vector3(Rand.Range(-radius, radius), 0f, Rand.Range(-radius, radius));
        }
    }
}
