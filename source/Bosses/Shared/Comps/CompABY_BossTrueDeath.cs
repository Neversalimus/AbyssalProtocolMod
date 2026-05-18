using RimWorld;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    public class CompABY_BossTrueDeath : ThingComp
    {
        private float currentBossHitPoints = -1f;
        private float maxBossHitPointsSnapshot = -1f;
        private bool deathAuthorized;
        private int lastRegisteredDamageTick = -999999;
        private int lastBodyStabilizeTick = -999999;
        private int lastSuppressedKillTick = -999999;

        private const int DamageBodyStabilizeCooldownTicks = 45;
        private const int ForcedBodyStabilizeCooldownTicks = 1;
        private const int DownedWatchdogIntervalTicks = 120;
        private float lastSuppressedKillDamage;
        private bool suppressedKillDamageConsumed;

        public CompProperties_ABY_BossTrueDeath Props => (CompProperties_ABY_BossTrueDeath)props;
        private Pawn PawnParent => parent as Pawn;

        public float MaxBossHitPoints
        {
            get
            {
                EnsureInitialized();
                return Mathf.Max(1f, maxBossHitPointsSnapshot);
            }
        }

        public float CurrentBossHitPoints
        {
            get
            {
                EnsureInitialized();
                return Mathf.Clamp(currentBossHitPoints, 0f, MaxBossHitPoints);
            }
        }

        public float HealthPercent
        {
            get
            {
                EnsureInitialized();
                return Mathf.Clamp01(CurrentBossHitPoints / Mathf.Max(1f, MaxBossHitPoints));
            }
        }

        public bool DeathAuthorized => deathAuthorized;
        public bool HasBossHitPointsRemaining => CurrentBossHitPoints > 0.001f;

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            EnsureInitialized();
            if (!deathAuthorized && HasBossHitPointsRemaining)
            {
                StabilizePawnBodyThrottled(force: true);
            }
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref currentBossHitPoints, "abyTrueDeath_currentBossHitPoints", -1f);
            Scribe_Values.Look(ref maxBossHitPointsSnapshot, "abyTrueDeath_maxBossHitPointsSnapshot", -1f);
            Scribe_Values.Look(ref deathAuthorized, "abyTrueDeath_deathAuthorized", false);
            Scribe_Values.Look(ref lastRegisteredDamageTick, "abyTrueDeath_lastRegisteredDamageTick", -999999);
            Scribe_Values.Look(ref lastBodyStabilizeTick, "abyTrueDeath_lastBodyStabilizeTick", -999999);
            Scribe_Values.Look(ref lastSuppressedKillTick, "abyTrueDeath_lastSuppressedKillTick", -999999);
            Scribe_Values.Look(ref lastSuppressedKillDamage, "abyTrueDeath_lastSuppressedKillDamage", 0f);
            Scribe_Values.Look(ref suppressedKillDamageConsumed, "abyTrueDeath_suppressedKillDamageConsumed", false);
        }


        public override string CompInspectStringExtra()
        {
            if (!ABY_StabilityDiagnosticsUtility.ShowDebugInspectStrings)
            {
                return null;
            }

            EnsureInitialized();
            return "ABY debug true HP: " + CurrentBossHitPoints.ToString("0.#") + " / " + MaxBossHitPoints.ToString("0.#")
                + " (" + (HealthPercent * 100f).ToString("0") + "%)"
                + "\nABY death authorized: " + deathAuthorized
                + "\nABY last damage tick: " + lastRegisteredDamageTick;
        }

        public override void CompTick()
        {
            base.CompTick();
            Pawn pawn = PawnParent;
            if (pawn == null || pawn.Destroyed)
            {
                return;
            }

            EnsureInitialized();
            if (deathAuthorized)
            {
                return;
            }

            if (currentBossHitPoints <= 0.001f)
            {
                AuthorizeAndKill(null, null);
                return;
            }

            // Keep the safety watchdog cheap. The Harmony health-state patches already suppress
            // vanilla death/downed transitions; polling ShouldBeDead/ShouldBeDowned every 30 ticks
            // re-enters those Harmony paths and caused regular spikes during boss fights.
            if (Find.TickManager != null && Find.TickManager.TicksGame % DownedWatchdogIntervalTicks != 0)
            {
                return;
            }

            if (!pawn.Dead && pawn.Downed)
            {
                StabilizePawnBodyThrottled(force: true);
                TryForceReengage();
            }
        }

        public override void PostPostApplyDamage(DamageInfo dinfo, float totalDamageDealt)
        {
            base.PostPostApplyDamage(dinfo, totalDamageDealt);
            if (deathAuthorized || PawnParent == null || PawnParent.Destroyed || PawnParent.Dead)
            {
                return;
            }

            if (ShouldConsumeSuppressedKillDamage(totalDamageDealt))
            {
                return;
            }

            RegisterBossDamage(totalDamageDealt, dinfo, null);
        }

        public void EnsureInitialized()
        {
            Pawn pawn = PawnParent;
            if (pawn == null)
            {
                return;
            }

            if (maxBossHitPointsSnapshot <= 0.001f)
            {
                maxBossHitPointsSnapshot = ResolveMaxBossHitPoints(pawn);
            }

            if (currentBossHitPoints < -0.001f)
            {
                float healthPct = 1f;
                try
                {
                    if (pawn.health?.summaryHealth != null)
                    {
                        healthPct = Mathf.Clamp01(pawn.health.summaryHealth.SummaryHealthPercent);
                    }
                }
                catch
                {
                    healthPct = 1f;
                }

                currentBossHitPoints = Mathf.Clamp(maxBossHitPointsSnapshot * healthPct, 1f, maxBossHitPointsSnapshot);
            }
            else
            {
                currentBossHitPoints = Mathf.Clamp(currentBossHitPoints, 0f, Mathf.Max(1f, maxBossHitPointsSnapshot));
            }
        }

        public bool ShouldSuppressVanillaDeathOrDowned()
        {
            EnsureInitialized();
            Pawn pawn = PawnParent;
            return pawn != null && !pawn.Destroyed && !deathAuthorized && currentBossHitPoints > 0.001f;
        }


        public void AuthorizeDevToolKill()
        {
            deathAuthorized = true;
            currentBossHitPoints = 0f;
        }

        public bool TrySuppressPrematureKill(DamageInfo? dinfo, Hediff exactCulprit)
        {
            if (!ShouldSuppressVanillaDeathOrDowned())
            {
                return false;
            }

            Pawn pawn = PawnParent;
            float fallbackDamage = ResolvePrematureKillFallbackDamage(dinfo);
            int tick = Find.TickManager != null ? Find.TickManager.TicksGame : 0;

            if (lastRegisteredDamageTick != tick && fallbackDamage > 0f)
            {
                RegisterBossDamage(fallbackDamage, dinfo, exactCulprit, fromSuppressedKill: true);
                lastSuppressedKillTick = tick;
                lastSuppressedKillDamage = fallbackDamage;
                suppressedKillDamageConsumed = false;
            }
            else
            {
                lastSuppressedKillTick = tick;
                lastSuppressedKillDamage = 0f;
                suppressedKillDamageConsumed = true;
            }

            if (!deathAuthorized && pawn != null && !pawn.Destroyed && !pawn.Dead)
            {
                StabilizePawnBodyThrottled(force: true);
                TryForceReengage();
            }

            return !deathAuthorized;
        }

        public void SuppressDownedState(DamageInfo? dinfo, Hediff hediff)
        {
            if (!ShouldSuppressVanillaDeathOrDowned())
            {
                return;
            }

            StabilizePawnBodyThrottled(force: true);
            TryForceReengage();
        }

        private void RegisterBossDamage(float rawDamage, DamageInfo? dinfo, Hediff exactCulprit, bool fromSuppressedKill = false)
        {
            EnsureInitialized();
            if (deathAuthorized || rawDamage <= 0f)
            {
                return;
            }

            float damage = Mathf.Max(0f, rawDamage) * Mathf.Max(0f, Props.damageTakenFactor);
            if (damage <= 0f)
            {
                return;
            }

            int tick = Find.TickManager != null ? Find.TickManager.TicksGame : 0;
            lastRegisteredDamageTick = tick;
            currentBossHitPoints = Mathf.Max(0f, currentBossHitPoints - damage);

            if (Props.debugLogging)
            {
                Log.Message("[Abyssal Protocol] TrueDeath damage " + damage.ToString("0.##") + " applied to " + PawnParent?.LabelShortCap + "; hp=" + currentBossHitPoints.ToString("0.##") + "/" + maxBossHitPointsSnapshot.ToString("0.##") + (fromSuppressedKill ? " suppressedKill" : string.Empty));
            }

            if (currentBossHitPoints <= 0.001f)
            {
                AuthorizeAndKill(dinfo, exactCulprit);
                return;
            }

            if (Props.stabilizeOnEveryDamage)
            {
                StabilizePawnBodyThrottled(force: false);
            }
        }

        private bool ShouldConsumeSuppressedKillDamage(float totalDamageDealt)
        {
            int tick = Find.TickManager != null ? Find.TickManager.TicksGame : 0;
            if (lastSuppressedKillTick != tick || suppressedKillDamageConsumed || lastSuppressedKillDamage <= 0f || totalDamageDealt <= 0f)
            {
                return false;
            }

            if (Mathf.Abs(totalDamageDealt - lastSuppressedKillDamage) <= Mathf.Max(0.5f, lastSuppressedKillDamage * 0.20f))
            {
                suppressedKillDamageConsumed = true;
                return true;
            }

            return false;
        }


        private void StabilizePawnBodyThrottled(bool force)
        {
            Pawn pawn = PawnParent;
            if (pawn == null || pawn.Destroyed || pawn.Dead)
            {
                return;
            }

            int tick = Find.TickManager != null ? Find.TickManager.TicksGame : 0;
            int cooldown = force ? ForcedBodyStabilizeCooldownTicks : DamageBodyStabilizeCooldownTicks;
            if (lastBodyStabilizeTick == tick || (!force && tick - lastBodyStabilizeTick < cooldown))
            {
                return;
            }

            ABY_BossTrueDeathUtility.StabilizePawnBody(pawn, Props);
            lastBodyStabilizeTick = tick;
        }

        private void AuthorizeAndKill(DamageInfo? dinfo, Hediff exactCulprit)
        {
            Pawn pawn = PawnParent;
            if (pawn == null || pawn.Destroyed || pawn.Dead)
            {
                return;
            }

            deathAuthorized = true;
            currentBossHitPoints = 0f;
            try
            {
                pawn.Kill(dinfo, exactCulprit);
            }
            catch (System.Exception ex)
            {
                Log.Warning("[Abyssal Protocol] TrueDeath failed to authorize boss death for " + pawn.LabelShortCap + ": " + ex.Message);
            }
        }

        private float ResolvePrematureKillFallbackDamage(DamageInfo? dinfo)
        {
            if (dinfo.HasValue)
            {
                float amount = dinfo.Value.Amount;
                if (amount > 0.01f)
                {
                    return amount;
                }
            }

            return Mathf.Max(1f, Props.fallbackKillDamage);
        }

        private float ResolveMaxBossHitPoints(Pawn pawn)
        {
            if (Props.maxBossHitPoints > 0.01f)
            {
                return Props.maxBossHitPoints;
            }

            float total = 0f;
            try
            {
                if (pawn?.RaceProps?.body?.AllParts != null)
                {
                    foreach (BodyPartRecord part in pawn.RaceProps.body.AllParts)
                    {
                        if (part?.def == null)
                        {
                            continue;
                        }

                        total += Mathf.Max(1f, part.def.GetMaxHealth(pawn));
                    }
                }
            }
            catch
            {
                total = 0f;
            }

            if (total > 1f)
            {
                return Mathf.Max(1f, total);
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

        private void TryForceReengage()
        {
            Pawn pawn = PawnParent;
            if (pawn == null || !Props.forceLordReengage || pawn.Destroyed || pawn.Dead || !pawn.Spawned || pawn.MapHeld == null || pawn.Faction == null || !pawn.HostileTo(Faction.OfPlayer))
            {
                return;
            }

            AbyssalLordUtility.EnsureAssaultLord(pawn, sappers: true);
        }
    }
}
