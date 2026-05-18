using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    public static class ABY_BossTrueDeathUtility
    {
        private static readonly System.Reflection.FieldInfo HealthTrackerPawnField = AccessTools.Field(typeof(Pawn_HealthTracker), "pawn");

        public static Pawn GetPawn(Pawn_HealthTracker tracker)
        {
            if (tracker == null || HealthTrackerPawnField == null)
            {
                return null;
            }

            try
            {
                return HealthTrackerPawnField.GetValue(tracker) as Pawn;
            }
            catch
            {
                return null;
            }
        }

        public static CompABY_BossTrueDeath GetComp(Pawn pawn)
        {
            return pawn?.TryGetComp<CompABY_BossTrueDeath>();
        }

        public static bool ShouldSuppressVanillaHealthState(Pawn pawn)
        {
            // This method is called from Pawn_HealthTracker.ShouldBeDead/ShouldBeDowned postfixes.
            // Those can run very often while a boss is active, so the first check must be a cheap
            // comp lookup. Never call the reflective/StackTrace debug-tool detector here.
            CompABY_BossTrueDeath comp = GetComp(pawn);
            if (comp == null)
            {
                return false;
            }

            return comp.ShouldSuppressVanillaDeathOrDowned();
        }

        public static bool TrySuppressPawnKill(Pawn pawn, DamageInfo? dinfo, Hediff exactCulprit)
        {
            // Pawn.Kill is rare, but keep the same cheap-first rule. Debug-tool bypass is driven by
            // the recent input marker instead of a per-call StackTrace probe.
            CompABY_BossTrueDeath comp = GetComp(pawn);
            if (comp == null)
            {
                return false;
            }

            if (ABY_DevToolUtility.IsDebugToolActiveOrExecuting())
            {
                comp.AuthorizeDevToolKill();
                return false;
            }

            return comp.TrySuppressPrematureKill(dinfo, exactCulprit);
        }

        public static void SuppressDowned(Pawn pawn, DamageInfo? dinfo, Hediff hediff)
        {
            CompABY_BossTrueDeath comp = GetComp(pawn);
            comp?.SuppressDownedState(dinfo, hediff);
        }

        public static bool TryGetBossHp(Pawn pawn, out float current, out float max, out float pct)
        {
            current = 0f;
            max = 0f;
            pct = 0f;
            CompABY_BossTrueDeath comp = GetComp(pawn);
            if (comp == null)
            {
                return false;
            }

            comp.EnsureInitialized();
            current = comp.CurrentBossHitPoints;
            max = comp.MaxBossHitPoints;
            pct = comp.HealthPercent;
            return max > 0.001f;
        }


        public static float ResolveBossHealthPercentForPhase(Pawn pawn)
        {
            if (pawn == null)
            {
                return 1f;
            }

            float current;
            float max;
            float pct;
            if (TryGetBossHp(pawn, out current, out max, out pct))
            {
                return Mathf.Clamp01(pct);
            }

            try
            {
                if (pawn.health?.summaryHealth != null)
                {
                    return Mathf.Clamp01(pawn.health.summaryHealth.SummaryHealthPercent);
                }
            }
            catch
            {
            }

            return 1f;
        }

        public static void StabilizePawnBody(Pawn pawn, CompProperties_ABY_BossTrueDeath props)
        {
            if (pawn == null || pawn.Destroyed || pawn.Dead || pawn.health?.hediffSet == null || props == null)
            {
                return;
            }

            ABY_AbyssalConstructPhysiologyUtility.ScrubConstructHediffs(pawn);
            ClampHediffSeverity(pawn, HediffDefOf.BloodLoss, Mathf.Max(0f, props.bloodLossClamp));
            ClampHediffSeverity(pawn, HediffDefOf.Heatstroke, Mathf.Max(0f, props.heatstrokeClamp));
            ClampHediffSeverity(pawn, DefDatabase<HediffDef>.GetNamedSilentFail("ToxicBuildup"), Mathf.Max(0f, props.toxicBuildupClamp));
            ClampHediffSeverity(pawn, DefDatabase<HediffDef>.GetNamedSilentFail("Hypothermia"), Mathf.Max(0f, props.heatstrokeClamp));

            List<Hediff> hediffs = pawn.health.hediffSet.hediffs;
            if (hediffs == null)
            {
                return;
            }

            List<Hediff> snapshot = new List<Hediff>(hediffs);
            for (int i = 0; i < snapshot.Count; i++)
            {
                Hediff hediff = snapshot[i];
                if (hediff == null || hediff.pawn != pawn)
                {
                    continue;
                }

                if (hediff is Hediff_AddedPart)
                {
                    continue;
                }

                if (hediff is Hediff_MissingPart)
                {
                    if (props.restoreMissingParts)
                    {
                        TryRemoveHediff(pawn, hediff);
                    }
                    continue;
                }

                if (hediff is Hediff_Injury injury)
                {
                    if (!injury.IsPermanent())
                    {
                        injury.Heal(Mathf.Max(1f, props.stabilizeInjuryHealAmount));
                    }
                    continue;
                }

                if (props.removeLethalBadHediffs && ShouldRemoveLethalBadHediff(hediff))
                {
                    TryRemoveHediff(pawn, hediff);
                }
            }

            pawn.health.hediffSet.DirtyCache();
            try
            {
                pawn.health.CheckForStateChange(null, null);
            }
            catch
            {
            }
        }

        private static bool ShouldRemoveLethalBadHediff(Hediff hediff)
        {
            if (hediff?.def == null)
            {
                return false;
            }

            if (hediff is Hediff_AddedPart)
            {
                return false;
            }

            bool lethalNow = false;
            try
            {
                lethalNow = hediff.CauseDeathNow() || hediff.IsCurrentlyLifeThreatening || hediff.IsLethal;
            }
            catch
            {
                lethalNow = false;
            }

            return lethalNow && hediff.def.isBad;
        }

        private static void ClampHediffSeverity(Pawn pawn, HediffDef def, float maxSeverity)
        {
            if (pawn?.health?.hediffSet == null || def == null)
            {
                return;
            }

            Hediff hediff = pawn.health.hediffSet.GetFirstHediffOfDef(def);
            if (hediff != null && hediff.Severity > maxSeverity)
            {
                hediff.Severity = maxSeverity;
            }
        }

        private static void TryRemoveHediff(Pawn pawn, Hediff hediff)
        {
            if (pawn?.health == null || hediff == null)
            {
                return;
            }

            try
            {
                pawn.health.RemoveHediff(hediff);
            }
            catch
            {
            }
        }
    }
}
