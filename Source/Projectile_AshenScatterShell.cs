using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    public class Projectile_AshenScatterShell : Bullet
    {
        private const int TrailIntervalTicks = 3;
        private const float TrailSmokeSize = 0.32f;
        private const float ImpactPrimaryGlow = 2.45f;
        private const float ImpactSecondaryGlow = 1.35f;
        private const float ImpactSmokeSize = 1.25f;
        private const float ImpactDustSize = 1.55f;
        private int ticksAlive;

        protected override void Tick()
        {
            base.Tick();

            if (!Spawned || Map == null)
            {
                return;
            }

            ticksAlive++;
            if (ticksAlive % TrailIntervalTicks != 0)
            {
                return;
            }

            FleckMaker.ThrowSmoke(ExactPosition, Map, TrailSmokeSize);
            if (Rand.Chance(0.45f))
            {
                FleckMaker.ThrowMicroSparks(ExactPosition, Map);
            }
        }

        protected override void Impact(Thing hitThing, bool blockedByShield = false)
        {
            Map impactMap = Map;
            IntVec3 impactCell = Position;
            Vector3 impactPosition = ExactPosition;

            base.Impact(hitThing, blockedByShield);

            if (impactMap == null || !impactCell.IsValid)
            {
                return;
            }

            SpawnImpactVfx(impactCell, impactPosition, impactMap, blockedByShield);
        }

        private static void SpawnImpactVfx(IntVec3 impactCell, Vector3 impactPosition, Map map, bool blockedByShield)
        {
            float primaryGlow = blockedByShield ? 1.25f : ImpactPrimaryGlow;
            float secondaryGlow = blockedByShield ? 0.75f : ImpactSecondaryGlow;
            float smokeSize = blockedByShield ? 0.65f : ImpactSmokeSize;
            float dustSize = blockedByShield ? 0.85f : ImpactDustSize;

            FleckMaker.ThrowLightningGlow(impactPosition, map, primaryGlow);
            FleckMaker.ThrowHeatGlow(impactPosition, map, secondaryGlow);
            FleckMaker.ThrowSmoke(impactPosition, map, smokeSize);
            FleckMaker.ThrowDustPuff(impactPosition, map, dustSize);
            FleckMaker.ThrowMicroSparks(impactPosition, map);
            FleckMaker.ThrowMicroSparks(impactPosition, map);

            List<IntVec3> radialCells = GenRadial.RadialCellsAround(impactCell, 1.9f, true);
            for (int i = 0; i < radialCells.Count; i++)
            {
                IntVec3 cell = radialCells[i];
                if (!cell.InBounds(map) || cell == impactCell)
                {
                    continue;
                }

                Vector3 cellPos = cell.ToVector3Shifted();
                float scaleFactor = 1f - (impactCell.DistanceTo(cell) * 0.22f);
                scaleFactor = Mathf.Clamp(scaleFactor, 0.45f, 1f);

                FleckMaker.ThrowDustPuff(cellPos, map, dustSize * scaleFactor);
                if (Rand.Chance(0.78f))
                {
                    FleckMaker.ThrowSmoke(cellPos, map, smokeSize * scaleFactor);
                }
                if (Rand.Chance(0.55f))
                {
                    FleckMaker.ThrowMicroSparks(cellPos, map);
                }
                if (Rand.Chance(0.38f))
                {
                    FleckMaker.ThrowHeatGlow(cellPos, map, secondaryGlow * scaleFactor);
                }
            }
        }
    }
}
