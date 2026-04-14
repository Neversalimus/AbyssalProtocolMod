using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace AbyssalProtocol
{
    public static class AbyssalSummoningConsoleUtility
    {
        public sealed class RitualDefinition
        {
            public string Id;
            public string LabelKey;
            public string SubtitleKey;
            public string DescriptionKey;
            public string BossLabel;
            public string SigilThingDefName;
            public string PawnKindDefName;
            public string RewardHintKey;
            public string SideEffectHintKey;
            public float BaseRisk;
            public int SpawnPoints;
        }

        public sealed class StatusEntry
        {
            public string Label;
            public string Value;
            public bool Satisfied;
        }

        public enum CircleRiskTier
        {
            Stable,
            Strained,
            Volatile,
            Catastrophic
        }

        private static readonly List<RitualDefinition> Rituals = new List<RitualDefinition>
        {
            new RitualDefinition
            {
                Id = "archon_beast",
                LabelKey = "ABY_CircleRitual_Archon_Label",
                SubtitleKey = "ABY_CircleRitual_Archon_Subtitle",
                DescriptionKey = "ABY_CircleRitual_Archon_Desc",
                BossLabel = "Archon Beast",
                SigilThingDefName = "ABY_ArchonSigil",
                PawnKindDefName = "ABY_ArchonBeast",
                RewardHintKey = "ABY_CircleRitual_Archon_Rewards",
                SideEffectHintKey = "ABY_CircleRitual_Archon_SideEffects",
                BaseRisk = 0.68f,
                SpawnPoints = 900
            }
        };

        public static IEnumerable<RitualDefinition> GetRituals()
        {
            return Rituals;
        }

        public static RitualDefinition GetDefaultRitual()
        {
            return Rituals[0];
        }

        public static ThingDef GetSigilDef(RitualDefinition ritual)
        {
            return ritual == null ? null : DefDatabase<ThingDef>.GetNamedSilentFail(ritual.SigilThingDefName);
        }

        public static int CountSigilsOnMap(Map map, RitualDefinition ritual)
        {
            ThingDef sigilDef = GetSigilDef(ritual);
            if (map == null || sigilDef == null)
            {
                return 0;
            }

            int count = 0;
            List<Thing> things = map.listerThings.ThingsOfDef(sigilDef);
            for (int i = 0; i < things.Count; i++)
            {
                Thing thing = things[i];
                if (thing == null || thing.Destroyed)
                {
                    continue;
                }

                count += Math.Max(1, thing.stackCount);
            }

            return count;
        }

        public static int CountAvailableOperators(Building_AbyssalSummoningCircle circle, RitualDefinition ritual)
        {
            if (circle == null || circle.Map == null)
            {
                return 0;
            }

            int count = 0;
            List<Pawn> pawns = circle.Map.mapPawns.FreeColonistsSpawned;
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn pawn = pawns[i];
                if (CanUseCircle(pawn, circle, ritual, false))
                {
                    count++;
                }
            }

            return count;
        }

        public static bool TryAssignInvocation(Building_AbyssalSummoningCircle circle, RitualDefinition ritual, out string failReason)
        {
            failReason = null;

            if (circle == null || circle.Destroyed || !circle.Spawned || circle.Map == null)
            {
                failReason = "ABY_CircleConsoleFail_NoCircle".Translate();
                return false;
            }

            if (!circle.IsReadyForSigil(out failReason))
            {
                return false;
            }

            Thing sigil = FindBestSigil(circle, ritual, out failReason);
            if (sigil == null)
            {
                return false;
            }

            Pawn pawn = FindBestOperator(circle, ritual, sigil, out failReason);
            if (pawn == null)
            {
                return false;
            }

            JobDef jobDef = DefDatabase<JobDef>.GetNamedSilentFail("ABY_CarrySigilToCircle");
            if (jobDef == null)
            {
                failReason = "Missing JobDef: ABY_CarrySigilToCircle";
                return false;
            }

            Job job = JobMaker.MakeJob(jobDef, sigil, circle);
            job.count = 1;
            pawn.jobs.TryTakeOrderedJob(job);
            return true;
        }

        public static Thing FindBestSigil(Building_AbyssalSummoningCircle circle, RitualDefinition ritual, out string failReason)
        {
            failReason = null;
            if (circle?.Map == null)
            {
                failReason = "ABY_CircleConsoleFail_NoCircle".Translate();
                return null;
            }

            ThingDef sigilDef = GetSigilDef(ritual);
            if (sigilDef == null)
            {
                failReason = "Missing ThingDef: " + ritual?.SigilThingDefName;
                return null;
            }

            Thing best = null;
            float bestScore = float.MaxValue;
            List<Thing> things = circle.Map.listerThings.ThingsOfDef(sigilDef);
            for (int i = 0; i < things.Count; i++)
            {
                Thing thing = things[i];
                if (thing == null || thing.Destroyed || !thing.Spawned || thing.MapHeld != circle.Map)
                {
                    continue;
                }

                float score = thing.PositionHeld.DistanceToSquared(circle.PositionHeld);
                if (score < bestScore)
                {
                    best = thing;
                    bestScore = score;
                }
            }

            if (best == null)
            {
                failReason = "ABY_CircleConsoleFail_NoSigil".Translate();
            }

            return best;
        }

        public static Pawn FindBestOperator(Building_AbyssalSummoningCircle circle, RitualDefinition ritual, Thing sigil, out string failReason)
        {
            failReason = null;
            if (circle?.Map == null || sigil == null)
            {
                failReason = "ABY_CircleConsoleFail_NoOperator".Translate();
                return null;
            }

            Pawn best = null;
            float bestScore = float.MaxValue;
            List<Pawn> pawns = circle.Map.mapPawns.FreeColonistsSpawned;
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn pawn = pawns[i];
                if (!CanUseCircle(pawn, circle, ritual, true))
                {
                    continue;
                }

                if (!pawn.CanReserveAndReach(sigil, PathEndMode.Touch, Danger.Deadly))
                {
                    continue;
                }

                if (!pawn.CanReserveAndReach(circle, PathEndMode.InteractionCell, Danger.Deadly))
                {
                    continue;
                }

                float score = pawn.PositionHeld.DistanceToSquared(sigil.PositionHeld);
                if (pawn.Drafted)
                {
                    score += 4000f;
                }
                if (score < bestScore)
                {
                    best = pawn;
                    bestScore = score;
                }
            }

            if (best == null)
            {
                failReason = "ABY_CircleConsoleFail_NoOperator".Translate();
            }

            return best;
        }

        public static bool CanUseCircle(Pawn pawn, Building_AbyssalSummoningCircle circle, RitualDefinition ritual, bool requireReservations)
        {
            if (pawn == null || circle == null || pawn.MapHeld != circle.Map || pawn.Dead || pawn.Downed || pawn.InMentalState)
            {
                return false;
            }

            if (!pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation))
            {
                return false;
            }

            if (!circle.IsReadyForSigil(out _))
            {
                return false;
            }

            if (!pawn.CanReach(circle, PathEndMode.InteractionCell, Danger.Deadly))
            {
                return false;
            }

            if (requireReservations && !pawn.CanReserve(circle))
            {
                return false;
            }

            return true;
        }

        public static List<StatusEntry> GetStatusEntries(Building_AbyssalSummoningCircle circle, RitualDefinition ritual)
        {
            List<StatusEntry> entries = new List<StatusEntry>();
            if (circle == null)
            {
                return entries;
            }

            bool interactionOk = circle.HasValidInteractionCell(out string interactionFail);
            bool focusOk = circle.HasClearRitualFocus(out string focusFail);
            bool encounterClear = circle.Map != null && !AbyssalBossSummonUtility.HasActiveAbyssalEncounter(circle.Map);
            int sigils = CountSigilsOnMap(circle.Map, ritual);
            int operators = CountAvailableOperators(circle, ritual);

            entries.Add(new StatusEntry
            {
                Label = "ABY_CircleStatus_Power".Translate(),
                Value = circle.IsPoweredForRitual ? "ABY_CircleStatus_Online".Translate() : "ABY_CircleStatus_Offline".Translate(),
                Satisfied = circle.IsPoweredForRitual
            });
            entries.Add(new StatusEntry
            {
                Label = "ABY_CircleStatus_Focus".Translate(),
                Value = focusOk ? "ABY_CircleStatus_Clear".Translate() : focusFail,
                Satisfied = focusOk
            });
            entries.Add(new StatusEntry
            {
                Label = "ABY_CircleStatus_Interaction".Translate(),
                Value = interactionOk ? "ABY_CircleStatus_Clear".Translate() : interactionFail,
                Satisfied = interactionOk
            });
            entries.Add(new StatusEntry
            {
                Label = "ABY_CircleStatus_Sigils".Translate(),
                Value = sigils.ToString(),
                Satisfied = sigils > 0
            });
            entries.Add(new StatusEntry
            {
                Label = "ABY_CircleStatus_Operators".Translate(),
                Value = operators.ToString(),
                Satisfied = operators > 0
            });
            entries.Add(new StatusEntry
            {
                Label = "ABY_CircleStatus_Encounter".Translate(),
                Value = encounterClear ? "ABY_CircleStatus_Clear".Translate() : "ABY_BossSummonFail_EncounterActive".Translate(),
                Satisfied = encounterClear
            });

            return entries;
        }

        public static CircleRiskTier GetRiskTier(Building_AbyssalSummoningCircle circle, RitualDefinition ritual)
        {
            if (circle == null)
            {
                return CircleRiskTier.Stable;
            }

            if (circle.RitualActive)
            {
                switch (circle.CurrentRitualPhase)
                {
                    case Building_AbyssalSummoningCircle.ConsoleRitualPhase.Charging:
                        return CircleRiskTier.Strained;
                    case Building_AbyssalSummoningCircle.ConsoleRitualPhase.Surge:
                        return CircleRiskTier.Volatile;
                    case Building_AbyssalSummoningCircle.ConsoleRitualPhase.Breach:
                        return CircleRiskTier.Catastrophic;
                    case Building_AbyssalSummoningCircle.ConsoleRitualPhase.Cooldown:
                        return CircleRiskTier.Strained;
                }
            }

            int missing = GetStatusEntries(circle, ritual).Count(entry => !entry.Satisfied);
            if (missing >= 3)
            {
                return CircleRiskTier.Stable;
            }
            if (missing == 2)
            {
                return CircleRiskTier.Strained;
            }
            if (missing == 1)
            {
                return CircleRiskTier.Volatile;
            }
            return CircleRiskTier.Volatile;
        }

        public static float GetRiskFill(Building_AbyssalSummoningCircle circle, RitualDefinition ritual)
        {
            switch (GetRiskTier(circle, ritual))
            {
                case CircleRiskTier.Stable:
                    return 0.25f;
                case CircleRiskTier.Strained:
                    return 0.48f;
                case CircleRiskTier.Volatile:
                    return 0.74f;
                case CircleRiskTier.Catastrophic:
                    return 1f;
                default:
                    return 0.25f;
            }
        }

        public static Color GetRiskColor(CircleRiskTier tier)
        {
            switch (tier)
            {
                case CircleRiskTier.Stable:
                    return new Color(0.86f, 0.40f, 0.18f, 1f);
                case CircleRiskTier.Strained:
                    return new Color(0.96f, 0.52f, 0.16f, 1f);
                case CircleRiskTier.Volatile:
                    return new Color(1f, 0.38f, 0.16f, 1f);
                case CircleRiskTier.Catastrophic:
                    return new Color(1f, 0.22f, 0.14f, 1f);
                default:
                    return Color.white;
            }
        }

        public static string GetRiskLabel(CircleRiskTier tier)
        {
            switch (tier)
            {
                case CircleRiskTier.Stable:
                    return "ABY_CircleRisk_Stable".Translate();
                case CircleRiskTier.Strained:
                    return "ABY_CircleRisk_Strained".Translate();
                case CircleRiskTier.Volatile:
                    return "ABY_CircleRisk_Volatile".Translate();
                case CircleRiskTier.Catastrophic:
                    return "ABY_CircleRisk_Catastrophic".Translate();
                default:
                    return "ABY_CircleRisk_Stable".Translate();
            }
        }

        public static string GetShortRequirementSummary(Building_AbyssalSummoningCircle circle, RitualDefinition ritual)
        {
            int ready = GetStatusEntries(circle, ritual).Count(entry => entry.Satisfied);
            return "ABY_CircleReadinessSummary".Translate(ready, 6);
        }
    }
}
