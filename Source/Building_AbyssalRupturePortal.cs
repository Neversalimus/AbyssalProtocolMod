using System.Collections.Generic;
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

        private const string WarmupTrailMoteDefName = "ABY_Mote_ArchonDashTrail";
        private const string WarmupEntryMoteDefName = "ABY_Mote_ArchonDashEntry";
        private const string WarmupExitMoteDefName = "ABY_Mote_ArchonDashExit";

        private const int ReleaseHoldTicks = 34;
        private const float MidWarmupThreshold = 0.54f;
        private const float LateWarmupThreshold = 0.84f;

        private Faction portalFaction;
        private PawnKindDef bossKindDef;
        private int warmupTicks = 90;
        private int lingerTicks = 300;
        private int ticksActive;
        private int finalDespawnTick = -1;
        private int lordReleaseTick = -1;
        private bool bossSpawned;
        private bool entryPulseTriggered;
        private bool midWarmupTriggered;
        private bool lateWarmupTriggered;
        private bool releasePulseTriggered;
        private int seed;
        private string bossLabel = "Archon of Rupture";
        private Pawn spawnedBoss;

        public void Initialize(Faction faction, PawnKindDef kindDef, int warmup, int linger, string label)
        {
            portalFaction = faction ?? AbyssalBossSummonUtility.ResolveHostileFaction();
            bossKindDef = kindDef;
            warmupTicks = Mathf.Max(45, warmup);
            lingerTicks = Mathf.Max(120, linger);
            bossLabel = label.NullOrEmpty() ? "Archon of Rupture" : label;
            ticksActive = 0;
            finalDespawnTick = -1;
            lordReleaseTick = -1;
            bossSpawned = false;
            entryPulseTriggered = false;
            midWarmupTriggered = false;
            lateWarmupTriggered = false;
            releasePulseTriggered = false;
            spawnedBoss = null;
            seed = thingIDNumber >= 0 ? thingIDNumber : Rand.Range(0, 1000000);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref portalFaction, "portalFaction");
            Scribe_Defs.Look(ref bossKindDef, "bossKindDef");
            Scribe_Values.Look(ref warmupTicks, "warmupTicks", 90);
            Scribe_Values.Look(ref lingerTicks, "lingerTicks", 300);
            Scribe_Values.Look(ref ticksActive, "ticksActive", 0);
            Scribe_Values.Look(ref finalDespawnTick, "finalDespawnTick", -1);
            Scribe_Values.Look(ref lordReleaseTick, "lordReleaseTick", -1);
            Scribe_Values.Look(ref bossSpawned, "bossSpawned", false);
            Scribe_Values.Look(ref entryPulseTriggered, "entryPulseTriggered", false);
            Scribe_Values.Look(ref midWarmupTriggered, "midWarmupTriggered", false);
            Scribe_Values.Look(ref lateWarmupTriggered, "lateWarmupTriggered", false);
            Scribe_Values.Look(ref releasePulseTriggered, "releasePulseTriggered", false);
            Scribe_Values.Look(ref seed, "seed", 0);
            Scribe_Values.Look(ref bossLabel, "bossLabel", "Archon of Rupture");
            Scribe_References.Look(ref spawnedBoss, "spawnedBoss");
        }

        protected override void Tick()
        {
            base.Tick();
            ticksActive++;

            TickWarmupPresentation();
            TickReleaseHold();

            if (!bossSpawned && ticksActive >= warmupTicks)
            {
                TrySpawnBoss();
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

            float warmupProgress = warmupTicks > 0 ? Mathf.Clamp01((float)ticksActive / warmupTicks) : 1f;
            float activePulse = 0.5f + 0.5f * Mathf.Sin((Find.TickManager.TicksGame + seed) * 0.06f);
            float emberPulse = 0.5f + 0.5f * Mathf.Sin((Find.TickManager.TicksGame + seed) * 0.11f + 2.1f);
            float breachBoost = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.62f, 1f, warmupProgress));
            float lingerFade = finalDespawnTick < 0 ? 1f : Mathf.Clamp01((finalDespawnTick - ticksActive) / (float)Mathf.Max(1, lingerTicks));
            float alpha = Mathf.Lerp(0.25f, 1f, warmupProgress) * lingerFade;

            Vector3 loc = drawLoc;
            loc.y += 0.034f;

            float ringScale = Mathf.Lerp(1.4f, 2.8f, warmupProgress) * (0.94f + activePulse * (0.10f + breachBoost * 0.06f));
            float glowScale = Mathf.Lerp(1.0f, 3.2f, warmupProgress) * (0.92f + emberPulse * (0.14f + breachBoost * 0.09f));
            float emberScale = (2.6f + activePulse * 0.18f) * (1f + breachBoost * 0.14f);
            float angleA = (Find.TickManager.TicksGame + seed) * (0.85f + breachBoost * 0.35f);
            float angleB = -(Find.TickManager.TicksGame + seed) * (0.52f + breachBoost * 0.18f);

            DrawPlane(PortalGlowPath, loc, glowScale, angleA * 0.10f, new Color(0.92f, 0.10f, 0.18f, alpha * (0.70f + breachBoost * 0.14f)));
            DrawPlane(PortalRingPath, loc, ringScale, angleA, new Color(1f, 0.22f, 0.24f, alpha));
            DrawPlane(PortalRingPath, loc + new Vector3(0f, 0.004f, 0f), ringScale * (0.82f + breachBoost * 0.08f), angleB, new Color(0.65f, 0.05f, 0.10f, alpha * 0.62f));
            DrawPlane(PortalEmbersPath, loc + new Vector3(0f, 0.008f, 0f), emberScale, angleB * 1.25f, new Color(1f, 0.48f, 0.55f, alpha * (0.74f + breachBoost * 0.10f)));
        }

        private void TickWarmupPresentation()
        {
            if (Map == null || bossSpawned || warmupTicks <= 0)
            {
                return;
            }

            float progress = Mathf.Clamp01((float)ticksActive / warmupTicks);
            AbyssalBossScreenFXGameComponent fxComp = Current.Game?.GetComponent<AbyssalBossScreenFXGameComponent>();

            if (!entryPulseTriggered && progress >= 0.16f)
            {
                entryPulseTriggered = true;
                fxComp?.RegisterRitualPulse(Map, 0.10f);
                ABY_SoundUtility.PlayAt("ABY_RupturePortalOpen", Position, Map);
                TrySpawnWarmupMote(WarmupTrailMoteDefName, 1.05f);
                FleckMaker.Static(Position, Map, FleckDefOf.ExplosionFlash, 1.15f);
            }

            if (!midWarmupTriggered && progress >= MidWarmupThreshold)
            {
                midWarmupTriggered = true;
                fxComp?.RegisterRitualPulse(Map, 0.16f);
                ABY_SoundUtility.PlayAt("ABY_RuptureImpact", Position, Map);
                SpawnWarmupBurst(1.18f, 4);
            }

            if (!lateWarmupTriggered && progress >= LateWarmupThreshold)
            {
                lateWarmupTriggered = true;
                fxComp?.RegisterRitualPulse(Map, 0.26f);
                ABY_SoundUtility.PlayAt("ABY_SigilSpawnImpulse", Position, Map);
                SpawnWarmupBurst(1.46f, 6);
                MakeAshScar(2);
            }

            if (ShouldDoHashInterval(progress >= LateWarmupThreshold ? 6 : 12))
            {
                float scale = progress >= LateWarmupThreshold ? 1.18f : 0.90f;
                TrySpawnWarmupMote(progress >= LateWarmupThreshold ? WarmupExitMoteDefName : WarmupTrailMoteDefName, scale);
            }

            if (progress >= MidWarmupThreshold && ShouldDoHashInterval(18))
            {
                MakeAshScar(1);
            }
        }

        private void TickReleaseHold()
        {
            if (spawnedBoss == null || spawnedBoss.Destroyed || !spawnedBoss.Spawned || spawnedBoss.MapHeld == null)
            {
                spawnedBoss = null;
                lordReleaseTick = -1;
                return;
            }

            if (lordReleaseTick < 0)
            {
                return;
            }

            if (ticksActive < lordReleaseTick)
            {
                Pawn nearestThreat = AbyssalThreatPawnUtility.FindClosestThreatWithin(spawnedBoss, 36f);
                if (nearestThreat != null)
                {
                    spawnedBoss.rotationTracker?.FaceCell(nearestThreat.Position);
                }

                spawnedBoss.pather?.StopDead();
                return;
            }

            if (!releasePulseTriggered)
            {
                releasePulseTriggered = true;
                Current.Game?.GetComponent<AbyssalBossScreenFXGameComponent>()?.RegisterRitualPulse(Map, 0.28f);
                ABY_SoundUtility.PlayAt("ABY_ArchonBossArrive", spawnedBoss.PositionHeld, spawnedBoss.MapHeld);
                FleckMaker.Static(spawnedBoss.PositionHeld, spawnedBoss.MapHeld, FleckDefOf.ExplosionFlash, 1.6f);
            }

            AbyssalLordUtility.EnsureAssaultLord(spawnedBoss, sappers: true);
            spawnedBoss = null;
            lordReleaseTick = -1;
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
            spawnedBoss = boss;
            lordReleaseTick = ticksActive + ReleaseHoldTicks;

            Pawn nearestThreat = AbyssalThreatPawnUtility.FindClosestThreatWithin(boss, 36f);
            if (nearestThreat != null)
            {
                boss.rotationTracker?.FaceCell(nearestThreat.Position);
            }

            boss.pather?.StopDead();
            ArchonInfernalVFXUtility.DoSummonVFX(Map, spawnCell);
            MakeAshScar(3);
            ABY_SoundUtility.PlayAt("ABY_RuptureArrive", spawnCell, Map);

            AbyssalBossScreenFXGameComponent fxComp = Current.Game?.GetComponent<AbyssalBossScreenFXGameComponent>();
            fxComp?.RegisterBoss(boss);
            fxComp?.RegisterRitualPulse(Map, 0.34f);

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

        private void SpawnWarmupBurst(float scale, int ashCount)
        {
            TrySpawnWarmupMote(WarmupEntryMoteDefName, scale);
            TrySpawnWarmupMote(WarmupExitMoteDefName, scale * 0.92f);
            FleckMaker.Static(Position, Map, FleckDefOf.ExplosionFlash, Mathf.Max(1f, scale * 0.9f));
            MakeAshScar(ashCount);
        }

        private void TrySpawnWarmupMote(string defName, float scale)
        {
            if (Map == null || string.IsNullOrEmpty(defName))
            {
                return;
            }

            ThingDef moteDef = DefDatabase<ThingDef>.GetNamedSilentFail(defName);
            if (moteDef == null)
            {
                return;
            }

            MoteMaker.MakeStaticMote(Position.ToVector3Shifted(), Map, moteDef, scale);
        }

        private void MakeAshScar(int amountPerCell)
        {
            if (Map == null || amountPerCell <= 0)
            {
                return;
            }

            foreach (IntVec3 cell in GenRadial.RadialCellsAround(Position, 1.6f, true))
            {
                if (!cell.InBounds(Map) || !cell.Standable(Map))
                {
                    continue;
                }

                FilthMaker.TryMakeFilth(cell, Map, ThingDefOf.Filth_Ash, amountPerCell);
            }
        }

        private bool ShouldDoHashInterval(int interval)
        {
            if (interval <= 0)
            {
                return true;
            }

            int ticksGame = Find.TickManager != null ? Find.TickManager.TicksGame : 0;
            int hashOffset = thingIDNumber >= 0 ? thingIDNumber : 0;
            return (ticksGame + hashOffset) % interval == 0;
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
