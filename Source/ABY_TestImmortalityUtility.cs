using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace AbyssalProtocol
{
    public static class ABY_TestImmortalityUtility
    {
        public const string HediffDefName = "ABY_TestImmortality";

        private static HediffDef cachedImmortalityDef;

        public static HediffDef ImmortalityDef =>
            cachedImmortalityDef ??= DefDatabase<HediffDef>.GetNamedSilentFail(HediffDefName);

        public static Hediff GetImmortalityHediff(Pawn pawn)
        {
            return pawn?.health?.hediffSet?.GetFirstHediffOfDef(ImmortalityDef);
        }

        public static bool HasImmortality(Pawn pawn)
        {
            return GetImmortalityHediff(pawn) != null;
        }

        public static bool ToggleImmortality(Pawn pawn)
        {
            if (pawn == null || pawn.health == null || ImmortalityDef == null)
            {
                return false;
            }

            if (HasImmortality(pawn))
            {
                RemoveImmortality(pawn);
                return false;
            }

            AddImmortality(pawn);
            return true;
        }

        public static void AddImmortality(Pawn pawn)
        {
            if (pawn == null || pawn.health == null || ImmortalityDef == null || HasImmortality(pawn))
            {
                return;
            }

            Hediff hediff = HediffMaker.MakeHediff(ImmortalityDef, pawn);
            pawn.health.AddHediff(hediff);
            StabilizePawn(pawn, true);

            Messages.Message(
                "ABY_TestImmortalityAdded".Translate(pawn.LabelShortCap),
                pawn,
                MessageTypeDefOf.TaskCompletion,
                false);
        }

        public static void RemoveImmortality(Pawn pawn)
        {
            Hediff hediff = GetImmortalityHediff(pawn);
            if (pawn == null || pawn.health == null || hediff == null)
            {
                return;
            }

            pawn.health.RemoveHediff(hediff);

            Messages.Message(
                "ABY_TestImmortalityRemoved".Translate(pawn.LabelShortCap),
                pawn,
                MessageTypeDefOf.TaskCompletion,
                false);
        }

        public static void StabilizePawn(Pawn pawn, bool aggressiveCleansing)
        {
            if (pawn == null || pawn.health == null || pawn.health.hediffSet == null)
            {
                return;
            }

            Hediff immortality = GetImmortalityHediff(pawn);
            List<Hediff> hediffs = pawn.health.hediffSet.hediffs.ToList();

            foreach (Hediff hediff in hediffs)
            {
                if (hediff == null || hediff == immortality)
                {
                    continue;
                }

                if (hediff is Hediff_AddedPart)
                {
                    continue;
                }

                if (hediff is Hediff_Injury injury)
                {
                    injury.Heal(injury.Severity);
                    continue;
                }

                if (hediff is Hediff_MissingPart)
                {
                    pawn.health.RemoveHediff(hediff);
                    continue;
                }

                if (aggressiveCleansing && hediff.def != null && hediff.def.isBad)
                {
                    pawn.health.RemoveHediff(hediff);
                }
            }
        }

        public static List<Pawn> GetToggleCandidates(Map map)
        {
            if (map?.mapPawns == null)
            {
                return new List<Pawn>();
            }

            return map.mapPawns.AllPawnsSpawned
                .Where(pawn => pawn != null && pawn.Spawned && !pawn.Dead && pawn.health != null)
                .OrderBy(pawn => pawn.Faction == Faction.OfPlayer ? 0 : 1)
                .ThenBy(pawn => pawn.LabelShortCap)
                .ToList();
        }
    }
}
