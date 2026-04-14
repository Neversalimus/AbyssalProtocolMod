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

        private const int VisualIntervalTicks = 1;
        private const int DamageIntervalTicks = 10;
        private const int PawnStreamDurationTicks = 78;
        private const int ThingStreamDurationTicks = 54;
        private const int PointStreamDurationTicks = 20;
        private const float PulseDamage = 7f;
        private const float PulseArmorPenetration = 1.40f;
        private const float MaxStreamRange = 28.9f;
        private const float EndpointInset = 0.28f;
        private const float BaseAmplitude = 0.045f;
        private const float MaxAmplitude = 0.16f;

        private ThingDef blobMoteDef;
        private ThingDef coreMoteDef;
        private ThingDef sparkMoteDef;
        private readonly List<ActiveStream> activeStreams = new List<ActiveStream>();

        private sealed class ActiveStream
        {
            public int mapId;
            public int sourcePawnId;
            public int targetThingId = -1;
            public int expireTick;
            public int nextDamageTick;
            public int seed;
            public bool damageEnabled;
            public Vector3 staticTargetPos;
        }

        public SpecterLashStreamGameComponent(Game game)
        {
        }

        public void TryStartStream(Pawn source, Thing target, Vector3 fallbackTargetPos)
        {
            if (!CanStartSourceStream(source))
            {
                return;
            }

            Vector3 targetPos = target != null ? target.DrawPos : fallbackTargetPos;
            if (!CanUseTargetPos(source, targetPos))
            {
                return;
            }

            int ticksGame = Find.TickManager != null ? Find.TickManager.TicksGame : 0;
            RemoveExistingStreamFor(source);

            activeStreams.Add(new ActiveStream
            {
                mapId = source.MapHeld.uniqueID,
                sourcePawnId = source.thingIDNumber,
                targetThingId = target != null ? target.thingIDNumber : -1,
                expireTick = ticksGame + (target is Pawn ? PawnStreamDurationTicks : ThingStreamDurationTicks),
                nextDamageTick = ticksGame,
                seed = source.thingIDNumber * 397 ^ (target != null ? target.thingIDNumber : fallbackTargetPos.GetHashCode()) * 17,
                damageEnabled = CanDamageTarget(source, target),
                staticTargetPos = targetPos
            });

            if (source.MapHeld != null)
            {
                ABY_SoundUtility.PlayAt(PulseSoundDefName, targetPos.ToIntVec3(), source.MapHeld);
                FleckMaker.ThrowLightningGlow(targetPos, source.MapHeld, 0.54f);
                FleckMaker.ThrowMicroSparks(targetPos, source.MapHeld);
            }
        }

        public void TryStartStreamToPoint(Pawn source, Vector3 targetPos, bool blockedByShield)
        {
            if (!CanStartSourceStream(source) || !CanUseTargetPos(source, targetPos))
            {
                return;
            }

            int ticksGame = Find.TickManager != null ? Find.TickManager.TicksGame : 0;
            RemoveExistingStreamFor(source);

            activeStreams.Add(new ActiveStream
            {
                mapId = source.MapHeld.uniqueID,
                sourcePawnId = source.thingIDNumber,
                targetThingId = -1,
                expireTick = ticksGame + PointStreamDurationTicks,
                nextDamageTick = ticksGame + DamageIntervalTicks,
                seed = source.thingIDNumber * 397 ^ targetPos.GetHashCode() * 17,
                damageEnabled = false,
                staticTargetPos = targetPos
            });

            if (source.MapHeld != null)
            {
                FleckMaker.ThrowLightningGlow(targetPos, source.MapHeld, blockedByShield ? 0.40f : 0.48f);
                FleckMaker.ThrowMicroSparks(targetPos, source.MapHeld);
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
                if (!CanContinueSourceStream(source, stream.staticTargetPos, ticksGame, stream.expireTick))
                {
                    PlayTailIfPossible(source, map);
                    activeStreams.RemoveAt(i);
                    continue;
                }

                Thing target = FindThing(map, stream.targetThingId);
                bool isTrackingPawn = false;
                if (target != null && CanUseTrackedTarget(source, target))
                {
                    stream.staticTargetPos = target.DrawPos;
                    stream.damageEnabled = CanDamageTarget(source, target);
                    isTrackingPawn = target is Pawn;
                }
                else
                {
                    stream.targetThingId = -1;
                    stream.damageEnabled = false;
                    target = null;
                }

                if (!CanUseTargetPos(source, stream.staticTargetPos))
                {
                    PlayTailIfPossible(source, map);
                    activeStreams.RemoveAt(i);
                    continue;
                }

                if (ticksGame % VisualIntervalTicks == 0)
                {
                    SpawnBeamVisuals(map, source, stream.staticTargetPos, stream.seed, ticksGame, isTrackingPawn);
                }

                if (target != null && stream.damageEnabled && ticksGame >= stream.nextDamageTick)
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

        private static bool CanStartSourceStream(Pawn source)
        {
            if (source == null || source.Dead || !source.Spawned || source.MapHeld == null)
            {
                return false;
            }

            ThingWithComps primary = source.equipment != null ? source.equipment.Primary : null;
            return primary != null && primary.def != null && primary.def.defName == WeaponDefName;
        }

        private static bool CanContinueSourceStream(Pawn source, Vector3 targetPos, int ticksGame, int expireTick)
        {
            if (ticksGame >= expireTick || !CanStartSourceStream(source))
            {
                return false;
            }

            if (source.Downed || (source.stances != null && source.stances.stunner != null && source.stances.stunner.Stunned))
            {
                return false;
            }

            return CanUseTargetPos(source, targetPos);
        }

        private static bool CanUseTrackedTarget(Pawn source, Thing target)
        {
            if (source == null || target == null || target == source || target.Destroyed || !target.Spawned)
            {
                return false;
            }

            if (target.MapHeld != source.MapHeld)
            {
                return false;
            }

            return CanUseTargetPos(source, target.DrawPos);
        }

        private static bool CanUseTargetPos(Pawn source, Vector3 targetPos)
        {
            if (source == null || source.MapHeld == null)
            {
                return false;
            }

            Vector3 sourcePos = source.DrawPos;
            sourcePos.y = 0f;
            targetPos.y = 0f;
            if ((targetPos - sourcePos).magnitude > MaxStreamRange + 1.8f)
            {
                return false;
            }

            IntVec3 sourceCell = source.PositionHeld;
            IntVec3 targetCell = targetPos.ToIntVec3();
            if (!targetCell.IsValid)
            {
                return false;
            }

            return GenSight.LineOfSight(sourceCell, targetCell, source.MapHeld);
        }

        private static bool CanDamageTarget(Pawn source, Thing target)
        {
            return source != null
                && target != null
                && target != source
                && !target.Destroyed
                && target.Spawned
                && target.def != null
                && target.def.useHitPoints;
        }

        private void ApplyPulseDamage(Pawn source, Thing target)
        {
            Map map = source != null ? source.MapHeld : null;
            if (map == null || target == null || target.Destroyed)
            {
                return;
            }

            ThingDef weaponDef = source.equipment != null && source.equipment.Primary != null ? source.equipment.Primary.def : null;
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

            Vector3 impactPos = target.DrawPos;
            FleckMaker.ThrowLightningGlow(impactPos, map, target is Pawn ? 0.56f : 0.44f);
            FleckMaker.ThrowMicroSparks(impactPos, map);
            if (target is Pawn)
            {
                FleckMaker.ThrowMicroSparks(impactPos, map);
            }

            IntVec3 impactCell = target.PositionHeld.IsValid ? target.PositionHeld : impactPos.ToIntVec3();
            ABY_SoundUtility.PlayAt(PulseSoundDefName, impactCell, map);
        }

        private void SpawnBeamVisuals(Map map, Pawn source, Vector3 rawTargetPos, int seed, int ticksGame, bool isTrackingPawn)
        {
            if (map == null || blobMoteDef == null || coreMoteDef == null)
            {
                return;
            }

            Vector3 targetPos = rawTargetPos;
            targetPos.y = Altitudes.AltitudeFor(AltitudeLayer.MoteOverhead);
            Vector3 sourcePos = GetMuzzleSourcePos(source, targetPos);

            Vector3 direction = targetPos - sourcePos;
            direction.y = 0f;
            float distance = direction.magnitude;
            if (distance <= 0.12f)
            {
                return;
            }

            Vector3 normal = direction / distance;
            Vector3 perpendicular = new Vector3(-normal.z, 0f, normal.x);
            sourcePos += normal * EndpointInset;
            targetPos -= normal * EndpointInset;

            int segmentCount = Mathf.Clamp(Mathf.CeilToInt(distance * 7.5f), 16, 60);
            float amplitude = Mathf.Lerp(BaseAmplitude, MaxAmplitude, Mathf.Clamp01(distance / 18f));
            float phaseBase = ticksGame * 0.41f + seed * 0.013f;

            for (int i = 0; i < segmentCount; i++)
            {
                float t = segmentCount == 1 ? 0f : i / (float)(segmentCount - 1);
                float envelope = Mathf.Sin(t * Mathf.PI);
                float sway = Mathf.Sin(phaseBase + t * 7.8f) * amplitude * envelope;
                float secondary = Mathf.Sin(phaseBase * 1.73f + t * 11.6f + 1.2f) * amplitude * 0.18f * envelope;

                Vector3 point = Vector3.Lerp(sourcePos, targetPos, t) + perpendicular * sway;
                point.y = Altitudes.AltitudeFor(AltitudeLayer.MoteOverhead) + secondary * 0.03f;

                float outerScale = Mathf.Lerp(0.18f, 0.36f, envelope);
                float coreScale = outerScale * 0.58f;
                MoteMaker.MakeStaticMote(point, map, blobMoteDef, outerScale);
                MoteMaker.MakeStaticMote(point + new Vector3(0f, 0.0022f, 0f), map, coreMoteDef, coreScale);

                if (sparkMoteDef != null && i > 0 && i < segmentCount - 1 && ((i + ticksGame + seed) % 4 == 0))
                {
                    Vector3 sparkPoint = point + perpendicular * (sway * 0.14f);
                    sparkPoint.y = Altitudes.AltitudeFor(AltitudeLayer.MoteOverhead) + 0.0012f;
                    MoteMaker.MakeStaticMote(sparkPoint, map, sparkMoteDef, 0.14f + envelope * 0.08f);
                }
            }

            FleckMaker.ThrowLightningGlow(sourcePos, map, isTrackingPawn ? 0.18f : 0.12f);
            FleckMaker.ThrowLightningGlow(targetPos, map, isTrackingPawn ? 0.24f : 0.16f);
        }

        private static Vector3 GetMuzzleSourcePos(Pawn source, Vector3 targetPos)
        {
            Vector3 sourcePos = source.DrawPos;
            sourcePos.y = Altitudes.AltitudeFor(AltitudeLayer.MoteOverhead);

            Vector3 direction = targetPos - sourcePos;
            direction.y = 0f;
            if (direction.sqrMagnitude > 0.0001f)
            {
                direction.Normalize();
                Vector3 side = new Vector3(-direction.z, 0f, direction.x);
                sourcePos += direction * 0.42f + side * 0.05f;
            }

            return sourcePos;
        }

        private void RemoveExistingStreamFor(Pawn source)
        {
            for (int i = activeStreams.Count - 1; i >= 0; i--)
            {
                if (activeStreams[i].sourcePawnId == source.thingIDNumber)
                {
                    activeStreams.RemoveAt(i);
                }
            }
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
            if (pawnId < 0 || map == null || map.mapPawns == null)
            {
                return null;
            }

            IReadOnlyList<Pawn> pawns = map.mapPawns.AllPawnsSpawned;
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

        private static Thing FindThing(Map map, int thingId)
        {
            if (thingId < 0 || map == null)
            {
                return null;
            }

            Pawn pawn = FindPawn(map, thingId);
            if (pawn != null)
            {
                return pawn;
            }

            if (map.listerThings == null)
            {
                return null;
            }

            List<Thing> allThings = map.listerThings.AllThings;
            for (int i = 0; i < allThings.Count; i++)
            {
                Thing thing = allThings[i];
                if (thing != null && thing.thingIDNumber == thingId)
                {
                    return thing;
                }
            }

            return null;
        }

        private static void PlayTailIfPossible(Pawn source, Map fallbackMap)
        {
            Map map = source != null ? source.MapHeld : fallbackMap;
            if (map == null)
            {
                return;
            }

            IntVec3 cell = source != null ? source.PositionHeld : IntVec3.Invalid;
            if (!cell.IsValid)
            {
                return;
            }

            ABY_SoundUtility.PlayAt(TailSoundDefName, cell, map);
        }
    }
}
