using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace AbyssalProtocol
{
    [StaticConstructorOnStartup]
    public class Building_ABY_ProtocolNexus : Building
    {
        private const string ActiveOverlayTexPath = "Things/Building/ABY_ProtocolNexus_ActiveOverlay";
        private const string CommandIconPath = "UI/ABY/Commands/ABY_OpenProtocolNexus";
        private const float OverlayAltitude = 0.043f;
        private static readonly Vector2 OverlaySize = new Vector2(3.25f, 3.25f);
        private static readonly Texture2D CommandIcon = ContentFinder<Texture2D>.Get(CommandIconPath, false);

        private string activeDecodeProjectDefName;
        private int activeDecodeTicksRemaining;
        private int activeDecodeTicksTotal;

        public string ActiveDecodeProjectDefName => activeDecodeProjectDefName;
        public int ActiveDecodeTicksRemaining => activeDecodeTicksRemaining;
        public int ActiveDecodeTicksTotal => activeDecodeTicksTotal;
        public bool HasActiveDecode => !activeDecodeProjectDefName.NullOrEmpty() && activeDecodeTicksRemaining > 0;
        public float ActiveDecodeProgress => activeDecodeTicksTotal <= 0 ? 0f : Mathf.Clamp01(1f - (float)activeDecodeTicksRemaining / activeDecodeTicksTotal);
        public bool IsPowerActive => GetComp<CompPowerTrader>()?.PowerOn ?? true;


        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref activeDecodeProjectDefName, "activeDecodeProjectDefName");
            Scribe_Values.Look(ref activeDecodeTicksRemaining, "activeDecodeTicksRemaining", 0);
            Scribe_Values.Look(ref activeDecodeTicksTotal, "activeDecodeTicksTotal", 0);
        }

        public bool BeginDecode(ABY_ProtocolResearchDef project, bool force = false)
        {
            if (project == null)
            {
                return false;
            }

            if (!force && HasActiveDecode)
            {
                return false;
            }

            if (!force && !ABY_ProtocolResearchUtility.CanStartDecode(project, out _))
            {
                return false;
            }

            activeDecodeProjectDefName = project.defName;
            activeDecodeTicksTotal = ABY_ProtocolResearchUtility.ResolveDecodeWorkTicks(project);
            activeDecodeTicksRemaining = activeDecodeTicksTotal;
            return true;
        }

        public ABY_ProtocolResearchDef ActiveDecodeProject()
        {
            if (activeDecodeProjectDefName.NullOrEmpty())
            {
                return null;
            }

            return DefDatabase<ABY_ProtocolResearchDef>.GetNamedSilentFail(activeDecodeProjectDefName);
        }

        public void NotifyDecodeWorkTick()
        {
            if (!HasActiveDecode)
            {
                return;
            }

            activeDecodeTicksRemaining = Mathf.Max(0, activeDecodeTicksRemaining - 1);
            if (activeDecodeTicksRemaining <= 0)
            {
                CompleteActiveDecode(null);
            }
        }

        public void CompleteActiveDecode(Pawn worker)
        {
            if (activeDecodeProjectDefName.NullOrEmpty())
            {
                return;
            }

            string completed = activeDecodeProjectDefName;
            ABY_ProtocolResearchGateUtility.MarkDecoded(completed);
            activeDecodeProjectDefName = null;
            activeDecodeTicksRemaining = 0;
            activeDecodeTicksTotal = 0;

            if (Spawned)
            {
                Messages.Message("ABY_ProtocolResearch_DecodeCompleteMessage".Translate(ABY_ProtocolResearchGateUtility.GetProtocolProjectLabel(completed)), this, MessageTypeDefOf.PositiveEvent, false);
            }
        }

        public void CancelActiveDecode()
        {
            activeDecodeProjectDefName = null;
            activeDecodeTicksRemaining = 0;
            activeDecodeTicksTotal = 0;
        }

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            base.DrawAt(drawLoc, flip);

            if (!Spawned || Map == null || !IsPowerActive)
            {
                return;
            }

            DrawActiveOverlay(drawLoc);
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

            yield return new Command_Action
            {
                defaultLabel = "ABY_ProtocolResearch_OpenNexusLabel".Translate(),
                defaultDesc = "ABY_ProtocolResearch_OpenNexusDesc".Translate(),
                icon = CommandIcon,
                action = delegate
                {
                    SoundDefOf.Tick_High.PlayOneShotOnCamera(null);
                    Find.WindowStack.Add(new Window_AbyssalProtocolNexus(this));
                }
            };
        }

        public override string GetInspectString()
        {
            string baseString = base.GetInspectString();
            List<string> lines = new List<string>();
            if (!string.IsNullOrWhiteSpace(baseString))
            {
                lines.Add(baseString.TrimEnd('\r', '\n'));
            }

            lines.Add(IsPowerActive
                ? "ABY_ProtocolResearch_InspectOnline".Translate()
                : "ABY_ProtocolResearch_InspectOffline".Translate());
            if (HasActiveDecode)
            {
                lines.Add("ABY_ProtocolResearch_InspectActiveDecode".Translate(ABY_ProtocolResearchGateUtility.GetProtocolProjectLabel(activeDecodeProjectDefName), ActiveDecodeProgress.ToStringPercent()));
            }
            lines.Add("ABY_ProtocolResearch_InspectExperimental".Translate());
            return string.Join("\n", lines);
        }

        private static void DrawActiveOverlay(Vector3 drawLoc)
        {
            if (ContentFinder<Texture2D>.Get(ActiveOverlayTexPath, false) == null)
            {
                return;
            }

            int ticks = Find.TickManager?.TicksGame ?? 0;
            float pulse = 0.5f + Mathf.Sin(ticks * 0.045f) * 0.5f;
            float alpha = Mathf.Lerp(0.11f, 0.23f, pulse);
            float angle = Mathf.Sin(ticks * 0.006f) * 0.9f;
            Vector3 loc = new Vector3(drawLoc.x, drawLoc.y + OverlayAltitude, drawLoc.z);
            Color color = new Color(1f, 0.52f, 0.24f, alpha);
            Material material = MaterialPool.MatFrom(ActiveOverlayTexPath, ShaderDatabase.TransparentPostLight, color);
            Matrix4x4 matrix = Matrix4x4.identity;
            matrix.SetTRS(loc, Quaternion.AngleAxis(angle, Vector3.up), new Vector3(OverlaySize.x, 1f, OverlaySize.y));
            Graphics.DrawMesh(MeshPool.plane10, matrix, material, 0);
        }
    }
}
