using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    public static class RuptureCrownUtility
    {
        public const string MarkDefName = "ABY_RuptureSentenceMark";
        public const int MarkTicks = 1080;

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

        public CompProperties_RuptureCrown()
        {
            compClass = typeof(CompRuptureCrown);
        }
    }

    public class CompRuptureCrown : ThingComp
    {
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

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref lastUseTick, "lastUseTick", -999999);
        }

        public bool CanUseNow(out string reason)
        {
            if (IsRecharged)
            {
                reason = null;
                return true;
            }

            reason = "Crown of Rupture is recharging: " + TicksUntilRecharged.ToStringTicksToPeriod();
            return false;
        }

        public void NotifyFired(Pawn wielder)
        {
            lastUseTick = Find.TickManager?.TicksGame ?? 0;
            if (wielder != null && wielder.Faction == Faction.OfPlayer)
            {
                Messages.Message("Rupture Sentence discharged. Crown recharge started.", wielder, MessageTypeDefOf.NeutralEvent, false);
            }
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
        }
    }

    public class Verb_RuptureSentence : Verb_Shoot
    {
        private CompRuptureCrown CrownComp => EquipmentSource?.GetComp<CompRuptureCrown>();

        public override bool Available()
        {
            return base.Available() && CrownComp != null && CrownComp.IsRecharged;
        }

        public override bool TryStartCastOn(LocalTargetInfo castTarg, LocalTargetInfo destTarg, bool surpriseAttack = false, bool canHitNonTargetPawns = true, bool preventFriendlyFire = false, bool nonInterruptingSelfCast = false)
        {
            CompRuptureCrown crown = CrownComp;
            if (crown == null)
            {
                return false;
            }

            if (!crown.CanUseNow(out string reason))
            {
                if (CasterPawn != null && CasterPawn.Faction == Faction.OfPlayer)
                {
                    Messages.Message(reason, CasterPawn, MessageTypeDefOf.RejectInput, false);
                }

                return false;
            }

            return base.TryStartCastOn(castTarg, destTarg, surpriseAttack, canHitNonTargetPawns, preventFriendlyFire, nonInterruptingSelfCast);
        }

        protected override bool TryCastShot()
        {
            CompRuptureCrown crown = CrownComp;
            if (crown == null)
            {
                return false;
            }

            Pawn markedPawn = null;
            if (currentTarget.IsValid && currentTarget.HasThing)
            {
                markedPawn = currentTarget.Thing as Pawn;
            }

            bool result = base.TryCastShot();
            if (!result)
            {
                return false;
            }

            if (markedPawn != null)
            {
                RuptureCrownUtility.TryApplyMark(markedPawn);
            }

            crown.NotifyFired(CasterPawn);
            return true;
        }
    }
}
