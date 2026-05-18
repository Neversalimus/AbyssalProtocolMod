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
        public string gizmoTagKey = "";
        public string gizmoIconTexPath = "";

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

        // Package C presentation polish. These are intentionally optional so future
        // aegis apparel can inherit safe defaults without extra XML.
        public bool showAegisCombatText = true;
        public bool showAegisScreenPulse = true;
        public string collapseTextKey = "ABY_ApparelAegis_TextCollapse";
        public string restoreTextKey = "ABY_ApparelAegis_TextRestore";
        public int majorFeedbackCooldownTicks = 90;
        public int minorFeedbackCooldownTicks = 12;
        public float collapsePulseStrength = 0.42f;
        public float restorePulseStrength = 0.28f;

        public float MaxShieldPointsSafe => maxShieldPoints < 1f ? 1f : maxShieldPoints;
        public int RechargeDelayTicksSafe => rechargeDelayTicks < 0 ? 0 : rechargeDelayTicks;
        public int RechargeIntervalTicksSafe => rechargeIntervalTicks < 15 ? 15 : rechargeIntervalTicks;
        public float RechargePerIntervalSafe => rechargePerInterval < 0f ? 0f : rechargePerInterval;
        public int MajorFeedbackCooldownTicksSafe => majorFeedbackCooldownTicks < 15 ? 15 : majorFeedbackCooldownTicks;
        public int MinorFeedbackCooldownTicksSafe => minorFeedbackCooldownTicks < 3 ? 3 : minorFeedbackCooldownTicks;
    }
}
