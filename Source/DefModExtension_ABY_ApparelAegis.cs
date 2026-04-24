using Verse;

namespace AbyssalProtocol
{
    public class DefModExtension_ABY_ApparelAegis : DefModExtension
    {
        public string labelKey = "ABY_ApparelAegis_Label";
        public string stableKey = "ABY_ApparelAegis_StateStable";
        public string rechargingKey = "ABY_ApparelAegis_StateRecharging";
        public string collapsedKey = "ABY_ApparelAegis_StateCollapsed";
        public string suppressedKey = "ABY_ApparelAegis_StateSuppressed";
        public string gizmoSubtitleKey = "ABY_ApparelAegis_GizmoIntegrity";
        public string gizmoTheme = "Aegis";

        public float maxShieldPoints = 120f;
        public int rechargeDelayTicks = 240;
        public int rechargeIntervalTicks = 30;
        public float rechargePerInterval = 3f;

        public bool absorbRanged = true;
        public bool absorbExplosive = true;
        public bool absorbMelee = false;
        public bool empDrainsShield = true;
        public bool suppressWhenExternalShieldWorn = true;

        public float empDrainMultiplier = 2.0f;
        public float hitFlashScale = 0.9f;
        public float breakFlashScale = 1.4f;
        public float restoreFlashScale = 1.05f;
        public string hitSoundDefName = "";
        public string breakSoundDefName = "ABY_ReactorSaintImpact";
        public string restoreSoundDefName = "ABY_ReactorSaintCharge";

        public float MaxShieldPointsSafe => maxShieldPoints < 1f ? 1f : maxShieldPoints;
        public int RechargeDelayTicksSafe => rechargeDelayTicks < 0 ? 0 : rechargeDelayTicks;
        public int RechargeIntervalTicksSafe => rechargeIntervalTicks < 15 ? 15 : rechargeIntervalTicks;
        public float RechargePerIntervalSafe => rechargePerInterval < 0f ? 0f : rechargePerInterval;
    }
}
