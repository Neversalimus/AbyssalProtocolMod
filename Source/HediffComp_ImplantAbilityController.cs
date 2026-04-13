using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    public enum ABY_ImplantAbilityAutoCastMode
    {
        Disabled = 0,
        DraftedOnly = 1,
        Always = 2
    }

    public class HediffCompProperties_ImplantAbilityController : HediffCompProperties
    {
        public AbilityDef abilityDef;

        public bool grantAbility = true;
        public bool removeAbilityWhenHediffIsRemoved = true;
        public bool showAutoCastGizmos = true;
        public bool requirePlayerControlForGizmos = true;

        public ABY_ImplantAbilityAutoCastMode defaultAutoCastMode = ABY_ImplantAbilityAutoCastMode.Disabled;

        public int autoCastCheckIntervalTicks = 30;
        public float autoCastMinRange = 0f;
        public float autoCastMaxRange = -1f;
        public bool requireLineOfSight = true;
        public bool onlyAutoCastHostilePawns = true;
        public bool skipDownedTargets = true;
        public bool skipTargetsInMentalState = false;
        public float avoidFriendlyRadius = 1.9f;

        public HediffCompProperties_ImplantAbilityController()
        {
            compClass = typeof(HediffComp_ImplantAbilityController);
        }
    }

    public class HediffComp_ImplantAbilityController : HediffComp
    {
        private const int AbilityResyncIntervalTicks = 180;
        private const int NeverTick = -999999;

        private bool initialized;
        private bool grantedByThisComp;
        private ABY_ImplantAbilityAutoCastMode autoCastMode;
        private int lastAutoCastTick = NeverTick;

        public HediffCompProperties_ImplantAbilityController Props =>
            (HediffCompProperties_ImplantAbilityController)props;

        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_Values.Look(ref initialized, "initialized", false);
            Scribe_Values.Look(ref grantedByThisComp, "grantedByThisComp", false);
            Scribe_Values.Look(ref autoCastMode, "autoCastMode", ABY_ImplantAbilityAutoCastMode.Disabled);
            Scribe_Values.Look(ref lastAutoCastTick, "lastAutoCastTick", NeverTick);
        }

        public override void CompPostPostAdd(DamageInfo? dinfo)
        {
            base.CompPostPostAdd(dinfo);
            EnsureInitialized();
            EnsureAbilityState();
        }

        public override void Notify_Spawned()
        {
            base.Notify_Spawned();
            EnsureInitialized();
            EnsureAbilityState();
        }

        public override void CompPostPostRemoved()
        {
            base.CompPostPostRemoved();
            RemoveGrantedAbility();
        }

        public override void Notify_SurgicallyRemoved(Pawn surgeon)
        {
            base.Notify_SurgicallyRemoved(surgeon);
            RemoveGrantedAbility();
        }

        public override void Notify_SurgicallyReplaced(Pawn surgeon)
        {
            base.Notify_SurgicallyReplaced(surgeon);
            RemoveGrantedAbility();
        }

        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);

            Pawn pawn = Pawn;
            if (pawn == null)
            {
                return;
            }

            EnsureInitialized();

            if (pawn.IsHashIntervalTick(AbilityResyncIntervalTicks))
            {
                EnsureAbilityState();
            }

            if (!ShouldTryAutoCast(pawn))
            {
                return;
            }

            if (!pawn.IsHashIntervalTick(Mathf.Max(1, Props.autoCastCheckIntervalTicks)))
            {
                return;
            }

            TryAutoCast(pawn);
        }

        public override IEnumerable<Gizmo> CompGetGizmos()
        {
            foreach (Gizmo gizmo in base.CompGetGizmos())
            {
                yield return gizmo;
            }

            Pawn pawn = Pawn;
            if (!ShouldShowAutoCastGizmos(pawn))
            {
                yield break;
            }

            string abilityLabel = Props.abilityDef != null ? Props.abilityDef.LabelCap : "implant ability";

            yield return new Command_Toggle
            {
                defaultLabel = "Auto-cast ability",
                defaultDesc = "Enable or disable automatic use of the implant ability.",
                isActive = () => autoCastMode != ABY_ImplantAbilityAutoCastMode.Disabled,
                toggleAction = delegate
                {
                    if (autoCastMode == ABY_ImplantAbilityAutoCastMode.Disabled)
                    {
                        autoCastMode = GetPreferredEnabledMode();
                    }
                    else
                    {
                        autoCastMode = ABY_ImplantAbilityAutoCastMode.Disabled;
                    }
                }
            };

            yield return new Command_Action
            {
                defaultLabel = "Auto-cast mode: " + GetModeLabel(autoCastMode),
                defaultDesc = "Cycle how " + abilityLabel + " is auto-used. Drafted Only is the safest default for combat implants.",
                action = CycleAutoCastMode
            };
        }

        public override string CompDebugString()
        {
            return "ability=" + (Props.abilityDef != null ? Props.abilityDef.defName : "null") +
                   " | mode=" + autoCastMode +
                   " | grantedByComp=" + grantedByThisComp +
                   " | lastAutoCastTick=" + lastAutoCastTick;
        }

        protected virtual bool ShouldSkipTarget(Pawn caster, Pawn target, Ability ability)
        {
            if (target == null || target == caster)
            {
                return true;
            }

            if (!target.Spawned || target.MapHeld != caster.MapHeld)
            {
                return true;
            }

            if (target.Dead)
            {
                return true;
            }

            if (Props.onlyAutoCastHostilePawns && !caster.HostileTo(target))
            {
                return true;
            }

            if (Props.skipDownedTargets && target.Downed)
            {
                return true;
            }

            if (Props.skipTargetsInMentalState && target.InMentalState)
            {
                return true;
            }

            float distanceSquared = caster.Position.DistanceToSquared(target.Position);
            if (distanceSquared < Props.autoCastMinRange * Props.autoCastMinRange)
            {
                return true;
            }

            float maxRange = GetResolvedMaxRange(ability);
            if (maxRange > 0f && distanceSquared > maxRange * maxRange)
            {
                return true;
            }

            if (Props.requireLineOfSight && !GenSight.LineOfSight(caster.Position, target.Position, caster.MapHeld))
            {
                return true;
            }

            if (WouldHitFriendlies(caster, target.Position))
            {
                return true;
            }

            LocalTargetInfo localTarget = new LocalTargetInfo(target);
            if (!ability.CanApplyOn(localTarget))
            {
                return true;
            }

            return false;
        }

        protected virtual float ScoreTarget(Pawn caster, Pawn target, Ability ability)
        {
            float distanceSquared = caster.Position.DistanceToSquared(target.Position);
            float score = 10000f - distanceSquared;

            if (!target.Downed)
            {
                score += 100f;
            }

            if (target.health != null)
            {
                score += (1f - target.health.summaryHealth.SummaryHealthPercent) * 25f;
            }

            return score;
        }

        protected virtual bool TryQueueAbilityOnTarget(Ability ability, LocalTargetInfo target)
        {
            if (ability == null || !target.IsValid)
            {
                return false;
            }

            if (!ability.CanCast || ability.OnCooldown || ability.Casting)
            {
                return false;
            }

            if (!ability.CanApplyOn(target))
            {
                return false;
            }

            GlobalTargetInfo globalTarget = target.HasThing
                ? new GlobalTargetInfo(target.Thing)
                : new GlobalTargetInfo(target.Cell, ability.pawn.MapHeld);

            ability.QueueCastingJob(globalTarget);
            lastAutoCastTick = Find.TickManager != null ? Find.TickManager.TicksGame : NeverTick;
            return true;
        }

        private void EnsureInitialized()
        {
            if (initialized)
            {
                return;
            }

            initialized = true;
            autoCastMode = Props.defaultAutoCastMode;
        }

        private void EnsureAbilityState()
        {
            if (!Props.grantAbility)
            {
                return;
            }

            if (Props.abilityDef == null)
            {
                return;
            }

            Pawn pawn = Pawn;
            if (pawn == null || pawn.abilities == null)
            {
                return;
            }

            Ability ability = pawn.abilities.GetAbility(Props.abilityDef, false);
            if (ability != null)
            {
                return;
            }

            pawn.abilities.GainAbility(Props.abilityDef);
            grantedByThisComp = true;
        }

        private void RemoveGrantedAbility()
        {
            if (!Props.removeAbilityWhenHediffIsRemoved)
            {
                return;
            }

            if (!grantedByThisComp)
            {
                return;
            }

            if (Props.abilityDef == null)
            {
                return;
            }

            Pawn pawn = Pawn;
            if (pawn == null || pawn.abilities == null)
            {
                return;
            }

            pawn.abilities.RemoveAbility(Props.abilityDef);
            grantedByThisComp = false;
        }

        private bool ShouldTryAutoCast(Pawn pawn)
        {
            if (autoCastMode == ABY_ImplantAbilityAutoCastMode.Disabled)
            {
                return false;
            }

            if (Props.abilityDef == null || pawn.abilities == null)
            {
                return false;
            }

            if (!pawn.Spawned || pawn.MapHeld == null)
            {
                return false;
            }

            if (pawn.Dead || pawn.Downed)
            {
                return false;
            }

            if (pawn.InMentalState)
            {
                return false;
            }

            if (autoCastMode == ABY_ImplantAbilityAutoCastMode.DraftedOnly && !pawn.Drafted)
            {
                return false;
            }

            Ability ability = pawn.abilities.GetAbility(Props.abilityDef, false);
            if (ability == null)
            {
                return false;
            }

            if (!ability.CanCast || ability.OnCooldown || ability.Casting)
            {
                return false;
            }

            return true;
        }

        private bool TryAutoCast(Pawn pawn)
        {
            Ability ability = pawn.abilities.GetAbility(Props.abilityDef, false);
            if (ability == null)
            {
                return false;
            }

            if (!Props.abilityDef.targetRequired)
            {
                return TryQueueAbilityOnTarget(ability, new LocalTargetInfo(pawn));
            }

            Pawn bestTarget = FindBestHostilePawnTarget(pawn, ability);
            if (bestTarget == null)
            {
                return false;
            }

            return TryQueueAbilityOnTarget(ability, new LocalTargetInfo(bestTarget));
        }

        private Pawn FindBestHostilePawnTarget(Pawn caster, Ability ability)
        {
            if (caster.MapHeld == null)
            {
                return null;
            }

            Pawn bestTarget = null;
            float bestScore = float.MinValue;

            IReadOnlyList<Pawn> pawns = caster.MapHeld.mapPawns.AllPawnsSpawned;
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn candidate = pawns[i];
                if (ShouldSkipTarget(caster, candidate, ability))
                {
                    continue;
                }

                float score = ScoreTarget(caster, candidate, ability);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestTarget = candidate;
                }
            }

            return bestTarget;
        }

        private bool ShouldShowAutoCastGizmos(Pawn pawn)
        {
            if (!Props.showAutoCastGizmos)
            {
                return false;
            }

            if (pawn == null)
            {
                return false;
            }

            if (Props.requirePlayerControlForGizmos && !pawn.IsColonistPlayerControlled)
            {
                return false;
            }

            return true;
        }

        private float GetResolvedMaxRange(Ability ability)
        {
            if (Props.autoCastMaxRange > 0f)
            {
                return Props.autoCastMaxRange;
            }

            if (ability != null && ability.def != null && ability.def.verbProperties != null)
            {
                return ability.def.verbProperties.range;
            }

            if (Props.abilityDef != null && Props.abilityDef.verbProperties != null)
            {
                return Props.abilityDef.verbProperties.range;
            }

            return -1f;
        }

        private bool WouldHitFriendlies(Pawn caster, IntVec3 targetCell)
        {
            if (Props.avoidFriendlyRadius <= 0f)
            {
                return false;
            }

            if (caster.MapHeld == null)
            {
                return false;
            }

            float radiusSquared = Props.avoidFriendlyRadius * Props.avoidFriendlyRadius;
            IReadOnlyList<Pawn> pawns = caster.MapHeld.mapPawns.AllPawnsSpawned;
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn other = pawns[i];
                if (other == null || other == caster)
                {
                    continue;
                }

                if (!other.Spawned || other.Dead)
                {
                    continue;
                }

                if (other.Faction != caster.Faction)
                {
                    continue;
                }

                if (other.Position.DistanceToSquared(targetCell) <= radiusSquared)
                {
                    return true;
                }
            }

            return false;
        }

        private void CycleAutoCastMode()
        {
            switch (autoCastMode)
            {
                case ABY_ImplantAbilityAutoCastMode.Disabled:
                    autoCastMode = ABY_ImplantAbilityAutoCastMode.DraftedOnly;
                    break;
                case ABY_ImplantAbilityAutoCastMode.DraftedOnly:
                    autoCastMode = ABY_ImplantAbilityAutoCastMode.Always;
                    break;
                default:
                    autoCastMode = ABY_ImplantAbilityAutoCastMode.Disabled;
                    break;
            }
        }

        private ABY_ImplantAbilityAutoCastMode GetPreferredEnabledMode()
        {
            if (Props.defaultAutoCastMode != ABY_ImplantAbilityAutoCastMode.Disabled)
            {
                return Props.defaultAutoCastMode;
            }

            return ABY_ImplantAbilityAutoCastMode.DraftedOnly;
        }

        private static string GetModeLabel(ABY_ImplantAbilityAutoCastMode mode)
        {
            switch (mode)
            {
                case ABY_ImplantAbilityAutoCastMode.DraftedOnly:
                    return "Drafted Only";
                case ABY_ImplantAbilityAutoCastMode.Always:
                    return "Always";
                default:
                    return "Disabled";
            }
        }
    }
}
