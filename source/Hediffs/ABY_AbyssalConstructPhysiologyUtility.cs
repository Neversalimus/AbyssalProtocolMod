using System.Collections.Generic;
using HarmonyLib;
using Verse;

namespace AbyssalProtocol
{
    public static class ABY_AbyssalConstructPhysiologyUtility
    {
        private const string BloodLossDefName = "BloodLoss";

        private static readonly System.Reflection.FieldInfo HealthTrackerPawnField = AccessTools.Field(typeof(Pawn_HealthTracker), "pawn");
        private static readonly System.Reflection.FieldInfo HediffSetPawnField = AccessTools.Field(typeof(HediffSet), "pawn");

        private static readonly HashSet<string> ConstructPawnDefNames = new HashSet<string>
        {
            "ABY_ReactorSaint",
            "ABY_SiegeIdol",
            "ABY_SiegeIdolEscort",
            "ABY_HaloHusk",
            "ABY_ChoirEngine"
        };

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

        public static Pawn GetPawn(HediffSet hediffSet)
        {
            if (hediffSet == null || HediffSetPawnField == null)
            {
                return null;
            }

            try
            {
                return HediffSetPawnField.GetValue(hediffSet) as Pawn;
            }
            catch
            {
                return null;
            }
        }

        public static bool IsConstructPhysiologyPawn(Pawn pawn)
        {
            if (pawn == null || pawn.Destroyed || pawn.Corpse != null)
            {
                return false;
            }

            string thingDefName = pawn.def?.defName;
            if (!string.IsNullOrEmpty(thingDefName) && ConstructPawnDefNames.Contains(thingDefName))
            {
                return true;
            }

            string kindDefName = pawn.kindDef?.defName;
            if (!string.IsNullOrEmpty(kindDefName) && ConstructPawnDefNames.Contains(kindDefName))
            {
                return true;
            }

            if (pawn.TryGetComp<CompABY_ReactorSaintShooter>() != null)
            {
                return true;
            }

            if (pawn.TryGetComp<CompABY_SiegeIdolSiegeShooter>() != null)
            {
                return true;
            }

            if (pawn.TryGetComp<CompABY_ChoirEngineAura>() != null || pawn.TryGetComp<CompABY_ChoirEngineRelay>() != null)
            {
                return true;
            }

            return false;
        }

        public static bool IsBloodLoss(HediffDef hediffDef)
        {
            return hediffDef != null && hediffDef.defName == BloodLossDefName;
        }

        public static bool IsBloodLoss(Hediff hediff)
        {
            return IsBloodLoss(hediff?.def);
        }

        public static bool MightBlockBloodLoss(HediffDef hediffDef)
        {
            return IsBloodLoss(hediffDef);
        }

        public static bool MightBlockBloodLoss(Hediff hediff)
        {
            return IsBloodLoss(hediff);
        }

        public static bool ShouldBlockBloodLoss(Pawn pawn, HediffDef hediffDef)
        {
            return IsConstructPhysiologyPawn(pawn) && IsBloodLoss(hediffDef);
        }

        public static bool ShouldBlockBloodLoss(Pawn pawn, Hediff hediff)
        {
            return IsConstructPhysiologyPawn(pawn) && IsBloodLoss(hediff);
        }

        public static bool TryBlockBloodLossAdd(Pawn pawn, HediffDef hediffDef)
        {
            return ShouldBlockBloodLoss(pawn, hediffDef);
        }

        public static bool TryBlockBloodLossAdd(Pawn pawn, Hediff hediff)
        {
            return ShouldBlockBloodLoss(pawn, hediff);
        }

        public static bool ScrubConstructHediffs(Pawn pawn)
        {
            bool changed = ScrubBloodLoss(pawn);
            changed |= StopConstructBleedingInjuries(pawn);
            return changed;
        }

        public static bool StopConstructBleedingInjuries(Pawn pawn)
        {
            if (!IsConstructPhysiologyPawn(pawn) || pawn.health?.hediffSet?.hediffs == null)
            {
                return false;
            }

            bool changed = false;
            List<Hediff> hediffs = pawn.health.hediffSet.hediffs;
            for (int i = 0; i < hediffs.Count; i++)
            {
                if (StopBleedingInjury(hediffs[i] as Hediff_Injury))
                {
                    changed = true;
                }
            }

            if (changed)
            {
                pawn.health.hediffSet.DirtyCache();
            }

            return changed;
        }

        public static bool StopBleedingInjury(Hediff_Injury injury)
        {
            if (injury == null || injury.pawn == null || !IsConstructPhysiologyPawn(injury.pawn))
            {
                return false;
            }

            bool changed = false;
            try
            {
                if (injury.BleedRate > 0.0001f)
                {
                    // Mechanical/construct bosses may keep the visible wound, but should not
                    // generate pawn-style bleeding. Aging the wound past its bleeding window
                    // is cheaper and less destructive than removing every injury outright.
                    if (injury.ageTicks < 999999)
                    {
                        injury.ageTicks = 999999;
                        changed = true;
                    }

                    injury.Tended(1f, 1f, 0);
                    changed = true;
                }
            }
            catch
            {
            }

            return changed;
        }

        public static bool ScrubBloodLoss(Pawn pawn)
        {
            if (!IsConstructPhysiologyPawn(pawn) || pawn.health?.hediffSet?.hediffs == null)
            {
                return false;
            }

            List<Hediff> hediffs = pawn.health.hediffSet.hediffs;
            List<Hediff> toRemove = null;
            for (int i = 0; i < hediffs.Count; i++)
            {
                Hediff hediff = hediffs[i];
                if (!IsBloodLoss(hediff))
                {
                    continue;
                }

                if (toRemove == null)
                {
                    toRemove = new List<Hediff>();
                }

                toRemove.Add(hediff);
            }

            if (toRemove == null)
            {
                return false;
            }

            bool removedAny = false;
            for (int i = 0; i < toRemove.Count; i++)
            {
                Hediff hediff = toRemove[i];
                if (hediff == null || hediff.pawn != pawn)
                {
                    continue;
                }

                try
                {
                    pawn.health.RemoveHediff(hediff);
                    removedAny = true;
                }
                catch
                {
                    // Another health system may have removed it first. The next scrub will catch leftovers.
                }
            }

            return removedAny;
        }
    }
}
