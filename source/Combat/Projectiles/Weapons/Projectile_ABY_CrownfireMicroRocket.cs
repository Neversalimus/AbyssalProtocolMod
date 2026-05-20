using RimWorld;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    public class Projectile_ABY_CrownfireMicroRocket : Bullet
    {
        private const int TrailIntervalTicks = 2;
        private const string MicroRocketVisualTexturePath = "Things/Projectile/ABY_CrownfireMicroRocket";

        private int ticksAlive;
        private Vector3 lastExactPosition;
        private Vector3 lastDrawDirection = Vector3.forward;
        private bool lastPositionInitialized;

        private bool visualProfileInitialized;
        private int visualProfileIndex;
        private float curveAmplitude;
        private float curveFrequency;
        private float curvePhase;
        private float bodyWidthScale = 1f;
        private float bodyLengthScale = 1f;
        private float trailScale = 1f;
        private Material cachedBodyMaterial;

        public void ConfigureCrownfireVisualProfile(int rocketIndex, int speedProfile)
        {
            visualProfileInitialized = true;
            visualProfileIndex = Mathf.Max(0, rocketIndex);
            float sign = ((rocketIndex + speedProfile) % 2 == 0) ? 1f : -1f;
            curveAmplitude = sign * Rand.Range(0.055f, 0.165f);
            curveFrequency = Rand.Range(0.31f, 0.47f);
            curvePhase = Rand.Range(0f, Mathf.PI * 2f) + rocketIndex * 0.37f;
            bodyWidthScale = Rand.Range(0.90f, 1.08f);
            bodyLengthScale = speedProfile == 2 ? Rand.Range(1.02f, 1.12f) : Rand.Range(0.92f, 1.06f);
            trailScale = speedProfile == 0 ? Rand.Range(0.88f, 1.00f) : Rand.Range(1.00f, 1.18f);
        }

        protected override void Tick()
        {
            EnsureVisualProfile();
            Vector3 previousPosition = ExactPosition;
            base.Tick();

            if (!Spawned || Map == null)
            {
                return;
            }

            ticksAlive++;
            if (!lastPositionInitialized)
            {
                lastExactPosition = previousPosition;
                lastPositionInitialized = true;
            }

            Vector3 currentPosition = ExactPosition;
            Vector3 movement = currentPosition - lastExactPosition;
            movement.y = 0f;
            if (movement.sqrMagnitude > 0.0001f)
            {
                lastDrawDirection = movement.normalized;
            }

            if (ticksAlive == 1 || ticksAlive % TrailIntervalTicks == 0)
            {
                Vector3 visualPosition = ResolveVisualPosition(currentPosition);
                CrownfireRocketChoirVfxUtility.SpawnMicroTrail(visualPosition, ResolveVisualDirection(), Map, trailScale);
            }

            lastExactPosition = currentPosition;
        }

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            EnsureVisualProfile();
            Material material = BodyMaterial;
            if (material == null)
            {
                return;
            }

            Vector3 direction = ResolveVisualDirection();
            Vector3 drawPos = ResolveVisualPosition(drawLoc);
            drawPos.y = Altitudes.AltitudeFor(AltitudeLayer.Projectile) + 0.008f;

            float angle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
            float weaveRotation = Mathf.Cos(ticksAlive * curveFrequency + curvePhase) * Mathf.Sign(curveAmplitude) * 6.5f;
            float hotPulse = 0.94f + Mathf.Abs(Mathf.Sin((ticksAlive + visualProfileIndex) * 0.42f)) * 0.10f;

            Matrix4x4 matrix = Matrix4x4.TRS(
                drawPos,
                Quaternion.AngleAxis(angle + weaveRotation, Vector3.up),
                new Vector3(0.22f * bodyWidthScale * hotPulse, 1f, 0.62f * bodyLengthScale));

            Graphics.DrawMesh(MeshPool.plane10, matrix, material, 0);
        }

        protected override void Impact(Thing hitThing, bool blockedByShield = false)
        {
            Map impactMap = Map;
            Vector3 impactPosition = ResolveVisualPosition(ExactPosition);
            Vector3 incomingDirection = ResolveVisualDirection();

            if (!ABY_ProjectileImpactSafetyUtility.TryRunBaseImpact(this, "Projectile_ABY_CrownfireMicroRocket", () => base.Impact(hitThing, blockedByShield)))
            {
                return;
            }

            if (impactMap == null)
            {
                return;
            }

            CrownfireRocketChoirVfxUtility.SpawnMicroDetonation(
                impactPosition,
                incomingDirection,
                impactMap,
                blockedByShield ? 0.92f : 1.22f);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref ticksAlive, "ticksAlive", 0);
            Scribe_Values.Look(ref lastExactPosition, "lastExactPosition");
            Scribe_Values.Look(ref lastDrawDirection, "lastDrawDirection", Vector3.forward);
            Scribe_Values.Look(ref lastPositionInitialized, "lastPositionInitialized", false);
            Scribe_Values.Look(ref visualProfileInitialized, "visualProfileInitialized", false);
            Scribe_Values.Look(ref visualProfileIndex, "visualProfileIndex", 0);
            Scribe_Values.Look(ref curveAmplitude, "curveAmplitude", 0f);
            Scribe_Values.Look(ref curveFrequency, "curveFrequency", 0.38f);
            Scribe_Values.Look(ref curvePhase, "curvePhase", 0f);
            Scribe_Values.Look(ref bodyWidthScale, "bodyWidthScale", 1f);
            Scribe_Values.Look(ref bodyLengthScale, "bodyLengthScale", 1f);
            Scribe_Values.Look(ref trailScale, "trailScale", 1f);
        }

        private void EnsureVisualProfile()
        {
            if (visualProfileInitialized)
            {
                return;
            }

            ConfigureCrownfireVisualProfile(Rand.Range(0, 8), Rand.RangeInclusive(0, 2));
        }

        private Vector3 ResolveVisualPosition(Vector3 basePosition)
        {
            Vector3 direction = ResolveVisualDirection();
            Vector3 side = new Vector3(direction.z, 0f, -direction.x);
            float warmup = Mathf.Clamp01(ticksAlive / 5f);
            float wave = Mathf.Sin(ticksAlive * curveFrequency + curvePhase);
            float forwardPulse = Mathf.Sin(ticksAlive * 0.23f + curvePhase * 0.5f) * 0.035f;
            Vector3 result = basePosition + side * (curveAmplitude * wave * warmup) + direction * forwardPulse;
            result.y = basePosition.y;
            return result;
        }

        private Vector3 ResolveVisualDirection()
        {
            Vector3 direction = lastDrawDirection;
            direction.y = 0f;
            if (direction.sqrMagnitude <= 0.0001f)
            {
                direction = Vector3.forward;
            }

            direction.Normalize();
            return direction;
        }

        private Material BodyMaterial
        {
            get
            {
                if (cachedBodyMaterial == null)
                {
                    cachedBodyMaterial = MaterialPool.MatFrom(MicroRocketVisualTexturePath, ShaderDatabase.MoteGlow);
                }

                return cachedBodyMaterial;
            }
        }
    }
}
