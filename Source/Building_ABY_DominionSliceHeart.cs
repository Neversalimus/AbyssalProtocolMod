using RimWorld;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    [StaticConstructorOnStartup]
    public class Building_ABY_DominionSliceHeart : Building
    {
        private const string HeartPlatformUnderlayPath = "Things/Building/DominionSlice/Platforms/ABY_DominionHeart_PlatformUnderlay";
        private static Graphic heartPlatformUnderlayGraphic;

        private int nextPulseTick = -1;
        private int lastBlockedMessageTick = -999999;

        public override AcceptanceReport ClaimableBy(Faction by)
        {
            return false;
        }

        public override AcceptanceReport DeconstructibleBy(Faction faction)
        {
            return false;
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            ABY_DominionTargetUtility.MakeDominionAnchorHostile(this);
            if (!respawningAfterLoad && Find.TickManager != null)
            {
                nextPulseTick = Find.TickManager.TicksGame + Rand.RangeInclusive(90, 180);
            }

            MapComponent_DominionSliceEncounter encounter = map != null ? map.GetComponent<MapComponent_DominionSliceEncounter>() : null;
            if (encounter != null)
            {
                encounter.RegisterHeart(this);
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref nextPulseTick, "nextPulseTick", -1);
            Scribe_Values.Look(ref lastBlockedMessageTick, "lastBlockedMessageTick", -999999);
        }

        protected override void Tick()
        {
            base.Tick();
            if (Destroyed || Map == null || Find.TickManager == null)
            {
                return;
            }

            MapComponent_DominionSliceEncounter encounter = Map.GetComponent<MapComponent_DominionSliceEncounter>();
            if (encounter == null || !encounter.IsActiveEncounter)
            {
                return;
            }

            int now = Find.TickManager.TicksGame;
            if (nextPulseTick < 0)
            {
                nextPulseTick = now + 150;
                return;
            }

            if (now < nextPulseTick)
            {
                return;
            }

            nextPulseTick = now + (encounter.IsHeartExposed ? 180 : 240);
            FleckMaker.ThrowLightningGlow(DrawPos, Map, encounter.IsHeartExposed ? 2.1f : 1.5f);
            DominionSliceHeartSetpieceVfxUtility.SpawnHeartbeatPulse(DrawPos, Map, encounter.IsHeartExposed);
            if (encounter.IsHeartExposed)
            {
                encounter.EmitHeartPulse(this);
            }
        }

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            DrawHeartPlatformUnderlay(drawLoc);
            base.DrawAt(drawLoc, flip);
            MapComponent_DominionSliceEncounter encounter = Map != null ? Map.GetComponent<MapComponent_DominionSliceEncounter>() : null;
            if (encounter != null && encounter.ShouldDrawHeartShield)
            {
                DominionSliceVfxUtility.DrawHeartShield(drawLoc, Map, encounter.LiveAnchorCount, thingIDNumber);
            }

            // Always draw the compact machine-heart core above the lower platform.
            // This keeps the heart readable even before the encounter starts and avoids
            // the platform visually swallowing the interactable object.
            DominionSliceHeartSetpieceVfxUtility.DrawHeartSetpiece(drawLoc, Map, encounter, thingIDNumber);
        }

        private static void DrawHeartPlatformUnderlay(Vector3 drawLoc)
        {
            if (heartPlatformUnderlayGraphic == null)
            {
                heartPlatformUnderlayGraphic = GraphicDatabase.Get<Graphic_Single>(HeartPlatformUnderlayPath, ShaderDatabase.Transparent, Vector2.one, Color.white);
            }

            if (heartPlatformUnderlayGraphic == null || heartPlatformUnderlayGraphic.MatSingle == null)
            {
                return;
            }

            Vector3 loc = drawLoc;
            loc.y = AltitudeLayer.FloorEmplacement.AltitudeFor() + 0.018f;
            Matrix4x4 matrix = Matrix4x4.TRS(loc, Quaternion.identity, new Vector3(10.2f, 1f, 10.2f));
            Graphics.DrawMesh(MeshPool.plane10, matrix, heartPlatformUnderlayGraphic.MatSingle, 0);
        }

        public void NotifyShieldBlocked()
        {
            int now = Find.TickManager != null ? Find.TickManager.TicksGame : 0;
            if (now - lastBlockedMessageTick > 120)
            {
                lastBlockedMessageTick = now;
                if (Map != null)
                {
                    Messages.Message("ABY_DominionSliceHeart_Shielded".Translate(), new TargetInfo(PositionHeld, Map), MessageTypeDefOf.RejectInput, false);
                }
            }

            if (Map != null)
            {
                FleckMaker.ThrowLightningGlow(DrawPos, Map, 1.3f);
                DominionSliceVfxUtility.SpawnHeartShieldBlockFlare(DrawPos, Map);
            }
        }

        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            if (Map != null)
            {
                MapComponent_DominionSliceEncounter encounter = Map.GetComponent<MapComponent_DominionSliceEncounter>();
                if (encounter != null)
                {
                    encounter.NotifyHeartDestroyed(this);
                }
            }

            base.Destroy(mode);
        }

        public override string GetInspectString()
        {
            string baseText = base.GetInspectString();
            string stateText = "ABY_DominionSliceHeart_InspectShielded".Translate();
            MapComponent_DominionSliceEncounter encounter = Map != null ? Map.GetComponent<MapComponent_DominionSliceEncounter>() : null;
            if (encounter != null && encounter.IsHeartExposed)
            {
                stateText = "ABY_DominionSliceHeart_InspectExposed".Translate(encounter.GetCollapseEta());
            }

            if (baseText.NullOrEmpty())
            {
                return stateText;
            }

            return baseText.TrimEnd() + "\n" + stateText;
        }
    }
}
