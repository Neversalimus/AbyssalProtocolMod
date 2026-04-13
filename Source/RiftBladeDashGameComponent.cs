using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace AbyssalProtocol
{
    public class RiftBladeDashGameComponent : GameComponent
    {
        private const int ScanIntervalTicks = 12;

        private static readonly IntVec3[] AdjacentOffsets =
        {
            new IntVec3( 1, 0,  0),
            new IntVec3(-1, 0,  0),
            new IntVec3( 0, 0,  1),
            new IntVec3( 0, 0, -1),
            new IntVec3( 1, 0,  1),
            new IntVec3( 1, 0, -1),
            new IntVec3(-1, 0,  1),
            new IntVec3(-1, 0, -1)
        };

        private Dictionary<int, int> nextDashTickByPawn = new Dictionary<int, int>();

        public RiftBladeDashGameComponent(Game game)
        {
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref nextDashTickByPawn, "nextDashTickByPawn", LookMode.Value, LookMode.Value);
            if (Scribe.mode == LoadSaveMode.PostLoadInit && nextDashTickByPawn == null)
            {
                nextDashTickByPawn = new Dictionary<int, int>();
            }
        }

        public override void GameComponentTick()
        {
            if (Find.TickManager == null || Find.Maps == null)
            {
                return;
            }

            int ticksGame = Find.TickManager.TicksGame;
            if (ticksGame % ScanIntervalTicks != 0)
            {
                return;
            }

            List<Map> maps = Find.Maps;
            for (int i = 0; i < maps.Count; i++)
            {
                Map map = maps[i];
                if (map == null || map.mapPawns == null)
                {
                    continue;
                }

                IReadOnlyList<Pawn> pawns = map.mapPawns.AllPawnsSpawned;
                for (int j = 0; j < pawns.Count; j++)
                {
                    Pawn pawn = pawns[j];
                    if (!CanPawnAttemptDash(pawn, ticksGame))
                    {
                        continue;
                    }

                    ThingWithComps primary = pawn.equipment.Primary;
                    RiftDashWeaponExtension extension = primary.def.GetModExtension<RiftDashWeaponExtension>();
                    if (extension == null)
                    {
                        continue;
                    }

                    if (!TryGetCurrentAttackTarget(pawn, extension, out Thing target))
                    {
                        continue;
                    }

                    if (!TryFindDashDestination(pawn, target, extension, out IntVec3 destination))
                    {
                        continue;
                    }

                    ExecuteDash(pawn, destination, extension, ticksGame);
                }
            }
        }

        private bool CanPawnAttemptDash(Pawn pawn, int ticksGame)
        {
            if (pawn == null || pawn.Dead || pawn.Downed || !pawn.Spawned || pawn.MapHeld == null)
            {
                return false;
            }

            if (pawn.stances?.stunner?.Stunned == true)
            {
                return false;
            }

            if (pawn.equipment == null || pawn.equipment.Primary == null)
            {
                return false;
            }

            if (pawn.CurJob == null || pawn.CurJob.def != JobDefOf.AttackMelee)
            {
                return false;
            }

            int nextDashTick;
            if (nextDashTickByPawn.TryGetValue(pawn.thingIDNumber, out nextDashTick) && ticksGame < nextDashTick)
            {
                return false;
            }

            return true;
        }

        private bool TryGetCurrentAttackTarget(Pawn pawn, RiftDashWeaponExtension extension, out Thing target)
        {
            target = null;

            LocalTargetInfo targetInfo = pawn.CurJob.targetA;
            if (!targetInfo.IsValid || !targetInfo.HasThing)
            {
                return false;
            }

            Thing thing = targetInfo.Thing;
            if (!IsValidDashTarget(pawn, thing, extension))
            {
                return false;
            }

            target = thing;
            return true;
        }

        private bool IsValidDashTarget(Pawn pawn, Thing target, RiftDashWeaponExtension extension)
        {
            if (pawn == null || target == null || target.Destroyed || !target.Spawned)
            {
                return false;
            }

            Map map = pawn.MapHeld;
            if (map == null || target.MapHeld != map)
            {
                return false;
            }

            if (!GenHostility.HostileTo(pawn, target))
            {
                return false;
            }

            IntVec3 pawnPos = pawn.Position;
            IntVec3 targetPos = target.Position;

            if (IsAdjacentOrSame(pawnPos, targetPos))
            {
                return false;
            }

            if (pawnPos.DistanceTo(targetPos) > extension.maxRange)
            {
                return false;
            }

            if (extension.requireLineOfSight && !GenSight.LineOfSight(pawnPos, targetPos, map))
            {
                return false;
            }

            return true;
        }

        private bool TryFindDashDestination(Pawn pawn, Thing target, RiftDashWeaponExtension extension, out IntVec3 bestCell)
        {
            bestCell = IntVec3.Invalid;

            Map map = pawn.MapHeld;
            IntVec3 targetPos = target.Position;
            IntVec3 pawnPos = pawn.Position;

            int bestScore = int.MaxValue;

            for (int i = 0; i < AdjacentOffsets.Length; i++)
            {
                IntVec3 candidate = targetPos + AdjacentOffsets[i];
                if (!candidate.IsValid || !candidate.InBounds(map) || !candidate.Standable(map))
                {
                    continue;
                }

                if (candidate == pawnPos)
                {
                    continue;
                }

                if (extension.requireLineOfSight && !GenSight.LineOfSight(pawnPos, candidate, map))
                {
                    continue;
                }

                List<Thing> thingList = candidate.GetThingList(map);
                bool blocked = false;
                for (int j = 0; j < thingList.Count; j++)
                {
                    Thing thing = thingList[j];
                    if (thing is Pawn && thing != pawn)
                    {
                        blocked = true;
                        break;
                    }
                }

                if (blocked)
                {
                    continue;
                }

                int score = DistanceSquared(pawnPos, candidate);
                if (score < bestScore)
                {
                    bestScore = score;
                    bestCell = candidate;
                }
            }

            return bestCell.IsValid;
        }

        private void ExecuteDash(Pawn pawn, IntVec3 destination, RiftDashWeaponExtension extension, int ticksGame)
        {
            Map map = pawn.MapHeld;
            if (map == null)
            {
                return;
            }

            IntVec3 start = pawn.Position;

            SpawnDashEffects(map, start, destination, extension);

            if (pawn.pather != null)
            {
                pawn.pather.StopDead();
            }

            pawn.Position = destination;

            if (pawn.pather != null)
            {
                pawn.pather.StopDead();
            }

            nextDashTickByPawn[pawn.thingIDNumber] = ticksGame + Math.Max(1, extension.cooldownTicks);
        }

        private void SpawnDashEffects(Map map, IntVec3 start, IntVec3 end, RiftDashWeaponExtension extension)
        {
            if (map == null)
            {
                return;
            }

            Vector3 startPos = start.ToVector3Shifted();
            Vector3 endPos = end.ToVector3Shifted();

            TrySpawnMote(map, extension.entryMoteDef, startPos, extension.entryMoteScale);

            int steps = Math.Max(0, extension.trailSteps);
            if (steps > 0)
            {
                for (int i = 1; i <= steps; i++)
                {
                    float t = (float)i / (steps + 1);
                    Vector3 pos = Vector3.Lerp(startPos, endPos, t);
                    TrySpawnMote(map, extension.trailMoteDef, pos, extension.trailMoteScale);
                }
            }

            TrySpawnMote(map, extension.exitMoteDef, endPos, extension.exitMoteScale);
            TryPlaySound(map, end, extension.soundDef);
        }

        private static void TrySpawnMote(Map map, string defName, Vector3 pos, float scale)
        {
            if (map == null || string.IsNullOrEmpty(defName))
            {
                return;
            }

            ThingDef moteDef = DefDatabase<ThingDef>.GetNamedSilentFail(defName);
            if (moteDef == null)
            {
                return;
            }

            MoteMaker.MakeStaticMote(pos, map, moteDef, scale);
        }

        private static void TryPlaySound(Map map, IntVec3 cell, string defName)
        {
            if (map == null || !cell.IsValid || string.IsNullOrEmpty(defName))
            {
                return;
            }

            SoundDef soundDef = DefDatabase<SoundDef>.GetNamedSilentFail(defName);
            if (soundDef == null)
            {
                return;
            }

            soundDef.PlayOneShot(new TargetInfo(cell, map));
        }

        private static bool IsAdjacentOrSame(IntVec3 a, IntVec3 b)
        {
            int dx = Math.Abs(a.x - b.x);
            int dz = Math.Abs(a.z - b.z);
            return dx <= 1 && dz <= 1;
        }

        private static int DistanceSquared(IntVec3 a, IntVec3 b)
        {
            int dx = a.x - b.x;
            int dz = a.z - b.z;
            return (dx * dx) + (dz * dz);
        }
    }
}
