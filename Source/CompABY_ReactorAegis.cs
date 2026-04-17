using RimWorld;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    public class CompABY_ReactorAegis : ThingComp
    {
        private float currentAegisPoints = -1f;
        private int lastAegisHitTick = -999999;
        private float tunedMaxFactor = 1f;
        private float tunedRechargeFactor = 1f;
        private float tunedDelayFactor = 1f;
        private bool wasBroken;

        public CompProperties_ABY_ReactorAegis Props => (CompProperties_ABY_ReactorAegis)props;

        private Pawn PawnParent => parent as Pawn;

        public float CurrentAegisPoints => Mathf.Max(0f, currentAegisPoints);
        public float MaxAegisPoints => Mathf.Max(1f, Props.maxAegisPoints * Mathf.Max(0.2f, tunedMaxFactor));
        public bool AegisActive => CurrentAegisPoints > 0.5f;
        public float AegisFraction => Mathf.Clamp01(CurrentAegisPoints / MaxAegisPoints);

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            if (currentAegisPoints < 0f)
            {
                currentAegisPoints = MaxAegisPoints;
            }
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref currentAegisPoints, "currentAegisPoints", -1f);
            Scribe_Values.Look(ref lastAegisHitTick, "lastAegisHitTick", -999999);
            Scribe_Values.Look(ref tunedMaxFactor, "tunedMaxFactor", 1f);
            Scribe_Values.Look(ref tunedRechargeFactor, "tunedRechargeFactor", 1f);
            Scribe_Values.Look(ref tunedDelayFactor, "tunedDelayFactor", 1f);
            Scribe_Values.Look(ref wasBroken, "wasBroken", false);
        }

        public override void CompTick()
        {
            base.CompTick();

            Pawn pawn = PawnParent;
            if (!ShouldOperateNow(pawn))
            {
                return;
            }

            if (currentAegisPoints < 0f)
            {
                currentAegisPoints = MaxAegisPoints;
            }

            if (!parent.IsHashIntervalTick(Mathf.Max(15, Props.rechargeIntervalTicks)))
            {
                return;
            }

            if (CurrentAegisPoints >= MaxAegisPoints)
            {
                wasBroken = false;
                return;
            }

            int ticksGame = Find.TickManager != null ? Find.TickManager.TicksGame : 0;
            int effectiveDelay = Mathf.Max(30, Mathf.RoundToInt(Props.rechargeDelayTicks * Mathf.Max(0.2f, tunedDelayFactor)));
            if (ticksGame - lastAegisHitTick < effectiveDelay)
            {
                return;
            }

            float rechargeAmount = Mathf.Max(1f, Props.rechargePerInterval * Mathf.Max(0.1f, tunedRechargeFactor));
            bool wasInactive = !AegisActive;
            currentAegisPoints = Mathf.Min(MaxAegisPoints, CurrentAegisPoints + rechargeAmount);

            if (wasInactive && AegisActive)
            {
                TriggerRestoreFeedback(pawn);
                wasBroken = false;
            }
        }

        public override void PostPreApplyDamage(ref DamageInfo dinfo, out bool absorbed)
        {
            absorbed = false;

            Pawn pawn = PawnParent;
            if (!ShouldOperateNow(pawn) || !AegisActive || !ShouldAbsorbDamage(pawn, dinfo))
            {
                return;
            }

            currentAegisPoints = Mathf.Max(0f, CurrentAegisPoints - Mathf.Max(1f, dinfo.Amount));
            lastAegisHitTick = Find.TickManager != null ? Find.TickManager.TicksGame : 0;
            absorbed = true;

            if (currentAegisPoints <= 0.5f)
            {
                currentAegisPoints = 0f;
                TriggerBreakFeedback(pawn);
                wasBroken = true;
            }
        }

        public void ApplyPhaseTuning(float maxFactor, float rechargeFactor, float delayFactor)
        {
            tunedMaxFactor = Mathf.Max(0.2f, maxFactor);
            tunedRechargeFactor = Mathf.Max(0.1f, rechargeFactor);
            tunedDelayFactor = Mathf.Max(0.2f, delayFactor);
            currentAegisPoints = Mathf.Min(CurrentAegisPoints, MaxAegisPoints);
        }

        public override string CompInspectStringExtra()
        {
            return "Reactor Aegis: " + Mathf.RoundToInt(CurrentAegisPoints) + " / " + Mathf.RoundToInt(MaxAegisPoints);
        }

        private bool ShouldAbsorbDamage(Pawn pawn, DamageInfo dinfo)
        {
            Thing instigator = dinfo.Instigator;
            if (instigator == null || instigator == pawn)
            {
                return false;
            }

            Pawn instigatorPawn = instigator as Pawn;
            if (instigatorPawn != null && instigatorPawn.Faction != null && pawn.Faction != null && !pawn.Faction.HostileTo(instigatorPawn.Faction))
            {
                return false;
            }

            if (dinfo.Def == DamageDefOf.EMP)
            {
                return true;
            }

            if (dinfo.Weapon != null && dinfo.Weapon.IsRangedWeapon)
            {
                return true;
            }

            if (instigator is Building building && building.Faction != null && pawn.Faction != null && pawn.Faction.HostileTo(building.Faction))
            {
                return true;
            }

            if (!instigator.Spawned || instigator.Map != pawn.Map)
            {
                return true;
            }

            return pawn.Position.DistanceTo(instigator.Position) > 2.1f;
        }

        private static bool ShouldOperateNow(Pawn pawn)
        {
            return pawn != null && pawn.Spawned && pawn.MapHeld != null && !pawn.Dead;
        }

        private void TriggerBreakFeedback(Pawn pawn)
        {
            if (pawn?.MapHeld == null)
            {
                return;
            }

            FleckMaker.Static(pawn.PositionHeld, pawn.MapHeld, FleckDefOf.ExplosionFlash, Props.breakFlashScale);
            if (!Props.breakSoundDefName.NullOrEmpty())
            {
                ABY_SoundUtility.PlayAt(Props.breakSoundDefName, pawn.PositionHeld, pawn.MapHeld);
            }
        }

        private void TriggerRestoreFeedback(Pawn pawn)
        {
            if (pawn?.MapHeld == null)
            {
                return;
            }

            FleckMaker.Static(pawn.PositionHeld, pawn.MapHeld, FleckDefOf.ExplosionFlash, Props.restoreFlashScale);
            if (!Props.restoreSoundDefName.NullOrEmpty())
            {
                ABY_SoundUtility.PlayAt(Props.restoreSoundDefName, pawn.PositionHeld, pawn.MapHeld);
            }
        }
    }
}
