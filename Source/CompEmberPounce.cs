using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    public class CompEmberPounce : ThingComp
    {
        private int nextPounceTick;

        public CompProperties_EmberPounce Props => (CompProperties_EmberPounce)props;

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref nextPounceTick, "nextPounceTick");
        }

        public override void CompTick()
        {
            base.CompTick();

            Pawn pawn = parent as Pawn;
            if (pawn == null || !pawn.Spawned || pawn.Map == null || pawn.Dead || pawn.Downed)
            {
                return;
            }

            if (!parent.IsHashIntervalTick(Mathf.Max(15, Props.scanIntervalTicks)))
            {
                return;
            }

            int currentTick = Find.TickManager.TicksGame;
            if (currentTick < nextPounceTick)
            {
                return;
            }

            Pawn target = FindBestTarget(pawn, requireRanged: true) ?? FindBestTarget(pawn, requireRanged: false);
            if (target == null)
            {
                return;
            }

            if (!TryFindLandingCell(pawn, target, out IntVec3 landingCell))
            {
                return;
            }

            DoPounce(pawn, target, landingCell);
            nextPounceTick = currentTick + Mathf.Max(60, Props.cooldownTicks) + Rand.RangeInclusive(-Mathf.Max(0, Props.cooldownJitterTicks), Mathf.Max(0, Props.cooldownJitterTicks));
        }

        private Pawn FindBestTarget(Pawn pawn, bool requireRanged)
        {
            IReadOnlyList<Pawn> pawns = pawn.Map.mapPawns.AllPawnsSpawned;
            Pawn best = null;
            float bestScore = float.MaxValue;

            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn candidate = pawns[i];
                if (candidate == null || candidate == pawn || candidate.Dead || candidate.Downed)
                {
                    continue;
                }

                if (!pawn.HostileTo(candidate))
                {
                    continue;
                }

                bool hasRangedWeapon = candidate.equipment?.Primary != null && candidate.equipment.Primary.def != null && candidate.equipment.Primary.def.IsRangedWeapon;
                if (requireRanged && !hasRangedWeapon)
                {
                    continue;
                }

                float distance = pawn.Position.DistanceTo(candidate.Position);
                if (distance < Props.minRange || distance > Props.maxRange)
                {
                    continue;
                }

                if (!GenSight.LineOfSight(pawn.Position, candidate.Position, pawn.Map))
                {
                    continue;
                }

                float score = distance;
                if (hasRangedWeapon)
                {
                    score -= 2.4f;
                }

                float healthFactor = 1f;
                if (candidate.health != null)
                {
                    healthFactor = candidate.health.summaryHealth.SummaryHealthPercent;
                }

                score += healthFactor * 2.0f;

                if (score < bestScore)
                {
                    bestScore = score;
                    best = candidate;
                }
            }

            return best;
        }

        private bool TryFindLandingCell(Pawn pawn, Pawn target, out IntVec3 landingCell)
        {
            landingCell = IntVec3.Invalid;
            Map map = pawn.Map;
            float bestDistance = float.MaxValue;

            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dz = -1; dz <= 1; dz++)
                {
                    if (dx == 0 && dz == 0)
                    {
                        continue;
                    }

                    IntVec3 cell = target.Position + new IntVec3(dx, 0, dz);
                    if (!cell.InBounds(map) || !cell.Standable(map) || CellHasPawn(cell, map, pawn))
                    {
                        continue;
                    }

                    float distance = pawn.Position.DistanceToSquared(cell);
                    if (distance < bestDistance)
                    {
                        bestDistance = distance;
                        landingCell = cell;
                    }
                }
            }

            return landingCell.IsValid;
        }

        private void DoPounce(Pawn pawn, Pawn target, IntVec3 landingCell)
        {
            Map map = pawn.Map;
            IntVec3 sourceCell = pawn.Position;

            SpawnMote(map, sourceCell);
            pawn.pather?.StopDead();
            pawn.Position = landingCell;
            pawn.Drawer?.tweener?.ResetTweenedPosToRoot();
            pawn.stances?.CancelBusyStanceSoft();
            pawn.rotationTracker?.FaceCell(target.Position);
            SpawnMote(map, landingCell);

            ABY_SoundUtility.PlayAt("ABY_SigilChargePulse", landingCell, map);
            ApplyImpactHediff(target);
        }

        private void ApplyImpactHediff(Pawn target)
        {
            if (target?.health == null || Props.impactHediffDefName.NullOrEmpty())
            {
                return;
            }

            HediffDef hediffDef = DefDatabase<HediffDef>.GetNamedSilentFail(Props.impactHediffDefName);
            if (hediffDef == null)
            {
                return;
            }

            if (target.health.hediffSet.GetFirstHediffOfDef(hediffDef) != null)
            {
                return;
            }

            target.health.AddHediff(hediffDef);
        }

        private void SpawnMote(Map map, IntVec3 cell)
        {
            if (map == null || !cell.IsValid)
            {
                return;
            }

            ThingDef moteDef = DefDatabase<ThingDef>.GetNamedSilentFail("ABY_Mote_ArchonDashTrail");
            if (moteDef == null)
            {
                return;
            }

            MoteMaker.MakeStaticMote(cell.ToVector3Shifted(), map, moteDef, 0.82f);
        }

        private bool CellHasPawn(IntVec3 cell, Map map, Pawn ignore)
        {
            List<Thing> things = cell.GetThingList(map);
            for (int i = 0; i < things.Count; i++)
            {
                if (things[i] is Pawn pawn && pawn != ignore)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
