using System;
using RimWorld;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    [StaticConstructorOnStartup]
    public class Building_ABY_RuptureCrownAnchor : Building
    {
        private const string DestabilizedTexPath = "Things/Building/RuptureCrownAnchor/ABY_RuptureCrownAnchor_Destabilized";
        private const string BrokenDefName = "ABY_RuptureCrownAnchor_Broken";

        private static Graphic destabilizedGraphic;
        private int sourceBossThingId = -1;
        private int nextPulseTick = -1;
        private bool brokenWreckSpawned;

        public int SourceBossThingId => sourceBossThingId;

        public void Initialize(int bossThingId, int targetHitPoints = -1)
        {
            sourceBossThingId = bossThingId;
            if (targetHitPoints > 0)
            {
                HitPoints = Mathf.Clamp(targetHitPoints, 1, MaxHitPoints);
            }
        }

        public override Graphic Graphic
        {
            get
            {
                if (ShouldUseDestabilizedGraphic())
                {
                    return ResolveDestabilizedGraphic();
                }

                return base.Graphic;
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref sourceBossThingId, "sourceBossThingId", -1);
            Scribe_Values.Look(ref nextPulseTick, "nextPulseTick", -1);
            Scribe_Values.Look(ref brokenWreckSpawned, "brokenWreckSpawned", false);
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            TrySetAbyssalFaction();
            if (!respawningAfterLoad && Find.TickManager != null)
            {
                nextPulseTick = Find.TickManager.TicksGame + Rand.RangeInclusive(60, 150);
            }
        }

        public override AcceptanceReport ClaimableBy(Faction by)
        {
            return false;
        }

        public override AcceptanceReport DeconstructibleBy(Faction faction)
        {
            return false;
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
                nextPulseTick = Find.TickManager.TicksGame + Rand.RangeInclusive(90, 180);
                return;
            }

            if (Find.TickManager.TicksGame < nextPulseTick)
            {
                return;
            }

            nextPulseTick = Find.TickManager.TicksGame + Rand.RangeInclusive(130, 240);
            FleckMaker.ThrowLightningGlow(DrawPos, Map, ShouldUseDestabilizedGraphic() ? 1.10f : 1.55f);
            if (this.IsHashIntervalTick(420))
            {
                ABY_SoundUtility.PlayAt("ABY_SigilChargePulse", PositionHeld, Map);
            }
        }

        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            Map map = Map;
            IntVec3 cell = PositionHeld;
            bool shouldSpawnBroken = !brokenWreckSpawned
                && map != null
                && cell.IsValid
                && mode != DestroyMode.Vanish;

            brokenWreckSpawned = true;
            base.Destroy(mode);

            if (shouldSpawnBroken)
            {
                TrySpawnBrokenWreck(map, cell);
            }
        }

        private bool ShouldUseDestabilizedGraphic()
        {
            if (Destroyed || MaxHitPoints <= 0)
            {
                return false;
            }

            return HitPoints > 0 && HitPoints <= Mathf.Max(1, Mathf.RoundToInt(MaxHitPoints * 0.50f));
        }

        private Graphic ResolveDestabilizedGraphic()
        {
            if (destabilizedGraphic == null)
            {
                Vector2 drawSize = def?.graphicData != null ? def.graphicData.drawSize : new Vector2(5.4f, 5.4f);
                destabilizedGraphic = GraphicDatabase.Get<Graphic_Single>(DestabilizedTexPath, ShaderDatabase.Cutout, drawSize, Color.white);
            }

            return destabilizedGraphic;
        }

        private void TrySetAbyssalFaction()
        {
            if (Faction != null)
            {
                return;
            }

            Faction abyssal = ABY_DominionTargetUtility.ResolveAbyssalFaction();
            if (abyssal == null)
            {
                return;
            }

            try
            {
                SetFaction(abyssal);
            }
            catch (Exception ex)
            {
                ABY_LogThrottleUtility.Warning(
                    "rupture-crown-anchor-faction",
                    "[Abyssal Protocol] Could not set rupture crown anchor faction: " + ex.Message,
                    5000);
            }
        }

        private static void TrySpawnBrokenWreck(Map map, IntVec3 cell)
        {
            ThingDef brokenDef = DefDatabase<ThingDef>.GetNamedSilentFail(BrokenDefName);
            if (brokenDef == null || map == null || !cell.IsValid || !cell.InBounds(map))
            {
                return;
            }

            try
            {
                Thing broken = ThingMaker.MakeThing(brokenDef);
                if (broken != null)
                {
                    GenSpawn.Spawn(broken, cell, map, Rot4.North);
                }
            }
            catch (Exception ex)
            {
                ABY_LogThrottleUtility.Warning(
                    "rupture-crown-anchor-broken-wreck",
                    "[Abyssal Protocol] Could not spawn broken rupture crown anchor wreck: " + ex.Message,
                    5000);
            }
        }
    }
}
