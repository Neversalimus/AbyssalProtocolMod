using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    public class CompProperties_AbilityEffect_RuptureSentence : CompProperties_AbilityEffect
    {
        public string markHediffDef = RuptureCrownUtility.MarkHediffDefName;
        public int markTicks = 4320;
        public int fallbackCooldownTicks = GenDate.TicksPerDay;
        public string projectileDefName = "ABY_Projectile_RuptureSentence";

        public CompProperties_AbilityEffect_RuptureSentence()
        {
            compClass = typeof(CompAbilityEffect_RuptureSentence);
        }
    }

    public class CompAbilityEffect_RuptureSentence : CompAbilityEffect
    {
        private const ProjectileHitFlags LaunchHitFlags =
            ProjectileHitFlags.IntendedTarget |
            ProjectileHitFlags.NonTargetPawns |
            ProjectileHitFlags.NonTargetWorld;

        public new CompProperties_AbilityEffect_RuptureSentence Props =>
            (CompProperties_AbilityEffect_RuptureSentence)props;

        public override bool CanApplyOn(LocalTargetInfo target, LocalTargetInfo dest)
        {
            return CanApplyInternal(target);
        }

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            ApplyInternal(target);
        }

        private bool CanApplyInternal(LocalTargetInfo target)
        {
            Pawn caster = parent?.pawn;
            Pawn targetPawn = ResolveTargetPawn(caster, target);
            return IsValidPawnTarget(caster, targetPawn);
        }

        private void ApplyInternal(LocalTargetInfo target)
        {
            Pawn caster = parent?.pawn;
            Pawn targetPawn = ResolveTargetPawn(caster, target);

            if (caster == null || targetPawn == null || targetPawn.health == null)
            {
                Reject(caster, "Rupture Sentence failed: no valid hostile pawn in the selected target.");
                return;
            }

            if (!IsValidPawnTarget(caster, targetPawn))
            {
                Reject(caster, "Rupture Sentence failed: target is invalid.");
                return;
            }

            ThingDef projectileDef = DefDatabase<ThingDef>.GetNamedSilentFail(Props.projectileDefName);
            if (projectileDef == null)
            {
                Reject(caster, "Rupture Sentence failed: projectile def is missing.");
                return;
            }

            base.Apply(new LocalTargetInfo(targetPawn), LocalTargetInfo.Invalid);

            Projectile projectile = GenSpawn.Spawn(
                ThingMaker.MakeThing(projectileDef),
                caster.PositionHeld,
                caster.MapHeld) as Projectile;

            if (projectile == null)
            {
                Reject(caster, "Rupture Sentence failed: projectile could not be spawned.");
                return;
            }

            projectile.Launch(
                caster,
                caster.DrawPos,
                new LocalTargetInfo(targetPawn),
                new LocalTargetInfo(targetPawn),
                LaunchHitFlags,
                false,
                null,
                null);

            CompRuptureCrown crownComp = RuptureCrownUtility.GetWornCrownComp(caster);
            if (crownComp != null)
            {
                crownComp.NotifyUsed();
                parent.StartCooldown(crownComp.Props.rechargeTicks);
            }
            else
            {
                parent.StartCooldown(Props.fallbackCooldownTicks);
            }

            if (caster.MapHeld != null)
            {
                ABY_SoundUtility.PlayAt("ABY_RuptureVerdict", caster.PositionHeld, caster.MapHeld);
            }

            if (caster.Faction == Faction.OfPlayer)
            {
                Messages.Message(
                    "Rupture Sentence launched.",
                    new LookTargets(targetPawn),
                    MessageTypeDefOf.NeutralEvent,
                    false);
            }
        }

        private static void Reject(Pawn caster, string message)
        {
            if (caster != null && caster.Faction == Faction.OfPlayer)
            {
                Messages.Message(message, caster, MessageTypeDefOf.RejectInput, false);
            }
        }

        private static bool IsValidPawnTarget(Pawn caster, Pawn targetPawn)
        {
            if (caster == null || caster.Dead || !caster.Spawned || caster.MapHeld == null)
            {
                return false;
            }

            if (targetPawn == null || targetPawn == caster || targetPawn.Dead || !targetPawn.Spawned || targetPawn.MapHeld != caster.MapHeld)
            {
                return false;
            }

            if (!targetPawn.HostileTo(caster))
            {
                return false;
            }

            return GenSight.LineOfSight(caster.PositionHeld, targetPawn.PositionHeld, caster.MapHeld);
        }

        private static Pawn ResolveTargetPawn(Pawn caster, LocalTargetInfo target)
        {
            if (caster?.MapHeld == null || !target.IsValid)
            {
                return null;
            }

            Pawn directPawn = target.Thing as Pawn;
            if (directPawn != null)
            {
                return directPawn;
            }

            if (!target.Cell.IsValid)
            {
                return null;
            }

            List<Thing> things = target.Cell.GetThingList(caster.MapHeld);
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
    }
}
