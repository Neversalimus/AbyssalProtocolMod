using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    public class Projectile_SpecterLashAnchor : Bullet
    {
        private const float ImpactGlowSize = 0.86f;
        private const float ShieldImpactGlowSize = 0.66f;
        private const float TargetSnapRadius = 2.25f;

        protected override void Impact(Thing hitThing, bool blockedByShield = false)
        {
            Map impactMap = Map;
            Vector3 impactPosition = ExactPosition;
            Pawn launcherPawn = Launcher as Pawn;
            Thing impactTarget = ResolveImpactThing(hitThing, launcherPawn, impactPosition, TargetSnapRadius);

            base.Impact(hitThing, blockedByShield);

            if (impactMap == null)
            {
                return;
            }

            FleckMaker.ThrowLightningGlow(impactPosition, impactMap, blockedByShield ? ShieldImpactGlowSize : ImpactGlowSize);
            FleckMaker.ThrowMicroSparks(impactPosition, impactMap);

            SpecterLashStreamGameComponent component = Current.Game != null ? Current.Game.GetComponent<SpecterLashStreamGameComponent>() : null;
            if (launcherPawn == null || component == null)
            {
                return;
            }

            if (impactTarget != null && !impactTarget.Destroyed)
            {
                component.TryStartStream(launcherPawn, impactTarget, impactPosition);
                return;
            }

            component.TryStartStreamToPoint(launcherPawn, impactPosition, blockedByShield);
        }

        private Thing ResolveImpactThing(Thing hitThing, Pawn launcherPawn, Vector3 impactPosition, float searchRadius)
        {
            if (IsDamageableTarget(hitThing, launcherPawn))
            {
                return hitThing;
            }

            if (Map == null)
            {
                return null;
            }

            if (Position.IsValid)
            {
                Thing bestAtCell = SelectBestDamageableThing(Position.GetThingList(Map), launcherPawn, impactPosition, searchRadius);
                if (bestAtCell != null)
                {
                    return bestAtCell;
                }
            }

            return SelectBestDamageableThing(Map.listerThings != null ? Map.listerThings.AllThings : null, launcherPawn, impactPosition, searchRadius);
        }

        private static Thing SelectBestDamageableThing(List<Thing> things, Pawn launcherPawn, Vector3 impactPosition, float searchRadius)
        {
            if (things == null)
            {
                return null;
            }

            Thing bestThing = null;
            float bestScore = searchRadius * searchRadius + 2f;
            Vector2 impactFlat = new Vector2(impactPosition.x, impactPosition.z);

            for (int i = 0; i < things.Count; i++)
            {
                Thing thing = things[i];
                if (!IsDamageableTarget(thing, launcherPawn))
                {
                    continue;
                }

                Vector3 drawPos = thing.DrawPos;
                float distSq = (new Vector2(drawPos.x, drawPos.z) - impactFlat).sqrMagnitude;
                if (distSq > searchRadius * searchRadius)
                {
                    continue;
                }

                float priorityBias = 0.7f;
                if (thing is Pawn)
                {
                    priorityBias = 0f;
                }
                else if (thing.def != null && thing.def.category == ThingCategory.Building)
                {
                    priorityBias = 0.12f;
                }
                else if (thing.def != null && thing.def.mineable)
                {
                    priorityBias = 0.24f;
                }

                float score = distSq + priorityBias;
                if (score < bestScore)
                {
                    bestScore = score;
                    bestThing = thing;
                }
            }

            return bestThing;
        }

        private static bool IsDamageableTarget(Thing thing, Pawn launcherPawn)
        {
            if (thing == null || thing == launcherPawn || thing.Destroyed || !thing.Spawned || thing.def == null)
            {
                return false;
            }

            if (!thing.def.useHitPoints)
            {
                return false;
            }

            if (thing.def.category == ThingCategory.Mote || thing.def.category == ThingCategory.Projectile || thing is Fire)
            {
                return false;
            }

            return true;
        }
    }
}
