using System.Collections.Generic;
using System.Text;
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
        private static readonly Vector2 HeatVentOverlaySize = new Vector2(3.20f, 2.08f);

        private int cachedSinterableCorpseCount = -1;
        private int nextCorpseCountRefreshTick;

        private bool IsPowered => GetComp<CompPowerTrader>()?.PowerOn ?? true;

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
            StringBuilder sb = new StringBuilder();
            string baseInspect = base.GetInspectString();
            if (!baseInspect.NullOrEmpty())
            {
                sb.Append(baseInspect.TrimEnd());
            }

            AppendInspectLine(sb, IsPowered
                ? "ABY_ResidueSinteringCrucible_InspectActive".Translate()
                : "ABY_ResidueSinteringCrucible_InspectOffline".Translate());

            RefreshCorpseCountIfNeeded(true);
            AppendInspectLine(sb, "ABY_ResidueSinteringCrucible_InspectSinterableCorpses".Translate(cachedSinterableCorpseCount));

            return sb.ToString();
        }

        private void DrawSinteringOverlays(Vector3 drawLoc)
        {
            int ticks = Find.TickManager != null ? Find.TickManager.TicksGame : 0;
            float seed = thingIDNumber * 0.01931f;
            float pulse = (Mathf.Sin(ticks * 0.058f + seed) + 1f) * 0.5f;
            float ventPulse = (Mathf.Sin(ticks * 0.093f + seed + 1.45f) + 1f) * 0.5f;
            float powerAlpha = IsPowered ? 1f : 0.16f;

            DrawOverlay(
                HeatVentOverlayTexPath,
                new Vector3(drawLoc.x, drawLoc.y + HeatVentAltitude, drawLoc.z),
                HeatVentOverlaySize * Mathf.Lerp(0.98f, 1.035f, ventPulse),
                Mathf.Sin(ticks * 0.0105f + seed) * 0.45f,
                new Color(1f, 0.28f, 0.08f, Mathf.Lerp(0.16f, 0.36f, ventPulse) * powerAlpha));

            if (!IsPowered)
            {
                return;
            }

            DrawOverlay(
                GlowOverlayTexPath,
                new Vector3(drawLoc.x, drawLoc.y + GlowAltitude, drawLoc.z),
                GlowOverlaySize * Mathf.Lerp(0.985f, 1.055f, pulse),
                Mathf.Sin(ticks * 0.008f + seed) * 0.55f,
                new Color(1f, 0.38f, 0.11f, Mathf.Lerp(0.18f, 0.42f, pulse)));

            DrawOverlay(
                GlowOverlayTexPath,
                new Vector3(drawLoc.x, drawLoc.y + GlowAltitude + 0.003f, drawLoc.z),
                GlowOverlaySize * Mathf.Lerp(0.78f, 0.86f, ventPulse),
                -Mathf.Sin(ticks * 0.014f + seed) * 0.8f,
                new Color(1f, 0.12f, 0.04f, Mathf.Lerp(0.06f, 0.18f, ventPulse)));
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

        private static void AppendInspectLine(StringBuilder sb, string line)
        {
            if (line.NullOrEmpty())
            {
                return;
            }

            if (sb.Length > 0)
            {
                sb.AppendLine();
            }

            sb.Append(line);
        }
    }
}
