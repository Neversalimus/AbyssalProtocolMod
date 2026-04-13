using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    public class CompProperties_ArchonHeadFlame : CompProperties
    {
        public string southTexPath = "Effects/ArchonHeadFlame/ArchonHeadFlame_south";
        public string northTexPath = "Effects/ArchonHeadFlame/ArchonHeadFlame_north";
        public string eastTexPath = "Effects/ArchonHeadFlame/ArchonHeadFlame_east";

        public int frameCount = 4;
        public int ticksPerFrame = 5;

        public float southWidth = 3.65f;
        public float southHeight = 4.35f;
        public float northWidth = 3.45f;
        public float northHeight = 4.15f;
        public float eastWidth = 3.05f;
        public float eastHeight = 3.85f;

        public float southOffsetX = 0f;
        public float southOffsetZ = 2.38f;

        public float northOffsetX = 0f;
        public float northOffsetZ = 2.28f;

        public float eastOffsetX = 0.30f;
        public float eastOffsetZ = 2.30f;

        public int emberIntervalMinTicks = 22;
        public int emberIntervalMaxTicks = 48;
        public float emberScatterRadius = 0.26f;
        public float emberScaleMin = 0.28f;
        public float emberScaleMax = 0.54f;
        public string emberMoteDefName = "ABY_Mote_ArchonHeadEmber";

        public bool disableWhenDead = true;
        public bool disableWhenDowned = false;

        public CompProperties_ArchonHeadFlame()
        {
            compClass = typeof(CompArchonHeadFlame);
        }
    }

    public class CompArchonHeadFlame : ThingComp
    {
        private static readonly Dictionary<string, Material[]> MaterialCache = new Dictionary<string, Material[]>();

        private int nextEmberTick = -1;
        private ThingDef cachedEmberMoteDef;

        public CompProperties_ArchonHeadFlame Props => (CompProperties_ArchonHeadFlame)props;

        private Pawn Pawn => parent as Pawn;

        public override void Initialize(CompProperties props)
        {
            base.Initialize(props);
            if (nextEmberTick < 0)
            {
                ScheduleNextEmber();
            }
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref nextEmberTick, "nextEmberTick", -1);

            if (Scribe.mode == LoadSaveMode.PostLoadInit && nextEmberTick < 0)
            {
                ScheduleNextEmber();
            }
        }

        public override void CompTick()
        {
            base.CompTick();

            Pawn pawn = Pawn;
            if (pawn == null || pawn.MapHeld == null || !pawn.Spawned)
            {
                return;
            }

            if (!ShouldBeActive(pawn))
            {
                return;
            }

            int ticksGame = Find.TickManager?.TicksGame ?? 0;
            if (ticksGame >= nextEmberTick)
            {
                TrySpawnEmber(pawn);
                ScheduleNextEmber();
            }
        }

        public override void PostDraw()
        {
            base.PostDraw();

            Pawn pawn = Pawn;
            if (pawn == null || pawn.MapHeld == null || !pawn.Spawned)
            {
                return;
            }

            if (!ShouldBeActive(pawn))
            {
                return;
            }

            Material material = GetCurrentMaterial(pawn);
            if (material == null)
            {
                return;
            }

            Vector2 drawSize = GetDrawSize(pawn);
            Vector3 drawPos = GetOverlayDrawPos(pawn);

            float pulse = 1f;
            if (Find.TickManager != null)
            {
                float t = (Find.TickManager.TicksGame + pawn.thingIDNumber * 13) / 10f;
                pulse = 0.96f + Mathf.Sin(t * 0.15f) * 0.04f;
            }

            Matrix4x4 matrix = Matrix4x4.TRS(
                drawPos,
                Quaternion.identity,
                new Vector3(drawSize.x * pulse, 1f, drawSize.y * pulse));

            Graphics.DrawMesh(MeshPool.plane10, matrix, material, 0);
        }

        private bool ShouldBeActive(Pawn pawn)
        {
            if (Props.disableWhenDead && pawn.Dead)
            {
                return false;
            }

            if (Props.disableWhenDowned && pawn.Downed)
            {
                return false;
            }

            return true;
        }

        private void ScheduleNextEmber()
        {
            int currentTick = Find.TickManager?.TicksGame ?? 0;
            int min = Mathf.Max(1, Props.emberIntervalMinTicks);
            int max = Mathf.Max(min, Props.emberIntervalMaxTicks);
            nextEmberTick = currentTick + Rand.RangeInclusive(min, max);
        }

        private void TrySpawnEmber(Pawn pawn)
        {
            if (pawn.MapHeld == null)
            {
                return;
            }

            if (cachedEmberMoteDef == null && !string.IsNullOrEmpty(Props.emberMoteDefName))
            {
                cachedEmberMoteDef = DefDatabase<ThingDef>.GetNamedSilentFail(Props.emberMoteDefName);
            }

            if (cachedEmberMoteDef == null)
            {
                return;
            }

            Vector3 pos = GetOverlayDrawPos(pawn);
            pos.x += Rand.Range(-Props.emberScatterRadius, Props.emberScatterRadius);
            pos.z += Rand.Range(-Props.emberScatterRadius * 0.65f, Props.emberScatterRadius * 0.65f);
            pos.y = Altitudes.AltitudeFor(AltitudeLayer.MoteOverhead);

            float scale = Rand.Range(Props.emberScaleMin, Props.emberScaleMax);
            MoteMaker.MakeStaticMote(pos, pawn.MapHeld, cachedEmberMoteDef, scale);
        }

        private Vector3 GetOverlayDrawPos(Pawn pawn)
        {
            Vector3 drawPos = pawn.DrawPos;
            drawPos.y = Altitudes.AltitudeFor(AltitudeLayer.MetaOverlays);

            switch (pawn.Rotation.AsInt)
            {
                case 0: // North
                    drawPos.x += Props.northOffsetX;
                    drawPos.z += Props.northOffsetZ;
                    break;
                case 1: // East
                    drawPos.x += Props.eastOffsetX;
                    drawPos.z += Props.eastOffsetZ;
                    break;
                case 2: // South
                    drawPos.x += Props.southOffsetX;
                    drawPos.z += Props.southOffsetZ;
                    break;
                case 3: // West (mirrors east art in RimWorld)
                    drawPos.x -= Props.eastOffsetX;
                    drawPos.z += Props.eastOffsetZ;
                    break;
            }

            return drawPos;
        }

        private Vector2 GetDrawSize(Pawn pawn)
        {
            switch (pawn.Rotation.AsInt)
            {
                case 0:
                    return new Vector2(Props.northWidth, Props.northHeight);
                case 1:
                case 3:
                    return new Vector2(Props.eastWidth, Props.eastHeight);
                default:
                    return new Vector2(Props.southWidth, Props.southHeight);
            }
        }

        private Material GetCurrentMaterial(Pawn pawn)
        {
            string baseTexPath;
            switch (pawn.Rotation.AsInt)
            {
                case 0:
                    baseTexPath = Props.northTexPath;
                    break;
                case 1:
                case 3:
                    baseTexPath = Props.eastTexPath;
                    break;
                default:
                    baseTexPath = Props.southTexPath;
                    break;
            }

            Material[] materials = GetMaterialsFor(baseTexPath);
            if (materials == null || materials.Length == 0)
            {
                return null;
            }

            int ticksGame = Find.TickManager?.TicksGame ?? 0;
            int frame = Mathf.Abs((ticksGame / Mathf.Max(1, Props.ticksPerFrame)) + pawn.thingIDNumber) % materials.Length;
            return materials[frame];
        }

        private Material[] GetMaterialsFor(string baseTexPath)
        {
            if (string.IsNullOrEmpty(baseTexPath))
            {
                return null;
            }

            if (MaterialCache.TryGetValue(baseTexPath, out Material[] cached))
            {
                return cached;
            }

            int frameCount = Mathf.Max(1, Props.frameCount);
            Material[] materials = new Material[frameCount];

            for (int i = 0; i < frameCount; i++)
            {
                string texPath = baseTexPath + "_" + i;
                materials[i] = MaterialPool.MatFrom(texPath, ShaderDatabase.TransparentPostLight);
            }

            MaterialCache[baseTexPath] = materials;
            return materials;
        }
    }
}
