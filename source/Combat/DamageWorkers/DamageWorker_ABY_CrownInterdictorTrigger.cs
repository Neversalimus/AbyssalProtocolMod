using RimWorld;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    /// <summary>
    /// Zero-damage extra-melee trigger for Crown Interdictor. RimWorld owns the direct weapon damage;
    /// this worker advances only the weapon-owned two-hit writ and applies a normal or boss-safe Edict Lock.
    /// </summary>
    public class DamageWorker_ABY_CrownInterdictorTrigger : DamageWorker
    {
        public override DamageResult Apply(DamageInfo dinfo, Thing victim)
        {
            DamageResult result = new DamageResult();

            Pawn target = victim as Pawn;
            Pawn wielder = dinfo.Instigator as Pawn;
            if (!IsValidWielder(wielder)
                || target == null
                || target.Dead
                || target.Downed
                || target.Destroyed
                || target.health == null)
            {
                return result;
            }

            CompABY_CrownInterdictor interdictor = ResolveInterdictor(wielder, dinfo.Weapon);
            if (interdictor == null)
            {
                return result;
            }

            CrownInterdictorHitProgress progress = interdictor.RegisterConfirmedHit(wielder, target);
            if (progress == CrownInterdictorHitProgress.WritMarked)
            {
                ShowMarkFeedback(interdictor, target);
                return result;
            }

            if (progress != CrownInterdictorHitProgress.ReadyToLock)
            {
                return result;
            }

            TryApplyEdictLock(interdictor, wielder, target);
            return result;
        }

        private static CompABY_CrownInterdictor ResolveInterdictor(Pawn wielder, ThingDef sourceWeaponDef)
        {
            if (wielder?.equipment == null || sourceWeaponDef == null)
            {
                return null;
            }

            ThingWithComps primary = wielder.equipment.Primary;
            if (primary == null || primary.def != sourceWeaponDef)
            {
                return null;
            }

            return primary.GetComp<CompABY_CrownInterdictor>();
        }

        private static void TryApplyEdictLock(CompABY_CrownInterdictor interdictor, Pawn wielder, Pawn target)
        {
            if (interdictor == null
                || wielder == null
                || target == null
                || target.Dead
                || target.Downed
                || target.Destroyed
                || target.health == null
                || target.MapHeld != wielder.MapHeld
                || !ABY_FactionHostilityUtility.SafeHostileTo(wielder, target))
            {
                return;
            }

            CompProperties_ABY_CrownInterdictor props = interdictor.Props;
            HediffDef authorityScarDef = DefDatabase<HediffDef>.GetNamedSilentFail(props.authorityScarHediffDefName);
            if (authorityScarDef == null)
            {
                return;
            }

            // A shared scar prevents an Interdictor squad from holding the same target under permanent lock.
            if (target.health.hediffSet.GetFirstHediffOfDef(authorityScarDef) != null)
            {
                ShowScarFeedback(target);
                return;
            }

            bool protectedTarget = ABY_AbyssalPawnClassificationUtility.IsBossOrMiniBoss(target);
            string lockDefName = protectedTarget ? props.bossLockHediffDefName : props.normalLockHediffDefName;
            int lockDuration = protectedTarget ? props.bossLockDurationTicks : props.normalLockDurationTicks;
            HediffDef lockDef = DefDatabase<HediffDef>.GetNamedSilentFail(lockDefName);
            if (lockDef == null)
            {
                return;
            }

            ApplyOrRefreshTimedHediff(target, lockDef, Mathf.Max(1, lockDuration));
            ApplyOrRefreshTimedHediff(target, authorityScarDef, Mathf.Max(1, props.authorityScarDurationTicks));

            if (!protectedTarget && props.normalFlinchTicks > 0)
            {
                TryApplyShortFlinch(target, wielder, props.normalFlinchTicks);
            }

            ShowLockFeedback(interdictor, target);
        }

        private static void ApplyOrRefreshTimedHediff(Pawn target, HediffDef hediffDef, int durationTicks)
        {
            if (target == null || target.health == null || hediffDef == null)
            {
                return;
            }

            Hediff hediff = target.health.hediffSet.GetFirstHediffOfDef(hediffDef);
            if (hediff == null)
            {
                hediff = HediffMaker.MakeHediff(hediffDef, target);
                if (hediff == null)
                {
                    return;
                }

                target.health.AddHediff(hediff);
            }

            hediff.Severity = Mathf.Max(hediff.Severity, 1f);
            HediffComp_Disappears disappears = hediff.TryGetComp<HediffComp_Disappears>();
            if (disappears != null)
            {
                disappears.ticksToDisappear = Mathf.Max(1, durationTicks);
            }

            target.health.hediffSet.DirtyCache();
        }

        private static void TryApplyShortFlinch(Pawn target, Pawn wielder, int ticks)
        {
            try
            {
                target.stances?.stunner?.StunFor(Mathf.Max(1, ticks), wielder);
            }
            catch
            {
                // The timed debuff is the authoritative gameplay effect. The flinch is presentation support
                // and must never break compatibility if another framework replaces a pawn's stance handler.
            }
        }

        private static void ShowMarkFeedback(CompABY_CrownInterdictor interdictor, Pawn target)
        {
            Map map = target?.MapHeld;
            if (map == null)
            {
                return;
            }

            FleckMaker.ThrowLightningGlow(target.DrawPos, map, Mathf.Max(0.15f, interdictor.Props.markVisualScale));
        }

        private static void ShowLockFeedback(CompABY_CrownInterdictor interdictor, Pawn target)
        {
            Map map = target?.MapHeld;
            if (map == null)
            {
                return;
            }

            CompProperties_ABY_CrownInterdictor props = interdictor.Props;
            FleckMaker.ThrowLightningGlow(target.DrawPos, map, Mathf.Max(0.25f, props.lockVisualScale));
            FleckMaker.ThrowMicroSparks(target.DrawPos, map);
            FleckMaker.Static(
                target.PositionHeld,
                map,
                FleckDefOf.ExplosionFlash,
                Mathf.Max(0.15f, props.lockFlashScale));

            if (!props.lockSoundDefName.NullOrEmpty())
            {
                ABY_SoundUtility.PlayAt(props.lockSoundDefName, target.PositionHeld, map);
            }
        }

        private static void ShowScarFeedback(Pawn target)
        {
            Map map = target?.MapHeld;
            if (map != null)
            {
                FleckMaker.ThrowMicroSparks(target.DrawPos, map);
            }
        }

        private static bool IsValidWielder(Pawn pawn)
        {
            return pawn != null
                && !pawn.Dead
                && !pawn.Destroyed
                && pawn.equipment != null
                && pawn.MapHeld != null;
        }
    }
}
