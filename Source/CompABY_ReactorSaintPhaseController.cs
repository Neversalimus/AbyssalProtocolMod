using RimWorld;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    public class CompABY_ReactorSaintPhaseController : ThingComp
    {
        private int currentPhase = -1;

        public CompProperties_ABY_ReactorSaintPhaseController Props => (CompProperties_ABY_ReactorSaintPhaseController)props;

        public int CurrentPhase => currentPhase < 1 ? 1 : currentPhase;
        public float Phase2HealthPct => Props.phase2HealthPct;
        public float Phase3HealthPct => Props.phase3HealthPct;

        private Pawn PawnParent => parent as Pawn;

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            Pawn pawn = PawnParent;
            if (pawn != null && pawn.Spawned)
            {
                UpdatePhase(force: true);
            }
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref currentPhase, "currentPhase", -1);
        }

        public override void CompTick()
        {
            base.CompTick();
            Pawn pawn = PawnParent;
            if (pawn == null || !pawn.Spawned || pawn.MapHeld == null || pawn.Dead)
            {
                return;
            }

            if (!parent.IsHashIntervalTick(15))
            {
                return;
            }

            UpdatePhase(force: false);
        }

        public override string CompInspectStringExtra()
        {
            return "Reactor Phase: " + ResolvePhaseLabel(currentPhase < 1 ? 1 : currentPhase);
        }

        private void UpdatePhase(bool force)
        {
            Pawn pawn = PawnParent;
            if (pawn == null || pawn.health == null)
            {
                return;
            }

            float hpPct = pawn.health.summaryHealth.SummaryHealthPercent;
            int newPhase = hpPct <= Props.phase3HealthPct ? 3 : (hpPct <= Props.phase2HealthPct ? 2 : 1);
            if (!force && newPhase == currentPhase)
            {
                return;
            }

            currentPhase = newPhase;
            ApplyPhaseState(newPhase);
            TriggerPhaseFeedback(pawn, force);
        }

        private void ApplyPhaseState(int phase)
        {
            CompABY_ReactorAegis aegis = parent.TryGetComp<CompABY_ReactorAegis>();
            CompABY_ReactorOverheatField overheat = parent.TryGetComp<CompABY_ReactorOverheatField>();
            CompABY_ReactorSaintShooter shooter = parent.TryGetComp<CompABY_ReactorSaintShooter>();

            switch (phase)
            {
                case 3:
                    aegis?.ApplyPhaseTuning(Props.phase3AegisMaxFactor, Props.phase3AegisRechargeFactor, Props.phase3AegisDelayFactor);
                    overheat?.ApplyPhaseTuning(Props.phase3OverheatRadius, Props.phase3OverheatSeverity, Props.phase3OverheatIntervalTicks);
                    shooter?.SetPhaseTuning(
                        Props.phase3PrimaryCooldownFactor,
                        Props.phase3BarrageCooldownFactor,
                        Props.phase3WarmupFactor,
                        Props.phase3BarrageChanceBonus,
                        Props.phase3BarrageShotBonus);
                    break;
                case 2:
                    aegis?.ApplyPhaseTuning(Props.phase2AegisMaxFactor, Props.phase2AegisRechargeFactor, Props.phase2AegisDelayFactor);
                    overheat?.ApplyPhaseTuning(Props.phase2OverheatRadius, Props.phase2OverheatSeverity, Props.phase2OverheatIntervalTicks);
                    shooter?.SetPhaseTuning(
                        Props.phase2PrimaryCooldownFactor,
                        Props.phase2BarrageCooldownFactor,
                        Props.phase2WarmupFactor,
                        Props.phase2BarrageChanceBonus,
                        Props.phase2BarrageShotBonus);
                    break;
                default:
                    aegis?.ApplyPhaseTuning(Props.phase1AegisMaxFactor, Props.phase1AegisRechargeFactor, Props.phase1AegisDelayFactor);
                    overheat?.ApplyPhaseTuning(Props.phase1OverheatRadius, Props.phase1OverheatSeverity, Props.phase1OverheatIntervalTicks);
                    shooter?.SetPhaseTuning(
                        Props.phase1PrimaryCooldownFactor,
                        Props.phase1BarrageCooldownFactor,
                        Props.phase1WarmupFactor,
                        Props.phase1BarrageChanceBonus,
                        Props.phase1BarrageShotBonus);
                    break;
            }
        }

        private void TriggerPhaseFeedback(Pawn pawn, bool force)
        {
            if (force || pawn?.MapHeld == null)
            {
                return;
            }

            FleckMaker.Static(pawn.PositionHeld, pawn.MapHeld, FleckDefOf.ExplosionFlash, Props.phaseTransitionFlashScale);
            if (!Props.phaseTransitionSoundDefName.NullOrEmpty())
            {
                ABY_SoundUtility.PlayAt(Props.phaseTransitionSoundDefName, pawn.PositionHeld, pawn.MapHeld);
            }
        }

        private static string ResolvePhaseLabel(int phase)
        {
            switch (phase)
            {
                case 3:
                    return "III";
                case 2:
                    return "II";
                default:
                    return "I";
            }
        }
    }
}
