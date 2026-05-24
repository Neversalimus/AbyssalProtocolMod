using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    [StaticConstructorOnStartup]
    public class Building_ABY_DominionSliceAnchor : Building
    {
        private const string SealPlatformUnderlayPath = "Things/Building/DominionSlice/Platforms/ABY_DominionAnchor_PlatformUnderlay_Seal";
        private const string ChoirPlatformUnderlayPath = "Things/Building/DominionSlice/Platforms/ABY_DominionAnchor_PlatformUnderlay_Choir";
        private const string LawPlatformUnderlayPath = "Things/Building/DominionSlice/Platforms/ABY_DominionAnchor_PlatformUnderlay_Law";

        private static readonly Dictionary<string, Graphic> GlowGraphics = new Dictionary<string, Graphic>();
        private static readonly Dictionary<string, Graphic> PlatformUnderlayGraphics = new Dictionary<string, Graphic>();
        private int nextPulseTick = -1;
        private MapComponent_DominionSliceEncounter cachedEncounter;
        private int nextEncounterResolveTick;

        private DefModExtension_DominionSliceAnchor SliceExtension
        {
            get { return def != null ? def.GetModExtension<DefModExtension_DominionSliceAnchor>() : null; }
        }

        public DominionSliceAnchorRole AnchorRole
        {
            get { return SliceExtension != null ? SliceExtension.role : DominionSliceAnchorRole.Seal; }
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            ABY_DominionTargetUtility.MakeDominionAnchorHostile(this);
            if (!respawningAfterLoad && Find.TickManager != null)
            {
                nextPulseTick = Find.TickManager.TicksGame + Rand.RangeInclusive(90, 210);
            }

            cachedEncounter = null;
            nextEncounterResolveTick = 0;
            MapComponent_DominionSliceEncounter encounter = ResolveEncounter();
            if (encounter != null)
            {
                encounter.RegisterAnchor(this);
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

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref nextPulseTick, "nextPulseTick", -1);
        }

        protected override void Tick()
        {
            base.Tick();
            if (Destroyed || Map == null || Find.TickManager == null)
            {
                return;
            }

            MapComponent_DominionSliceEncounter encounter = ResolveEncounter();
            if (encounter == null || !encounter.IsActiveEncounter || !encounter.IsAnchorfallActive)
            {
                return;
            }

            int now = Find.TickManager.TicksGame;
            if (nextPulseTick < 0)
            {
                nextPulseTick = now + 180;
                return;
            }

            if (now < nextPulseTick)
            {
                return;
            }

            nextPulseTick = now + Mathf.Max(120, SliceExtension != null ? SliceExtension.pulseIntervalTicks : 240);
            ExecutePulse(encounter);
        }

        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            if (Map != null)
            {
                MapComponent_DominionSliceEncounter encounter = ResolveEncounter();
                if (encounter != null)
                {
                    encounter.NotifyAnchorDestroyed(this);
                }
            }

            base.Destroy(mode);
        }

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            DrawPlatformUnderlay(drawLoc);
            base.DrawAt(drawLoc, flip);

            MapComponent_DominionSliceEncounter encounter = ResolveEncounter();
            DominionSliceAnchorIdentityVfxUtility.DrawAnchorIdentityZone(
                drawLoc,
                Map,
                AnchorRole,
                encounter != null && encounter.IsActiveEncounter,
                encounter != null && encounter.IsAnchorfallActive,
                thingIDNumber);

            if (encounter != null && encounter.ShouldDrawAnchorLinks)
            {
                Building_ABY_DominionSliceHeart linkedHeart = encounter.HeartBuilding;
                if (linkedHeart != null && !linkedHeart.Destroyed)
                {
                    DominionSliceVfxUtility.DrawAnchorLink(drawLoc, linkedHeart.DrawPos, Map, AnchorRole, thingIDNumber);
                }
            }

            string glowPath = SliceExtension != null ? SliceExtension.glowTexPath : null;
            if (glowPath.NullOrEmpty())
            {
                return;
            }

            Graphic glowGraphic;
            if (!GlowGraphics.TryGetValue(glowPath, out glowGraphic))
            {
                glowGraphic = GraphicDatabase.Get<Graphic_Single>(glowPath, ShaderDatabase.Transparent, Vector2.one, Color.white);
                GlowGraphics[glowPath] = glowGraphic;
            }

            if (glowGraphic == null)
            {
                return;
            }

            int ticks = Find.TickManager != null ? Find.TickManager.TicksGame : 0;
            float pulse = 1f + Mathf.Sin((ticks + thingIDNumber) * 0.05f) * 0.05f;
            float scale = (SliceExtension != null ? SliceExtension.glowDrawScale : 2.35f) * pulse;
            Vector3 loc = drawLoc;
            loc.y = AltitudeLayer.BuildingOnTop.AltitudeFor() + 0.027f;
            Matrix4x4 matrix = Matrix4x4.TRS(loc, Quaternion.AngleAxis(0f, Vector3.up), new Vector3(scale, 1f, scale));
            Graphics.DrawMesh(MeshPool.plane10, matrix, glowGraphic.MatSingle, 0);
        }

        public override string GetInspectString()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            string baseText = base.GetInspectString();
            if (!baseText.NullOrEmpty())
            {
                sb.Append(baseText.TrimEnd());
                sb.AppendLine();
            }

            sb.Append("ABY_DominionSliceAnchor_Inspect".Translate(GetRoleLabel()));
            sb.AppendLine();
            sb.Append(GetRoleEffectLabel());

            MapComponent_DominionSliceEncounter encounter = ResolveEncounter();
            if (encounter != null)
            {
                sb.AppendLine();
                sb.Append("ABY_DominionSliceAnchor_InspectTelemetry".Translate(encounter.LiveAnchorCount, encounter.GetNextWaveEtaValue()));
            }

            return sb.ToString().TrimEnd();
        }


        private MapComponent_DominionSliceEncounter ResolveEncounter()
        {
            return ABY_DominionSliceEncounterResolveUtility.Resolve(Map, ref cachedEncounter, ref nextEncounterResolveTick);
        }

        public string GetRoleLabel()
        {
            switch (AnchorRole)
            {
                case DominionSliceAnchorRole.Choir:
                    return "ABY_DominionSliceAnchorRole_Choir".Translate();
                case DominionSliceAnchorRole.Law:
                    return "ABY_DominionSliceAnchorRole_Law".Translate();
                default:
                    return "ABY_DominionSliceAnchorRole_Seal".Translate();
            }
        }

        private string GetRoleEffectLabel()
        {
            switch (AnchorRole)
            {
                case DominionSliceAnchorRole.Choir:
                    return "ABY_DominionSliceAnchor_ChoirEffect".Translate();
                case DominionSliceAnchorRole.Law:
                    return "ABY_DominionSliceAnchor_LawEffect".Translate();
                default:
                    return "ABY_DominionSliceAnchor_SealEffect".Translate();
            }
        }

        private void DrawPlatformUnderlay(Vector3 drawLoc)
        {
            string path;
            switch (AnchorRole)
            {
                case DominionSliceAnchorRole.Choir:
                    path = ChoirPlatformUnderlayPath;
                    break;
                case DominionSliceAnchorRole.Law:
                    path = LawPlatformUnderlayPath;
                    break;
                default:
                    path = SealPlatformUnderlayPath;
                    break;
            }

            Graphic platformGraphic;
            if (!PlatformUnderlayGraphics.TryGetValue(path, out platformGraphic))
            {
                platformGraphic = GraphicDatabase.Get<Graphic_Single>(path, ShaderDatabase.Transparent, Vector2.one, Color.white);
                PlatformUnderlayGraphics[path] = platformGraphic;
            }

            if (platformGraphic == null || platformGraphic.MatSingle == null)
            {
                return;
            }

            Vector3 loc = drawLoc;
            // The approved anchor sprites have a tall crown body, so their visual base sits
            // slightly south of the full texture center. Nudge only the platform underlay down
            // to align the machine socket with the anchor base instead of the top-heavy sprite.
            loc.z -= 0.32f;
            loc.y = AltitudeLayer.FloorEmplacement.AltitudeFor() + 0.020f;
            Matrix4x4 matrix = Matrix4x4.TRS(loc, Quaternion.identity, new Vector3(6.35f, 1f, 6.35f));
            Graphics.DrawMesh(MeshPool.plane10, matrix, platformGraphic.MatSingle, 0);
        }

        private void ExecutePulse(MapComponent_DominionSliceEncounter encounter)
        {
            if (encounter == null)
            {
                return;
            }

            bool anyPlayerNearby = false;
            List<Pawn> pawns = Map.mapPawns != null ? Map.mapPawns.FreeColonistsSpawned : null;
            if (pawns != null)
            {
                for (int i = 0; i < pawns.Count; i++)
                {
                    Pawn pawn = pawns[i];
                    if (pawn != null && pawn.Spawned && pawn.PositionHeld.DistanceTo(PositionHeld) <= 12f)
                    {
                        anyPlayerNearby = true;
                        break;
                    }
                }
            }

            switch (AnchorRole)
            {
                case DominionSliceAnchorRole.Choir:
                    encounter.AccelerateNextWave(150);
                    break;
                case DominionSliceAnchorRole.Law:
                    encounter.AddHazardPressure(1);
                    break;
                default:
                    encounter.ReinforceHeartShield(0.015f);
                    break;
            }

            if (anyPlayerNearby)
            {
                FleckMaker.ThrowLightningGlow(DrawPos, Map, 1.6f);
                DominionSliceAnchorIdentityVfxUtility.SpawnAnchorPulse(DrawPos, Map, AnchorRole);
                ABY_SoundUtility.PlayAt("ABY_SigilChargePulse", PositionHeld, Map);
            }
        }
    }
}
