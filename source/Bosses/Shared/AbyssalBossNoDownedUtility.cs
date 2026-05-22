using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    public static class AbyssalBossNoDownedUtility
    {
        public static bool TryPreventDowned(Pawn pawn, float bloodLossClamp, float heatstrokeClamp, float healWorstInjuryAmount, int maxHealPasses, bool forceLordReengage)
        {
            if (pawn == null || pawn.Dead || pawn.health == null)
            {
                return false;
            }

            if (!pawn.Downed)
            {
                return false;
            }

            bool changed = false;
            changed |= ClampHediffSeverity(pawn, HediffDefOf.BloodLoss, bloodLossClamp);
            changed |= ClampHediffSeverity(pawn, HediffDefOf.Heatstroke, heatstrokeClamp);

            int passes = Mathf.Max(1, maxHealPasses);
            for (int i = 0; i < passes; i++)
            {
                if (!HealWorstVisibleInjury(pawn, healWorstInjuryAmount))
                {
                    break;
                }

                changed = true;
            }

            if (changed)
            {
                RefreshHealthState(pawn);
            }

            if (pawn.Downed && HealRandomNonPermanentInjury(pawn, healWorstInjuryAmount * 0.75f))
            {
                changed = true;
                RefreshHealthState(pawn);
            }

            if (!pawn.Downed && forceLordReengage && pawn.Spawned && pawn.MapHeld != null && pawn.Faction != null && ABY_FactionHostilityUtility.SafeHostileToPlayer(pawn))
            {
                AbyssalLordUtility.EnsureAssaultLord(pawn, sappers: true);
            }

            return changed;
        }

        private static bool ClampHediffSeverity(Pawn pawn, HediffDef def, float maxSeverity)
        {
            if (pawn?.health?.hediffSet == null || def == null)
            {
                return false;
            }

            Hediff hediff = pawn.health.hediffSet.GetFirstHediffOfDef(def);
            if (hediff != null && hediff.Severity > maxSeverity)
            {
                hediff.Severity = maxSeverity;
                return true;
            }

            return false;
        }

        private static bool HealWorstVisibleInjury(Pawn pawn, float amount)
        {
            if (pawn?.health?.hediffSet?.hediffs == null || amount <= 0f)
            {
                return false;
            }

            Hediff_Injury best = null;
            float bestSeverity = 0f;
            List<Hediff> hediffs = pawn.health.hediffSet.hediffs;
            for (int i = 0; i < hediffs.Count; i++)
            {
                if (!(hediffs[i] is Hediff_Injury injury))
                {
                    continue;
                }

                if (injury.IsPermanent() || injury.Severity <= bestSeverity)
                {
                    continue;
                }

                best = injury;
                bestSeverity = injury.Severity;
            }

            if (best == null)
            {
                return false;
            }

            best.Heal(amount);
            return true;
        }

        private static bool HealRandomNonPermanentInjury(Pawn pawn, float amount)
        {
            if (pawn?.health?.hediffSet?.hediffs == null || amount <= 0f)
            {
                return false;
            }

            List<Hediff_Injury> injuries = new List<Hediff_Injury>();
            List<Hediff> hediffs = pawn.health.hediffSet.hediffs;
            for (int i = 0; i < hediffs.Count; i++)
            {
                if (hediffs[i] is Hediff_Injury injury && !injury.IsPermanent())
                {
                    injuries.Add(injury);
                }
            }

            if (injuries.Count == 0)
            {
                return false;
            }

            injuries.RandomElement().Heal(amount);
            return true;
        }

        private static void RefreshHealthState(Pawn pawn)
        {
            if (pawn?.health == null)
            {
                return;
            }

            try
            {
                pawn.health.hediffSet?.DirtyCache();
                pawn.health.CheckForStateChange(null, null);
            }
            catch
            {
                // Health recovery is defensive; an unexpected modded hediff state should not cascade.
            }
        }
    }
}
