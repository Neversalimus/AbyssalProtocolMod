using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    public class Projectile_RuptureSentence : Bullet
    {
        private const int DefaultMarkTicks = 4320;

        protected override void Impact(Thing hitThing, bool blockedByShield = false)
        {
            Pawn caster = Launcher as Pawn;
            Pawn impactPawn = ResolveImpactPawn(hitThing);

            if (impactPawn != null && IsValidPawnTarget(caster, impactPawn))
            {
                ApplyMark(impactPawn);

                if (impactPawn.MapHeld != null)
                {
                    ABY_SoundUtility.PlayAt("ABY_RuptureImpact", impactPawn.PositionHeld, impactPawn.MapHeld);
                    FleckMaker.ThrowLightningGlow(impactPawn.DrawPos, impactPawn.MapHeld, 1.8f);
                }

                if (caster != null && caster.Faction == Faction.OfPlayer)
                {
                    Messages.Message(
                        "Rupture Sentence discharged.",
                        new LookTargets(impactPawn),
                        MessageTypeDefOf.NeutralEvent,
                        false);
                }
            }
            else if (caster != null && caster.Faction == Faction.OfPlayer)
            {
                Messages.Message(
                    "Rupture Sentence failed: projectile found no valid hostile pawn on impact.",
                    caster,
                    MessageTypeDefOf.RejectInput,
                    false);
            }

            base.Impact(hitThing, blockedByShield);
        }

        private static bool IsValidPawnTarget(Pawn caster, Pawn targetPawn)
        {
            if (caster == null || caster.Dead || caster.MapHeld == null)
            {
                return false;
            }

            if (targetPawn == null || targetPawn == caster || targetPawn.Dead || !targetPawn.Spawned || targetPawn.MapHeld != caster.MapHeld)
            {
                return false;
            }

            return ABY_FactionHostilityUtility.SafeHostileTo(targetPawn, caster);
        }

        private Pawn ResolveImpactPawn(Thing hitThing)
        {
            Pawn directPawn = hitThing as Pawn;
            if (directPawn != null)
            {
                return directPawn;
            }

            if (Map == null || !Position.IsValid)
            {
                return null;
            }

            List<Thing> things = Position.GetThingList(Map);
            for (int i = 0; i < things.Count; i++)
            {
                Pawn pawn = things[i] as Pawn;
                if (pawn != null)
                {
                    return pawn;
                }
            }

            return null;
        }

        private static void ApplyMark(Pawn targetPawn)
        {
            HediffDef markDef = DefDatabase<HediffDef>.GetNamedSilentFail(RuptureCrownUtility.MarkHediffDefName);
            if (markDef == null || targetPawn.health == null)
            {
                return;
            }

            ABY_ProjectileProcUtility.ApplyOrRefreshFixedHediff(
                targetPawn,
                RuptureCrownUtility.MarkHediffDefName,
                1f,
                DefaultMarkTicks);
        }
    }
}
