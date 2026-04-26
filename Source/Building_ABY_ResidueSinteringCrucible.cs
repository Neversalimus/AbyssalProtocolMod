using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace AbyssalProtocol
{
    [StaticConstructorOnStartup]
    public class Building_ABY_ResidueSinteringCrucible : Building_WorkTable
    {
        private const string GlowOverlayTexPath = "Things/Building/ABY_ResidueSinteringCrucible_GlowOverlay";
        private const string HeatVentOverlayTexPath = "Things/Building/ABY_ResidueSinteringCrucible_HeatVentOverlay";

        private const float GlowAltitude = 0.034f;
        private const float HeatVentAltitude = 0.037f;

        private const int CorpseCountRefreshInterval = 250;
        private const int AshFilthCheckInterval = 180;

        private static readonly Vector2 GlowOverlaySize = new Vector2(3.32f, 2.22f);
        private static readonly Vector2 HeatVentOverlaySize = new Vector2(3.22f, 2.12f);

        private int cachedSinterableCorpseCount = -1;
        private int nextCorpseCountRefreshTick;

        private bool IsPowered
        {
            get
            {
                CompPowerTrader power = GetComp<CompPowerTrader>();
                return power == null || power.PowerOn;
            }
        }

        public override void TickRare()
        {
            base.TickRare();

            if (!Spawned || Map == null)
            {
                return;
            }

            RefreshCorpseCountIfNeeded();

            if (IsPowered && this.IsHashIntervalTick(AshFilthCheckInterval) && IsCurrentlyWorked() && Rand.Chance(0.08f))
            {
                TryMakeAshFilthNearby();
            }
        }

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            base.DrawAt(drawLoc, flip);

            if (!Spawned || Map == null)
            {
                return;
            }

            DrawSinteringOverlays(drawLoc);
        }

        public override string GetInspectString()
        {
            // Keep this inspect string deliberately self-contained. Some vanilla/worktable
            // inspect blocks can inject hidden empty lines after filtering; RimWorld logs an
            // error for that and may break the inspect pane. This block never returns a
            // leading, trailing, or doubled newline.
            RefreshCorpseCountIfNeeded(true);

            string statusLine = IsPowered
                ? "ABY_ResidueSinteringCrucible_InspectActive".Translate()
                : "ABY_ResidueSinteringCrucible_InspectOffline".Translate();

            string corpseLine = "ABY_ResidueSinteringCrucible_InspectSinterableCorpses".Translate(cachedSinterableCorpseCount);

            return statusLine + "
" + corpseLine;
        }

        private void DrawSinteringOverlays(Vector3 drawLoc)
        {
            int ticks = Find.TickManager != null ? Find.TickManager.TicksGame : 0;
            float seed = thingIDNumber * 0.01931f;

            float slowBreath = (Mathf.Sin(ticks * 0.045f + seed) + 1f) * 0.5f;
            float fastVent = (Mathf.Sin(ticks * 0.115f + seed + 1.45f) + 1f) * 0.5f;
            float heatFlicker = (Mathf.Sin(ticks * 0.173f + seed * 0.71f) + 1f) * 0.5f;

            if (!IsPowered)
            {
                DrawOverlay(
                    HeatVentOverlayTexPath,
                    new Vector3(drawLoc.x, drawLoc.y + HeatVentAltitude, drawLoc.z),
                    HeatVentOverlaySize * Mathf.Lerp(0.985f, 1.005f, fastVent),
                    0f,
                    new Color(1f, 0.22f, 0.06f, 0.055f));

                return;
            }

            DrawOverlay(
                HeatVentOverlayTexPath,
                new Vector3(drawLoc.x, drawLoc.y + HeatVentAltitude, drawLoc.z),
                HeatVentOverlaySize * Mathf.Lerp(0.99f, 1.035f, fastVent),
                Mathf.Sin(ticks * 0.011f + seed) * 0.32f,
                new Color(1f, 0.30f, 0.08f, Mathf.Lerp(0.16f, 0.34f, fastVent)));

            DrawOverlay(
                GlowOverlayTexPath,
                new Vector3(drawLoc.x, drawLoc.y + GlowAltitude, drawLoc.z),
                GlowOverlaySize * Mathf.Lerp(0.985f, 1.045f, slowBreath),
                Mathf.Sin(ticks * 0.007f + seed) * 0.42f,
                new Color(1f, 0.36f, 0.10f, Mathf.Lerp(0.18f, 0.40f, slowBreath)));

            DrawOverlay(
                GlowOverlayTexPath,
                new Vector3(drawLoc.x, drawLoc.y + GlowAltitude + 0.003f, drawLoc.z),
                GlowOverlaySize * Mathf.Lerp(0.78f, 0.87f, heatFlicker),
                -Mathf.Sin(ticks * 0.014f + seed) * 0.72f,
                new Color(1f, 0.12f, 0.04f, Mathf.Lerp(0.045f, 0.14f, heatFlicker)));
        }

        private static void DrawOverlay(string texPath, Vector3 loc, Vector2 size, float angle, Color color)
        {
            if (ContentFinder<Texture2D>.Get(texPath, false) == null)
            {
                return;
            }

            Material material = MaterialPool.MatFrom(texPath, ShaderDatabase.TransparentPostLight, color);
            Matrix4x4 matrix = Matrix4x4.identity;
            matrix.SetTRS(loc, Quaternion.AngleAxis(angle, Vector3.up), new Vector3(size.x, 1f, size.y));
            Graphics.DrawMesh(MeshPool.plane10, matrix, material, 0);
        }

        private void RefreshCorpseCountIfNeeded(bool force = false)
        {
            if (Map == null)
            {
                cachedSinterableCorpseCount = 0;
                return;
            }

            int ticks = Find.TickManager != null ? Find.TickManager.TicksGame : 0;
            if (!force && cachedSinterableCorpseCount >= 0 && ticks < nextCorpseCountRefreshTick)
            {
                return;
            }

            cachedSinterableCorpseCount = ABY_ResidueSinteringUtility.CountSinterableCorpses(Map);
            nextCorpseCountRefreshTick = ticks + CorpseCountRefreshInterval;
        }

        private bool IsCurrentlyWorked()
        {
            if (Map == null)
            {
                return false;
            }

            List<Pawn> pawns = Map.mapPawns?.FreeColonistsSpawned;
            if (pawns == null)
            {
                return false;
            }

            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn pawn = pawns[i];
                Job job = pawn?.CurJob;
                if (job == null)
                {
                    continue;
                }

                if (job.targetA.Thing == this || job.targetB.Thing == this || job.targetC.Thing == this)
                {
                    return true;
                }
            }

            return false;
        }

        private void TryMakeAshFilthNearby()
        {
            if (Map == null)
            {
                return;
            }

            foreach (IntVec3 cell in GenAdj.CellsAdjacent8Way(this).InRandomOrder())
            {
                if (!cell.InBounds(Map) || cell.Fogged(Map) || !cell.Walkable(Map))
                {
                    continue;
                }

                FilthMaker.TryMakeFilth(cell, Map, ThingDefOf.Filth_Ash);
                return;
            }
        }

        private static void AppendInspectBlock(List<string> lines, string block)
        {
            if (block.NullOrEmpty())
            {
                return;
            }

            string[] splitLines = block.Split(new[] { '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < splitLines.Length; i++)
            {
                AppendInspectLine(lines, splitLines[i]);
            }
        }

        private static void AppendInspectLine(List<string> lines, string line)
        {
            if (line.NullOrEmpty())
            {
                return;
            }

            string trimmed = line.Trim();
            if (trimmed.Length == 0)
            {
                return;
            }

            lines.Add(trimmed);
        }
    }
}
