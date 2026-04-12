using RimWorld;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    [StaticConstructorOnStartup]
    public class Building_AbyssalImpPortal : Building
    {
        private const string PortalRingPath = "Things/VFX/ImpPortal/ABY_ImpPortal_Ring";
        private const string PortalGlowPath = "Things/VFX/ImpPortal/ABY_ImpPortal_Glow";
        private const string PortalEmbersPath = "Things/VFX/ImpPortal/ABY_ImpPortal_Embers";

        private Faction portalFaction;
        private PawnKindDef impKindDef;
        private int warmupTicks = 45;
        private int spawnIntervalTicks = 18;
        private int lingerTicks = 180;
        private int ticksActive;
        private int impsRemaining;
        private int nextImpSpawnTick = 45;
        private int finalDespawnTick = -1;
        private int seed;

        public void Initialize(Faction faction, PawnKindDef impPawnKind, int impCount, int warmup, int interval, int linger)
        {
            portalFaction = faction;
            impKindDef = impPawnKind;
            impsRemaining = Mathf.Max(1, impCount);
            warmupTicks = Mathf.Max(20, warmup);
            spawnIntervalTicks = Mathf.Max(8, interval);
            lingerTicks = Mathf.Max(60, linger);
            nextImpSpawnTick = warmupTicks;
            finalDespawnTick = -1;
            ticksActive = 0;
            seed = thingIDNumber >= 0 ? thingIDNumber : Rand.Range(0, 1000000);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref portalFaction, "portalFaction");
            Scribe_Defs.Look(ref impKindDef, "impKindDef");
            Scribe_Values.Look(ref warmupTicks, "warmupTicks", 45);
            Scribe_Values.Look(ref spawnIntervalTicks, "spawnIntervalTicks", 18);
            Scribe_Values.Look(ref lingerTicks, "lingerTicks", 180);
            Scribe_Values.Look(ref ticksActive, "ticksActive", 0);
            Scribe_Values.Look(ref impsRemaining, "impsRemaining", 0);
            Scribe_Values.Look(ref nextImpSpawnTick, "nextImpSpawnTick", 45);
            Scribe_Values.Look(ref finalDespawnTick, "finalDespawnTick", -1);
            Scribe_Values.Look(ref seed, "seed", 0);
        }

        protected override void Tick()
        {
            base.Tick();

            ticksActive++;

            if (impsRemaining > 0 && ticksActive >= nextImpSpawnTick)
            {
                SpawnImp();
                nextImpSpawnTick += spawnIntervalTicks;
            }

            if (impsRemaining <= 0 && finalDespawnTick < 0)
            {
                finalDespawnTick = ticksActive + lingerTicks;
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
            float activePulse = 0.5f + 0.5f * Mathf.Sin((Find.TickManager.TicksGame + seed) * 0.08f);
            float emberPulse = 0.5f + 0.5f * Mathf.Sin((Find.TickManager.TicksGame + seed) * 0.13f + 1.5f);
            float lingerFade = finalDespawnTick < 0 ? 1f : Mathf.Clamp01((finalDespawnTick - ticksActive) / (float)Mathf.Max(1, lingerTicks));
            float alpha = Mathf.Lerp(0.35f, 1f, openProgress) * lingerFade;

            Vector3 loc = drawLoc;
            loc.y += 0.032f;

            float ringScale = Mathf.Lerp(0.65f, 1.18f, openProgress) * (0.96f + activePulse * 0.08f);
            float glowScale = Mathf.Lerp(0.42f, 1.34f, openProgress) * (0.92f + emberPulse * 0.12f);
            float emberScale = 1.18f + activePulse * 0.10f;
            float angleA = (Find.TickManager.TicksGame + seed) * 1.15f;
            float angleB = -(Find.TickManager.TicksGame + seed) * 0.70f;

            DrawPlane(PortalGlowPath, loc, glowScale, angleA * 0.12f, new Color(1f, 0.40f, 0.16f, alpha * 0.62f));
            DrawPlane(PortalRingPath, loc, ringScale, angleA, new Color(1f, 0.66f, 0.28f, alpha));
            DrawPlane(PortalRingPath, loc + new Vector3(0f, 0.004f, 0f), ringScale * 0.84f, angleB, new Color(1f, 0.24f, 0.10f, alpha * 0.48f));
            DrawPlane(PortalEmbersPath, loc + new Vector3(0f, 0.008f, 0f), emberScale, angleB * 1.4f, new Color(1f, 0.78f, 0.38f, alpha * 0.82f));
        }

        private void SpawnImp()
        {
            if (Map == null || impKindDef == null || portalFaction == null)
            {
                impsRemaining = 0;
                return;
            }

            if (!TryFindSpawnCell(out IntVec3 spawnCell))
            {
                impsRemaining--;
                return;
            }

            if (!ABY_Phase2PortalUtility.TryGenerateImp(impKindDef, portalFaction, Map, out Pawn imp))
            {
                impsRemaining--;
                return;
            }

            GenSpawn.Spawn(imp, spawnCell, Map, Rot4.Random);
            ABY_Phase2PortalUtility.GiveAssaultLord(imp);
            impsRemaining--;
        }

        private bool TryFindSpawnCell(out IntVec3 cell)
        {
            cell = IntVec3.Invalid;
            foreach (IntVec3 candidate in GenRadial.RadialCellsAround(Position, 2.9f, true))
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
