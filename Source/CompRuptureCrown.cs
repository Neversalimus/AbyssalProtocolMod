using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    public static class RuptureCrownUtility
    {
        public const string MarkDefName = "ABY_RuptureSentenceMark";
        public const int MarkTicks = 4320;

        public static bool TryApplyMark(Pawn pawn)
        {
            if (pawn == null || pawn.Dead || pawn.health == null)
            {
                return false;
            }

            HediffDef markDef = DefDatabase<HediffDef>.GetNamedSilentFail(MarkDefName);
            if (markDef == null)
            {
                return false;
            }

            Hediff existing = pawn.health.hediffSet.GetFirstHediffOfDef(markDef);
            if (existing == null)
            {
                existing = HediffMaker.MakeHediff(markDef, pawn);
                pawn.health.AddHediff(existing);
            }

            existing.Severity = 1f;
            HediffComp_Disappears disappears = existing.TryGetComp<HediffComp_Disappears>();
            if (disappears != null)
            {
                disappears.ticksToDisappear = MarkTicks;
            }

            pawn.health.hediffSet.DirtyCache();
            return true;
        }
    }

    public class CompProperties_RuptureCrown : CompProperties
    {
        public int rechargeTicks = GenDate.TicksPerDay;
        public float sentenceRange = 35.9f;

        public CompProperties_RuptureCrown()
        {
            compClass = typeof(CompRuptureCrown);
        }
    }

    public class CompRuptureCrown : ThingComp
    {
        private const string CommandLabel = "Rupture Sentence";
        private const string CommandDescReady = "Condemn one visible hostile pawn. The verdict sharply slows the target and collapses its combat efficiency for a long duration.";
        private static Texture2D cachedIcon;

        private int lastUseTick = -999999;

        public CompProperties_RuptureCrown Props => (CompProperties_RuptureCrown)props;

        public bool IsRecharged
        {
            get
            {
                if (Find.TickManager == null)
                {
                    return true;
                }

                return Find.TickManager.TicksGame - lastUseTick >= Props.rechargeTicks;
            }
        }

        public int TicksUntilRecharged
        {
            get
            {
                if (Find.TickManager == null)
                {
                    return 0;
                }

                return Mathf.Max(0, Props.rechargeTicks - (Find.TickManager.TicksGame - lastUseTick));
            }
        }

        private static Texture2D CommandIcon
        {
            get
            {
                if (cachedIcon == null)
                {
                    cachedIcon = ContentFinder<Texture2D>.Get("Things/Item/ABY_CrownOfRupture", true);
                }

                return cachedIcon;
            }
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref lastUseTick, "lastUseTick", -999999);
        }

        public override string CompInspectStringExtra()
        {
            if (IsRecharged)
            {
                return "Rupture charge: ready";
            }

            return "Rupture charge recharging: " + TicksUntilRecharged.ToStringTicksToPeriod();
        }

        public override IEnumerable<StatDrawEntry> SpecialDisplayStats()
        {
            foreach (StatDrawEntry entry in base.SpecialDisplayStats())
            {
                yield return entry;
            }

            yield return new StatDrawEntry(
                StatCategoryDefOf.BasicsNonPawnImportant,
                "Rupture recharge",
                GenDate.ToStringTicksToDays(Props.rechargeTicks),
                "Time needed to condense another verdict charge after firing.",
                1000);

            yield return new StatDrawEntry(
                StatCategoryDefOf.BasicsNonPawnImportant,
                "Rupture range",
                Props.sentenceRange.ToString("F1"),
                "Maximum range of the crown verdict command.",
                999);
        }

        public override IEnumerable<Gizmo> CompGetWornGizmosExtra()
        {
            foreach (Gizmo gizmo in base.CompGetWornGizmosExtra())
            {
                yield return gizmo;
            }

            Apparel apparel = parent as Apparel;
            Pawn wearer = apparel?.Wearer;
            if (wearer == null || wearer.Faction != Faction.OfPlayer)
            {
                yield break;
            }

            Command_Action command = new Command_Action
            {
                defaultLabel = CommandLabel,
                defaultDesc = IsRecharged ? CommandDescReady : ("Recharging: " + TicksUntilRecharged.ToStringTicksToPeriod()),
                icon = CommandIcon,
                action = delegate { BeginTargeting(wearer); },
                Order = 220f,
                hotKey = KeyBindingDefOf.Misc1,
                activateSound = SoundDef.Named("ABY_RuptureVerdict")
            };

            if (!IsRecharged)
            {
                command.Disable("Recharging: " + TicksUntilRecharged.ToStringTicksToPeriod());
            }

            yield return command;
        }

        private void BeginTargeting(Pawn wearer)
        {
            if (!CanUseNow(wearer, out string reason))
            {
                if (!reason.NullOrEmpty())
                {
                    Messages.Message(reason, wearer, MessageTypeDefOf.RejectInput, false);
                }

                return;
            }

            Find.Targeter.BeginTargeting(BuildTargetingParameters(wearer), delegate(LocalTargetInfo target)
            {
                TryUseOn(wearer, target);
            });
        }

        private TargetingParameters BuildTargetingParameters(Pawn wearer)
        {
            TargetingParameters targetingParameters = new TargetingParameters
            {
                canTargetPawns = true,
                canTargetBuildings = false,
                canTargetItems = false,
                canTargetLocations = false,
                validator = delegate(TargetInfo target)
                {
                    return IsValidTarget(wearer, target);
                }
            };

            return targetingParameters;
        }

        private bool IsValidTarget(Pawn wearer, TargetInfo target)
        {
            if (wearer == null || wearer.Dead || !wearer.Spawned || wearer.MapHeld == null)
            {
                return false;
            }

            if (!target.HasThing)
            {
                return false;
            }

            Pawn targetPawn = target.Thing as Pawn;
            if (targetPawn == null || targetPawn.Dead || !targetPawn.Spawned || targetPawn.MapHeld != wearer.MapHeld)
            {
                return false;
            }

            if (!targetPawn.HostileTo(wearer))
            {
                return false;
            }

            if (wearer.Position.DistanceTo(targetPawn.Position) > Props.sentenceRange)
            {
                return false;
            }

            if (!GenSight.LineOfSight(wearer.Position, targetPawn.Position, wearer.MapHeld))
            {
                return false;
            }

            return true;
        }


        private bool IsValidTarget(Pawn wearer, LocalTargetInfo target)
        {
            if (!target.IsValid)
            {
                return false;
            }

            TargetInfo asTargetInfo = target.HasThing
                ? new TargetInfo(target.Thing)
                : new TargetInfo(target.Cell, wearer.MapHeld, false);

            return IsValidTarget(wearer, asTargetInfo);
        }

        private void TryUseOn(Pawn wearer, LocalTargetInfo target)
        {
            if (!CanUseNow(wearer, out string reason))
            {
                if (!reason.NullOrEmpty())
                {
                    Messages.Message(reason, wearer, MessageTypeDefOf.RejectInput, false);
                }

                return;
            }

            if (!IsValidTarget(wearer, target))
            {
                Messages.Message("No valid hostile pawn in line of sight.", wearer, MessageTypeDefOf.RejectInput, false);
                return;
            }

            Pawn targetPawn = target.Thing as Pawn;
            if (targetPawn == null)
            {
                Messages.Message("No valid hostile pawn in line of sight.", wearer, MessageTypeDefOf.RejectInput, false);
                return;
            }

            if (!RuptureCrownUtility.TryApplyMark(targetPawn))
            {
                Messages.Message("Rupture Sentence failed to resolve on target.", wearer, MessageTypeDefOf.RejectInput, false);
                return;
            }

            lastUseTick = Find.TickManager != null ? Find.TickManager.TicksGame : 0;

            if (wearer.MapHeld != null)
            {
                ABY_SoundUtility.PlayAt("ABY_RuptureVerdict", wearer.PositionHeld, wearer.MapHeld);
                ABY_SoundUtility.PlayAt("ABY_RuptureImpact", targetPawn.PositionHeld, targetPawn.MapHeld);
            }

            if (wearer.Faction == Faction.OfPlayer)
            {
                Messages.Message("Rupture Sentence discharged. Crown recharge started.", wearer, MessageTypeDefOf.NeutralEvent, false);
            }
        }

        public bool CanUseNow(Pawn wearer, out string reason)
        {
            if (wearer == null || wearer.Dead || !wearer.Spawned || wearer.MapHeld == null)
            {
                reason = "The wearer must be spawned on a map.";
                return false;
            }

            if (wearer.Downed)
            {
                reason = "The wearer cannot activate the crown while downed.";
                return false;
            }

            if (!IsRecharged)
            {
                reason = "Crown of Rupture is recharging: " + TicksUntilRecharged.ToStringTicksToPeriod();
                return false;
            }

            reason = null;
            return true;
        }
    }
}
