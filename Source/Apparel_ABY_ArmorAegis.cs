using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    /// <summary>
    /// Passive armor-mounted aegis shield.
    ///
    /// This intentionally follows the vanilla shield-belt interception point
    /// by overriding Apparel.CheckPreAbsorbDamage, but it does not inherit the
    /// vanilla shield belt class and therefore does not block the wearer's own
    /// outgoing ranged attacks.
    /// </summary>
    public class Apparel_ABY_ArmorAegis : Apparel
    {
        private float currentShieldPoints = -1f;
        private int lastHitTick = -999999;
        private int lastRechargeTick = -999999;
        private string trackedDefName = string.Empty;
        private bool wasCollapsed;

        private DefModExtension_ABY_ApparelAegis AegisExtension => def?.GetModExtension<DefModExtension_ABY_ApparelAegis>();

        private int CurrentTick => Find.TickManager != null ? Find.TickManager.TicksGame : 0;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref currentShieldPoints, "currentShieldPoints", -1f);
            Scribe_Values.Look(ref lastHitTick, "lastHitTick", -999999);
            Scribe_Values.Look(ref lastRechargeTick, "lastRechargeTick", -999999);
            Scribe_Values.Look(ref trackedDefName, "trackedDefName", string.Empty);
            Scribe_Values.Look(ref wasCollapsed, "wasCollapsed", false);
        }

        protected override void Tick()
        {
            base.Tick();

            DefModExtension_ABY_ApparelAegis ext = AegisExtension;
            if (ext == null || Wearer == null || Wearer.Dead)
            {
                return;
            }

            SyncShield(ext);
            if (IsSuppressedByExternalShield(Wearer, ext))
            {
                return;
            }

            ApplyRecharge(ext);
        }

        public override bool CheckPreAbsorbDamage(DamageInfo dinfo)
        {
            DefModExtension_ABY_ApparelAegis ext = AegisExtension;
            if (ext == null || Wearer == null || Wearer.Dead)
            {
                return false;
            }

            SyncShield(ext);

            if (IsSuppressedByExternalShield(Wearer, ext))
            {
                return false;
            }

            ApplyRecharge(ext);

            if (currentShieldPoints <= 0.5f || !ShouldAbsorbDamage(Wearer, dinfo, ext))
            {
                return false;
            }

            float drain = ResolveShieldDrain(dinfo, ext);
            if (drain <= 0f)
            {
                return false;
            }

            currentShieldPoints = Mathf.Max(0f, currentShieldPoints - drain);
            lastHitTick = CurrentTick;
            lastRechargeTick = CurrentTick;
            TriggerHitFeedback(Wearer, ext);

            if (currentShieldPoints <= 0.5f)
            {
                currentShieldPoints = 0f;
                wasCollapsed = true;
                TriggerBreakFeedback(Wearer, ext);
            }

            return true;
        }

        public override IEnumerable<Gizmo> GetWornGizmos()
        {
            foreach (Gizmo gizmo in base.GetWornGizmos())
            {
                yield return gizmo;
            }

            // The visible player-facing status gizmo is emitted by
            // CompABY_WornArmorAegisTracker so it reflects the same shield state
            // that handles damage absorption and lazy recharge. Keeping this class
            // quiet prevents duplicate or desynced aegis gizmos when the pawn-level
            // tracker is also present on humanlike races.
        }

        public override string GetInspectString()
        {
            string baseString = base.GetInspectString();
            DefModExtension_ABY_ApparelAegis ext = AegisExtension;
            if (ext == null)
            {
                return baseString;
            }

            SyncShield(ext);
            bool suppressed = Wearer != null && IsSuppressedByExternalShield(Wearer, ext);
            if (!suppressed)
            {
                ApplyRecharge(ext);
            }

            string line = ABY_ApparelAegisUtility.AegisLabel(ext) + ": ";
            if (suppressed)
            {
                line += ABY_ApparelAegisUtility.TranslateOrFallback(ext.suppressedKey, "suppressed by external shield");
            }
            else
            {
                line += ABY_ApparelAegisUtility.FormatPoints(currentShieldPoints, ext.MaxShieldPointsSafe) + " — " + CurrentStateLabel(ext);
            }

            return baseString.NullOrEmpty() ? line : baseString + "\n" + line;
        }

        private void SyncShield(DefModExtension_ABY_ApparelAegis ext)
        {
            string defName = def?.defName ?? string.Empty;
            if (trackedDefName == defName && currentShieldPoints >= 0f)
            {
                currentShieldPoints = Mathf.Min(currentShieldPoints, ext.MaxShieldPointsSafe);
                return;
            }

            trackedDefName = defName;
            currentShieldPoints = ext.MaxShieldPointsSafe;
            lastHitTick = -999999;
            lastRechargeTick = CurrentTick;
            wasCollapsed = false;
        }

        private void ApplyRecharge(DefModExtension_ABY_ApparelAegis ext)
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
                TriggerRestoreFeedback(Wearer, ext);
            }
        }

        private static bool IsSuppressedByExternalShield(Pawn wearer, DefModExtension_ABY_ApparelAegis ext)
        {
            if (wearer?.apparel?.WornApparel == null || ext == null || !ext.suppressWhenExternalShieldWorn)
            {
                return false;
            }

            List<Apparel> wornApparel = wearer.apparel.WornApparel;
            for (int i = 0; i < wornApparel.Count; i++)
            {
                Apparel apparel = wornApparel[i];
                if (apparel == null || ABY_ApparelAegisUtility.HasAegisExtension(apparel))
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

        private static bool ShouldAbsorbDamage(Pawn wearer, DamageInfo dinfo, DefModExtension_ABY_ApparelAegis ext)
        {
            if (wearer == null || ext == null)
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
            if (instigator == wearer)
            {
                return false;
            }

            // Some projectile and modded damage paths arrive without a stable instigator/weapon.
            // Treat unknown non-EMP, non-explosive incoming damage as ranged-like so the aegis
            // actually protects against modded projectiles and dev-test shots.
            if (instigator == null)
            {
                return ext.absorbRanged;
            }

            if (instigator.MapHeld == wearer.MapHeld && instigator.Spawned && wearer.Spawned)
            {
                float distance = wearer.Position.DistanceTo(instigator.Position);
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

        private string BuildGizmoDescription(DefModExtension_ABY_ApparelAegis ext, bool suppressed, string state)
        {
            string desc = LabelCap.ToString();
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

        private static void TriggerHitFeedback(Pawn wearer, DefModExtension_ABY_ApparelAegis ext)
        {
            if (wearer?.MapHeld == null || ext == null)
            {
                return;
            }

            FleckMaker.Static(wearer.PositionHeld, wearer.MapHeld, FleckDefOf.ExplosionFlash, Mathf.Max(0.1f, ext.hitFlashScale));
            ABY_ApparelAegisUtility.PlaySound(ext.hitSoundDefName, wearer.PositionHeld, wearer.MapHeld);
        }

        private static void TriggerBreakFeedback(Pawn wearer, DefModExtension_ABY_ApparelAegis ext)
        {
            if (wearer?.MapHeld == null || ext == null)
            {
                return;
            }

            FleckMaker.Static(wearer.PositionHeld, wearer.MapHeld, FleckDefOf.ExplosionFlash, Mathf.Max(0.1f, ext.breakFlashScale));
            ABY_ApparelAegisUtility.PlaySound(ext.breakSoundDefName, wearer.PositionHeld, wearer.MapHeld);
        }

        private static void TriggerRestoreFeedback(Pawn wearer, DefModExtension_ABY_ApparelAegis ext)
        {
            if (wearer?.MapHeld == null || ext == null)
            {
                return;
            }

            FleckMaker.Static(wearer.PositionHeld, wearer.MapHeld, FleckDefOf.ExplosionFlash, Mathf.Max(0.1f, ext.restoreFlashScale));
            ABY_ApparelAegisUtility.PlaySound(ext.restoreSoundDefName, wearer.PositionHeld, wearer.MapHeld);
        }
    }
}
