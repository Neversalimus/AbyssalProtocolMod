using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    public class ABY_HoverArmorMapComponent : MapComponent
    {
        private const float RingYOffset = 0.018f;
        private const float FlightRigPawnBackOffset = -0.034f;
        private const int BaseMaxSparks = 96;
        private const int ActivePawnRefreshIntervalTicks = 60;
        private readonly List<HoverSpark> sparks = new List<HoverSpark>();
        private readonly List<Pawn> activeHoverPawns = new List<Pawn>();
        private readonly Dictionary<Pawn, int> nextSparkTickByPawn = new Dictionary<Pawn, int>();
        private readonly Dictionary<Pawn, int> activeSinceTickByPawn = new Dictionary<Pawn, int>();
        private int nextActivePawnRefreshTick;

        public ABY_HoverArmorMapComponent(Map map) : base(map)
        {
        }

        public override void MapComponentTick()
        {
            base.MapComponentTick();

            int now = Find.TickManager.TicksGame;
            CleanupSparkSchedule();
            CleanupActiveSinceSchedule();
            TickSparks(now);

            if (now >= nextActivePawnRefreshTick)
            {
                RefreshActiveHoverPawns(now);
            }

            if (activeHoverPawns.Count == 0)
            {
                return;
            }

            for (int i = activeHoverPawns.Count - 1; i >= 0; i--)
            {
                Pawn pawn = activeHoverPawns[i];
                if (!IsTrackedPawnStillValid(pawn) || !ABY_HoverArmorUtility.TryGetActiveHoverExtension(pawn, out ABY_HoverArmorExtension extension))
                {
                    activeHoverPawns.RemoveAt(i);
                    if (pawn != null)
                    {
                        activeSinceTickByPawn.Remove(pawn);
                        nextSparkTickByPawn.Remove(pawn);
                        ABY_HoverArmorUtility.Invalidate(pawn);
                    }
                    continue;
                }

                if (extension == null || !extension.enableMovingSparks || !IsMoving(pawn))
                {
                    continue;
                }

                int nextTick;
                if (nextSparkTickByPawn.TryGetValue(pawn, out nextTick) && now < nextTick)
                {
                    continue;
                }

                int interval = ABY_VfxBudget.ScaleInterval(Mathf.Max(8, extension.sparkIntervalTicks));
                nextSparkTickByPawn[pawn] = now + interval;
                TryAddSpark(pawn, extension, now);
            }
        }

        public override void MapComponentUpdate()
        {
            base.MapComponentUpdate();

            if (Current.ProgramState != ProgramState.Playing || map == null)
            {
                return;
            }

            int ticks = Find.TickManager.TicksGame;
            // Rig and underfoot ring are drawn from the PawnRenderer prefix so they are ordered behind the pawn.
            // This component keeps only transient moving sparks/trails.
            DrawSparks(ticks);
        }

        private void RefreshActiveHoverPawns(int ticks)
        {
            nextActivePawnRefreshTick = ticks + ActivePawnRefreshIntervalTicks + Mathf.Abs((map?.uniqueID ?? 0) % 11);
            activeHoverPawns.Clear();

            IReadOnlyList<Pawn> pawns = ABY_RuntimeTargetCache.SpawnedLivingPawnsFor(map);
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn pawn = pawns[i];
                if (!ABY_HoverArmorUtility.TryGetActiveHoverExtensionUncached(pawn, out _))
                {
                    activeSinceTickByPawn.Remove(pawn);
                    nextSparkTickByPawn.Remove(pawn);
                    ABY_HoverArmorUtility.Invalidate(pawn);
                    continue;
                }

                activeHoverPawns.Add(pawn);
                if (!activeSinceTickByPawn.ContainsKey(pawn))
                {
                    activeSinceTickByPawn[pawn] = ticks;
                }
            }
        }

        private bool IsTrackedPawnStillValid(Pawn pawn)
        {
            return pawn != null && !pawn.Destroyed && !pawn.Dead && pawn.Spawned && pawn.MapHeld == map;
        }

        private void DrawActiveFlightRigs(int ticks)
        {
            if (activeHoverPawns.Count == 0)
            {
                RefreshActiveHoverPawns(ticks);
            }

            for (int i = 0; i < activeHoverPawns.Count; i++)
            {
                Pawn pawn = activeHoverPawns[i];
                if (!ABY_HoverArmorUtility.TryGetActiveHoverExtension(pawn, out ABY_HoverArmorExtension extension))
                {
                    continue;
                }

                if (extension == null || !extension.enableFlightRigFx)
                {
                    continue;
                }

                DrawFlightRig(pawn, extension, ticks);
            }
        }

        private void DrawFlightRig(Pawn pawn, ABY_HoverArmorExtension extension, int ticks)
        {
            if (!TryGetFlightRigTexture(extension, pawn.Rotation, out string texPath))
            {
                return;
            }

            int activeSince;
            if (!activeSinceTickByPawn.TryGetValue(pawn, out activeSince))
            {
                activeSince = ticks;
                activeSinceTickByPawn[pawn] = ticks;
            }

            float seed = Mathf.Abs((pawn.thingIDNumber * 61) % 997);
            float age = Mathf.Max(0f, ticks - activeSince);
            float fade = Mathf.Clamp01(age / 18f);
            float pulse = Mathf.Sin((ticks + seed) * 0.082f);
            float energyPulse = 0.5f + 0.5f * Mathf.Sin((ticks + seed) * 0.145f);
            float bob = Mathf.Sin((ticks + seed) * 0.055f) * Mathf.Max(0f, extension.flightRigBobAmplitude) * fade;
            float scale = Mathf.Max(0.1f, extension.flightRigScale + pulse * Mathf.Max(0f, extension.flightRigPulseScale));
            float alpha = Mathf.Clamp01(extension.flightRigAlpha * fade * (1f - extension.flightRigPulseAlpha * 0.5f + energyPulse * extension.flightRigPulseAlpha));

            Vector3 drawPos = pawn.DrawPos + FlightRigOffset(extension, pawn.Rotation);
            drawPos.z += bob;
            drawPos.y = AltitudeLayer.Pawn.AltitudeFor() + FlightRigPawnBackOffset;

            // Keep positive mesh scale. West uses its own flipped asset; negative scale can be culled.
            DrawPlane(texPath, drawPos, scale, scale, alpha, 0f);
        }

        private void DrawActiveHoverRings(int ticks)
        {
            if (activeHoverPawns.Count == 0)
            {
                RefreshActiveHoverPawns(ticks);
            }

            for (int i = 0; i < activeHoverPawns.Count; i++)
            {
                Pawn pawn = activeHoverPawns[i];
                if (!ABY_HoverArmorUtility.TryGetActiveHoverExtension(pawn, out ABY_HoverArmorExtension extension))
                {
                    continue;
                }

                if (extension == null || !extension.enableUnderfootFx)
                {
                    continue;
                }

                DrawHoverRing(pawn, extension, ticks);
            }
        }

        private void DrawHoverRing(Pawn pawn, ABY_HoverArmorExtension extension, int ticks)
        {
            string texPath = extension.ringTexPath;
            if (texPath.NullOrEmpty())
            {
                return;
            }

            float seed = Mathf.Abs((pawn.thingIDNumber * 37) % 997);
            float pulse = 0.5f + 0.5f * Mathf.Sin((ticks + seed) * 0.105f);
            float movementBonus = IsMoving(pawn) ? Mathf.Max(0f, extension.movingRingScaleBonus) : 0f;
            float scale = Mathf.Max(0.12f, extension.ringScale + movementBonus + (pulse - 0.5f) * Mathf.Max(0f, extension.pulseAmplitude));
            float alpha = Mathf.Clamp01(extension.ringAlpha * (0.72f + pulse * 0.42f));

            Vector3 drawPos = pawn.DrawPos;
            drawPos.y = AltitudeLayer.MoteLow.AltitudeFor() + RingYOffset;
            DrawPlane(texPath, drawPos, scale, scale, alpha, 0f);
        }

        private void TryAddSpark(Pawn pawn, ABY_HoverArmorExtension extension, int ticks)
        {
            if (extension.sparkTexPath.NullOrEmpty() || sparks.Count >= ResolveMaxSparks())
            {
                return;
            }

            Vector3 offset = BackTrailOffset(pawn.Rotation);
            Vector3 randomJitter = new Vector3(Rand.Range(-0.07f, 0.07f), 0f, Rand.Range(-0.05f, 0.05f));
            Vector3 pos = pawn.DrawPos + offset + randomJitter;
            pos.y = AltitudeLayer.MoteLow.AltitudeFor() + RingYOffset + 0.006f;

            if (!ABY_VfxBudget.TrySpend(map, ABY_VfxBudgetCategory.UIOrDecorative, 1))
            {
                return;
            }

            sparks.Add(new HoverSpark
            {
                TexPath = extension.sparkTexPath,
                Position = pos,
                SpawnTick = ticks,
                LifetimeTicks = Mathf.Max(6, extension.sparkLifetimeTicks),
                Scale = Mathf.Max(0.05f, extension.sparkScale * Rand.Range(0.78f, 1.22f)),
                Alpha = Mathf.Clamp01(extension.sparkAlpha),
                Angle = Rand.Range(0f, 360f)
            });
        }

        private static int ResolveMaxSparks()
        {
            if (ABY_PerformanceSettingsUtility.IsMinimal)
            {
                return 24;
            }

            return ABY_PerformanceSettingsUtility.IsReducedOrLower ? 48 : BaseMaxSparks;
        }

        private void TickSparks(int ticks)
        {
            for (int i = sparks.Count - 1; i >= 0; i--)
            {
                if (ticks - sparks[i].SpawnTick > sparks[i].LifetimeTicks)
                {
                    sparks.RemoveAt(i);
                }
            }
        }

        private void DrawSparks(int ticks)
        {
            for (int i = sparks.Count - 1; i >= 0; i--)
            {
                HoverSpark spark = sparks[i];
                float age = ticks - spark.SpawnTick;
                if (age < 0f || age > spark.LifetimeTicks)
                {
                    continue;
                }

                float progress = Mathf.Clamp01(age / Mathf.Max(1f, spark.LifetimeTicks));
                float alpha = spark.Alpha * (1f - progress);
                float scale = spark.Scale * (1f + progress * 0.45f);
                Vector3 pos = spark.Position;
                pos.y += progress * 0.012f;
                DrawPlane(spark.TexPath, pos, scale, scale, alpha, spark.Angle + progress * 25f);
            }
        }

        private void CleanupSparkSchedule()
        {
            if (nextSparkTickByPawn.Count == 0 || Find.TickManager.TicksGame % 250 != 0)
            {
                return;
            }

            List<Pawn> remove = null;
            foreach (Pawn pawn in nextSparkTickByPawn.Keys)
            {
                if (pawn == null || pawn.Destroyed || !pawn.Spawned || pawn.MapHeld != map)
                {
                    if (remove == null)
                    {
                        remove = new List<Pawn>();
                    }
                    remove.Add(pawn);
                }
            }

            if (remove == null)
            {
                return;
            }

            for (int i = 0; i < remove.Count; i++)
            {
                nextSparkTickByPawn.Remove(remove[i]);
            }
        }

        private void CleanupActiveSinceSchedule()
        {
            if (activeSinceTickByPawn.Count == 0 || Find.TickManager.TicksGame % 250 != 0)
            {
                return;
            }

            List<Pawn> remove = null;
            foreach (Pawn pawn in activeSinceTickByPawn.Keys)
            {
                if (pawn == null || pawn.Destroyed || !pawn.Spawned || pawn.MapHeld != map)
                {
                    if (remove == null)
                    {
                        remove = new List<Pawn>();
                    }
                    remove.Add(pawn);
                }
            }

            if (remove == null)
            {
                return;
            }

            for (int i = 0; i < remove.Count; i++)
            {
                activeSinceTickByPawn.Remove(remove[i]);
            }
        }

        private static bool IsMoving(Pawn pawn)
        {
            return pawn?.pather != null && pawn.pather.MovingNow;
        }

        private static bool TryGetFlightRigTexture(ABY_HoverArmorExtension extension, Rot4 rot, out string texPath)
        {
            texPath = null;

            if (extension == null)
            {
                return false;
            }

            if (rot == Rot4.North)
            {
                texPath = extension.flightRigTexPathNorth;
            }
            else if (rot == Rot4.East)
            {
                texPath = extension.flightRigTexPathEast;
            }
            else if (rot == Rot4.West)
            {
                texPath = extension.flightRigTexPathWest.NullOrEmpty()
                    ? "Effects/FlightRig/ABY_FlightRig_West"
                    : extension.flightRigTexPathWest;
            }
            else
            {
                texPath = extension.flightRigTexPathSouth;
            }

            return !texPath.NullOrEmpty();
        }

        private static Vector3 FlightRigOffset(ABY_HoverArmorExtension extension, Rot4 rot)
        {
            if (extension == null)
            {
                return Vector3.zero;
            }

            if (rot == Rot4.North)
            {
                return new Vector3(extension.flightRigOffsetNorthX, 0f, extension.flightRigOffsetNorthZ);
            }

            if (rot == Rot4.East)
            {
                return new Vector3(extension.flightRigOffsetEastX, 0f, extension.flightRigOffsetEastZ);
            }

            if (rot == Rot4.West)
            {
                return new Vector3(-extension.flightRigOffsetEastX, 0f, extension.flightRigOffsetEastZ);
            }

            return new Vector3(extension.flightRigOffsetSouthX, 0f, extension.flightRigOffsetSouthZ);
        }

        private static Vector3 BackTrailOffset(Rot4 rot)
        {
            if (rot == Rot4.North)
            {
                return new Vector3(0f, 0f, -0.25f);
            }

            if (rot == Rot4.South)
            {
                return new Vector3(0f, 0f, 0.25f);
            }

            if (rot == Rot4.East)
            {
                return new Vector3(-0.25f, 0f, 0f);
            }

            if (rot == Rot4.West)
            {
                return new Vector3(0.25f, 0f, 0f);
            }

            return Vector3.zero;
        }

        private static void DrawPlane(string texPath, Vector3 loc, float width, float depth, float alpha, float angle)
        {
            if (texPath.NullOrEmpty() || alpha <= 0.01f || Mathf.Abs(width) <= 0.01f || Mathf.Abs(depth) <= 0.01f)
            {
                return;
            }

            try
            {
                Color color = new Color(1f, 1f, 1f, QuantizeAlpha(alpha));
                Material material = MaterialPool.MatFrom(texPath, ShaderDatabase.TransparentPostLight, color);
                Matrix4x4 matrix = Matrix4x4.identity;
                matrix.SetTRS(loc, Quaternion.AngleAxis(angle, Vector3.up), new Vector3(width, 1f, depth));
                Graphics.DrawMesh(MeshPool.plane10, matrix, material, 0);
            }
            catch (Exception ex)
            {
                ABY_LogThrottleUtility.Warning("hoverArmorFxDraw:" + texPath, "[Abyssal Protocol] Failed to draw hover armor FX texture '" + texPath + "': " + ex.Message, 600);
            }
        }

        private static float QuantizeAlpha(float alpha)
        {
            return Mathf.Round(Mathf.Clamp01(alpha) * 16f) / 16f;
        }

        private struct HoverSpark
        {
            public string TexPath;
            public Vector3 Position;
            public int SpawnTick;
            public int LifetimeTicks;
            public float Scale;
            public float Alpha;
            public float Angle;
        }
    }
}
