using RimWorld;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    [StaticConstructorOnStartup]
    public class Building_AbyssalRupturePortal : Building
    {
        private const string PortalRingPath = "Things/VFX/RupturePortal/ABY_RupturePortal_Ring";
        private const string PortalGlowPath = "Things/VFX/RupturePortal/ABY_RupturePortal_Glow";
        private const string PortalEmbersPath = "Things/VFX/RupturePortal/ABY_RupturePortal_Embers";

        private const int DefaultReleaseHoldTicks = 24;

        private Faction portalFaction;
        private PawnKindDef bossKindDef;
        private int warmupTicks = 90;
        private int lingerTicks = 300;
        private int releaseHoldTicks = DefaultReleaseHoldTicks;
        private int ticksActive;
        private int finalDespawnTick = -1;
        private bool bossSpawned;
        private int seed;
        private string bossLabel = "Archon of Rupture";
        private bool stageOnePulseTriggered;
        private bool stageTwoPulseTriggered;
        private bool releaseWarmupTriggered;

        public void Initialize(Faction faction, PawnKindDef kindDef, int warmup, int linger, string label)
        {
            Initialize(faction, kindDef, warmup, linger, label, DefaultReleaseHoldTicks);
        }

        public void Initialize(Faction faction, PawnKindDef kindDef, int warmup, int linger, string label, int releaseHold)
        {
            portalFaction = faction ?? AbyssalBossSummonUtility.ResolveHostileFaction();
            bossKindDef = kindDef;
            warmupTicks = Mathf.Max(45, warmup);
            lingerTicks = Mathf.Max(120, linger);
            releaseHoldTicks = Mathf.Max(0, releaseHold);
            bossLabel = label.NullOrEmpty() ? "Archon of Rupture" : label;
            ticksActive = 0;
            finalDespawnTick = -1;
            bossSpawned = false;
            seed = thingIDNumber >= 0 ? thingIDNumber : Rand.Range(0, 1000000);
            stageOnePulseTriggered = false;
            stageTwoPulseTriggered = false;
            releaseWarmupTriggered = false;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref portalFaction, "portalFaction");
            Scribe_Defs.Look(ref bossKindDef, "bossKindDef");
            Scribe_Values.Look(ref warmupTicks, "warmupTicks", 90);
            Scribe_Values.Look(ref lingerTicks, "lingerTicks", 300);
            Scribe_Values.Look(ref releaseHoldTicks, "releaseHoldTicks", DefaultReleaseHoldTicks);
            Scribe_Values.Look(ref ticksActive, "ticksActive", 0);
            Scribe_Values.Look(ref finalDespawnTick, "finalDespawnTick", -1);
            Scribe_Values.Look(ref bossSpawned, "bossSpawned", false);
            Scribe_Values.Look(ref seed, "seed", 0);
            Scribe_Values.Look(ref bossLabel, "bossLabel", "Archon of Rupture");
            Scribe_Values.Look(ref stageOnePulseTriggered, "stageOnePulseTriggered", false);
            Scribe_Values.Look(ref stageTwoPulseTriggered, "stageTwoPulseTriggered", false);
            Scribe_Values.Look(ref releaseWarmupTriggered, "releaseWarmupTriggered", false);
        }

        protected override void Tick()
        {
            base.Tick();
            ticksActive++;

            if (!bossSpawned)
            {
                TickArrivalEffects();

                if (ticksActive >= warmupTicks + releaseHoldTicks)
                {
                    TrySpawnBoss();
                }
            }
            else if (ticksActive % 28 == 0 && Map != null && Rand.Chance(0.28f))
            {
                FilthMaker.TryMakeFilth(Position, Map, ThingDefOf.Filth_Ash, 1);
            }

            if (finalDespawnTick >= 0 && ticksActive >= finalDespawnTick)
            {
                Destroy(DestroyMode.Vanish);
            }
        }

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            base.DrawAt(drawLoc, flip);
            if (!Spawned || Map == null)
            {
                return;
            }

            int ticks = Find.TickManager != null ? Find.TickManager.TicksGame : 0;
            float breachProgress = Mathf.Clamp01(ticksActive / (float)Mathf.Max(1, warmupTicks));
            float releaseProgress = ticksActive <= warmupTicks
                ? 0f
                : Mathf.Clamp01((ticksActive - warmupTicks) / (float)Mathf.Max(1, releaseHoldTicks));
            float activePulse = 0.5f + 0.5f * Mathf.Sin((ticks + seed) * 0.06f);
            float emberPulse = 0.5f + 0.5f * Mathf.Sin((ticks + seed) * 0.11f + 2.1f);
            float cracklePulse = 0.5f + 0.5f * Mathf.Sin((ticks + seed) * 0.17f + 0.9f);
            float lingerFade = finalDespawnTick < 0 ? 1f : Mathf.Clamp01((finalDespawnTick - ticksActive) / (float)Mathf.Max(1, lingerTicks));
            float alpha = Mathf.Lerp(0.22f, 1f, breachProgress) * (0.92f + releaseProgress * 0.18f) * lingerFade;

            Vector3 loc = drawLoc;
            loc.y += 0.034f;

            float ringScale = Mathf.Lerp(1.45f, 3.05f, breachProgress) * (0.94f + activePulse * 0.10f);
            float innerRingScale = ringScale * Mathf.Lerp(0.74f, 0.90f, releaseProgress);
            float glowScale = Mathf.Lerp(1.10f, 3.55f, breachProgress) * (0.90f + emberPulse * 0.16f);
            float outerGlowScale = glowScale * Mathf.Lerp(1.08f, 1.22f, releaseProgress);
            float emberScale = Mathf.Lerp(2.10f, 2.95f, breachProgress) * (0.92f + cracklePulse * 0.18f);
            float angleA = (ticks + seed) * 0.92f;
            float angleB = -(ticks + seed) * 0.58f;
            float emberAngle = angleB * 1.28f + Mathf.Sin((ticks + seed) * 0.05f) * 6f;

            DrawPlane(PortalGlowPath, loc, outerGlowScale, angleA * 0.10f, new Color(0.82f, 0.06f, 0.08f, alpha * (0.34f + releaseProgress * 0.10f)));
            DrawPlane(PortalGlowPath, loc + new Vector3(0f, 0.004f, 0f), glowScale, angleB * 0.08f, new Color(1f, 0.22f, 0.20f, alpha * (0.58f + emberPulse * 0.12f)));
            DrawPlane(PortalRingPath, loc + new Vector3(0f, 0.006f, 0f), ringScale, angleA, new Color(1f, 0.28f, 0.24f, alpha));
            DrawPlane(PortalRingPath, loc + new Vector3(0f, 0.010f, 0f), innerRingScale, angleB, new Color(0.56f, 0.03f, 0.05f, alpha * 0.72f));
            DrawPlane(PortalEmbersPath, loc + new Vector3(0f, 0.013f, 0f), emberScale, emberAngle, new Color(1f, 0.50f, 0.42f, alpha * (0.58f + cracklePulse * 0.20f)));
            DrawPlane(PortalEmbersPath, loc + new Vector3(0f, 0.015f, 0f), emberScale * 0.72f, -emberAngle * 0.82f, new Color(1f, 0.92f, 0.84f, alpha * (0.16f + releaseProgress * 0.08f)));
        }

        private void TickArrivalEffects()
        {
            if (Map == null)
            {
                return;
            }

            float progress = Mathf.Clamp01(ticksActive / (float)Mathf.Max(1, warmupTicks));
            AbyssalBossScreenFXGameComponent fxComp = Current.Game?.GetComponent<AbyssalBossScreenFXGameComponent>();

            if (!stageOnePulseTriggered && progress >= 0.35f)
            {
                stageOnePulseTriggered = true;
                fxComp?.RegisterRitualPulse(Map, 0.12f);
                ABY_SoundUtility.PlayAt("ABY_SigilChargePulse", Position, Map);
                FleckMaker.ThrowLightningGlow(DrawPos, Map, 1.10f);
            }

            if (!stageTwoPulseTriggered && progress >= 0.68f)
            {
                stageTwoPulseTriggered = true;
                fxComp?.RegisterRitualPulse(Map, 0.18f);
                ABY_SoundUtility.PlayAt("ABY_SigilChargePulse", Position, Map);
                FleckMaker.ThrowLightningGlow(DrawPos, Map, 1.45f);
                FleckMaker.ThrowHeatGlow(Position, Map, 0.85f);
            }

            if (!releaseWarmupTriggered && ticksActive >= warmupTicks)
            {
                releaseWarmupTriggered = true;
                fxComp?.RegisterRitualPulse(Map, 0.28f);
                ABY_SoundUtility.PlayAt("ABY_SigilSpawnImpulse", Position, Map);
                FleckMaker.ThrowLightningGlow(DrawPos, Map, 2.10f);
                FleckMaker.ThrowHeatGlow(Position, Map, 1.15f);
                CreateAshScar(2, 0.55f);
            }

            if (ticksActive % 15 == 0)
            {
                FleckMaker.ThrowMicroSparks(DrawPos, Map);
            }

            if (ticksActive % 28 == 0)
            {
                FleckMaker.ThrowLightningGlow(DrawPos, Map, 0.82f + progress * 0.90f);
            }

            if (ticksActive % 34 == 0)
            {
                CreateAshScar(1, 0.22f + progress * 0.20f);
            }
        }

        private void TrySpawnBoss()
        {
            if (Map == null || bossKindDef == null)
            {
                finalDespawnTick = ticksActive + 60;
                bossSpawned = true;
                return;
            }

            if (!TryFindSpawnCell(out IntVec3 spawnCell))
            {
                return;
            }

            if (!AbyssalBossSummonUtility.TryGenerateBoss(Map, bossKindDef, portalFaction ?? AbyssalBossSummonUtility.ResolveHostileFaction(), bossLabel, out Pawn boss, out string _))
            {
                finalDespawnTick = ticksActive + 60;
                bossSpawned = true;
                return;
            }

            GenSpawn.Spawn(boss, spawnCell, Map, Rot4.Random);
            ArchonInfernalVFXUtility.DoSummonVFX(Map, spawnCell);
            ABY_SoundUtility.PlayAt("ABY_RuptureArrive", spawnCell, Map);
            FleckMaker.ThrowLightningGlow(spawnCell.ToVector3Shifted(), Map, 2.35f);
            FleckMaker.ThrowHeatGlow(spawnCell, Map, 1.45f);
            CreateAshScar(3, 0.85f);

            AbyssalBossScreenFXGameComponent fxComp = Current.Game?.GetComponent<AbyssalBossScreenFXGameComponent>();
            fxComp?.RegisterBoss(boss, bossLabel);
            fxComp?.RegisterRitualPulse(Map, 0.32f);

            string ritualId = string.Equals(bossKindDef?.defName, "ABY_ArchonOfRupture", System.StringComparison.OrdinalIgnoreCase)
                ? "archon_of_rupture"
                : "archon_beast";

            bool spawnedEscort = AbyssalBossOrchestrationUtility.TrySpawnEscortPackThroughPortal(
                Map,
                portalFaction ?? AbyssalBossSummonUtility.ResolveHostileFaction(),
                ritualId,
                bossKindDef?.defName,
                Position,
                string.Equals(ritualId, "archon_of_rupture", System.StringComparison.OrdinalIgnoreCase) ? 760f : 620f,
                bossLabel,
                out string escortFailReason);

            if (!spawnedEscort && !escortFailReason.NullOrEmpty())
            {
                Log.Warning("[Abyssal Protocol] Rupture portal escort plan warning: " + escortFailReason);
            }

            AbyssalLordUtility.EnsureAssaultLord(boss, sappers: true);

            Find.LetterStack.ReceiveLetter(
                "ABY_BossSummonSuccessLabel".Translate(),
                "ABY_BossSummonSuccessDesc".Translate(bossLabel),
                LetterDefOf.ThreatBig,
                new TargetInfo(spawnCell, Map));

            bossSpawned = true;
            finalDespawnTick = ticksActive + lingerTicks;
        }

        private bool TryFindSpawnCell(out IntVec3 cell)
        {
            cell = IntVec3.Invalid;
            foreach (IntVec3 candidate in GenRadial.RadialCellsAround(Position, 3.9f, true))
            {
                if (!candidate.InBounds(Map) || !candidate.Standable(Map))
                {
                    continue;
                }

                if (candidate.GetFirstPawn(Map) != null)
                {
                    continue;
                }

                cell = candidate;
                return true;
            }

            return false;
        }

        private void CreateAshScar(int amount, float chancePerCell)
        {
            if (Map == null || amount <= 0)
            {
                return;
            }

            int attempts = Mathf.Max(6, amount * 6);
            int spawned = 0;
            for (int i = 0; i < attempts && spawned < amount; i++)
            {
                IntVec3 cell = Position + GenRadial.RadialPattern[i % GenRadial.RadialPattern.Length];
                if (!cell.InBounds(Map))
                {
                    continue;
                }

                if (Rand.Chance(Mathf.Clamp01(chancePerCell)))
                {
                    FilthMaker.TryMakeFilth(cell, Map, ThingDefOf.Filth_Ash, 1);
                    spawned++;
                }
            }
        }

        private static void DrawPlane(string texPath, Vector3 loc, float scale, float angle, Color color)
        {
            if (string.IsNullOrEmpty(texPath))
            {
                return;
            }

            Material material = MaterialPool.MatFrom(texPath, ShaderDatabase.TransparentPostLight, color);
            Matrix4x4 matrix = Matrix4x4.identity;
            matrix.SetTRS(loc, Quaternion.AngleAxis(angle, Vector3.up), new Vector3(scale, 1f, scale));
            Graphics.DrawMesh(MeshPool.plane10, matrix, material, 0);
        }
    }
}
