using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    public class CompProperties_RuptureCrown : CompProperties
    {
        public int rechargeTicks = GenDate.TicksPerDay;
        public string ringMoteDef = "ABY_Mote_RuptureHaloRing";
        public string coreMoteDef = "ABY_Mote_RuptureHaloCore";

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

            yield return new StatDrawEntry(StatCategoryDefOf.BasicsNonPawnImportant, "Rupture recharge", GenDate.ToStringTicksToDays(Props.rechargeTicks), "Time needed to condense another verdict charge after firing.", 1000);
        }
    }

    public class Verb_RuptureSentence : Verb_Shoot
    {
        private CompRuptureCrown CrownComp => EquipmentSource?.GetComp<CompRuptureCrown>();

        public override bool Available()
        {
            return base.Available() && CrownComp != null;
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

            bool result = base.TryCastShot();
            if (result)
            {
                crown.NotifyFired(CasterPawn);
            }

            return result;
        }
    }
}
