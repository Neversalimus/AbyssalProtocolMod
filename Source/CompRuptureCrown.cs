using System.Collections;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    public static class RuptureCrownUtility
    {
        public const string MarkDefName = "ABY_RuptureSentenceMark";

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
                disappears.ticksToDisappear = Mathf.Max(disappears.ticksToDisappear, 1080);
            }

            pawn.health.hediffSet.DirtyCache();
            return true;
        }
    }

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
        private static Texture2D cachedCommandIcon;

        public CompProperties_RuptureCrown Props => (CompProperties_RuptureCrown)props;

        private static Texture2D CommandIcon => cachedCommandIcon ??= ContentFinder<Texture2D>.Get("Things/Item/ABY_CrownOfRupture", true);

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

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (Gizmo gizmo in GetSharedGizmos())
            {
                yield return gizmo;
            }
        }

        public override IEnumerable<Gizmo> CompGetWornGizmosExtra()
        {
            foreach (Gizmo gizmo in GetSharedGizmos())
            {
                yield return gizmo;
            }
        }

        private IEnumerable<Gizmo> GetSharedGizmos()
        {
            CompEquippable equippable = parent.TryGetComp<CompEquippable>();
            Verb_RuptureSentence verb = equippable?.PrimaryVerb as Verb_RuptureSentence;
            Pawn casterPawn = verb?.CasterPawn;
            if (verb == null || casterPawn == null || casterPawn.Faction != Faction.OfPlayer)
            {
                yield break;
            }

            Command_RuptureSentence command = new Command_RuptureSentence(this, verb)
            {
                defaultLabel = "Rupture Sentence",
                defaultDesc = "Condemn a single visible hostile with the hidden archon's verdict. The target becomes slower, less accurate and more vulnerable for a short time.\n\n" + CompInspectStringExtra(),
                icon = CommandIcon,
                order = 220f,
                hotKey = KeyBindingDefOf.Misc1
            };

            if (!CanUseNow(out string reason))
            {
                command.Disable(reason);
            }

            yield return command;
        }
    }

    public class Command_RuptureSentence : Command_VerbTarget
    {
        private readonly CompRuptureCrown crown;

        public Command_RuptureSentence(CompRuptureCrown crown, Verb_RuptureSentence verb)
        {
            this.crown = crown;
            this.verb = verb;
        }

        public override string TopRightLabel
        {
            get
            {
                if (crown == null)
                {
                    return null;
                }

                if (crown.IsRecharged)
                {
                    return "RDY";
                }

                int hours = Mathf.Max(1, Mathf.CeilToInt(crown.TicksUntilRecharged / 2500f));
                return hours + "h";
            }
        }

        public override string Desc
        {
            get
            {
                string baseDesc = base.Desc;
                if (crown == null)
                {
                    return baseDesc;
                }

                return baseDesc + "\n\n" + crown.CompInspectStringExtra();
            }
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

            bool result = base.TryCastShot();
            if (!result)
            {
                return false;
            }

            if (currentTarget.IsValid && currentTarget.HasThing && currentTarget.Thing is Pawn pawn)
            {
                RuptureCrownUtility.TryApplyMark(pawn);
            }

            crown.NotifyFired(CasterPawn);
            return true;
        }
    }
}
