using System.Text;
using RimWorld;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    [StaticConstructorOnStartup]
    public class Building_AbyssalHordeCommandGate : Building
    {
        private const string GlowTexPath = "Things/Building/Horde/ABY_HordeCommandGate_Glow";
        private const string RingTexPath = "Things/Building/Horde/ABY_HordeCommandGate_Ring";

        private int nextPulseTick = -1;
        private int reservedBursts;
        private int spentBursts;
        private int configuredHitPoints = -1;
        private float cadenceFactor = 0.84f;
        private string doctrineLabel = string.Empty;
        private int hordeBand;
        private int difficultyOrder;
        private int frontCount;
        private int pulseCount;
        private int phaseCount;
        private bool usesCommandGate;
        private bool hasSurgePhase;
        private string doctrineDefName = string.Empty;
        private bool suppressRewardOnDestroy;

        public int RemainingBursts => Mathf.Max(0, reservedBursts - spentBursts);
        public float CadenceFactor => Mathf.Clamp(cadenceFactor, 0.72f, 1f);

        public void Initialize(int reservedBursts, float cadenceFactor, int targetHitPoints, string doctrineLabel, AbyssalHordeRewardUtility.RewardSnapshot rewardSnapshot = null)
        {
            this.reservedBursts = Mathf.Max(1, reservedBursts);
            this.spentBursts = 0;
            this.cadenceFactor = Mathf.Clamp(cadenceFactor, 0.72f, 1f);
            this.configuredHitPoints = Mathf.Clamp(targetHitPoints, 1, MaxHitPoints);
            this.doctrineLabel = doctrineLabel ?? string.Empty;
            hordeBand = rewardSnapshot != null ? rewardSnapshot.Band : 0;
            difficultyOrder = rewardSnapshot != null ? rewardSnapshot.DifficultyOrder : 0;
            frontCount = rewardSnapshot != null ? rewardSnapshot.FrontCount : 0;
            pulseCount = rewardSnapshot != null ? rewardSnapshot.PulseCount : 0;
            phaseCount = rewardSnapshot != null ? rewardSnapshot.PhaseCount : 0;
            usesCommandGate = rewardSnapshot != null && rewardSnapshot.UsesCommandGate;
            hasSurgePhase = rewardSnapshot != null && rewardSnapshot.HasSurgePhase;
            doctrineDefName = rewardSnapshot != null ? rewardSnapshot.DoctrineDefName ?? string.Empty : string.Empty;

            if (configuredHitPoints > 0)
            {
                HitPoints = Mathf.Min(MaxHitPoints, configuredHitPoints);
            }
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            if (!respawningAfterLoad && Find.TickManager != null)
            {
                nextPulseTick = Find.TickManager.TicksGame + Rand.RangeInclusive(45, 120);
            }
        }

        public override AcceptanceReport ClaimableBy(Faction by) => false;
        public override AcceptanceReport DeconstructibleBy(Faction faction) => false;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref nextPulseTick, "nextPulseTick", -1);
            Scribe_Values.Look(ref reservedBursts, "reservedBursts", 1);
            Scribe_Values.Look(ref spentBursts, "spentBursts", 0);
            Scribe_Values.Look(ref configuredHitPoints, "configuredHitPoints", -1);
            Scribe_Values.Look(ref cadenceFactor, "cadenceFactor", 0.84f);
            Scribe_Values.Look(ref doctrineLabel, "doctrineLabel", string.Empty);
            Scribe_Values.Look(ref hordeBand, "hordeBand", 0);
            Scribe_Values.Look(ref difficultyOrder, "difficultyOrder", 0);
            Scribe_Values.Look(ref frontCount, "frontCount", 0);
            Scribe_Values.Look(ref pulseCount, "pulseCount", 0);
            Scribe_Values.Look(ref phaseCount, "phaseCount", 0);
            Scribe_Values.Look(ref usesCommandGate, "usesCommandGate", false);
            Scribe_Values.Look(ref hasSurgePhase, "hasSurgePhase", false);
            Scribe_Values.Look(ref doctrineDefName, "doctrineDefName", string.Empty);
            Scribe_Values.Look(ref suppressRewardOnDestroy, "suppressRewardOnDestroy", false);
        }

        protected override void Tick()
        {
            base.Tick();

            if (Destroyed || Map == null || Find.TickManager == null)
            {
                return;
            }

            if (nextPulseTick < 0)
            {
                nextPulseTick = Find.TickManager.TicksGame + Rand.RangeInclusive(60, 150);
                return;
            }

            if (Find.TickManager.TicksGame < nextPulseTick)
            {
                return;
            }

            nextPulseTick = Find.TickManager.TicksGame + Rand.RangeInclusive(120, 210);
            FleckMaker.ThrowLightningGlow(DrawPos, Map, 1.45f);
            if (this.IsHashIntervalTick(300))
            {
                ABY_SoundUtility.PlayAt("ABY_SigilChargePulse", PositionHeld, Map);
            }
        }

        public void NotifyCommandBurstSpent()
        {
            spentBursts = Mathf.Min(reservedBursts, spentBursts + 1);
        }

        public void DismissWithoutRewards()
        {
            suppressRewardOnDestroy = true;
            Destroy(DestroyMode.Vanish);
        }

        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            Map map = Map;
            IntVec3 cell = PositionHeld;
            if (map != null && cell.IsValid)
            {
                ArchonInfernalVFXUtility.DoSummonVFX(map, cell);
                ABY_SoundUtility.PlayAt("ABY_RupturePortalOpen", cell, map);
                map.GetComponent<MapComponent_AbyssalPortalWave>()?.NotifyCommandGateDestroyed(this);
                if (!suppressRewardOnDestroy)
                {
                    AbyssalHordeRewardUtility.SpawnCommandRewards(map, cell, BuildRewardSnapshot(), RemainingBursts);
                }
            }

            base.Destroy(mode);
        }

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            base.DrawAt(drawLoc, flip);
            if (!Spawned || Map == null)
            {
                return;
            }

            int ticks = Find.TickManager != null ? Find.TickManager.TicksGame : 0;
            float pulse = 1f + Mathf.Sin((ticks + thingIDNumber * 19) * 0.05f) * 0.06f;
            float ringPulse = 1f + Mathf.Sin((ticks + thingIDNumber * 23) * 0.035f) * 0.04f;
            Vector3 loc = drawLoc;
            loc.y += 0.029f;

            ResolveDoctrineColors(out Color glowColor, out Color ringColor);
            glowColor.a = 0.72f;
            ringColor.a = 0.92f;
            DrawPlane(GlowTexPath, loc, 2.18f * pulse, (ticks + thingIDNumber * 13) * 0.22f, glowColor);
            DrawPlane(RingTexPath, loc + new Vector3(0f, 0.005f, 0f), 1.88f * ringPulse, -(ticks + thingIDNumber * 17) * 0.68f, ringColor);
        }

        private void ResolveDoctrineColors(out Color glowColor, out Color ringColor)
        {
            string doctrine = doctrineDefName ?? string.Empty;
            if (string.Equals(doctrine, "ABY_Doctrine_HordeFireline", System.StringComparison.OrdinalIgnoreCase))
            {
                glowColor = new Color(0.86f, 0.18f, 0.32f, 0.72f);
                ringColor = new Color(1f, 0.48f, 0.62f, 0.92f);
                return;
            }

            if (string.Equals(doctrine, "ABY_Doctrine_HordeGrinder", System.StringComparison.OrdinalIgnoreCase))
            {
                glowColor = new Color(0.92f, 0.28f, 0.14f, 0.72f);
                ringColor = new Color(1f, 0.70f, 0.36f, 0.92f);
                return;
            }

            if (string.Equals(doctrine, "ABY_Doctrine_HordeSiege", System.StringComparison.OrdinalIgnoreCase))
            {
                glowColor = new Color(0.98f, 0.22f, 0.18f, 0.72f);
                ringColor = new Color(1f, 0.82f, 0.48f, 0.92f);
                return;
            }

            glowColor = new Color(0.92f, 0.22f, 0.20f, 0.72f);
            ringColor = new Color(1f, 0.56f, 0.30f, 0.92f);
        }

        public override string GetInspectString()
        {
            StringBuilder sb = new StringBuilder();
            string baseText = base.GetInspectString();
            if (!baseText.NullOrEmpty())
            {
                sb.AppendLine(baseText.TrimEnd());
            }

            string doctrine = doctrineLabel.NullOrEmpty()
                ? AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_HordeDoctrine_Unknown_Label", "Unshaped breach")
                : doctrineLabel;

            sb.Append(AbyssalSummoningConsoleUtility.TranslateOrFallback(
                "ABY_HordeCommandGate_Inspect",
                "Command gate node. Doctrine: {0}. Remaining command bursts: {1}. While intact it accelerates portal cadence on its front and preserves reserved reinforcements.",
                doctrine,
                RemainingBursts));
            sb.AppendLine();
            sb.Append(AbyssalSummoningConsoleUtility.TranslateOrFallback(
                "ABY_HordeCommandGate_InspectReward",
                "If destroyed it spills a command salvage bonus tied to remaining bursts."));

            return sb.ToString().TrimEnd('\n', '\r');
        }

        private AbyssalHordeRewardUtility.RewardSnapshot BuildRewardSnapshot()
        {
            return new AbyssalHordeRewardUtility.RewardSnapshot
            {
                Band = hordeBand,
                DifficultyOrder = difficultyOrder,
                FrontCount = frontCount,
                PulseCount = pulseCount,
                PhaseCount = phaseCount,
                UsesCommandGate = usesCommandGate,
                HasSurgePhase = hasSurgePhase,
                DoctrineDefName = doctrineDefName ?? string.Empty
            };
        }

        private static void DrawPlane(string texPath, Vector3 loc, float scale, float angle, Color color)
        {
            if (texPath.NullOrEmpty())
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
