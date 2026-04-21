using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace AbyssalProtocol
{
    public class CompProperties_ABY_BreachFeedback : CompProperties
    {
        public int movingStepIntervalTicks = 24;
        public float stepDustScale = 1.75f;
        public int impactOverlayTicks = 18;
        public float heatOverlayMaxAlpha = 0.62f;
        public string heatOverlayPath = "Things/VFX/SeamBreach/ABY_SeamBreach_Rift";
        public string sparkOverlayPath = "Things/VFX/SeamBreach/ABY_SeamBreach_Sparks";
        public string arrivalSoundDefName = "ABY_BreachBruteArrival";
        public string stepSoundDefName = "ABY_BreachBruteStep";
        public string impactSoundDefName = "ABY_BreachBruteImpact";

        public CompProperties_ABY_BreachFeedback()
        {
            compClass = typeof(CompABY_BreachFeedback);
        }
    }

    public class CompABY_BreachFeedback : ThingComp
    {
        private int nextStepTick = -1;
        private int impactOverlayEndTick = -1;
        private Rot4 lastImpactRotation = Rot4.South;

        private CompProperties_ABY_BreachFeedback Props => (CompProperties_ABY_BreachFeedback)props;
        private Pawn PawnParent => parent as Pawn;

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref nextStepTick, "nextStepTick", -1);
            Scribe_Values.Look(ref impactOverlayEndTick, "impactOverlayEndTick", -1);
            Scribe_Values.Look(ref lastImpactRotation, "lastImpactRotation", Rot4.South);
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);

            int now = CurrentTicks;
            nextStepTick = now + Mathf.Max(8, Props.movingStepIntervalTicks / 2);

            if (!respawningAfterLoad)
            {
                TryPlaySound(Props.arrivalSoundDefName);
            }
        }

        public override void CompTick()
        {
            base.CompTick();

            Pawn pawn = PawnParent;
            if (pawn == null || pawn.Dead || pawn.Downed || !pawn.Spawned || pawn.MapHeld == null)
            {
                return;
            }

            int now = CurrentTicks;
            if (pawn.pather != null && pawn.pather.Moving && now >= nextStepTick)
            {
                nextStepTick = now + Mathf.Max(8, Props.movingStepIntervalTicks);
                TryPlaySound(Props.stepSoundDefName);
                FleckMaker.ThrowDustPuff(pawn.DrawPos, pawn.MapHeld, Mathf.Max(0.6f, Props.stepDustScale));
            }
        }

        public override void PostDraw()
        {
            base.PostDraw();

            Pawn pawn = PawnParent;
            if (pawn == null || pawn.MapHeld == null || pawn.Dead || pawn.Downed)
            {
                return;
            }

            int now = CurrentTicks;
            if (impactOverlayEndTick <= now)
            {
                return;
            }

            float remaining = Mathf.Clamp01((impactOverlayEndTick - now) / (float)Mathf.Max(1, Props.impactOverlayTicks));
            float heatAlpha = remaining * Props.heatOverlayMaxAlpha;
            DrawOverlayPlane(
                Props.heatOverlayPath,
                pawn.DrawPos + ResolveOverlayOffset(lastImpactRotation, 0.38f, 0.20f),
                0.62f,
                0.86f,
                lastImpactRotation.AsAngle,
                new Color(1f, 0.54f, 0.16f, heatAlpha));

            DrawOverlayPlane(
                Props.sparkOverlayPath,
                pawn.DrawPos + ResolveOverlayOffset(lastImpactRotation, 0.44f, 0.22f),
                0.82f,
                0.88f,
                lastImpactRotation.AsAngle,
                new Color(1f, 0.82f, 0.30f, heatAlpha * 0.55f));
        }

        public void NotifySiegeSwing(Thing struckThing)
        {
            Pawn pawn = PawnParent;
            if (pawn == null || pawn.MapHeld == null)
            {
                return;
            }

            lastImpactRotation = pawn.Rotation;
            impactOverlayEndTick = CurrentTicks + Mathf.Max(6, Props.impactOverlayTicks);
            TryPlaySound(Props.impactSoundDefName);
            FleckMaker.ThrowDustPuff(pawn.DrawPos + ResolveOverlayOffset(lastImpactRotation, 0.46f, 0.04f), pawn.MapHeld, Props.stepDustScale + 0.55f);

            if (struckThing != null && struckThing.Spawned && struckThing.MapHeld == pawn.MapHeld)
            {
                FleckMaker.ThrowDustPuff(struckThing.DrawPos, pawn.MapHeld, Props.stepDustScale + 0.85f);
            }
        }

        private void TryPlaySound(string soundDefName)
        {
            if (string.IsNullOrEmpty(soundDefName))
            {
                return;
            }

            SoundDef soundDef = DefDatabase<SoundDef>.GetNamedSilentFail(soundDefName);
            if (soundDef == null || PawnParent == null || PawnParent.MapHeld == null)
            {
                return;
            }

            soundDef.PlayOneShot(new TargetInfo(PawnParent.Position, PawnParent.MapHeld));
        }

        private static Vector3 ResolveOverlayOffset(Rot4 rot, float forward, float height)
        {
            IntVec3 facing = rot.FacingCell;
            Vector3 offset = new Vector3(facing.x * forward, 0f, facing.z * forward);
            offset.y += height;
            return offset;
        }

        private static void DrawOverlayPlane(string texPath, Vector3 loc, float scaleX, float scaleZ, float angle, Color color)
        {
            if (string.IsNullOrEmpty(texPath))
            {
                return;
            }

            Material material = MaterialPool.MatFrom(texPath, ShaderDatabase.TransparentPostLight, color);
            Matrix4x4 matrix = Matrix4x4.identity;
            matrix.SetTRS(loc, Quaternion.AngleAxis(angle, Vector3.up), new Vector3(scaleX, 1f, scaleZ));
            Graphics.DrawMesh(MeshPool.plane10, matrix, material, 0);
        }

        private int CurrentTicks => Find.TickManager != null ? Find.TickManager.TicksGame : 0;
    }
}
