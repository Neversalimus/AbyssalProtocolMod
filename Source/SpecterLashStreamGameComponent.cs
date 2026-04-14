using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    public class SpecterLashStreamGameComponent : GameComponent
    {
        private const string WeaponDefName = "ABY_SpecterLashProjector";
        private const string BlobMoteDefName = "ABY_Mote_SpecterLashBlob";
        private const string CoreMoteDefName = "ABY_Mote_SpecterLashCore";
        private const string SparkMoteDefName = "ABY_Mote_SpecterLashSpark";
        private const string PulseSoundDefName = "ABY_SpecterLashPulse";
        private const string TailSoundDefName = "ABY_SpecterLashTail";

        private const int VisualIntervalTicks = 2;
        private const int DamageIntervalTicks = 10;
        private const int StreamDurationTicks = 78;
        private const float PulseDamage = 7f;
        private const float PulseArmorPenetration = 1.40f;
        private const float MaxStreamRange = 28.9f;
        private const float EndpointInset = 0.38f;
        private const float BaseAmplitude = 0.15f;
        private const float MaxAmplitude = 0.38f;

        private ThingDef blobMoteDef;
        private ThingDef coreMoteDef;
        private ThingDef sparkMoteDef;
        private readonly List<ActiveStream> activeStreams = new List<ActiveStream>();

        private sealed class ActiveStream
        {
            public int mapId;
            public int sourcePawnId;
            public int targetPawnId;
            public int expireTick;
            public int nextDamageTick;
            public int seed;
        }

        public SpecterLashStreamGameComponent(Game game)
        {
        }

        public void TryStartStream(Pawn source, Pawn target)
        {
            if (!CanStartStream(source, target))
            {
                return;
            }

            int ticksGame = Find.TickManager != null ? Find.TickManager.TicksGame : 0;

            for (int i = activeStreams.Count - 1; i >= 0; i--)
            {
                if (activeStreams[i].sourcePawnId == source.thingIDNumber)
                {
                    activeStreams.RemoveAt(i);
                }
            }

            activeStreams.Add(new ActiveStream
            {
                mapId = source.MapHeld.uniqueID,
                sourcePawnId = source.thingIDNumber,
                targetPawnId = target.thingIDNumber,
                expireTick = ticksGame + StreamDurationTicks,
                nextDamageTick = ticksGame,
                seed = source.thingIDNumber * 397 ^ target.thingIDNumber * 17
            });

            if (source.MapHeld != null)
            {
                ABY_SoundUtility.PlayAt(PulseSoundDefName, target.PositionHeld, source.MapHeld);
                FleckMaker.ThrowLightningGlow(target.DrawPos, source.MapHeld, 1.10f);
                FleckMaker.ThrowMicroSparks(target.DrawPos, source.MapHeld);
            }
        }

        public override void GameComponentTick()
        {
            if (activeStreams.Count <= 0 || Find.TickManager == null || Find.Maps == null)
            {
                return;
            }

            EnsureDefsLoaded();

            int ticksGame = Find.TickManager.TicksGame;
            for (int i = activeStreams.Count - 1; i >= 0; i--)
            {
                ActiveStream stream = activeStreams[i];
                Map map = FindMap(stream.mapId);
                Pawn source = FindPawn(map, stream.sourcePawnId);
                Pawn target = FindPawn(map, stream.targetPawnId);

                if (!CanContinueStream(source, target, ticksGame, stream.expireTick))
                {
                    PlayTailIfPossible(source, map);
                    activeStreams.RemoveAt(i);
                    continue;
                }

                if (ticksGame % VisualIntervalTicks == 0)
                {
                    SpawnBeamVisuals(map, source, target, stream.seed, ticksGame);
                }

                if (ticksGame >= stream.nextDamageTick)
                {
                    ApplyPulseDamage(source, target);
                    stream.nextDamageTick = ticksGame + DamageIntervalTicks;
                }
            }
        }

        private void EnsureDefsLoaded()
        {
            if (blobMoteDef == null)
            {
                blobMoteDef = DefDatabase<ThingDef>.GetNamedSilentFail(BlobMoteDefName);
            }

            if (coreMoteDef == null)
            {
                coreMoteDef = DefDatabase<ThingDef>.GetNamedSilentFail(CoreMoteDefName);
            }

            if (sparkMoteDef == null)
            {
                sparkMoteDef = DefDatabase<ThingDef>.GetNamedSilentFail(SparkMoteDefName);
            }
        }

        private static bool CanStartStream(Pawn source, Pawn target)
        {
            if (source == null || target == null || source.Dead || target.Dead)
            {
                return false;
            }

            if (!source.Spawned || !target.Spawned || source.MapHeld == null || target.MapHeld != source.MapHeld)
            {
                return false;
            }

            if (!GenHostility.HostileTo(source, target))
            {
                return false;
            }

            ThingWithComps primary = source.equipment?.Primary;
            if (primary?.def == null || primary.def.defName != WeaponDefName)
            {
                return false;
            }

            if (source.PositionHeld.DistanceTo(target.PositionHeld) > MaxStreamRange)
            {
                return false;
            }

            return GenSight.LineOfSight(source.PositionHeld, target.PositionHeld, source.MapHeld);
        }

        private static bool CanContinueStream(Pawn source, Pawn target, int ticksGame, int expireTick)
        {
            if (ticksGame >= expireTick)
            {
                return false;
            }

            if (source == null || target == null || source.Dead || target.Dead || target.Downed)
            {
                return false;
            }

            if (!source.Spawned || !target.Spawned || source.MapHeld == null || target.MapHeld != source.MapHeld)
            {
                return false;
            }

            if (source.Downed || source.stances?.stunner?.Stunned == true)
            {
                return false;
            }

            if (!GenHostility.HostileTo(source, target))
            {
                return false;
            }

            ThingWithComps primary = source.equipment?.Primary;
            if (primary?.def == null || primary.def.defName != WeaponDefName)
            {
                return false;
            }

            if (source.PositionHeld.DistanceTo(target.PositionHeld) > MaxStreamRange + 1.6f)
            {
                return false;
            }

            return GenSight.LineOfSight(source.PositionHeld, target.PositionHeld, source.MapHeld);
        }

        private void ApplyPulseDamage(Pawn source, Pawn target)
        {
            Map map = source.MapHeld;
            if (map == null)
            {
                return;
            }

            ThingDef weaponDef = source.equipment?.Primary?.def;
            DamageInfo damageInfo = new DamageInfo(
                DamageDefOf.Burn,
                PulseDamage,
                PulseArmorPenetration,
                -1f,
                source,
                null,
                weaponDef,
                DamageInfo.SourceCategory.ThingOrUnknown);

            target.TakeDamage(damageInfo);
            FleckMaker.ThrowLightningGlow(target.DrawPos, map, 0.76f);
            FleckMaker.ThrowMicroSparks(target.DrawPos, map);
            ABY_SoundUtility.PlayAt(PulseSoundDefName, target.PositionHeld, map);
        }

        private void SpawnBeamVisuals(Map map, Pawn source, Pawn target, int seed, int ticksGame)
        {
            if (map == null || blobMoteDef == null || coreMoteDef == null)
            {
                return;
            }

            Vector3 sourcePos = source.DrawPos;
            Vector3 targetPos = target.DrawPos;
            sourcePos.y = 0f;
            targetPos.y = 0f;

            Vector3 direction = targetPos - sourcePos;
            direction.y = 0f;
            float distance = direction.magnitude;
            if (distance <= 0.15f)
            {
                return;
            }

            Vector3 normal = direction / distance;
            Vector3 perpendicular = new Vector3(-normal.z, 0f, normal.x);
            sourcePos += normal * EndpointInset;
            targetPos -= normal * EndpointInset;

            int segmentCount = Mathf.Clamp(Mathf.CeilToInt(distance * 2.3f), 6, 12);
            float amplitude = Mathf.Lerp(BaseAmplitude, MaxAmplitude, Mathf.Clamp01(distance / 16f));
            float phaseBase = ticksGame * 0.32f + seed * 0.013f;

            for (int i = 0; i < segmentCount; i++)
            {
                float t = segmentCount == 1 ? 0f : i / (float)(segmentCount - 1);
                float envelope = Mathf.Sin(t * Mathf.PI);
                float sway = Mathf.Sin(phaseBase + t * 7.4f) * amplitude * envelope;
                float secondary = Mathf.Sin(phaseBase * 1.73f + t * 12.6f + 1.2f) * amplitude * 0.38f * envelope;

                Vector3 point = Vector3.Lerp(sourcePos, targetPos, t) + perpendicular * sway;
                point.y += secondary * 0.10f;

                float outerScale = Mathf.Lerp(0.26f, 0.54f, envelope);
                float coreScale = outerScale * 0.54f;
                MoteMaker.MakeStaticMote(point, map, blobMoteDef, outerScale);
                MoteMaker.MakeStaticMote(point + new Vector3(0f, 0.005f, 0f), map, coreMoteDef, coreScale);

                if (sparkMoteDef != null && i > 0 && i < segmentCount - 1 && ((i + ticksGame) % 3 == 0))
                {
                    MoteMaker.MakeStaticMote(point + perpendicular * (sway * 0.25f), map, sparkMoteDef, 0.22f + envelope * 0.12f);
                }
            }

            FleckMaker.ThrowLightningGlow(sourcePos, map, 0.28f);
            FleckMaker.ThrowLightningGlow(targetPos, map, 0.34f);
        }

        private static Map FindMap(int mapId)
        {
            List<Map> maps = Find.Maps;
            for (int i = 0; i < maps.Count; i++)
            {
                if (maps[i] != null && maps[i].uniqueID == mapId)
                {
                    return maps[i];
                }
            }

            return null;
        }

        private static Pawn FindPawn(Map map, int pawnId)
        {
            if (map?.mapPawns == null)
            {
                return null;
            }

            var pawns = map.mapPawns.AllPawnsSpawned;
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn pawn = pawns[i];
                if (pawn != null && pawn.thingIDNumber == pawnId)
                {
                    return pawn;
                }
            }

            return null;
        }

        private static void PlayTailIfPossible(Pawn source, Map fallbackMap)
        {
            Map map = source?.MapHeld ?? fallbackMap;
            if (map == null)
            {
                return;
            }

            IntVec3 cell = source?.PositionHeld ?? IntVec3.Invalid;
            if (!cell.IsValid)
            {
                return;
            }

            ABY_SoundUtility.PlayAt(TailSoundDefName, cell, map);
        }
    }
}
