using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    [StaticConstructorOnStartup]
    public class Building_AbyssalDominionAnchor : Building
    {
        private static readonly Dictionary<string, Graphic> GlowGraphics = new Dictionary<string, Graphic>();

        private int nextPulseTick = -1;

        private DefModExtension_DominionAnchor AnchorExtension => def?.GetModExtension<DefModExtension_DominionAnchor>();

        public DominionAnchorRole AnchorRole => AnchorExtension?.role ?? DominionAnchorRole.Suppression;

        public float PulseRadius => AnchorExtension?.pulseRadius ?? 12f;

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);

            if (!respawningAfterLoad && Find.TickManager != null)
            {
                nextPulseTick = Find.TickManager.TicksGame + Rand.RangeInclusive(45, 150);
            }

            map?.GetComponent<MapComponent_DominionCrisis>()?.RegisterAnchor(this);
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

            MapComponent_DominionCrisis crisis = Map.GetComponent<MapComponent_DominionCrisis>();
            if (crisis == null || !crisis.IsAnchorPhaseActive || !crisis.IsRegisteredAnchor(this))
            {
                return;
            }

            int now = Find.TickManager.TicksGame;
            if (nextPulseTick < 0)
            {
                nextPulseTick = now + Rand.RangeInclusive(60, 150);
                return;
            }

            if (now < nextPulseTick)
            {
                return;
            }

            nextPulseTick = now + Mathf.Max(90, AnchorExtension?.pulseIntervalTicks ?? 240);
            ExecutePulse(crisis);
        }

        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            Map?.GetComponent<MapComponent_DominionCrisis>()?.NotifyAnchorDestroyed(this);
            base.Destroy(mode);
        }

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            base.DrawAt(drawLoc, flip);

            Graphic glowGraphic = GetGlowGraphic(def);
            if (glowGraphic == null)
            {
                return;
            }

            int ticks = Find.TickManager != null ? Find.TickManager.TicksGame : 0;
            float pulse = 1f + Mathf.Sin(ticks * 0.055f + thingIDNumber * 0.17f) * 0.045f;
            DrawLayer(glowGraphic, drawLoc, new Vector2((AnchorExtension?.glowDrawScale ?? 2.4f) * pulse, (AnchorExtension?.glowDrawScale ?? 2.4f) * pulse), 0f, 0.028f);
        }

        public override string GetInspectString()
        {
            string baseText = base.GetInspectString();
            string roleLabel = GetRoleLabel();
            string effectText = GetEffectSummary();
            string crisisText = Map?.GetComponent<MapComponent_DominionCrisis>()?.GetAnchorStatusShort() ?? string.Empty;

            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            if (!baseText.NullOrEmpty())
            {
                sb.Append(baseText.TrimEnd());
                sb.AppendLine();
            }

            sb.Append("ABY_DominionAnchor_Inspect".Translate(roleLabel, effectText, crisisText));
            return sb.ToString().TrimEnd('\n', '\r');
        }

        public string GetRoleLabel()
        {
            switch (AnchorRole)
            {
                case DominionAnchorRole.Drain:
                    return "ABY_DominionAnchor_Role_Drain".Translate();
                case DominionAnchorRole.Ward:
                    return "ABY_DominionAnchor_Role_Ward".Translate();
                case DominionAnchorRole.Breach:
                    return "ABY_DominionAnchor_Role_Breach".Translate();
                default:
                    return "ABY_DominionAnchor_Role_Suppression".Translate();
            }
        }

        public string GetEffectSummary()
        {
            switch (AnchorRole)
            {
                case DominionAnchorRole.Drain:
                    return "ABY_DominionAnchor_Effect_Drain".Translate();
                case DominionAnchorRole.Ward:
                    return "ABY_DominionAnchor_Effect_Ward".Translate();
                case DominionAnchorRole.Breach:
                    return "ABY_DominionAnchor_Effect_Breach".Translate();
                default:
                    return "ABY_DominionAnchor_Effect_Suppression".Translate();
            }
        }

        private void ExecutePulse(MapComponent_DominionCrisis crisis)
        {
            switch (AnchorRole)
            {
                case DominionAnchorRole.Drain:
                    ExecuteDrainPulse(crisis);
                    break;
                case DominionAnchorRole.Ward:
                    ExecuteWardPulse(crisis);
                    break;
                case DominionAnchorRole.Breach:
                    ExecuteBreachPulse(crisis);
                    break;
                default:
                    ExecuteSuppressionPulse(crisis);
                    break;
            }
        }

        private void ExecuteSuppressionPulse(MapComponent_DominionCrisis crisis)
        {
            int affected = 0;
            foreach (Thing thing in GetNearbyDistinctThings(PulseRadius))
            {
                if (!(thing is Building_Turret turret) || turret.Destroyed || turret.Faction != Faction.OfPlayer)
                {
                    continue;
                }

                turret.TakeDamage(new DamageInfo(DamageDefOf.EMP, AnchorExtension?.empDamage ?? 3f, 0f, -1f, this, null, null));
                affected++;
                if (affected >= Mathf.Max(1, AnchorExtension?.maxAffectedTargets ?? 4))
                {
                    break;
                }
            }

            if (affected > 0)
            {
                bool lowFx = AbyssalDominionBalanceUtility.ShouldUseLowFxMode(Map, crisis);
                FleckMaker.ThrowLightningGlow(DrawPos, Map, lowFx ? 1.2f : 1.8f);
                if (!lowFx || this.IsHashIntervalTick(180))
                {
                    ABY_SoundUtility.PlayAt("ABY_SigilChargePulse", Position, Map);
                }
            }
        }

        private void ExecuteDrainPulse(MapComponent_DominionCrisis crisis)
        {
            int affected = 0;
            foreach (Thing thing in GetNearbyDistinctThings(PulseRadius))
            {
                if (!(thing is Building building) || building.Destroyed)
                {
                    continue;
                }

                bool isPlayerPowerTarget = building.Faction == Faction.OfPlayer
                    && (building.GetComp<CompPowerBattery>() != null || building.GetComp<CompPowerTrader>() != null);

                if (!isPlayerPowerTarget)
                {
                    continue;
                }

                building.TakeDamage(new DamageInfo(DamageDefOf.EMP, AnchorExtension?.empDamage ?? 2.4f, 0f, -1f, this, null, null));
                affected++;
                if (affected >= Mathf.Max(1, AnchorExtension?.maxAffectedTargets ?? 4))
                {
                    break;
                }
            }

            crisis.AddExternalContamination((AnchorExtension?.contaminationPulse ?? 0.014f) * (affected > 0 ? 1.15f : 0.75f));
            if (affected > 0)
            {
                bool lowFx = AbyssalDominionBalanceUtility.ShouldUseLowFxMode(Map, crisis);
                FleckMaker.ThrowLightningGlow(DrawPos, Map, lowFx ? 1.15f : 1.65f);
            }
        }

        private void ExecuteWardPulse(MapComponent_DominionCrisis crisis)
        {
            int healed = 0;
            int healAmount = Mathf.Max(1, AnchorExtension?.healAmount ?? 14);
            foreach (Building_AbyssalDominionAnchor anchor in crisis.GetLiveAnchors())
            {
                if (anchor == null || anchor == this || anchor.Destroyed)
                {
                    continue;
                }

                if (anchor.PositionHeld.DistanceTo(PositionHeld) > PulseRadius + 6f)
                {
                    continue;
                }

                int oldHitPoints = anchor.HitPoints;
                anchor.HitPoints = Mathf.Min(anchor.MaxHitPoints, anchor.HitPoints + healAmount);
                if (anchor.HitPoints > oldHitPoints)
                {
                    healed++;
                }
            }

            if (healed > 0)
            {
                bool lowFx = AbyssalDominionBalanceUtility.ShouldUseLowFxMode(Map, crisis);
                FleckMaker.ThrowLightningGlow(DrawPos, Map, lowFx ? 1.25f : 1.9f);
                crisis.AddExternalContamination((AnchorExtension?.contaminationPulse ?? 0.010f) * 0.8f);
            }
        }

        private void ExecuteBreachPulse(MapComponent_DominionCrisis crisis)
        {
            crisis.AddExternalContamination(AnchorExtension?.contaminationPulse ?? 0.018f);
            crisis.AccelerateAnchorDeadline(Mathf.Max(30, AnchorExtension?.timerDrainTicks ?? 120));
            bool lowFx = AbyssalDominionBalanceUtility.ShouldUseLowFxMode(Map, crisis);
            FleckMaker.ThrowLightningGlow(DrawPos, Map, lowFx ? 1.3f : 2.05f);
            if (!lowFx)
            {
                FleckMaker.ThrowMicroSparks(DrawPos, Map);
            }
        }

        private IEnumerable<Thing> GetNearbyDistinctThings(float radius)
        {
            if (Map == null)
            {
                yield break;
            }

            HashSet<Thing> yielded = new HashSet<Thing>();
            foreach (IntVec3 cell in GenRadial.RadialCellsAround(PositionHeld, radius, true))
            {
                if (!cell.InBounds(Map))
                {
                    continue;
                }

                List<Thing> things = cell.GetThingList(Map);
                for (int i = 0; i < things.Count; i++)
                {
                    Thing thing = things[i];
                    if (thing != null && yielded.Add(thing))
                    {
                        yield return thing;
                    }
                }
            }
        }

        private static Graphic GetGlowGraphic(ThingDef thingDef)
        {
            if (thingDef == null)
            {
                return null;
            }

            DefModExtension_DominionAnchor ext = thingDef.GetModExtension<DefModExtension_DominionAnchor>();
            if (ext == null || ext.glowTexPath.NullOrEmpty())
            {
                return null;
            }

            if (!GlowGraphics.TryGetValue(ext.glowTexPath, out Graphic graphic))
            {
                graphic = GraphicDatabase.Get<Graphic_Single>(ext.glowTexPath, ShaderDatabase.TransparentPostLight, Vector2.one, Color.white);
                GlowGraphics[ext.glowTexPath] = graphic;
            }

            return graphic;
        }

        private static void DrawLayer(Graphic graphic, Vector3 center, Vector2 drawSize, float angle, float yOffset)
        {
            if (graphic == null)
            {
                return;
            }

            Vector3 drawPos = center;
            drawPos.y = AltitudeLayer.BuildingOnTop.AltitudeFor() + yOffset;

            Matrix4x4 matrix = default;
            matrix.SetTRS(
                drawPos,
                Quaternion.AngleAxis(angle, Vector3.up),
                new Vector3(drawSize.x, 1f, drawSize.y));

            Graphics.DrawMesh(MeshPool.plane10, matrix, graphic.MatSingle, 0);
        }
    }
}
