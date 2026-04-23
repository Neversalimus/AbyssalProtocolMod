using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    public static class AbyssalHordeRewardUtility
    {
        public const string HordeFragmentThingDefName = "ABY_HordeFragment";
        public const string LitanyGrinderThingDefName = "ABY_LitanyGrinder";
        private const string ResidueThingDefName = "ABY_AbyssalResidue";
        private const string IndustrialComponentThingDefName = "ComponentIndustrial";
        private const string SpacerComponentThingDefName = "ComponentSpacer";

        public sealed class RewardSnapshot : IExposable
        {
            public int Band;
            public int DifficultyOrder;
            public int FrontCount;
            public int PulseCount;
            public int PhaseCount;
            public bool UsesCommandGate;
            public bool HasSurgePhase;
            public string DoctrineDefName = string.Empty;

            public void ExposeData()
            {
                Scribe_Values.Look(ref Band, "band", 0);
                Scribe_Values.Look(ref DifficultyOrder, "difficultyOrder", 0);
                Scribe_Values.Look(ref FrontCount, "frontCount", 2);
                Scribe_Values.Look(ref PulseCount, "pulseCount", 2);
                Scribe_Values.Look(ref PhaseCount, "phaseCount", 1);
                Scribe_Values.Look(ref UsesCommandGate, "usesCommandGate", false);
                Scribe_Values.Look(ref HasSurgePhase, "hasSurgePhase", false);
                Scribe_Values.Look(ref DoctrineDefName, "doctrineDefName", string.Empty);
            }
        }

        private struct RewardProfile
        {
            public int Residue;
            public int HordeFragments;
            public int IndustrialComponents;
            public int SpacerComponents;
        }

        public static RewardSnapshot BuildSnapshot(AbyssalHordeSigilUtility.HordePlan plan)
        {
            if (plan == null)
            {
                return null;
            }

            return new RewardSnapshot
            {
                Band = Mathf.Clamp(plan.Band, 0, 3),
                DifficultyOrder = Mathf.Max(0, AbyssalDifficultyUtility.GetCurrentProfileOrder()),
                FrontCount = Mathf.Clamp(plan.FrontCount, 1, 6),
                PulseCount = Mathf.Clamp(plan.PulseCount, 1, 8),
                PhaseCount = Mathf.Clamp(plan.PhaseCount, 1, 4),
                UsesCommandGate = plan.UsesCommandGate,
                HasSurgePhase = plan.HasSurgePhase,
                DoctrineDefName = plan.PrimaryDoctrineDefName ?? string.Empty
            };
        }

        public static void ApplyAdditionalBacklash(Building_AbyssalSummoningCircle circle, RewardSnapshot snapshot)
        {
            if (circle == null || circle.Destroyed || circle.Map == null || snapshot == null)
            {
                return;
            }

            float extraHeat = Mathf.Clamp(0.05f + snapshot.Band * 0.02f + snapshot.FrontCount * 0.008f + snapshot.PulseCount * 0.004f, 0.05f, 0.16f);
            if (snapshot.HasSurgePhase)
            {
                extraHeat += 0.015f;
            }

            extraHeat *= Mathf.Clamp(AbyssalDifficultyUtility.GetInstabilityMultiplier(), 0.75f, 1.85f);
            circle.DebugAdjustInstabilityHeat(extraHeat);

            float contamination = Mathf.Clamp(0.035f + snapshot.Band * 0.015f + snapshot.FrontCount * 0.006f, 0.035f, 0.11f);
            if (snapshot.HasSurgePhase)
            {
                contamination += 0.01f;
            }

            circle.Map.GetComponent<MapComponent_AbyssalCircleInstability>()?.AddContamination(contamination);
        }

        public static string GetForecastSummary(RewardSnapshot snapshot)
        {
            RewardProfile closure = BuildClosureProfile(snapshot);
            RewardProfile command = BuildCommandProfile(snapshot, snapshot != null && snapshot.UsesCommandGate ? 1 : 0);
            string closureLabel = GetRewardProfileSummary(closure);
            string commandLabel = snapshot != null && snapshot.UsesCommandGate
                ? GetRewardProfileSummary(command)
                : AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_HordeEconomy_CommandNone", "no command bonus expected");

            return AbyssalSummoningConsoleUtility.TranslateOrFallback(
                "ABY_HordeEconomy_Summary",
                "Closure payout: {0}. Command node kill bonus: {1}. No boss or miniboss cores are routed through this ritual, but horde fragments can be forged into {2} once the forge reaches 1000 residue.",
                closureLabel,
                commandLabel,
                GetLitanyGrinderLabel());
        }

        public static List<string> GetForecastLines(RewardSnapshot snapshot)
        {
            List<string> lines = new List<string>();
            if (snapshot == null)
            {
                lines.Add(AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_HordeEconomy_Unknown", "Reward routing could not be stabilized. Expect residue-heavy salvage without boss-tier drops."));
                return lines;
            }

            RewardProfile closure = BuildClosureProfile(snapshot);
            lines.Add(AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_HordeEconomy_LineClosure", "Breach closure cache: {0}", GetRewardProfileSummary(closure)));
            if (snapshot.UsesCommandGate)
            {
                RewardProfile command = BuildCommandProfile(snapshot, 1 + Mathf.Max(0, snapshot.Band - 1));
                lines.Add(AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_HordeEconomy_LineCommand", "Command node kill bonus: {0}", GetRewardProfileSummary(command)));
            }
            else
            {
                lines.Add(AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_HordeEconomy_LineCommandAbsent", "No command node bonus is expected for this forecast."));
            }

            lines.Add(AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_HordeEconomy_LineNoBoss", "No boss cores, saint caches, or miniboss-unique drops are routed through Horde payouts."));
            lines.Add(AbyssalSummoningConsoleUtility.TranslateOrFallback(
                "ABY_HordeEconomy_LineFragments",
                "War-lattice fragments from this rite feed the {0} heavy weapon pattern once the forge reaches 1000 residue.",
                GetLitanyGrinderLabel()));
            lines.Add(AbyssalSummoningConsoleUtility.TranslateOrFallback(
                "ABY_HordeEconomy_LineBacklash",
                "Additional backlash: +{0}% instability heat and +{1}% contamination pressure on invocation.",
                Mathf.RoundToInt(GetBacklashHeatPercent(snapshot)),
                Mathf.RoundToInt(GetBacklashContaminationPercent(snapshot))));
            return lines;
        }

        public static string GetClosureBulletin(RewardSnapshot snapshot)
        {
            RewardProfile closure = BuildClosureProfile(snapshot);
            return AbyssalSummoningConsoleUtility.TranslateOrFallback(
                "ABY_HordeEconomy_BulletinClosure",
                "{0} residue + {1} fragments",
                Mathf.Max(0, closure.Residue),
                Mathf.Max(0, closure.HordeFragments));
        }

        public static string GetBacklashBulletin(RewardSnapshot snapshot)
        {
            snapshot ??= new RewardSnapshot();
            return AbyssalSummoningConsoleUtility.TranslateOrFallback(
                "ABY_HordeEconomy_BulletinBacklash",
                "Heat +{0}% / Contam +{1}%",
                Mathf.RoundToInt(GetBacklashHeatPercent(snapshot)),
                Mathf.RoundToInt(GetBacklashContaminationPercent(snapshot)));
        }

        public static void SpawnClosureRewards(Map map, IntVec3 dropCell, RewardSnapshot snapshot)
        {
            RewardProfile profile = BuildClosureProfile(snapshot);
            SpawnRewardProfile(map, dropCell, profile);
            if (map != null && dropCell.IsValid)
            {
                FleckMaker.ThrowLightningGlow(dropCell.ToVector3Shifted(), map, 1.9f);
                FleckMaker.ThrowMicroSparks(dropCell.ToVector3Shifted(), map);
                ABY_SoundUtility.PlayAt("ABY_SigilSpawnImpulse", dropCell, map);
                Messages.Message(
                    AbyssalSummoningConsoleUtility.TranslateOrFallback(
                        "ABY_HordeEconomy_ClosureSpawned",
                        "The {0} operation starts folding shut. Condensed horde salvage spills out near the last active front.",
                        GetDoctrineLabel(snapshot)),
                    new TargetInfo(dropCell, map),
                    MessageTypeDefOf.PositiveEvent);
            }
        }

        public static void SpawnCommandRewards(Map map, IntVec3 dropCell, RewardSnapshot snapshot, int remainingBursts)
        {
            RewardProfile profile = BuildCommandProfile(snapshot, remainingBursts);
            SpawnRewardProfile(map, dropCell, profile);
            if (map != null && dropCell.IsValid)
            {
                FleckMaker.ThrowLightningGlow(dropCell.ToVector3Shifted(), map, 2.2f);
                FleckMaker.ThrowMicroSparks(dropCell.ToVector3Shifted(), map);
                ABY_SoundUtility.PlayAt("ABY_RupturePortalOpen", dropCell, map);
                Messages.Message(
                    AbyssalSummoningConsoleUtility.TranslateOrFallback(
                        "ABY_HordeEconomy_CommandSpawned",
                        "The {0} command gate ruptures into salvage. Its condensed war-lattice fragments and routed supply caches spill onto the field.",
                        GetDoctrineLabel(snapshot)),
                    new TargetInfo(dropCell, map),
                    MessageTypeDefOf.PositiveEvent);
            }
        }

        private static string GetLitanyGrinderLabel()
        {
            ThingDef def = DefDatabase<ThingDef>.GetNamedSilentFail(LitanyGrinderThingDefName);
            return def != null ? def.LabelCap : "Litany Grinder";
        }

        private static string GetDoctrineLabel(RewardSnapshot snapshot)
        {
            string doctrine = snapshot?.DoctrineDefName ?? string.Empty;
            if (string.Equals(doctrine, "ABY_Doctrine_HordeFlood", StringComparison.OrdinalIgnoreCase))
            {
                return AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_HordeDoctrine_Flood_Label", "Ravenous Breach");
            }

            if (string.Equals(doctrine, "ABY_Doctrine_HordeFireline", StringComparison.OrdinalIgnoreCase))
            {
                return AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_HordeDoctrine_Fireline_Label", "Black Procession");
            }

            if (string.Equals(doctrine, "ABY_Doctrine_HordeGrinder", StringComparison.OrdinalIgnoreCase))
            {
                return AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_HordeDoctrine_Grinder_Label", "Grinder Host");
            }

            if (string.Equals(doctrine, "ABY_Doctrine_HordeSiege", StringComparison.OrdinalIgnoreCase))
            {
                return AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_HordeDoctrine_Siege_Label", "Siege Liturgy");
            }

            return AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_HordeDoctrine_Unknown_Label", "Unshaped breach");
        }

        private static RewardProfile BuildClosureProfile(RewardSnapshot snapshot)
        {
            snapshot ??= new RewardSnapshot();
            int band = Mathf.Clamp(snapshot.Band, 0, 3);
            int difficulty = Mathf.Max(0, snapshot.DifficultyOrder);
            RewardProfile profile = new RewardProfile
            {
                Residue = 20 + band * 8 + snapshot.FrontCount * 2 + snapshot.PulseCount * 2 + difficulty * 3,
                HordeFragments = 1 + (band >= 1 ? 1 : 0) + (band >= 3 || difficulty >= 3 ? 1 : 0),
                IndustrialComponents = 2 + band + Mathf.Max(0, snapshot.FrontCount - 2),
                SpacerComponents = band >= 2 ? 1 : 0
            };

            if (snapshot.HasSurgePhase)
            {
                profile.Residue += 4;
            }

            if (string.Equals(snapshot.DoctrineDefName, "ABY_Doctrine_HordeSiege", StringComparison.OrdinalIgnoreCase))
            {
                profile.IndustrialComponents += 1;
            }
            else if (string.Equals(snapshot.DoctrineDefName, "ABY_Doctrine_HordeFireline", StringComparison.OrdinalIgnoreCase))
            {
                profile.HordeFragments += 1;
            }

            profile.Residue = Mathf.Max(1, Mathf.RoundToInt(profile.Residue * AbyssalDifficultyUtility.GetResidueRewardMultiplier()));
            profile.Residue = ABY_BestiaryRewardUtility.ApplyExtractionBonus(profile.Residue);
            profile.HordeFragments = profile.HordeFragments > 0 ? ABY_BestiaryRewardUtility.ApplyExtractionBonus(profile.HordeFragments) : 0;
            profile.IndustrialComponents = Mathf.Max(0, Mathf.RoundToInt(profile.IndustrialComponents * Mathf.Clamp(AbyssalDifficultyUtility.GetBonusLootMultiplier(), 0.6f, 1.75f)));
            profile.IndustrialComponents = profile.IndustrialComponents > 0 ? ABY_BestiaryRewardUtility.ApplyExtractionBonus(profile.IndustrialComponents) : 0;
            profile.SpacerComponents = Mathf.Max(0, Mathf.RoundToInt(profile.SpacerComponents * Mathf.Clamp(AbyssalDifficultyUtility.GetBonusLootMultiplier(), 0.6f, 1.75f)));
            profile.SpacerComponents = profile.SpacerComponents > 0 ? ABY_BestiaryRewardUtility.ApplyExtractionBonus(profile.SpacerComponents) : 0;
            return profile;
        }

        private static RewardProfile BuildCommandProfile(RewardSnapshot snapshot, int remainingBursts)
        {
            snapshot ??= new RewardSnapshot();
            int band = Mathf.Clamp(snapshot.Band, 0, 3);
            int burstFactor = Mathf.Clamp(remainingBursts, 0, 4);
            RewardProfile profile = new RewardProfile
            {
                Residue = 8 + band * 4 + burstFactor * 3,
                HordeFragments = 1 + (burstFactor >= 2 ? 1 : 0) + (band >= 2 ? 1 : 0),
                IndustrialComponents = 1 + Mathf.Min(2, burstFactor / 2),
                SpacerComponents = (band >= 2 || burstFactor >= 3) ? 1 : 0
            };

            profile.Residue = Mathf.Max(1, Mathf.RoundToInt(profile.Residue * AbyssalDifficultyUtility.GetResidueRewardMultiplier()));
            profile.Residue = ABY_BestiaryRewardUtility.ApplyExtractionBonus(profile.Residue);
            profile.HordeFragments = profile.HordeFragments > 0 ? ABY_BestiaryRewardUtility.ApplyExtractionBonus(profile.HordeFragments) : 0;
            profile.IndustrialComponents = Mathf.Max(0, Mathf.RoundToInt(profile.IndustrialComponents * Mathf.Clamp(AbyssalDifficultyUtility.GetBonusLootMultiplier(), 0.6f, 1.75f)));
            profile.IndustrialComponents = profile.IndustrialComponents > 0 ? ABY_BestiaryRewardUtility.ApplyExtractionBonus(profile.IndustrialComponents) : 0;
            profile.SpacerComponents = Mathf.Max(0, Mathf.RoundToInt(profile.SpacerComponents * Mathf.Clamp(AbyssalDifficultyUtility.GetBonusLootMultiplier(), 0.6f, 1.75f)));
            profile.SpacerComponents = profile.SpacerComponents > 0 ? ABY_BestiaryRewardUtility.ApplyExtractionBonus(profile.SpacerComponents) : 0;
            return profile;
        }

        private static string GetRewardProfileSummary(RewardProfile profile)
        {
            List<string> parts = new List<string>();
            AddResourceLabel(parts, ResidueThingDefName, profile.Residue);
            AddResourceLabel(parts, HordeFragmentThingDefName, profile.HordeFragments);
            AddResourceLabel(parts, IndustrialComponentThingDefName, profile.IndustrialComponents);
            AddResourceLabel(parts, SpacerComponentThingDefName, profile.SpacerComponents);
            return parts.Count > 0
                ? string.Join(", ", parts)
                : AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_HordeEconomy_Unknown", "reward routing pending");
        }

        private static void AddResourceLabel(List<string> parts, string defName, int count)
        {
            if (parts == null || count <= 0 || defName.NullOrEmpty())
            {
                return;
            }

            ThingDef def = DefDatabase<ThingDef>.GetNamedSilentFail(defName);
            parts.Add(count + " " + (def != null ? def.label : defName));
        }

        private static void SpawnRewardProfile(Map map, IntVec3 dropCell, RewardProfile profile)
        {
            if (map == null || !dropCell.IsValid)
            {
                return;
            }

            TrySpawnRewardStack(map, dropCell, ResidueThingDefName, profile.Residue);
            TrySpawnRewardStack(map, dropCell, HordeFragmentThingDefName, profile.HordeFragments);
            TrySpawnRewardStack(map, dropCell, IndustrialComponentThingDefName, profile.IndustrialComponents);
            TrySpawnRewardStack(map, dropCell, SpacerComponentThingDefName, profile.SpacerComponents);
        }

        private static void TrySpawnRewardStack(Map map, IntVec3 nearCell, string defName, int count)
        {
            if (map == null || !nearCell.IsValid || count <= 0 || defName.NullOrEmpty())
            {
                return;
            }

            ThingDef def = DefDatabase<ThingDef>.GetNamedSilentFail(defName);
            if (def == null)
            {
                return;
            }

            int remaining = count;
            while (remaining > 0)
            {
                Thing thing = ThingMaker.MakeThing(def);
                int stackCount = Mathf.Clamp(remaining, 1, Math.Max(1, def.stackLimit));
                thing.stackCount = stackCount;
                GenPlace.TryPlaceThing(thing, nearCell, map, ThingPlaceMode.Near);
                remaining -= stackCount;
            }
        }

        private static float GetBacklashHeatPercent(RewardSnapshot snapshot)
        {
            snapshot ??= new RewardSnapshot();
            float value = 5f + snapshot.Band * 2f + snapshot.FrontCount * 0.8f + snapshot.PulseCount * 0.4f;
            if (snapshot.HasSurgePhase)
            {
                value += 1.5f;
            }

            return value * Mathf.Clamp(AbyssalDifficultyUtility.GetInstabilityMultiplier(), 0.75f, 1.85f);
        }

        private static float GetBacklashContaminationPercent(RewardSnapshot snapshot)
        {
            snapshot ??= new RewardSnapshot();
            float value = 3.5f + snapshot.Band * 1.5f + snapshot.FrontCount * 0.6f;
            if (snapshot.HasSurgePhase)
            {
                value += 1f;
            }

            return value;
        }
    }
}
