using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    [StaticConstructorOnStartup]
    public static class ABY_HoverArmorRenderUtility
    {
        private const float DefaultFlightRigBackAltitudeOffset = -0.034f;
        private const float RingYOffset = 0.030f;

        private static readonly Dictionary<int, int> FlightRigStartTicksByPawn = new Dictionary<int, int>();
        private static readonly Dictionary<int, int> FlightRigLastSeenTicksByPawn = new Dictionary<int, int>();
        private static int lastFlightRigCleanupTick = -1;

        public static void DrawVectorThrusterFx(Pawn pawn, Vector3 pawnDrawLoc, ABY_HoverArmorExtension extension)
        {
            if (pawn == null || extension == null || !extension.enableVectorThrusterFx)
            {
                return;
            }

            if (extension.vectorThrusterBurstTexPath.NullOrEmpty() && extension.vectorThrusterGlowTexPath.NullOrEmpty())
            {
                return;
            }

            int ticks = ABY_HoverArmorUtility.SafeTicksGame();
            int startTick = ResolveFlightRigStartTick(pawn, ticks);
            float age = Mathf.Max(0f, ticks - startTick);
            float fade = Mathf.Clamp01(age / 10f);
            float seed = Mathf.Abs((pawn.thingIDNumber * 47) % 997);
            bool moving = pawn.pather != null && pawn.pather.MovingNow;
            float pulse = 0.5f + 0.5f * Mathf.Sin((ticks + seed) * (moving ? 0.245f : 0.150f));
            float hotPulse = 0.5f + 0.5f * Mathf.Sin((ticks + seed * 0.37f) * (moving ? 0.360f : 0.205f));
            float motionAlpha = moving ? Mathf.Max(0f, extension.vectorThrusterMotionAlphaBonus) : 0f;
            float motionScale = moving ? Mathf.Max(0f, extension.vectorThrusterMotionScaleBonus) : 0f;

            Vector3 forward = FacingVector(pawn.Rotation);
            Vector3 back = -forward;
            Vector3 right = RightVector(forward);
            float angle = AngleForDirection(back);

            // Drawn from the PawnRenderer postfix, so the jets stay visible instead of being buried under
            // apparel/body layers. The offsets keep the bursts on the shoulder/hip sides rather than below
            // the feet, so the effect reads as integrated vector thrusters built into the armor.
            DrawThrusterEmitter(pawn, extension, pawnDrawLoc, back, right, angle, ticks, fade, pulse, hotPulse, motionAlpha, motionScale, extension.vectorThrusterBackOffset, extension.vectorThrusterSideOffset, extension.vectorThrusterShoulderScale, 0);
            DrawThrusterEmitter(pawn, extension, pawnDrawLoc, back, right, angle, ticks, fade, pulse, hotPulse, motionAlpha, motionScale, extension.vectorThrusterBackOffset, -extension.vectorThrusterSideOffset, extension.vectorThrusterShoulderScale, 1);
            DrawThrusterEmitter(pawn, extension, pawnDrawLoc, back, right, angle, ticks, fade, pulse, hotPulse, motionAlpha, motionScale, extension.vectorThrusterLowerBackOffset, extension.vectorThrusterLowerSideOffset, extension.vectorThrusterHipScale, 2);
            DrawThrusterEmitter(pawn, extension, pawnDrawLoc, back, right, angle, ticks, fade, pulse, hotPulse, motionAlpha, motionScale, extension.vectorThrusterLowerBackOffset, -extension.vectorThrusterLowerSideOffset, extension.vectorThrusterHipScale, 3);

            if (moving)
            {
                DrawVectorMotionWake(extension, pawnDrawLoc, back, right, angle, ticks, fade, pulse, hotPulse);
            }
        }

        public static void DrawBackFlightRigFx(Pawn pawn, Vector3 pawnDrawLoc, ABY_HoverArmorExtension extension)
        {
            if (pawn == null || extension == null || !extension.enableFlightRigFx)
            {
                return;
            }

            if (!TryGetFlightRigTexture(extension, pawn.Rotation, out string texPath))
            {
                return;
            }

            int ticks = ABY_HoverArmorUtility.SafeTicksGame();
            int startTick = ResolveFlightRigStartTick(pawn, ticks);
            float age = Mathf.Max(0f, ticks - startTick);
            float fade = Mathf.Clamp01(age / 18f);
            float seed = Mathf.Abs((pawn.thingIDNumber * 61) % 997);
            float pulse = Mathf.Sin((ticks + seed) * 0.082f);
            float energyPulse = 0.5f + 0.5f * Mathf.Sin((ticks + seed) * 0.145f);
            float bob = Mathf.Sin((ticks + seed) * 0.055f) * Mathf.Max(0f, extension.flightRigBobAmplitude) * fade;
            float scale = Mathf.Max(0.1f, extension.flightRigScale + pulse * Mathf.Max(0f, extension.flightRigPulseScale));
            float alpha = Mathf.Clamp01(extension.flightRigAlpha * fade * (1f - extension.flightRigPulseAlpha * 0.5f + energyPulse * extension.flightRigPulseAlpha));

            Vector3 loc = pawnDrawLoc + FlightRigOffset(extension, pawn.Rotation);
            loc.z += bob;
            loc.y = AltitudeLayer.Pawn.AltitudeFor() + ResolveFlightRigAltitudeOffset(extension);

            // Do not mirror by using negative mesh scale. Unity/RimWorld transparent pawn FX can be culled
            // when the plane has a negative determinant. West uses an explicit flipped texture.
            DrawPlane(texPath, loc, scale, scale, ShaderDatabase.TransparentPostLight, alpha, 0f);

            float glowAlpha = Mathf.Clamp01(extension.flightRigGlowAlpha * fade * (0.35f + energyPulse * 0.65f));
            if (glowAlpha > 0.001f)
            {
                DrawPlane(texPath, loc + new Vector3(0f, 0.004f, 0f), scale * 1.025f, scale * 1.025f, ShaderDatabase.MoteGlow, glowAlpha, 0f);
            }
        }

        public static void DrawUnderfootFx(Pawn pawn, Vector3 groundDrawLoc, ABY_HoverArmorExtension extension)
        {
            if (pawn == null || extension == null || !extension.enableUnderfootFx || extension.ringTexPath.NullOrEmpty())
            {
                return;
            }

            int ticks = ABY_HoverArmorUtility.SafeTicksGame() + pawn.thingIDNumber * 11;
            float phase = (ticks % 96) / 96f;
            float pulse = 0.5f + 0.5f * Mathf.Sin(phase * Mathf.PI * 2f);
            float movingBonus = pawn.pather != null && pawn.pather.MovingNow ? Mathf.Max(0f, extension.movingRingScaleBonus) : 0f;
            float scale = Mathf.Max(0.12f, extension.ringScale + movingBonus + (pulse - 0.5f) * Mathf.Max(0f, extension.pulseAmplitude));
            float alpha = Mathf.Clamp01(extension.ringAlpha * (0.72f + pulse * 0.42f));

            Vector3 loc = groundDrawLoc;
            loc.y = AltitudeLayer.MoteLow.AltitudeFor() + RingYOffset;
            DrawPlane(extension.ringTexPath, loc, scale, scale, ShaderDatabase.MoteGlow, alpha, 0f);

            DrawIdleSparkSet(pawn, loc, extension, ticks, phase);
            if (pawn.pather != null && pawn.pather.MovingNow)
            {
                DrawTrailSparkSet(pawn, loc, extension, ticks, phase);
            }
        }

        public static void DrawHaloFx(Pawn pawn, Vector3 pawnDrawLoc, ABY_HoverArmorExtension extension)
        {
            // Kept as a compatibility entry point for older call sites. Current vector-thruster hover mode
            // does not draw a separate overhead halo.
        }

        private static void DrawThrusterEmitter(Pawn pawn, ABY_HoverArmorExtension extension, Vector3 pawnDrawLoc, Vector3 back, Vector3 right, float angle, int ticks, float fade, float pulse, float hotPulse, float motionAlpha, float motionScale, float backOffset, float sideOffset, float scaleMult, int slot)
        {
            float slotPhase = Mathf.Sin((ticks + pawn.thingIDNumber * 13 + slot * 17) * 0.260f);
            float slotFlicker = 0.5f + 0.5f * Mathf.Sin((ticks + pawn.thingIDNumber * 5 + slot * 23) * 0.410f);
            Vector3 loc = pawnDrawLoc + back * backOffset + right * sideOffset;
            loc.z += slotPhase * 0.012f;
            loc.y = VectorThrusterLayerY(extension, slot);

            float baseScale = Mathf.Max(0.045f, extension.vectorThrusterBurstScale);
            float pulseScale = pulse * Mathf.Max(0f, extension.vectorThrusterPulseScale);
            float burstScale = (baseScale + pulseScale + motionScale) * Mathf.Max(0.10f, scaleMult);
            float burstAlpha = Mathf.Clamp01((extension.vectorThrusterAlpha + hotPulse * extension.vectorThrusterPulseAlpha + motionAlpha + slotFlicker * 0.08f) * fade);
            float glowScale = Mathf.Max(0.060f, extension.vectorThrusterGlowScale + pulseScale * 1.45f + motionScale * 1.35f) * Mathf.Max(0.10f, scaleMult);
            float glowAlpha = Mathf.Clamp01((extension.vectorThrusterGlowAlpha + pulse * extension.vectorThrusterPulseAlpha * 0.55f + motionAlpha * 0.55f) * fade);

            if (!extension.vectorThrusterGlowTexPath.NullOrEmpty())
            {
                DrawPlane(extension.vectorThrusterGlowTexPath, loc + back * 0.018f + new Vector3(0f, 0.004f, 0f), glowScale, glowScale * 1.18f, ShaderDatabase.MoteGlow, glowAlpha, angle);
            }

            if (!extension.vectorThrusterBurstTexPath.NullOrEmpty())
            {
                DrawPlane(extension.vectorThrusterBurstTexPath, loc, burstScale * 0.78f, burstScale * 1.55f, ShaderDatabase.MoteGlow, burstAlpha, angle);
            }
        }

        private static void DrawVectorMotionWake(ABY_HoverArmorExtension extension, Vector3 pawnDrawLoc, Vector3 back, Vector3 right, float angle, int ticks, float fade, float pulse, float hotPulse)
        {
            if (extension.vectorThrusterBurstTexPath.NullOrEmpty())
            {
                return;
            }

            float baseScale = Mathf.Max(0.045f, extension.vectorThrusterBurstScale);
            Vector3 baseLoc = pawnDrawLoc + back * (extension.vectorThrusterLowerBackOffset + 0.145f);
            baseLoc.y = VectorThrusterLayerY(extension, 6);

            for (int i = 0; i < 3; i++)
            {
                float side = (i - 1) * extension.vectorThrusterLowerSideOffset * 1.10f;
                float backDistance = 0.085f + i * 0.075f;
                float wobble = Mathf.Sin((ticks + i * 19) * 0.220f) * 0.036f;
                Vector3 loc = baseLoc + back * backDistance + right * (side + wobble);
                float scale = baseScale * (0.92f - i * 0.16f) + pulse * 0.040f;
                float alpha = Mathf.Clamp01((0.34f + hotPulse * 0.30f) * fade * (1f - i * 0.23f));
                DrawPlane(extension.vectorThrusterBurstTexPath, loc, scale * 0.70f, scale * 1.32f, ShaderDatabase.MoteGlow, alpha, angle);
            }
        }

        private static int ResolveFlightRigStartTick(Pawn pawn, int ticks)
        {
            int id = pawn != null ? pawn.thingIDNumber : 0;
            if (!FlightRigLastSeenTicksByPawn.TryGetValue(id, out int lastSeen) || ticks - lastSeen > 30)
            {
                FlightRigStartTicksByPawn[id] = ticks;
            }

            FlightRigLastSeenTicksByPawn[id] = ticks;
            CleanupFlightRigStateIfNeeded(ticks);
            return FlightRigStartTicksByPawn.TryGetValue(id, out int startTick) ? startTick : ticks;
        }

        private static void CleanupFlightRigStateIfNeeded(int ticks)
        {
            if (lastFlightRigCleanupTick >= 0 && ticks - lastFlightRigCleanupTick < 360)
            {
                return;
            }

            lastFlightRigCleanupTick = ticks;
            List<int> staleIds = null;
            foreach (KeyValuePair<int, int> pair in FlightRigLastSeenTicksByPawn)
            {
                if (ticks - pair.Value > 720)
                {
                    if (staleIds == null)
                    {
                        staleIds = new List<int>();
                    }
                    staleIds.Add(pair.Key);
                }
            }

            if (staleIds == null)
            {
                return;
            }

            for (int i = 0; i < staleIds.Count; i++)
            {
                FlightRigLastSeenTicksByPawn.Remove(staleIds[i]);
                FlightRigStartTicksByPawn.Remove(staleIds[i]);
            }
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

        private static float ResolveFlightRigAltitudeOffset(ABY_HoverArmorExtension extension)
        {
            if (extension == null || Mathf.Abs(extension.flightRigAltitudeOffset) < 0.0001f)
            {
                return DefaultFlightRigBackAltitudeOffset;
            }

            return extension.flightRigAltitudeOffset;
        }

        private static float ResolveVectorThrusterAltitudeOffset(ABY_HoverArmorExtension extension)
        {
            if (extension == null || Mathf.Abs(extension.vectorThrusterAltitudeOffset) < 0.0001f)
            {
                return 0.020f;
            }

            return extension.vectorThrusterAltitudeOffset;
        }

        private static float VectorThrusterLayerY(ABY_HoverArmorExtension extension, int slot)
        {
            return AltitudeLayer.MoteOverhead.AltitudeFor() + ResolveVectorThrusterAltitudeOffset(extension) + slot * 0.002f;
        }

        private static Vector3 FacingVector(Rot4 rot)
        {
            if (rot == Rot4.North)
            {
                return new Vector3(0f, 0f, 1f);
            }

            if (rot == Rot4.South)
            {
                return new Vector3(0f, 0f, -1f);
            }

            if (rot == Rot4.East)
            {
                return new Vector3(1f, 0f, 0f);
            }

            if (rot == Rot4.West)
            {
                return new Vector3(-1f, 0f, 0f);
            }

            return new Vector3(0f, 0f, -1f);
        }

        private static Vector3 RightVector(Vector3 forward)
        {
            return new Vector3(forward.z, 0f, -forward.x);
        }

        private static float AngleForDirection(Vector3 direction)
        {
            if (direction.sqrMagnitude < 0.0001f)
            {
                return 0f;
            }

            return Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
        }

        private static void DrawIdleSparkSet(Pawn pawn, Vector3 groundDrawLoc, ABY_HoverArmorExtension extension, int ticks, float phase)
        {
            if (extension.sparkTexPath.NullOrEmpty())
            {
                return;
            }

            Vector3 baseLoc = groundDrawLoc;
            baseLoc.y = AltitudeLayer.MoteOverhead.AltitudeFor() + 0.020f;

            for (int i = 0; i < 3; i++)
            {
                float angle = (phase * 360f + i * 120f + pawn.thingIDNumber * 7) * Mathf.Deg2Rad;
                float radius = 0.19f + 0.025f * Mathf.Sin((phase + i * 0.17f) * Mathf.PI * 2f);
                Vector3 offset = new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius * 0.62f);
                float scale = Mathf.Max(0.030f, extension.sparkScale * 0.46f);
                DrawPlane(extension.sparkTexPath, baseLoc + offset + new Vector3(0f, i * 0.002f, 0f), scale, scale, ShaderDatabase.MoteGlow, extension.ringAlpha * 0.38f, 0f);
            }
        }

        private static void DrawTrailSparkSet(Pawn pawn, Vector3 groundDrawLoc, ABY_HoverArmorExtension extension, int ticks, float phase)
        {
            if (extension.sparkTexPath.NullOrEmpty())
            {
                return;
            }

            Vector3 trailDir = ResolveTrailDirection(pawn);
            Vector3 right = new Vector3(trailDir.z, 0f, -trailDir.x);
            Vector3 baseLoc = groundDrawLoc - trailDir * 0.34f;
            baseLoc.y = AltitudeLayer.MoteOverhead.AltitudeFor() + 0.030f;

            for (int i = 0; i < 4; i++)
            {
                float side = (i - 1.5f) * 0.095f;
                float back = 0.075f * i;
                float wobble = Mathf.Sin((phase + i * 0.31f) * Mathf.PI * 2f) * 0.040f;
                Vector3 loc = baseLoc - trailDir * back + right * (side + wobble);
                float scale = Mathf.Max(0.030f, extension.sparkScale * (0.74f - i * 0.10f));
                DrawPlane(extension.sparkTexPath, loc + new Vector3(0f, i * 0.002f, 0f), scale, scale, ShaderDatabase.MoteGlow, extension.ringAlpha * 0.58f, 0f);
            }
        }

        private static Vector3 ResolveTrailDirection(Pawn pawn)
        {
            if (pawn?.pather != null && pawn.pather.MovingNow)
            {
                IntVec3 next = pawn.pather.nextCell;
                IntVec3 cur = pawn.Position;
                Vector3 dir = new Vector3(next.x - cur.x, 0f, next.z - cur.z);
                if (dir.sqrMagnitude > 0.001f)
                {
                    return dir.normalized;
                }
            }

            if (pawn != null)
            {
                if (pawn.Rotation == Rot4.North) return new Vector3(0f, 0f, 1f);
                if (pawn.Rotation == Rot4.South) return new Vector3(0f, 0f, -1f);
                if (pawn.Rotation == Rot4.East) return new Vector3(1f, 0f, 0f);
                if (pawn.Rotation == Rot4.West) return new Vector3(-1f, 0f, 0f);
            }

            return new Vector3(0f, 0f, -1f);
        }

        private static void DrawPlane(string texPath, Vector3 loc, float width, float depth, Shader shader, float alpha, float angle)
        {
            if (texPath.NullOrEmpty() || shader == null || alpha <= 0.001f || Mathf.Abs(width) <= 0.001f || Mathf.Abs(depth) <= 0.001f)
            {
                return;
            }

            try
            {
                Material material = MaterialPool.MatFrom(texPath, shader, Color.white);
                MaterialPropertyBlock block = Alpha(alpha);
                Matrix4x4 matrix = Matrix4x4.identity;
                matrix.SetTRS(loc, Quaternion.AngleAxis(angle, Vector3.up), new Vector3(width, 1f, depth));
                Graphics.DrawMesh(MeshPool.plane10, matrix, material, 0, null, 0, block);
            }
            catch (Exception ex)
            {
                ABY_LogThrottleUtility.Warning("hoverArmorFxDraw:" + texPath, "[Abyssal Protocol] Failed to draw hover armor FX texture '" + texPath + "': " + ex.Message, 600);
            }
        }

        private static MaterialPropertyBlock Alpha(float alpha)
        {
            MaterialPropertyBlock block = new MaterialPropertyBlock();
            block.SetColor("_Color", new Color(1f, 1f, 1f, Mathf.Clamp01(alpha)));
            return block;
        }
    }
}
