using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    public class MapComponent_AbyssalCircleInstability : MapComponent
    {
        private float residualContamination;
        private int nextDecayTick;
        private int ventCooldownUntilTick;

        public MapComponent_AbyssalCircleInstability(Map map) : base(map)
        {
        }

        public float ResidualContamination => residualContamination;
        public int TicksUntilVentReady
        {
            get
            {
                int ticksGame = Find.TickManager != null ? Find.TickManager.TicksGame : 0;
                return Mathf.Max(0, ventCooldownUntilTick - ticksGame);
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref residualContamination, "residualContamination", 0f);
            Scribe_Values.Look(ref nextDecayTick, "nextDecayTick", 0);
            Scribe_Values.Look(ref ventCooldownUntilTick, "ventCooldownUntilTick", 0);
        }

        public override void MapComponentTick()
        {
            base.MapComponentTick();
            if (Find.TickManager == null)
            {
                return;
            }

            int ticksGame = Find.TickManager.TicksGame;
            if (ticksGame < nextDecayTick)
            {
                return;
            }

            nextDecayTick = ticksGame + AbyssalCircleInstabilityUtility.ContaminationDecayInterval;
            if (residualContamination <= 0f)
            {
                residualContamination = 0f;
                return;
            }

            float decay = 0.010f;
            if (TicksUntilVentReady == 0)
            {
                decay += 0.004f;
            }

            residualContamination = Mathf.Max(0f, residualContamination - decay);
        }

        public void AddContamination(float amount)
        {
            if (amount <= 0f)
            {
                return;
            }

            residualContamination = Mathf.Clamp01(residualContamination + amount);
        }

        public bool CanVent(Building_AbyssalSummoningCircle circle, out string failReason)
        {
            failReason = null;

            if (circle == null || circle.Map == null || circle.Destroyed)
            {
                failReason = "ABY_CircleVentFail_NoCircle".Translate();
                return false;
            }

            if (circle.RitualActive)
            {
                failReason = "ABY_CircleVentFail_Busy".Translate();
                return false;
            }

            if (!circle.IsPoweredForRitual)
            {
                failReason = "ABY_CircleFail_NoPower".Translate();
                return false;
            }

            if (residualContamination < 0.05f)
            {
                failReason = "ABY_CircleVentFail_LowContamination".Translate();
                return false;
            }

            if (TicksUntilVentReady > 0)
            {
                failReason = "ABY_CircleVentFail_Cooldown".Translate(AbyssalSummoningConsoleUtility.FormatTicksShort(TicksUntilVentReady));
                return false;
            }

            return true;
        }

        public bool TryVent(Building_AbyssalSummoningCircle circle, out float removed, out string failReason)
        {
            removed = 0f;
            if (!CanVent(circle, out failReason))
            {
                return false;
            }

            removed = Mathf.Min(residualContamination, AbyssalCircleInstabilityUtility.GetVentRemovedContamination(circle));
            if (removed <= 0f)
            {
                failReason = "ABY_CircleVentFail_LowContamination".Translate();
                return false;
            }

            residualContamination = Mathf.Max(0f, residualContamination - removed);
            int ticksGame = Find.TickManager != null ? Find.TickManager.TicksGame : 0;
            ventCooldownUntilTick = ticksGame + AbyssalCircleInstabilityUtility.VentCooldownTicks;
            failReason = null;
            return true;
        }
    }
}
