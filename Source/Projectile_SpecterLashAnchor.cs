using RimWorld;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    public class Projectile_SpecterLashAnchor : Bullet
    {
        private const float ImpactGlowSize = 1.25f;
        private const float ShieldImpactGlowSize = 0.92f;
        private const float TargetSnapRadius = 1.85f;

        protected override void Impact(Thing hitThing, bool blockedByShield = false)
        {
            Map impactMap = Map;
            Vector3 impactPosition = ExactPosition;
            Pawn launcherPawn = Launcher as Pawn;
            Pawn impactPawn = ResolveImpactPawn(hitThing, launcherPawn, impactPosition, TargetSnapRadius);

            base.Impact(hitThing, blockedByShield);

            if (impactMap == null)
            {
                return;
            }

            FleckMaker.ThrowLightningGlow(impactPosition, impactMap, blockedByShield ? ShieldImpactGlowSize : ImpactGlowSize);
            FleckMaker.ThrowMicroSparks(impactPosition, impactMap);

            SpecterLashStreamGameComponent component = Current.Game?.GetComponent<SpecterLashStreamGameComponent>();
            if (launcherPawn == null || component == null)
            {
                return;
            }

            if (impactPawn != null && !impactPawn.Dead)
            {
                component.TryStartStream(launcherPawn, impactPawn, impactPosition);
                return;
            }

            component.TryStartStreamToPoint(launcherPawn, impactPosition, blockedByShield);
        }

        private Pawn ResolveImpactPawn(Thing hitThing, Pawn launcherPawn, Vector3 impactPosition, float searchRadius)
        {
            Pawn directPawn = hitThing as Pawn;
            if (directPawn != null)
            {
                return directPawn;
            }

            if (Map == null)
            {
                return null;
            }

            if (Position.IsValid)
            {
                var things = Position.GetThingList(Map);
                for (int i = 0; i < things.Count; i++)
                {
                    Pawn pawn = things[i] as Pawn;
                    if (pawn != null && pawn != launcherPawn)
                    {
                        return pawn;
                    }
                }
            }

            Pawn bestHostile = null;
            float bestHostileDistSq = searchRadius * searchRadius;
            Pawn bestAny = null;
            float bestAnyDistSq = bestHostileDistSq;

            var pawns = Map.mapPawns?.AllPawnsSpawned;
            if (pawns == null)
            {
                return null;
            }

            Vector2 impactFlat = new Vector2(impactPosition.x, impactPosition.z);
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn pawn = pawns[i];
                if (pawn == null || pawn == launcherPawn || pawn.Dead)
                {
                    continue;
                }

                Vector3 drawPos = pawn.DrawPos;
                float distSq = (new Vector2(drawPos.x, drawPos.z) - impactFlat).sqrMagnitude;
                if (distSq > bestAnyDistSq)
                {
                    continue;
                }

                if (launcherPawn != null && GenHostility.HostileTo(launcherPawn, pawn))
                {
                    bestHostile = pawn;
                    bestHostileDistSq = distSq;
                    bestAnyDistSq = distSq;
                }
                else if (bestHostile == null && distSq < bestAnyDistSq)
                {
                    bestAny = pawn;
                    bestAnyDistSq = distSq;
                }
            }

            return bestHostile ?? bestAny;
        }
    }
}
