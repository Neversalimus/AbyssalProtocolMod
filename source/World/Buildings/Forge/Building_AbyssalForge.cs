using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

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
        private const string ConsoleCommandIconPath = "UI/ABY/Commands/ABY_OpenCommunionConsole";

        private const float ReactorAltitude = 0.036f;
        private const float GlowAltitude = 0.031f;
        private const float RuneAltitude = 0.043f;
        private const float VentAltitude = 0.0335f;
        private const float SparkAltitude = 0.0445f;

        private static readonly Vector2 ReactorSize = new Vector2(2.24f, 1.98f);
        private static readonly Vector2 GlowSize = new Vector2(5.20f, 2.34f);
        private static readonly Vector2 RuneSize = new Vector2(4.28f, 0.82f);
        private static readonly Vector2 VentSize = new Vector2(6.06f, 1.62f);
        private static readonly Vector2 SparkSize = new Vector2(2.90f, 1.16f);
        private static readonly Texture2D ConsoleCommandIcon = ContentFinder<Texture2D>.Get(ConsoleCommandIconPath, false);

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
                icon = ConsoleCommandIcon,
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

            if (Prefs.DevMode && ProgressComponent != null)
            {
                yield return new Command_Action
                {
                    defaultLabel = "ABY_ForgeDevResidueButton".Translate(),
                    defaultDesc = "ABY_ForgeDevResidueDesc".Translate(),
                    icon = ContentFinder<Texture2D>.Get("Things/Item/ABY_AbyssalResidue"),
                    action = delegate
                    {
                        ProgressComponent.DebugGrantResidue(this, 1000);
                        SoundDefOf.Tick_High.PlayOneShotOnCamera(null);
                    }
                };

                yield return new Command_Action
                {
                    defaultLabel = "ABY_ForgeDevImmortalityLabel".Translate(),
                    defaultDesc = "ABY_ForgeDevImmortalityDesc".Translate(),
                    icon = ContentFinder<Texture2D>.Get("UI/AbyssalForge/ABY_Category_Implants"),
                    action = delegate
                    {
                        OpenDevImmortalityMenu();
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

        private void OpenDevImmortalityMenu()
        {
            List<Pawn> pawns = ABY_TestImmortalityUtility.GetToggleCandidates(Map);
            if (pawns.Count == 0)
            {
                Messages.Message(
                    "ABY_ForgeDevImmortalityNoPawns".Translate(),
                    this,
                    MessageTypeDefOf.RejectInput,
                    false);
                return;
            }

            List<FloatMenuOption> options = new List<FloatMenuOption>();
            foreach (Pawn pawn in pawns)
            {
                bool immortal = ABY_TestImmortalityUtility.HasImmortality(pawn);
                string state = immortal
                    ? "ABY_TestImmortalityStateOn".Translate()
                    : "ABY_TestImmortalityStateOff".Translate();

                options.Add(new FloatMenuOption(
                    "ABY_ForgeDevImmortalityOption".Translate(pawn.LabelShortCap, state),
                    delegate
                    {
                        ABY_TestImmortalityUtility.ToggleImmortality(pawn);
                    }));
            }

            Find.WindowStack.Add(new FloatMenu(options));
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
            if (!Spawned || Map == null)
            {
                return;
            }

            bool powered = IsPowerActive;
            bool activeBills = BillStack != null && BillStack.Bills != null && BillStack.Bills.Count > 0;
            int ticks = Find.TickManager?.TicksGame ?? 0;
            float workBoost = activeBills ? 0.10f : 0f;
            float attunementBoost = Mathf.Clamp01((ProgressComponent?.GetCurrentAttunementTier(false) ?? 0) / 50f) * 0.12f;
            float powerFactor = powered ? 1f : 0.12f;
            float intensity = Mathf.Clamp01(powerFactor * (1f + workBoost + attunementBoost));

            Vector3 center = drawLoc;
            center.z += 0.014f;

            // Stable foundation glow: the whole forge should read as lit, not breathing.
            DrawLayer(
                VentGlowTexPath,
                new Vector3(center.x, center.y + VentAltitude, center.z),
                VentSize,
                0f,
                new Color(1f, 0.34f, 0.11f, 0.17f * intensity),
                true);

            DrawLayer(
                GlowTexPath,
                new Vector3(center.x, center.y + GlowAltitude, center.z),
                GlowSize,
                0f,
                new Color(1f, 0.38f, 0.13f, 0.23f * intensity),
                true);

            DrawLayer(
                ReactorTexPath,
                new Vector3(center.x, center.y + ReactorAltitude, center.z),
                ReactorSize,
                0f,
                new Color(1f, 0.80f, 0.55f, 0.76f * intensity),
                true);

            DrawLayer(
                RuneSweepTexPath,
                new Vector3(center.x, center.y + RuneAltitude, center.z),
                RuneSize,
                0f,
                new Color(1f, 0.50f, 0.16f, 0.12f * intensity),
                true);

            if (!powered)
            {
                return;
            }

            // Active motion is deliberately confined to small flow/spark accents.
            // This restores visible animation without scaling or alpha-pulsing the entire building.
            float flowA = NormalizedLoop(ticks + thingIDNumber * 17, 180);
            float flowB = NormalizedLoop(ticks + thingIDNumber * 17 + 90, 180);
            float shimmerFast = Triangle01(NormalizedLoop(ticks + thingIDNumber * 31, 44));
            float shimmerSlow = Triangle01(NormalizedLoop(ticks + thingIDNumber * 7, 96));
            float workMotionBoost = activeBills ? 1.20f : 1f;

            DrawFlowSweep(center, flowA, 0.18f * intensity * workMotionBoost);
            DrawFlowSweep(center, flowB, 0.10f * intensity * workMotionBoost);

            DrawLayer(
                VentGlowTexPath,
                new Vector3(center.x, center.y + VentAltitude + 0.0015f, center.z),
                VentSize,
                0f,
                new Color(1f, 0.28f, 0.07f, (0.035f + 0.040f * shimmerSlow) * intensity * workMotionBoost),
                true);

            DrawLayer(
                SparkTexPath,
                new Vector3(center.x, center.y + SparkAltitude, center.z),
                SparkSize,
                0f,
                new Color(1f, 0.70f, 0.42f, (0.055f + 0.050f * shimmerFast) * intensity * workMotionBoost),
                true);

            if (activeBills)
            {
                float workFlow = NormalizedLoop(ticks + thingIDNumber * 11, 120);
                DrawFlowSweep(center, workFlow, 0.14f * intensity);
            }
        }

        private static void DrawFlowSweep(Vector3 center, float flow, float alpha)
        {
            if (alpha <= 0.001f)
            {
                return;
            }

            float travel = Mathf.Lerp(-1.68f, 1.68f, flow);
            float fade = Mathf.Sin(flow * Mathf.PI);
            float clampedAlpha = alpha * Mathf.Clamp01(fade);
            if (clampedAlpha <= 0.001f)
            {
                return;
            }

            DrawLayer(
                RuneSweepTexPath,
                new Vector3(center.x + travel, center.y + RuneAltitude + 0.006f, center.z),
                new Vector2(1.42f, 0.78f),
                0f,
                new Color(1f, 0.62f, 0.22f, clampedAlpha),
                true);
        }

        private static float NormalizedLoop(int ticks, int period)
        {
            if (period <= 1)
            {
                return 0f;
            }

            int wrapped = ticks % period;
            if (wrapped < 0)
            {
                wrapped += period;
            }

            return wrapped / (float)period;
        }

        private static float Triangle01(float t)
        {
            return 1f - Mathf.Abs((Mathf.Repeat(t, 1f) * 2f) - 1f);
        }

        private static void DrawLayer(string texPath, Vector3 loc, Vector2 size, float angle, Color color, bool postLight)
        {
            if (ContentFinder<Texture2D>.Get(texPath, false) == null)
            {
                return;
            }

            Material material = ABY_MaterialCacheUtility.MatFrom(texPath, postLight ? ShaderDatabase.TransparentPostLight : ShaderDatabase.Transparent, color);
            Matrix4x4 matrix = Matrix4x4.identity;
            matrix.SetTRS(loc, Quaternion.AngleAxis(angle, Vector3.up), new Vector3(size.x, 1f, size.y));
            Graphics.DrawMesh(MeshPool.plane10, matrix, material, 0);
        }
    }
}
