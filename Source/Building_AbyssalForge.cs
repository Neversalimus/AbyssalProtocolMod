using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    [StaticConstructorOnStartup]
    public class Building_AbyssalForge : Building_WorkTable
    {
        private const string CoreTexPath = "Things/Building/ABY_AbyssalForge_CoreOverlay";
        private const string GlowTexPath = "Things/Building/ABY_AbyssalForge_GlowOverlay";

        private const float CoreAltitude = 0.031f;
        private const float GlowAltitude = 0.029f;
        private const float HoverAmplitude = 0.0105f;
        private const float HoverSpeed = 0.036f;
        private const float PulseSpeed = 0.064f;
        private const float SecondaryPulseSpeed = 0.111f;

        private static readonly Vector2 CoreSize = new Vector2(1.12f, 1.12f);
        private static readonly Vector2 GlowSize = new Vector2(1.70f, 1.32f);

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

            DrawAnimatedCore(drawLoc);
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
        }

        public override string GetInspectString()
        {
            string baseString = base.GetInspectString();
            List<string> lines = new List<string>();
            if (!string.IsNullOrWhiteSpace(baseString))
            {
                lines.Add(baseString.TrimEnd(new char[] { '\r', '\n' }));
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

                lines.Add("ABY_ForgeInspectAttunement".Translate(("ABY_AttunementTier_" + progress.GetCurrentAttunementTier(false)).Translate()));
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

        private void DrawAnimatedCore(Vector3 drawLoc)
        {
            int ticks = Find.TickManager != null ? Find.TickManager.TicksGame : 0;
            float seed = thingIDNumber * 0.01429f;
            bool powered = IsPowerActive;
            float workBoost = (BillStack != null && BillStack.Bills != null && BillStack.Bills.Count > 0) ? 0.08f : 0f;
            float powerFactor = powered ? 1f : 0.22f;

            float pulse = (Mathf.Sin(ticks * PulseSpeed + seed) + 1f) * 0.5f;
            float secondary = (Mathf.Sin(ticks * SecondaryPulseSpeed + 0.9f + seed) + 1f) * 0.5f;
            float hover = Mathf.Sin(ticks * HoverSpeed + seed) * HoverAmplitude;

            float coreScale = Mathf.Lerp(0.94f, 1.08f + workBoost, pulse) * powerFactor;
            float glowScale = Mathf.Lerp(0.92f, 1.20f + workBoost, secondary) * powerFactor;
            float rotation = Mathf.Sin(ticks * 0.012f + seed) * 3.4f;
            float inverseRotation = -rotation * 0.55f;

            Vector3 center = drawLoc;
            center.z += 0.010f;
            center.y += hover;

            DrawLayer(
                GlowTexPath,
                new Vector3(center.x, center.y + GlowAltitude, center.z),
                GlowSize * glowScale,
                inverseRotation,
                new Color(1f, 0.43f, 0.17f, Mathf.Lerp(0.18f, 0.42f, secondary) * powerFactor),
                true);

            DrawLayer(
                CoreTexPath,
                new Vector3(center.x, center.y + CoreAltitude, center.z),
                CoreSize * coreScale,
                rotation,
                new Color(1f, 0.80f, 0.62f, Mathf.Lerp(0.62f, 0.96f, pulse) * powerFactor),
                true);

            if (powered)
            {
                DrawLayer(
                    GlowTexPath,
                    new Vector3(center.x, center.y + GlowAltitude + 0.001f, center.z),
                    GlowSize * (glowScale * 0.86f),
                    rotation * 1.4f,
                    new Color(1f, 0.25f, 0.08f, Mathf.Lerp(0.10f, 0.24f, pulse)),
                    true);
            }
        }

        private static void DrawLayer(string texPath, Vector3 loc, Vector2 size, float angle, Color color, bool postLight)
        {
            if (ContentFinder<Texture2D>.Get(texPath, false) == null)
            {
                return;
            }

            Material material = MaterialPool.MatFrom(
                texPath,
                postLight ? ShaderDatabase.TransparentPostLight : ShaderDatabase.Transparent,
                color);

            Matrix4x4 matrix = Matrix4x4.identity;
            matrix.SetTRS(loc, Quaternion.AngleAxis(angle, Vector3.up), new Vector3(size.x, 1f, size.y));
            Graphics.DrawMesh(MeshPool.plane10, matrix, material, 0);
        }
    }
}
