using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    [StaticConstructorOnStartup]
    public class Building_AbyssalForge : Building_WorkTable
    {
        private const string ReactorTexPath = "Things/Building/ABY_AbyssalForge_CoreOverlay";
        private const string GlowTexPath = "Things/Building/ABY_AbyssalForge_GlowOverlay";
        private const string RuneSweepTexPath = "Things/Building/ABY_AbyssalForge_RuneSweepOverlay";
        private const string VentGlowTexPath = "Things/Building/ABY_AbyssalForge_VentGlowOverlay";
        private const string SparkTexPath = "Things/Building/ABY_AbyssalForge_SparkOverlay";

        private const float ReactorAltitude = 0.036f;
        private const float GlowAltitude = 0.031f;
        private const float RuneAltitude = 0.043f;
        private const float VentAltitude = 0.0335f;
        private const float SparkAltitude = 0.0445f;

        private const float HoverAmplitude = 0.012f;
        private const float HoverSpeed = 0.032f;
        private const float ReactorPulseSpeed = 0.061f;
        private const float SecondaryPulseSpeed = 0.109f;
        private const float SweepSpeed = 0.0175f;
        private const float SparkSpeed = 0.083f;

        private static readonly Vector2 ReactorSize = new Vector2(2.24f, 1.98f);
        private static readonly Vector2 GlowSize = new Vector2(5.20f, 2.34f);
        private static readonly Vector2 RuneSize = new Vector2(4.28f, 0.82f);
        private static readonly Vector2 VentSize = new Vector2(6.06f, 1.62f);
        private static readonly Vector2 SparkSize = new Vector2(2.90f, 1.16f);

        public MapComponent_AbyssalForgeProgress ProgressComponent => Map?.GetComponent<MapComponent_AbyssalForgeProgress>();
        public bool IsPowerActive => GetComp<CompPowerTrader>()?.PowerOn ?? true;

        public int OfferResidue(int requestedAmount)
        {
            return ProgressComponent?.OfferResidue(this, requestedAmount) ?? 0;
        }

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            base.DrawAt(drawLoc, flip);

            if (!Spawned || Map == null)
            {
                return;
            }

            DrawAnimatedSuperstructure(drawLoc);
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (Gizmo gizmo in base.GetGizmos())
            {
                yield return gizmo;
            }

            if (!Spawned || Map == null)
            {
                yield break;
            }

            int availableResidue = ProgressComponent?.CountAvailableResidue() ?? 0;

            yield return new Command_Action
            {
                defaultLabel = "ABY_ForgeOpenConsoleLabel".Translate(),
                defaultDesc = "ABY_ForgeOpenConsoleDesc".Translate(),
                icon = ContentFinder<Texture2D>.Get("UI/AbyssalForge/ABY_Category_Core"),
                action = delegate
                {
                    Find.WindowStack.Add(new Window_AbyssalForgeConsole(this));
                }
            };

            yield return new Command_Action
            {
                defaultLabel = "ABY_ForgeGizmoOfferLabel".Translate(),
                defaultDesc = "ABY_ForgeGizmoOfferDesc".Translate(),
                icon = ContentFinder<Texture2D>.Get("Things/Item/ABY_AbyssalResidue"),
                action = delegate
                {
                    List<FloatMenuOption> options = new List<FloatMenuOption>();
                    AddOfferOption(options, 10, availableResidue);
                    AddOfferOption(options, 50, availableResidue);
                    AddOfferOption(options, 100, availableResidue);
                    if (availableResidue > 0)
                    {
                        options.Add(new FloatMenuOption("ABY_ForgeOfferAll".Translate(availableResidue), delegate
                        {
                            OfferResidue(availableResidue);
                        }));
                    }
                    else
                    {
                        options.Add(new FloatMenuOption("ABY_ForgeOfferNoneAvailable".Translate(), null));
                    }

                    Find.WindowStack.Add(new FloatMenu(options));
                }
            };
            if (Prefs.DevMode)
            {
                yield return new Command_Action
                {
                    defaultLabel = "ABY_ForgeDevResidueLabel".Translate(),
                    defaultDesc = "ABY_ForgeDevResidueDesc".Translate(),
                    icon = ContentFinder<Texture2D>.Get("Things/Item/ABY_AbyssalResidue"),
                    action = delegate
                    {
                        ProgressComponent?.DebugAddResidue(this, 1000);
                    }
                };
            }

        }

        public override string GetInspectString()
        {
            string baseString = base.GetInspectString();
            List<string> lines = new List<string>();
            if (!string.IsNullOrWhiteSpace(baseString))
            {
                lines.Add(baseString.TrimEnd('\r', '\n'));
            }

            MapComponent_AbyssalForgeProgress progress = ProgressComponent;
            if (progress != null)
            {
                lines.Add("ABY_ForgeInspectResidue".Translate(progress.TotalResidueOffered, progress.CountAvailableResidue()));
                int nextUnlock = progress.GetNextUnlockResidue();
                if (nextUnlock > 0)
                {
                    RecipeDef nextRecipe = progress.GetNextUnlockRecipe();
                    lines.Add("ABY_ForgeInspectNextUnlock".Translate(nextUnlock, nextRecipe != null ? AbyssalForgeProgressUtility.GetRecipeDisplayLabel(nextRecipe) : "?"));
                }
                else
                {
                    lines.Add("ABY_ForgeInspectAllKnown".Translate());
                }

                lines.Add("ABY_ForgeInspectAttunement".Translate(AbyssalForgeProgressUtility.GetAttunementDisplayLabel(progress.GetCurrentAttunementTier(false))));
            }

            return string.Join("\n", lines);
        }

        private void AddOfferOption(List<FloatMenuOption> options, int requestedAmount, int availableResidue)
        {
            if (availableResidue <= 0)
            {
                return;
            }

            int amount = Mathf.Min(requestedAmount, availableResidue);
            options.Add(new FloatMenuOption("ABY_ForgeOfferAmount".Translate(amount), delegate
            {
                OfferResidue(amount);
            }));
        }

        private void DrawAnimatedSuperstructure(Vector3 drawLoc)
        {
            int ticks = Find.TickManager != null ? Find.TickManager.TicksGame : 0;
            float seed = thingIDNumber * 0.01429f;
            bool powered = IsPowerActive;
            bool activeBills = BillStack != null && BillStack.Bills != null && BillStack.Bills.Count > 0;
            float workBoost = activeBills ? 0.12f : 0f;
            float powerFactor = powered ? 1f : 0.18f;
            float attunementFactor = Mathf.Clamp01((ProgressComponent?.GetCurrentAttunementTier(false) ?? 0) / 50f);

            float reactorPulse = (Mathf.Sin(ticks * ReactorPulseSpeed + seed) + 1f) * 0.5f;
            float secondary = (Mathf.Sin(ticks * SecondaryPulseSpeed + seed + 0.8f) + 1f) * 0.5f;
            float hover = Mathf.Sin(ticks * HoverSpeed + seed) * HoverAmplitude;
            float sweep = Mathf.Sin(ticks * SweepSpeed + seed) * 0.11f;
            float sparkPulse = (Mathf.Sin(ticks * SparkSpeed + seed + 1.7f) + 1f) * 0.5f;

            float reactorScale = Mathf.Lerp(0.94f, 1.16f + workBoost + attunementFactor * 0.08f, reactorPulse) * powerFactor;
            float glowScale = Mathf.Lerp(0.94f, 1.12f + workBoost + attunementFactor * 0.06f, secondary) * powerFactor;
            float ventScale = Mathf.Lerp(0.98f, 1.06f + workBoost * 0.5f, reactorPulse) * powerFactor;
            float runeScale = Mathf.Lerp(0.98f, 1.04f + attunementFactor * 0.04f, secondary) * powerFactor;
            float sparkScale = Mathf.Lerp(0.94f, 1.06f + workBoost, sparkPulse) * powerFactor;

            Vector3 center = drawLoc;
            center.z += 0.014f;
            center.y += hover;

            DrawLayer(
                VentGlowTexPath,
                new Vector3(center.x, center.y + VentAltitude, center.z),
                VentSize * ventScale,
                Mathf.Sin(ticks * 0.011f + seed) * 0.6f,
                new Color(1f, 0.34f, 0.11f, Mathf.Lerp(0.12f, 0.26f, reactorPulse) * powerFactor),
                true);

            DrawLayer(
                GlowTexPath,
                new Vector3(center.x, center.y + GlowAltitude, center.z),
                GlowSize * glowScale,
                Mathf.Sin(ticks * 0.009f + seed) * 0.8f,
                new Color(1f, 0.38f, 0.13f, Mathf.Lerp(0.18f, 0.34f, secondary) * powerFactor),
                true);

            DrawLayer(
                ReactorTexPath,
                new Vector3(center.x, center.y + ReactorAltitude, center.z),
                ReactorSize * reactorScale,
                Mathf.Sin(ticks * 0.014f + seed) * 4.6f,
                new Color(1f, 0.83f, 0.64f, Mathf.Lerp(0.60f, 0.98f, reactorPulse) * powerFactor),
                true);

            DrawLayer(
                RuneSweepTexPath,
                new Vector3(center.x + sweep, center.y + RuneAltitude, center.z),
                RuneSize * runeScale,
                Mathf.Sin(ticks * 0.0075f + seed) * 1.2f,
                new Color(1f, 0.60f, 0.22f, Mathf.Lerp(0.12f, 0.32f, secondary) * powerFactor),
                true);

            if (powered)
            {
                DrawLayer(
                    SparkTexPath,
                    new Vector3(center.x + sweep * 0.35f, center.y + SparkAltitude, center.z),
                    SparkSize * sparkScale,
                    Mathf.Sin(ticks * 0.018f + seed) * 3.2f,
                    new Color(1f, 0.74f, 0.46f, Mathf.Lerp(0.06f, 0.20f, sparkPulse)),
                    true);

                DrawLayer(
                    GlowTexPath,
                    new Vector3(center.x, center.y + GlowAltitude + 0.002f, center.z),
                    GlowSize * (glowScale * 0.82f),
                    -Mathf.Sin(ticks * 0.013f + seed) * 1.1f,
                    new Color(1f, 0.22f, 0.07f, Mathf.Lerp(0.08f, 0.18f, reactorPulse + attunementFactor * 0.2f)),
                    true);
            }
        }

        private static void DrawLayer(string texPath, Vector3 loc, Vector2 size, float angle, Color color, bool postLight)
        {
            if (ContentFinder<Texture2D>.Get(texPath, false) == null)
            {
                return;
            }

            Material material = MaterialPool.MatFrom(texPath, postLight ? ShaderDatabase.TransparentPostLight : ShaderDatabase.Transparent, color);
            Matrix4x4 matrix = Matrix4x4.identity;
            matrix.SetTRS(loc, Quaternion.AngleAxis(angle, Vector3.up), new Vector3(size.x, 1f, size.y));
            Graphics.DrawMesh(MeshPool.plane10, matrix, material, 0);
        }
    }
}
