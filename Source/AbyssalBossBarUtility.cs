using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    public sealed class ABY_BossBarPhaseSnapshot
    {
        public int phaseIndex;
        public float triggerHealthPct;
        public string label;
        public bool reached;
        public bool current;
    }

    public sealed class ABY_BossBarState
    {
        public Pawn boss;
        public ABY_BossBarProfileDef profile;
        public string displayLabel;
        public float healthPct;
        public float currentHealth;
        public float maxHealth;
        public int currentPhase;
        public string currentPhaseLabel;
        public string specialStateTag;
        public bool introStateActive;
        public bool criticalStateActive;
        public bool hasSecondaryBar;
        public bool secondaryCriticalStateActive;
        public float secondaryPct;
        public float secondaryCurrent;
        public float secondaryMax;
        public string secondaryLabel;
        public List<ABY_BossBarPhaseSnapshot> phases = new List<ABY_BossBarPhaseSnapshot>();
    }

    public static class AbyssalBossBarUtility
    {
        public static ABY_BossBarProfileDef ResolveProfileFor(Pawn pawn)
        {
            if (pawn == null)
            {
                return null;
            }

            ABY_BossBarProfileDef best = null;
            List<ABY_BossBarProfileDef> allDefs = DefDatabase<ABY_BossBarProfileDef>.AllDefsListForReading;
            for (int i = 0; i < allDefs.Count; i++)
            {
                ABY_BossBarProfileDef candidate = allDefs[i];
                if (candidate == null || !candidate.Matches(pawn))
                {
                    continue;
                }

                if (best == null || candidate.priority > best.priority)
                {
                    best = candidate;
                }
            }

            return best;
        }

        public static bool TryBuildState(Pawn pawn, ABY_BossBarProfileDef profile, string displayLabelOverride, out ABY_BossBarState state)
        {
            state = null;
            if (pawn == null || profile == null || pawn.health?.summaryHealth == null)
            {
                return false;
            }

            if (pawn.Destroyed || pawn.Dead || !pawn.Spawned || pawn.MapHeld == null)
            {
                return false;
            }

            if (pawn.Downed && !profile.showWhenDowned)
            {
                return false;
            }

            float healthPct = Mathf.Clamp01(pawn.health.summaryHealth.SummaryHealthPercent);
            float maxHealth = Mathf.Max(1f, GetApproximateMaxHealth(pawn));
            int currentPhase = ResolveCurrentPhase(pawn, profile, healthPct);

            state = new ABY_BossBarState
            {
                boss = pawn,
                profile = profile,
                displayLabel = profile.ResolveDisplayLabel(pawn, displayLabelOverride),
                healthPct = healthPct,
                maxHealth = maxHealth,
                currentHealth = maxHealth * healthPct,
                currentPhase = currentPhase,
                currentPhaseLabel = ResolvePhaseLabel(currentPhase)
            };

            PopulatePhaseSnapshots(state, profile, currentPhase);
            ApplyPhaseSpecialStateTag(state, profile);
            ApplySpecialStateContext(state, pawn, profile);
            for (int i = 0; i < state.phases.Count; i++)
            {
                ABY_BossBarPhaseSnapshot phase = state.phases[i];
                if (phase != null && phase.current && !phase.label.NullOrEmpty())
                {
                    state.currentPhaseLabel = phase.label;
                    break;
                }
            }

            if (state.introStateActive)
            {
                string introLabel = profile.ResolveIntroLabel();
                if (!introLabel.NullOrEmpty())
                {
                    state.currentPhaseLabel = introLabel;
                }
            }

            PopulateSecondaryBar(state, pawn, profile);
            return true;
        }

        public static string ResolvePhaseLabel(int phase)
        {
            switch (phase)
            {
                case 6:
                    return "VI";
                case 5:
                    return "V";
                case 4:
                    return "IV";
                case 3:
                    return "III";
                case 2:
                    return "II";
                default:
                    return "I";
            }
        }

        public static HediffComp_ArchonCoreController GetArchonCoreController(Pawn pawn)
        {
            if (pawn?.health?.hediffSet?.hediffs == null)
            {
                return null;
            }

            List<Hediff> hediffs = pawn.health.hediffSet.hediffs;
            for (int i = 0; i < hediffs.Count; i++)
            {
                HediffComp_ArchonCoreController comp = hediffs[i]?.TryGetComp<HediffComp_ArchonCoreController>();
                if (comp != null)
                {
                    return comp;
                }
            }

            return null;
        }

        public static HediffComp_RuptureCoreController GetRuptureCoreController(Pawn pawn)
        {
            if (pawn?.health?.hediffSet?.hediffs == null)
            {
                return null;
            }

            List<Hediff> hediffs = pawn.health.hediffSet.hediffs;
            for (int i = 0; i < hediffs.Count; i++)
            {
                HediffComp_RuptureCoreController comp = hediffs[i]?.TryGetComp<HediffComp_RuptureCoreController>();
                if (comp != null)
                {
                    return comp;
                }
            }

            return null;
        }

        public static CompABY_ReactorSaintPhaseController GetReactorSaintPhaseController(Pawn pawn)
        {
            return pawn?.TryGetComp<CompABY_ReactorSaintPhaseController>();
        }

        public static CompABY_ReactorAegis GetReactorAegis(Pawn pawn)
        {
            return pawn?.TryGetComp<CompABY_ReactorAegis>();
        }

        private static void ApplySpecialStateContext(ABY_BossBarState state, Pawn pawn, ABY_BossBarProfileDef profile)
        {
            if (state == null || pawn == null || profile == null)
            {
                return;
            }

            if (profile.phaseSourceMode == "RuptureCoreController")
            {
                HediffComp_RuptureCoreController controller = GetRuptureCoreController(pawn);
                if (controller == null)
                {
                    return;
                }

                if (controller.SpawnShieldActive)
                {
                    state.introStateActive = true;
                    state.specialStateTag = "rupture_intro";
                }
                else if (controller.FinalFrenzyTriggered || state.currentPhase >= 4)
                {
                    state.criticalStateActive = true;
                    state.specialStateTag = "rupture_frenzy";
                }

                return;
            }

            if (profile.phaseSourceMode == "ReactorSaintPhaseController")
            {
                CompABY_ReactorSaintPhaseController controller = GetReactorSaintPhaseController(pawn);
                CompABY_ReactorAegis aegis = GetReactorAegis(pawn);

                if (aegis != null && aegis.CollapseWindowActive)
                {
                    state.secondaryCriticalStateActive = true;
                    state.specialStateTag = "saint_aegis_collapsed";
                }
                else if (controller != null && controller.CurrentPhase >= 3)
                {
                    state.specialStateTag = "saint_phase_three";
                }
            }
        }

        private static float GetApproximateMaxHealth(Pawn pawn)
        {
            if (pawn == null)
            {
                return 100f;
            }

            try
            {
                float statValue = pawn.GetStatValue(StatDefOf.MaxHitPoints, true);
                if (statValue > 0.01f)
                {
                    return statValue;
                }
            }
            catch
            {
            }

            return 100f;
        }

        private static int ResolveCurrentPhase(Pawn pawn, ABY_BossBarProfileDef profile, float healthPct)
        {
            if (profile == null)
            {
                return 1;
            }

            switch (profile.phaseSourceMode)
            {
                case "ArchonCoreController":
                    return GetArchonCoreController(pawn)?.CurrentPhase ?? InferPhaseFromThresholds(profile, healthPct);
                case "RuptureCoreController":
                    return GetRuptureCoreController(pawn)?.CurrentPhase ?? InferPhaseFromThresholds(profile, healthPct);
                case "ReactorSaintPhaseController":
                    return GetReactorSaintPhaseController(pawn)?.CurrentPhase ?? InferPhaseFromThresholds(profile, healthPct);
                default:
                    return InferPhaseFromThresholds(profile, healthPct);
            }
        }

        private static int InferPhaseFromThresholds(ABY_BossBarProfileDef profile, float healthPct)
        {
            int currentPhase = 1;
            if (profile?.phaseEntries == null)
            {
                return currentPhase;
            }

            for (int i = 0; i < profile.phaseEntries.Count; i++)
            {
                ABY_BossBarPhaseEntry phaseEntry = profile.phaseEntries[i];
                if (phaseEntry == null)
                {
                    continue;
                }

                if (healthPct <= phaseEntry.triggerHealthPct && phaseEntry.phaseIndex > currentPhase)
                {
                    currentPhase = phaseEntry.phaseIndex;
                }
            }

            return currentPhase;
        }

        private static void PopulatePhaseSnapshots(ABY_BossBarState state, ABY_BossBarProfileDef profile, int currentPhase)
        {
            if (state == null || profile?.phaseEntries == null)
            {
                return;
            }

            List<ABY_BossBarPhaseEntry> ordered = new List<ABY_BossBarPhaseEntry>();
            for (int i = 0; i < profile.phaseEntries.Count; i++)
            {
                ABY_BossBarPhaseEntry entry = profile.phaseEntries[i];
                if (entry != null)
                {
                    ordered.Add(entry);
                }
            }

            ordered.Sort((a, b) => a.phaseIndex.CompareTo(b.phaseIndex));
            for (int i = 0; i < ordered.Count; i++)
            {
                ABY_BossBarPhaseEntry entry = ordered[i];
                state.phases.Add(new ABY_BossBarPhaseSnapshot
                {
                    phaseIndex = entry.phaseIndex,
                    triggerHealthPct = entry.triggerHealthPct,
                    label = entry.ResolveLabel(),
                    reached = currentPhase >= entry.phaseIndex,
                    current = currentPhase == entry.phaseIndex
                });
            }
        }

        private static void ApplyPhaseSpecialStateTag(ABY_BossBarState state, ABY_BossBarProfileDef profile)
        {
            if (state == null || profile?.phaseEntries == null)
            {
                return;
            }

            for (int i = 0; i < profile.phaseEntries.Count; i++)
            {
                ABY_BossBarPhaseEntry entry = profile.phaseEntries[i];
                if (entry == null || entry.phaseIndex != state.currentPhase || entry.specialStateTag.NullOrEmpty())
                {
                    continue;
                }

                state.specialStateTag = entry.specialStateTag;
                return;
            }
        }

        private static void PopulateSecondaryBar(ABY_BossBarState state, Pawn pawn, ABY_BossBarProfileDef profile)
        {
            if (state == null || pawn == null || profile == null || profile.secondaryBarSource.NullOrEmpty())
            {
                return;
            }

            switch (profile.secondaryBarSource)
            {
                case "ReactorAegis":
                    CompABY_ReactorAegis aegis = GetReactorAegis(pawn);
                    if (aegis == null)
                    {
                        return;
                    }

                    state.hasSecondaryBar = true;
                    state.secondaryPct = Mathf.Clamp01(aegis.AegisFraction);
                    state.secondaryCurrent = Mathf.Max(0f, aegis.CurrentAegisPoints);
                    state.secondaryMax = Mathf.Max(1f, aegis.MaxAegisPoints);
                    state.secondaryCriticalStateActive = aegis.CollapseWindowActive;
                    state.secondaryLabel = aegis.CollapseWindowActive
                        ? "ABY_BossBar_SecondaryAegisCollapsed".Translate()
                        : "ABY_BossBar_SecondaryAegis".Translate();
                    break;
            }
        }
    }
}
