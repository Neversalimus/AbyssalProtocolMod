using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    public class SpecterLashStreamGameComponent : GameComponent
    {
        private const string WeaponDefName = "ABY_SpecterLashProjector";
        private const string BeamHaloDefName = "ABY_Mote_SpecterLashBeamHalo";
        private const string BeamCoreDefName = "ABY_Mote_SpecterLashBeamCore";
        private const string SparkMoteDefName = "ABY_Mote_SpecterLashSpark";
        private const string PulseSoundDefName = "ABY_SpecterLashPulse";
        private const string TailSoundDefName = "ABY_SpecterLashTail";

        private const string BeamHaloTexturePath = "Things/VFX/SpecterLash/ABY_SpecterLash_StreamBlob";
        private const string BeamCoreTexturePath = "Things/VFX/SpecterLash/ABY_SpecterLash_StreamCore";

        private const int VisualIntervalTicks = 1;
        private const int DamageIntervalTicks = 10;
        private const int PawnStreamDurationTicks = 88;
        private const int PointStreamDurationTicks = 18;
        private const int BeamSegmentLifetimeTicks = 4;
        private const float PulseDamage = 16f;
        private const float PulseArmorPenetration = 0.24f;
        private const float MaxStreamRange = 28.9f;
        private const float EndpointInset = 0.34f;
        private const float BaseAmplitude = 0.18f;
        private const float MaxAmplitude = 0.54f;
        private const float SourceBreakPadding = 1.8f;

        private ThingDef beamHaloDef;
        private ThingDef beamCoreDef;
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
            public bool requireLineOfSight;
            public Vector3 staticTargetPos;
        }

        public SpecterLashStreamGameComponent(Game game)
        {
        }

        public static bool IsSpecterLashWeapon(ThingWithComps equipment)
        {
            return equipment?.def != null && equipment.def.defName == WeaponDefName;
        }

        public void TryStartStream(Pawn source, Pawn target, Vector3 fallbackTargetPos)
        {
            TryStartStream(source, target as Thing, fallbackTargetPos, true, true);
        }

        public void TryStartStream(Pawn source, Thing target, Vector3 fallbackTargetPos, bool allowDamage, bool requireLineOfSight)
        {
            if (!CanStartSourceStream(source))
            {
                return;
            }

            Vector3 targetPos = target != null && !target.Destroyed ? target.DrawPos : fallbackTargetPos;
            if (!CanUseTargetPos(source, targetPos))
            {
                return;
            }

            int ticksGame = Find.TickManager != null ? Find.TickManager.TicksGame : 0;
            RemoveExistingStreamFor(source);

            bool damageEnabled = allowDamage && CanDamageTarget(source, target);
            activeStreams.Add(new ActiveStream
            {
                mapId = source.MapHeld.uniqueID,
                sourcePawnId = source.thingIDNumber,
                targetThingId = target != null && !target.Destroyed ? target.thingIDNumber : -1,
                expireTick = ticksGame + (target is Pawn ? PawnStreamDurationTicks : PointStreamDurationTicks),
                nextDamageTick = ticksGame + Mathf.Max(3, DamageIntervalTicks / 2),
                seed = source.thingIDNumber * 397 ^ (target?.thingIDNumber ?? fallbackTargetPos.GetHashCode()) * 17,
                damageEnabled = damageEnabled,
                requireLineOfSight = requireLineOfSight,
                staticTargetPos = targetPos
            });

            if (source.MapHeld != null)
            {
                ABY_SoundUtility.PlayAt(PulseSoundDefName, targetPos.ToIntVec3(), source.MapHeld);
                FleckMaker.ThrowLightningGlow(targetPos, source.MapHeld, damageEnabled ? 0.88f : 0.54f);
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
                targetThingId = -1,
                expireTick = ticksGame + PointStreamDurationTicks,
                nextDamageTick = ticksGame + DamageIntervalTicks,
                seed = source.thingIDNumber * 397 ^ targetPos.GetHashCode() * 17,
                damageEnabled = false,
                requireLineOfSight = false,
                staticTargetPos = targetPos
            });

            if (source.MapHeld != null)
            {
                FleckMaker.ThrowLightningGlow(targetPos, source.MapHeld, blockedByShield ? 0.78f : 0.92f);
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
                if (target != null && CanUseTrackedTarget(source, target))
                {
                    stream.staticTargetPos = target.DrawPos;
                    stream.damageEnabled = CanDamageTarget(source, target);
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

                if (stream.requireLineOfSight && target != null && !HasLineOfSight(source, target))
                {
                    PlayTailIfPossible(source, map);
                    activeStreams.RemoveAt(i);
                    continue;
                }

                if (ticksGame % GetVisualIntervalTicks() == 0)
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

        private int GetVisualIntervalTicks()
        {
            int count = activeStreams != null ? activeStreams.Count : 0;
            if (count >= 6)
            {
                return 2;
            }

            return VisualIntervalTicks;
        }

        private int GetSegmentCap()
        {
            int count = activeStreams != null ? activeStreams.Count : 0;
            if (count >= 6)
            {
                return 7;
            }

            if (count >= 3)
            {
                return 9;
            }

            return 12;
        }

        private void EnsureDefsLoaded()
        {
            if (beamHaloDef == null)
            {
                beamHaloDef = DefDatabase<ThingDef>.GetNamedSilentFail(BeamHaloDefName);
            }

            if (beamCoreDef == null)
            {
                beamCoreDef = DefDatabase<ThingDef>.GetNamedSilentFail(BeamCoreDefName);
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

            return IsSpecterLashWeapon(source.equipment?.Primary);
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

        private static bool CanUseTrackedTarget(Pawn source, Thing target)
        {
            if (source == null || target == null || target.Destroyed || !target.Spawned)
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
            return (targetPos - sourcePos).magnitude <= MaxStreamRange + SourceBreakPadding;
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
            if ((targetPos - sourcePos).magnitude > MaxStreamRange + SourceBreakPadding)
            {
                return false;
            }

            IntVec3 targetCell = targetPos.ToIntVec3();
            return targetCell.IsValid && targetCell.InBounds(source.MapHeld);
        }

        private static bool CanDamageTarget(Pawn source, Thing target)
        {
            if (source == null || target == null || target == source || target.Destroyed || !target.Spawned || target.def == null)
            {
                return false;
            }

            if (target.def.category == ThingCategory.Mote || target.def.category == ThingCategory.Projectile || target is Fire)
            {
                return false;
            }

            Pawn targetPawn = target as Pawn;
            if (targetPawn != null)
            {
                return !targetPawn.Dead && !targetPawn.Destroyed;
            }

            if (!target.def.useHitPoints)
            {
                return false;
            }

            if (target.Faction != null && source.Faction != null && target.Faction == source.Faction)
            {
                return false;
            }

            return true;
        }

        private static bool HasLineOfSight(Pawn source, Thing target)
        {
            if (source?.MapHeld == null || target == null || !target.Spawned)
            {
                return false;
            }

            IntVec3 sourceCell = source.PositionHeld;
            IntVec3 targetCell = target.PositionHeld;
            if (!sourceCell.IsValid || !targetCell.IsValid || !sourceCell.InBounds(source.MapHeld) || !targetCell.InBounds(source.MapHeld))
            {
                return false;
            }

            return GenSight.LineOfSight(sourceCell, targetCell, source.MapHeld, true);
        }

        private void ApplyPulseDamage(Pawn source, Thing target)
        {
            Map map = source.MapHeld;
            if (map == null || target == null || target.Destroyed)
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
            FleckMaker.ThrowLightningGlow(target.DrawPos, map, 0.58f);
            FleckMaker.ThrowMicroSparks(target.DrawPos, map);
            if (Rand.Chance(0.55f))
            {
                FleckMaker.ThrowMicroSparks(target.DrawPos, map);
            }
            ABY_SoundUtility.PlayAt(PulseSoundDefName, target.PositionHeld, map);
        }

        private void SpawnBeamVisuals(Map map, Pawn source, Vector3 rawTargetPos, int seed, int ticksGame, bool isTrackingThing)
        {
            if (map == null || beamHaloDef == null || beamCoreDef == null)
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

            int segmentCount = Mathf.Clamp(Mathf.CeilToInt(distance * 0.48f), 5, GetSegmentCap());
            Vector3 previousPoint = sourcePos;
            float amplitude = Mathf.Lerp(BaseAmplitude, MaxAmplitude, Mathf.Clamp01(distance / 14f));
            float phaseBase = ticksGame * 0.42f + seed * 0.017f;

            for (int i = 1; i < segmentCount; i++)
            {
                float t = i / (float)(segmentCount - 1);
                Vector3 point = GetCurvedPoint(sourcePos, targetPos, perpendicular, amplitude, phaseBase, t);
                SpawnBeamSegment(map, previousPoint, point, i, segmentCount, isTrackingThing, seed, ticksGame);
                previousPoint = point;
            }

            FleckMaker.ThrowLightningGlow(sourcePos, map, isTrackingThing ? 0.42f : 0.28f);
            FleckMaker.ThrowLightningGlow(targetPos, map, isTrackingThing ? 0.54f : 0.34f);
        }

        private Vector3 GetCurvedPoint(Vector3 sourcePos, Vector3 targetPos, Vector3 perpendicular, float amplitude, float phaseBase, float t)
        {
            float envelope = Mathf.Sin(t * Mathf.PI);
            float sway = Mathf.Sin(phaseBase + t * 7.35f) * amplitude * envelope;
            float secondary = Mathf.Sin(phaseBase * 1.73f + t * 12.8f + 1.1f) * amplitude * 0.34f * envelope;
            Vector3 point = Vector3.Lerp(sourcePos, targetPos, t) + perpendicular * sway;
            point.y = Altitudes.AltitudeFor(AltitudeLayer.MoteOverhead) + secondary * 0.045f;
            return point;
        }

        private void SpawnBeamSegment(Map map, Vector3 start, Vector3 end, int segmentIndex, int segmentCount, bool isTrackingThing, int seed, int ticksGame)
        {
            float t = segmentCount <= 1 ? 0f : segmentIndex / (float)(segmentCount - 1);
            float envelope = Mathf.Sin(t * Mathf.PI);
            float haloWidth = Mathf.Lerp(0.42f, 0.82f, envelope) * (isTrackingThing ? 1.0f : 0.72f);
            float coreWidth = Mathf.Lerp(0.12f, 0.23f, envelope) * (isTrackingThing ? 1.0f : 0.76f);

            SpawnBeamThing(beamHaloDef, start, end, map, haloWidth, BeamSegmentLifetimeTicks, BeamHaloTexturePath, true);
            SpawnBeamThing(beamCoreDef, start, end, map, coreWidth, BeamSegmentLifetimeTicks - 1, BeamCoreTexturePath, false);

            if (sparkMoteDef != null && segmentIndex > 1 && segmentIndex < segmentCount - 1 && ((segmentIndex + seed + ticksGame) % 5 == 0))
            {
                Vector3 sparkPoint = Vector3.Lerp(start, end, 0.5f);
                sparkPoint.y = Altitudes.AltitudeFor(AltitudeLayer.MoteOverhead) + 0.004f;
                MoteMaker.MakeStaticMote(sparkPoint, map, sparkMoteDef, 0.20f + envelope * 0.16f);
            }
        }

        private static void SpawnBeamThing(ThingDef def, Vector3 source, Vector3 target, Map map, float width, int ticks, string texturePath, bool pulse)
        {
            if (def == null || map == null || ticks <= 0)
            {
                return;
            }

            Mote_CrownspikeRailBeam beam = ThingMaker.MakeThing(def) as Mote_CrownspikeRailBeam;
            if (beam == null)
            {
                return;
            }

            beam.start = source;
            beam.end = target;
            beam.width = width;
            beam.ticksLeft = ticks;
            beam.startingTicks = ticks;
            beam.texturePath = texturePath;
            beam.additivePulse = pulse;

            IntVec3 spawnCell = ((source + target) * 0.5f).ToIntVec3();
            if (!spawnCell.InBounds(map))
            {
                spawnCell = source.ToIntVec3();
            }
            if (!spawnCell.InBounds(map))
            {
                spawnCell = target.ToIntVec3();
            }
            if (!spawnCell.InBounds(map))
            {
                return;
            }

            GenSpawn.Spawn(beam, spawnCell, map);
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
            return FindThing(map, pawnId) as Pawn;
        }

        private static Thing FindThing(Map map, int thingId)
        {
            if (thingId < 0 || map?.listerThings == null)
            {
                return null;
            }

            List<Thing> things = map.listerThings.AllThings;
            for (int i = 0; i < things.Count; i++)
            {
                Thing thing = things[i];
                if (thing != null && thing.thingIDNumber == thingId)
                {
                    return thing;
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
