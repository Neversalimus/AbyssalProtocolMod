using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    public static class AbyssalBossNoDownedUtility
    {
        public static void TryPreventDowned(Pawn pawn, float bloodLossClamp, float heatstrokeClamp, float healWorstInjuryAmount, int maxHealPasses, bool forceLordReengage)
        {
            if (pawn == null || pawn.Dead || pawn.health == null)
            {
                return;
            }

            if (!pawn.Downed)
            {
                return;
            }

            ClampHediffSeverity(pawn, HediffDefOf.BloodLoss, bloodLossClamp);
            ClampHediffSeverity(pawn, HediffDefOf.Heatstroke, heatstrokeClamp);

            int passes = Mathf.Max(1, maxHealPasses);
            for (int i = 0; i < passes; i++)
            {
                if (!pawn.Downed)
                {
                    break;
                }

                if (!HealWorstVisibleInjury(pawn, healWorstInjuryAmount))
                {
                    break;
                }

                pawn.health.hediffSet?.DirtyCache();
                pawn.health.CheckForStateChange(null, null);
            }

            if (pawn.Downed)
            {
                HealRandomNonPermanentInjury(pawn, healWorstInjuryAmount * 0.75f);
                pawn.health.hediffSet?.DirtyCache();
                pawn.health.CheckForStateChange(null, null);
            }

            if (!pawn.Downed && forceLordReengage && pawn.Spawned && pawn.MapHeld != null && pawn.Faction != null && pawn.HostileTo(Faction.OfPlayer))
            {
                AbyssalLordUtility.EnsureAssaultLord(pawn, sappers: true);
            }
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

        private static void HealRandomNonPermanentInjury(Pawn pawn, float amount)
        {
            if (pawn?.health?.hediffSet?.hediffs == null || amount <= 0f)
            {
                return;
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
                return;
            }

            injuries.RandomElement().Heal(amount);
        }
    }
}
