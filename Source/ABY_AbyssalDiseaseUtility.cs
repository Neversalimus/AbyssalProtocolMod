using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace AbyssalProtocol
{
    public static class ABY_AbyssalDiseaseUtility
    {
        private static readonly System.Reflection.FieldInfo HealthTrackerPawnField = AccessTools.Field(typeof(Pawn_HealthTracker), "pawn");
        private static readonly System.Reflection.FieldInfo HediffSetPawnField = AccessTools.Field(typeof(HediffSet), "pawn");

        private static readonly HashSet<string> ExplicitBlockedDiseaseDefNames = new HashSet<string>
        {
            "Flu",
            "Plague",
            "Malaria",
            "SleepingSickness",
            "GutWorms",
            "MuscleParasites",
            "FibrousMechanites",
            "SensoryMechanites",
            "WoundInfection",
            "Infection",
            "FoodPoisoning",
            "BloodRot"
        };

        private static readonly HashSet<string> ExplicitBlockedSpawnConditionDefNames = new HashSet<string>
        {
            "Frail",
            "BadBack",
            "Cataract",
            "Dementia",
            "Alzheimers",
            "HearingLoss",
            "Asthma",
            "Carcinoma",
            "HeartArteryBlockage",
            "ChemicalDamageSevere",
            "ChemicalDamageModerate"
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

        public static bool IsDiseaseProtectedPawn(Pawn pawn)
        {
            if (pawn == null || pawn.Destroyed || pawn.Corpse != null || pawn.health == null)
            {
                return false;
            }

            if (pawn.TryGetComp<CompAbyssalPawnController>() != null)
            {
                return true;
            }

            if (pawn.TryGetComp<CompABY_BossTrueDeath>() != null || pawn.TryGetComp<CompABY_BossNoDowned>() != null)
            {
                return true;
            }

            string thingDefName = pawn.def?.defName;
            string kindDefName = pawn.kindDef?.defName;
            if (!string.IsNullOrEmpty(thingDefName) && thingDefName.StartsWith("ABY_"))
            {
                return true;
            }

            if (!string.IsNullOrEmpty(kindDefName) && kindDefName.StartsWith("ABY_"))
            {
                return true;
            }

            return false;
        }


        public static bool CouldBeBlockedHediff(HediffDef hediffDef)
        {
            if (hediffDef == null || IsAbyssalOwnedHediff(hediffDef))
            {
                return false;
            }

            return IsBlockedDiseaseLikeHediff(hediffDef) || IsBlockedSpawnConditionHediff(hediffDef);
        }

        public static bool CouldBeBlockedHediff(Hediff hediff)
        {
            return CouldBeBlockedHediff(hediff?.def);
        }

        public static bool ShouldBlockHediff(Pawn pawn, HediffDef hediffDef)
        {
            if (!CouldBeBlockedHediff(hediffDef))
            {
                return false;
            }

            return IsDiseaseProtectedPawn(pawn);
        }

        public static bool ShouldBlockHediff(Pawn pawn, Hediff hediff)
        {
            return ShouldBlockHediff(pawn, hediff?.def);
        }

        public static bool TryBlockHediffAdd(Pawn pawn, HediffDef hediffDef)
        {
            return ShouldBlockHediff(pawn, hediffDef);
        }

        public static bool TryBlockHediffAdd(Pawn pawn, Hediff hediff)
        {
            return ShouldBlockHediff(pawn, hediff);
        }

        public static bool ScrubBlockedHediffs(Pawn pawn)
        {
            if (!IsDiseaseProtectedPawn(pawn) || pawn.health?.hediffSet?.hediffs == null)
            {
                return false;
            }

            List<Hediff> hediffs = pawn.health.hediffSet.hediffs;
            List<Hediff> toRemove = null;
            for (int i = 0; i < hediffs.Count; i++)
            {
                Hediff hediff = hediffs[i];
                if (!ShouldBlockHediff(pawn, hediff))
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
                    // Another health system may have removed it first. The next rare scrub will catch leftovers.
                }
            }

            return removedAny;
        }

        private static bool IsAbyssalOwnedHediff(HediffDef hediffDef)
        {
            string defName = hediffDef?.defName;
            return !string.IsNullOrEmpty(defName) && defName.StartsWith("ABY_");
        }

        private static bool IsBlockedDiseaseLikeHediff(HediffDef hediffDef)
        {
            if (hediffDef == null)
            {
                return false;
            }

            string defName = hediffDef.defName;
            if (!string.IsNullOrEmpty(defName) && ExplicitBlockedDiseaseDefNames.Contains(defName))
            {
                return true;
            }

            if (!hediffDef.isBad || hediffDef.comps == null)
            {
                return false;
            }

            for (int i = 0; i < hediffDef.comps.Count; i++)
            {
                if (hediffDef.comps[i] is HediffCompProperties_Immunizable)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsBlockedSpawnConditionHediff(HediffDef hediffDef)
        {
            if (hediffDef == null)
            {
                return false;
            }

            string defName = hediffDef.defName;
            return !string.IsNullOrEmpty(defName) && ExplicitBlockedSpawnConditionDefNames.Contains(defName);
        }
    }
}
