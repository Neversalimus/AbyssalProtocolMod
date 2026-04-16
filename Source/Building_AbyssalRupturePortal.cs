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

        private Faction portalFaction;
        private PawnKindDef bossKindDef;
        private int warmupTicks = 90;
        private int lingerTicks = 300;
        private int ticksActive;
        private int finalDespawnTick = -1;
        private bool bossSpawned;
        private int seed;
        private string bossLabel = "Archon of Rupture";

        public void Initialize(Faction faction, PawnKindDef kindDef, int warmup, int linger, string label)
        {
            portalFaction = faction ?? AbyssalBossSummonUtility.ResolveHostileFaction();
            bossKindDef = kindDef;
            warmupTicks = Mathf.Max(45, warmup);
            lingerTicks = Mathf.Max(120, linger);
            bossLabel = label.NullOrEmpty() ? "Archon of Rupture" : label;
            ticksActive = 0;
            finalDespawnTick = -1;
            bossSpawned = false;
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
            Scribe_Values.Look(ref bossSpawned, "bossSpawned", false);
            Scribe_Values.Look(ref seed, "seed", 0);
            Scribe_Values.Look(ref bossLabel, "bossLabel", "Archon of Rupture");
        }

        protected override void Tick()
        {
            base.Tick();
            ticksActive++;

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

            float openProgress = Mathf.Clamp01((float)ticksActive / warmupTicks);
            float activePulse = 0.5f + 0.5f * Mathf.Sin((Find.TickManager.TicksGame + seed) * 0.06f);
            float emberPulse = 0.5f + 0.5f * Mathf.Sin((Find.TickManager.TicksGame + seed) * 0.11f + 2.1f);
            float lingerFade = finalDespawnTick < 0 ? 1f : Mathf.Clamp01((finalDespawnTick - ticksActive) / (float)Mathf.Max(1, lingerTicks));
            float alpha = Mathf.Lerp(0.25f, 1f, openProgress) * lingerFade;

            Vector3 loc = drawLoc;
            loc.y += 0.034f;

            float ringScale = Mathf.Lerp(1.4f, 2.8f, openProgress) * (0.94f + activePulse * 0.10f);
            float glowScale = Mathf.Lerp(1.0f, 3.2f, openProgress) * (0.92f + emberPulse * 0.14f);
            float emberScale = 2.6f + activePulse * 0.18f;
            float angleA = (Find.TickManager.TicksGame + seed) * 0.85f;
            float angleB = -(Find.TickManager.TicksGame + seed) * 0.52f;

            DrawPlane(PortalGlowPath, loc, glowScale, angleA * 0.10f, new Color(0.92f, 0.10f, 0.18f, alpha * 0.70f));
            DrawPlane(PortalRingPath, loc, ringScale, angleA, new Color(1f, 0.22f, 0.24f, alpha));
            DrawPlane(PortalRingPath, loc + new Vector3(0f, 0.004f, 0f), ringScale * 0.82f, angleB, new Color(0.65f, 0.05f, 0.10f, alpha * 0.62f));
            DrawPlane(PortalEmbersPath, loc + new Vector3(0f, 0.008f, 0f), emberScale, angleB * 1.25f, new Color(1f, 0.48f, 0.55f, alpha * 0.74f));
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

            AbyssalBossScreenFXGameComponent fxComp = Current.Game?.GetComponent<AbyssalBossScreenFXGameComponent>();
            fxComp?.RegisterBoss(boss);

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
