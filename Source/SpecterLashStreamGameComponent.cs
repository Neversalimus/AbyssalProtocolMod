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
        private const int PawnStreamDurationTicks = 90;
        private const int PointStreamDurationTicks = 24;
        private const float PulseDamage = 8f;
        private const float PulseArmorPenetration = 0.24f;
        private const float MaxStreamRange = 28.9f;
        private const float EndpointInset = 0.34f;
        private const float BaseAmplitude = 0.20f;
        private const float MaxAmplitude = 0.48f;

        private ThingDef blobMoteDef;
        private ThingDef coreMoteDef;
        private ThingDef sparkMoteDef;
        private readonly List<ActiveStream> activeStreams = new List<ActiveStream>();

        private sealed class ActiveStream
        {
            public int mapId;
            public int sourcePawnId;
            public int targetPawnId = -1;
            public int expireTick;
            public int nextDamageTick;
            public int seed;
            public bool damageEnabled;
            public Vector3 staticTargetPos;
        }

        public SpecterLashStreamGameComponent(Game game)
        {
        }

        public void TryStartStream(Pawn source, Pawn target, Vector3 fallbackTargetPos)
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

            bool damageEnabled = target != null && GenHostility.HostileTo(source, target);

            activeStreams.Add(new ActiveStream
            {
                mapId = source.MapHeld.uniqueID,
                sourcePawnId = source.thingIDNumber,
                targetPawnId = target?.thingIDNumber ?? -1,
                expireTick = ticksGame + PawnStreamDurationTicks,
                nextDamageTick = ticksGame + Mathf.Max(4, DamageIntervalTicks / 2),
                seed = source.thingIDNumber * 397 ^ (target?.thingIDNumber ?? fallbackTargetPos.GetHashCode()) * 17,
                damageEnabled = damageEnabled,
                staticTargetPos = targetPos
            });

            if (source.MapHeld != null)
            {
                ABY_SoundUtility.PlayAt(PulseSoundDefName, targetPos.ToIntVec3(), source.MapHeld);
                FleckMaker.ThrowLightningGlow(targetPos, source.MapHeld, 0.95f);
                FleckMaker.ThrowMicroSparks(targetPos, source.MapHeld);
            }

            if (damageEnabled && target != null)
            {
                ApplyPulseDamage(source, target);
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
                targetPawnId = -1,
                expireTick = ticksGame + PointStreamDurationTicks,
                nextDamageTick = ticksGame + DamageIntervalTicks,
                seed = source.thingIDNumber * 397 ^ targetPos.GetHashCode() * 17,
                damageEnabled = false,
                staticTargetPos = targetPos
            });

            if (source.MapHeld != null)
            {
                FleckMaker.ThrowLightningGlow(targetPos, source.MapHeld, blockedByShield ? 0.88f : 1.02f);
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

                Pawn target = FindPawn(map, stream.targetPawnId);
                if (target != null && CanUseTrackedTarget(source, target))
                {
                    stream.staticTargetPos = target.DrawPos;
                    stream.damageEnabled = GenHostility.HostileTo(source, target);
                }
                else
                {
                    stream.targetPawnId = -1;
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
                    SpawnBeamVisuals(map, source, stream.staticTargetPos, stream.seed, ticksGame, target != null);
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

            ThingWithComps primary = source.equipment?.Primary;
            return primary?.def != null && primary.def.defName == WeaponDefName;
        }

        private static bool CanContinueSourceStream(Pawn source, Vector3 targetPos, int ticksGame, int expireTick)
        {
            if (ticksGame >= expireTick || !CanStartSourceStream(source))
            {
                return false;
            }

            if (source.Downed || source.stances?.stunner?.Stunned == true)
            {
                return false;
            }

            return CanUseTargetPos(source, targetPos);
        }

        private static bool CanUseTrackedTarget(Pawn source, Pawn target)
        {
            if (source == null || target == null || target.Dead || !target.Spawned)
            {
                return false;
            }

            if (target.MapHeld != source.MapHeld)
            {
                return false;
            }

            Vector3 sourcePos = source.DrawPos;
            Vector3 targetPos = target.DrawPos;
            sourcePos.y = 0f;
            targetPos.y = 0f;
            return (targetPos - sourcePos).magnitude <= MaxStreamRange + 1.8f;
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

            IntVec3 targetCell = targetPos.ToIntVec3();
            return targetCell.IsValid && targetCell.InBounds(source.MapHeld);
        }

        private void ApplyPulseDamage(Pawn source, Pawn target)
        {
            Map map = source.MapHeld;
            if (map == null || target == null || target.Dead)
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
            FleckMaker.ThrowLightningGlow(target.DrawPos, map, 0.62f);
            FleckMaker.ThrowMicroSparks(target.DrawPos, map);
            FleckMaker.ThrowMicroSparks(target.DrawPos, map);
            ABY_SoundUtility.PlayAt(PulseSoundDefName, target.PositionHeld, map);
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

            int segmentCount = Mathf.Clamp(Mathf.CeilToInt(distance * 3.4f), 9, 18);
            float amplitude = Mathf.Lerp(BaseAmplitude, MaxAmplitude, Mathf.Clamp01(distance / 14f));
            float phaseBase = ticksGame * 0.47f + seed * 0.019f;

            for (int i = 0; i < segmentCount; i++)
            {
                float t = segmentCount == 1 ? 0f : i / (float)(segmentCount - 1);
                float envelope = Mathf.Sin(t * Mathf.PI);
                float sway = Mathf.Sin(phaseBase + t * 8.2f) * amplitude * envelope;
                float secondary = Mathf.Sin(phaseBase * 1.91f + t * 13.6f + 1.2f) * amplitude * 0.42f * envelope;

                Vector3 point = Vector3.Lerp(sourcePos, targetPos, t) + perpendicular * sway;
                point.y = Altitudes.AltitudeFor(AltitudeLayer.MoteOverhead) + secondary * 0.06f;

                float outerScale = Mathf.Lerp(0.60f, 1.08f, envelope);
                float coreScale = outerScale * 0.60f;
                MoteMaker.MakeStaticMote(point, map, blobMoteDef, outerScale);
                MoteMaker.MakeStaticMote(point + new Vector3(0f, 0.0035f, 0f), map, coreMoteDef, coreScale);

                if (sparkMoteDef != null && i > 0 && i < segmentCount - 1 && ((i + ticksGame + seed) % 2 == 0))
                {
                    Vector3 sparkPoint = point + perpendicular * (sway * 0.22f);
                    sparkPoint.y = Altitudes.AltitudeFor(AltitudeLayer.MoteOverhead) + 0.002f;
                    MoteMaker.MakeStaticMote(sparkPoint, map, sparkMoteDef, 0.36f + envelope * 0.22f);
                }
            }

            FleckMaker.ThrowLightningGlow(sourcePos, map, isTrackingPawn ? 0.46f : 0.34f);
            FleckMaker.ThrowLightningGlow(targetPos, map, isTrackingPawn ? 0.62f : 0.42f);
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
                sourcePos += direction * 0.46f + side * 0.06f;
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
            if (pawnId < 0 || map?.mapPawns == null)
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
