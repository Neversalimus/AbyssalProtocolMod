using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    public class CompABY_WornArmorAegisTracker : ThingComp
    {
        private float currentShieldPoints = -1f;
        private int lastHitTick = -999999;
        private int lastRechargeTick = -999999;
        private int lastArmorChangeTick = -999999;
        private string trackedArmorDefName = string.Empty;
        private bool wasCollapsed;
        private bool wasSuppressed;

        private Pawn PawnParent => parent as Pawn;

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref currentShieldPoints, "currentShieldPoints", -1f);
            Scribe_Values.Look(ref lastHitTick, "lastHitTick", -999999);
            Scribe_Values.Look(ref lastRechargeTick, "lastRechargeTick", -999999);
            Scribe_Values.Look(ref lastArmorChangeTick, "lastArmorChangeTick", -999999);
            Scribe_Values.Look(ref trackedArmorDefName, "trackedArmorDefName", string.Empty);
            Scribe_Values.Look(ref wasCollapsed, "wasCollapsed", false);
            Scribe_Values.Look(ref wasSuppressed, "wasSuppressed", false);
        }

        public override void CompTick()
        {
            base.CompTick();

            Pawn pawn = PawnParent;
            if (!CanOperateOn(pawn))
            {
                return;
            }

            // TPS guard: this tracker is only a fallback/UI synchronizer. The real
            // damage interception path is event-driven through the worn apparel and
            // PostPreApplyDamage, so there is no need to scan apparel every tick on
            // every humanlike pawn. Spread checks across ticks to avoid spikes in
            // large colonies and raid maps.
            int tick = CurrentTick;
            if ((tick + pawn.thingIDNumber) % 60 != 0)
            {
                return;
            }

            Apparel armor = ResolveAegisArmor(pawn, out DefModExtension_ABY_ApparelAegis ext);
            if (armor == null || ext == null)
            {
                ResetIfNoArmor();
                return;
            }

            SyncTrackedArmor(armor, ext);

            if (IsSuppressedByExternalShield(pawn, armor, ext))
            {
                wasSuppressed = true;
                return;
            }

            wasSuppressed = false;
            ApplyRecharge(pawn, ext);
        }

        public override void PostPreApplyDamage(ref DamageInfo dinfo, out bool absorbed)
        {
            absorbed = false;

            Pawn pawn = PawnParent;
            if (!CanOperateOn(pawn))
            {
                return;
            }

            Apparel armor = ResolveAegisArmor(pawn, out DefModExtension_ABY_ApparelAegis ext);
            if (armor == null || ext == null)
            {
                ResetIfNoArmor();
                return;
            }

            SyncTrackedArmor(armor, ext);

            if (IsSuppressedByExternalShield(pawn, armor, ext))
            {
                wasSuppressed = true;
                return;
            }

            wasSuppressed = false;
            ApplyRecharge(pawn, ext);

            if (currentShieldPoints <= 0.5f || !ShouldAbsorbDamage(pawn, dinfo, ext))
            {
                return;
            }

            float drain = ResolveShieldDrain(dinfo, ext);
            if (drain <= 0f)
            {
                return;
            }

            currentShieldPoints = Mathf.Max(0f, currentShieldPoints - drain);
            lastHitTick = CurrentTick;
            lastRechargeTick = CurrentTick;
            absorbed = true;
            TriggerHitFeedback(pawn, ext);

            if (currentShieldPoints <= 0.5f)
            {
                currentShieldPoints = 0f;
                wasCollapsed = true;
                TriggerBreakFeedback(pawn, ext);
            }
        }

        public override string CompInspectStringExtra()
        {
            Pawn pawn = PawnParent;
            if (!CanOperateOn(pawn))
            {
                return null;
            }

            Apparel armor = ResolveAegisArmor(pawn, out DefModExtension_ABY_ApparelAegis ext);
            if (armor == null || ext == null)
            {
                return null;
            }

            SyncTrackedArmor(armor, ext);
            string label = ABY_ApparelAegisUtility.AegisLabel(ext);
            if (IsSuppressedByExternalShield(pawn, armor, ext))
            {
                return label + ": " + ABY_ApparelAegisUtility.TranslateOrFallback(ext.suppressedKey, "suppressed by external shield");
            }

            ApplyRecharge(pawn, ext);
            string state = CurrentStateLabel(ext);
            return label + ": " + ABY_ApparelAegisUtility.FormatPoints(currentShieldPoints, ext.MaxShieldPointsSafe) + " — " + state;
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (Gizmo gizmo in base.CompGetGizmosExtra())
            {
                yield return gizmo;
            }

            Pawn pawn = PawnParent;
            if (!CanOperateOn(pawn) || !pawn.IsColonistPlayerControlled)
            {
                yield break;
            }

            Apparel armor = ResolveAegisArmor(pawn, out DefModExtension_ABY_ApparelAegis ext);
            if (armor == null || ext == null)
            {
                yield break;
            }

            SyncTrackedArmor(armor, ext);
            bool suppressed = IsSuppressedByExternalShield(pawn, armor, ext);
            if (!suppressed)
            {
                ApplyRecharge(pawn, ext);
            }

            string label = ABY_ApparelAegisUtility.AegisLabel(ext);
            string points = ABY_ApparelAegisUtility.FormatPoints(currentShieldPoints, ext.MaxShieldPointsSafe);
            string state = suppressed ? ABY_ApparelAegisUtility.TranslateOrFallback(ext.suppressedKey, "suppressed by external shield") : CurrentStateLabel(ext);

            Command_Action command = new Command_Action
            {
                defaultLabel = label + " " + points,
                defaultDesc = BuildGizmoDescription(armor, ext, suppressed, state),
                icon = armor.def?.uiIcon,
                action = delegate { }
            };
            command.Disable(state);
            yield return command;
        }

        private static bool CanOperateOn(Pawn pawn)
        {
            return pawn != null && !pawn.Dead && pawn.health != null;
        }

        private int CurrentTick => Find.TickManager != null ? Find.TickManager.TicksGame : 0;

        private void ResetIfNoArmor()
        {
            trackedArmorDefName = string.Empty;
            currentShieldPoints = -1f;
            wasCollapsed = false;
            wasSuppressed = false;
        }

        private void SyncTrackedArmor(Apparel armor, DefModExtension_ABY_ApparelAegis ext)
        {
            string defName = armor?.def?.defName ?? string.Empty;
            if (defName == trackedArmorDefName && currentShieldPoints >= 0f)
            {
                currentShieldPoints = Mathf.Min(currentShieldPoints, ext.MaxShieldPointsSafe);
                return;
            }

            trackedArmorDefName = defName;
            currentShieldPoints = ext.MaxShieldPointsSafe;
            lastArmorChangeTick = CurrentTick;
            lastHitTick = -999999;
            lastRechargeTick = CurrentTick;
            wasCollapsed = false;
            wasSuppressed = false;
        }

        private void ApplyRecharge(Pawn pawn, DefModExtension_ABY_ApparelAegis ext)
        {
            if (ext == null || currentShieldPoints < 0f)
            {
                return;
            }

            float max = ext.MaxShieldPointsSafe;
            if (currentShieldPoints >= max - 0.01f)
            {
                currentShieldPoints = max;
                wasCollapsed = false;
                lastRechargeTick = CurrentTick;
                return;
            }

            int tick = CurrentTick;
            int rechargeStartTick = lastHitTick + ext.RechargeDelayTicksSafe;
            if (tick < rechargeStartTick)
            {
                return;
            }

            int interval = ext.RechargeIntervalTicksSafe;
            int effectiveFrom = Mathf.Max(lastRechargeTick, rechargeStartTick);
            if (effectiveFrom < rechargeStartTick)
            {
                effectiveFrom = rechargeStartTick;
            }

            int elapsed = tick - effectiveFrom;
            if (elapsed < interval)
            {
                return;
            }

            int intervals = elapsed / interval;
            if (intervals <= 0)
            {
                return;
            }

            bool wasInactive = currentShieldPoints <= 0.5f;
            currentShieldPoints = Mathf.Min(max, currentShieldPoints + intervals * ext.RechargePerIntervalSafe);
            lastRechargeTick = effectiveFrom + intervals * interval;

            if (currentShieldPoints >= max - 0.01f)
            {
                currentShieldPoints = max;
                wasCollapsed = false;
            }

            if (wasInactive && currentShieldPoints > 0.5f)
            {
                wasCollapsed = false;
                TriggerRestoreFeedback(pawn, ext);
            }
        }

        private static Apparel ResolveAegisArmor(Pawn pawn, out DefModExtension_ABY_ApparelAegis ext)
        {
            ext = null;
            if (pawn?.apparel?.WornApparel == null)
            {
                return null;
            }

            Apparel best = null;
            DefModExtension_ABY_ApparelAegis bestExt = null;
            float bestMax = -1f;
            List<Apparel> wornApparel = pawn.apparel.WornApparel;
            for (int i = 0; i < wornApparel.Count; i++)
            {
                Apparel apparel = wornApparel[i];
                DefModExtension_ABY_ApparelAegis candidateExt = ABY_ApparelAegisUtility.GetAegisExtension(apparel);
                if (candidateExt == null)
                {
                    continue;
                }

                if (candidateExt.MaxShieldPointsSafe > bestMax)
                {
                    best = apparel;
                    bestExt = candidateExt;
                    bestMax = candidateExt.MaxShieldPointsSafe;
                }
            }

            ext = bestExt;
            return best;
        }

        private static bool IsSuppressedByExternalShield(Pawn pawn, Apparel aegisArmor, DefModExtension_ABY_ApparelAegis ext)
        {
            if (pawn?.apparel?.WornApparel == null || ext == null || !ext.suppressWhenExternalShieldWorn)
            {
                return false;
            }

            List<Apparel> wornApparel = pawn.apparel.WornApparel;
            for (int i = 0; i < wornApparel.Count; i++)
            {
                Apparel apparel = wornApparel[i];
                if (apparel == null || apparel == aegisArmor)
                {
                    continue;
                }

                if (ABY_ApparelAegisUtility.IsExternalShieldApparel(apparel))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool ShouldAbsorbDamage(Pawn pawn, DamageInfo dinfo, DefModExtension_ABY_ApparelAegis ext)
        {
            if (ext == null || pawn == null)
            {
                return false;
            }

            if (dinfo.Def == DamageDefOf.EMP)
            {
                return ext.empDrainsShield;
            }

            if (ext.absorbExplosive && ABY_ApparelAegisUtility.IsExplosiveDamage(dinfo.Def))
            {
                return true;
            }

            if (dinfo.Weapon != null && dinfo.Weapon.IsRangedWeapon)
            {
                return ext.absorbRanged;
            }

            Thing instigator = dinfo.Instigator;
            if (instigator == pawn)
            {
                return false;
            }

            // Keep this permissive on purpose: dev tests, friendly-fire tests and many modded
            // projectile paths do not always provide a hostile pawn faction or a ranged weapon def.
            // The aegis is an incoming-damage shield, not a faction-filtered hostile-only shield.
            if (instigator == null)
            {
                return ext.absorbRanged;
            }

            if (instigator.MapHeld == pawn.MapHeld && instigator.Spawned && pawn.Spawned)
            {
                float distance = pawn.Position.DistanceTo(instigator.Position);
                if (distance <= 2.1f)
                {
                    return ext.absorbMelee;
                }

                return ext.absorbRanged;
            }

            return ext.absorbRanged;
        }

        private static float ResolveShieldDrain(DamageInfo dinfo, DefModExtension_ABY_ApparelAegis ext)
        {
            float amount = Mathf.Max(1f, dinfo.Amount);
            if (dinfo.Def == DamageDefOf.EMP && ext != null)
            {
                amount *= Mathf.Max(0.1f, ext.empDrainMultiplier);
            }

            return amount;
        }

        private string CurrentStateLabel(DefModExtension_ABY_ApparelAegis ext)
        {
            if (currentShieldPoints <= 0.5f || wasCollapsed)
            {
                return ABY_ApparelAegisUtility.TranslateOrFallback(ext.collapsedKey, "collapsed");
            }

            if (currentShieldPoints >= ext.MaxShieldPointsSafe - 0.5f)
            {
                return ABY_ApparelAegisUtility.TranslateOrFallback(ext.stableKey, "stable");
            }

            return ABY_ApparelAegisUtility.TranslateOrFallback(ext.rechargingKey, "recharging");
        }

        private string BuildGizmoDescription(Apparel armor, DefModExtension_ABY_ApparelAegis ext, bool suppressed, string state)
        {
            string desc = armor?.LabelCap.ToString() ?? ABY_ApparelAegisUtility.AegisLabel(ext);
            desc += "\n" + ABY_ApparelAegisUtility.TranslateOrFallback("ABY_ApparelAegis_GizmoState", "State") + ": " + state;
            desc += "\n" + ABY_ApparelAegisUtility.TranslateOrFallback("ABY_ApparelAegis_GizmoCharge", "Charge") + ": " + ABY_ApparelAegisUtility.FormatPoints(currentShieldPoints, ext.MaxShieldPointsSafe);
            desc += "\n" + ABY_ApparelAegisUtility.TranslateOrFallback("ABY_ApparelAegis_GizmoDoesNotBlockOutgoing", "Does not block outgoing fire.");
            desc += "\n" + ABY_ApparelAegisUtility.TranslateOrFallback("ABY_ApparelAegis_GizmoNoStack", "Suppressed while an external shield belt or shield apparel is worn.");
            if (!suppressed && currentShieldPoints < ext.MaxShieldPointsSafe)
            {
                int remainingDelay = Mathf.Max(0, ext.RechargeDelayTicksSafe - (CurrentTick - lastHitTick));
                if (remainingDelay > 0)
                {
                    desc += "\n" + ABY_ApparelAegisUtility.TranslateOrFallback("ABY_ApparelAegis_GizmoRechargeDelay", "Recharge delay") + ": " + ABY_ApparelAegisUtility.SecondsFromTicks(remainingDelay);
                }
            }

            return desc;
        }

        private static void TriggerHitFeedback(Pawn pawn, DefModExtension_ABY_ApparelAegis ext)
        {
            if (pawn?.MapHeld == null || ext == null)
            {
                return;
            }

            FleckMaker.Static(pawn.PositionHeld, pawn.MapHeld, FleckDefOf.ExplosionFlash, Mathf.Max(0.1f, ext.hitFlashScale));
            ABY_ApparelAegisUtility.PlaySound(ext.hitSoundDefName, pawn.PositionHeld, pawn.MapHeld);
        }

        private static void TriggerBreakFeedback(Pawn pawn, DefModExtension_ABY_ApparelAegis ext)
        {
            if (pawn?.MapHeld == null || ext == null)
            {
                return;
            }

            FleckMaker.Static(pawn.PositionHeld, pawn.MapHeld, FleckDefOf.ExplosionFlash, Mathf.Max(0.1f, ext.breakFlashScale));
            ABY_ApparelAegisUtility.PlaySound(ext.breakSoundDefName, pawn.PositionHeld, pawn.MapHeld);
        }

        private static void TriggerRestoreFeedback(Pawn pawn, DefModExtension_ABY_ApparelAegis ext)
        {
            if (pawn?.MapHeld == null || ext == null)
            {
                return;
            }

            FleckMaker.Static(pawn.PositionHeld, pawn.MapHeld, FleckDefOf.ExplosionFlash, Mathf.Max(0.1f, ext.restoreFlashScale));
            ABY_ApparelAegisUtility.PlaySound(ext.restoreSoundDefName, pawn.PositionHeld, pawn.MapHeld);
        }
    }
}
