using Verse;

namespace AbyssalProtocol
{
    public sealed class PlaceWorker_ABY_ModularTurretFeatureEnabled : PlaceWorker
    {
        public override AcceptanceReport AllowsPlacing(BuildableDef checkingDef, IntVec3 loc, Rot4 rot, Map map, Thing thingToIgnore = null, Thing thing = null)
        {
            if (ABY_ModularTurretUtility.Enabled)
            {
                return true;
            }

            return new AcceptanceReport(ABY_ModularTurretUtility.TranslateOrFallback("ABY_TurretDisabledMessage", "Modular turret systems are disabled in mod settings."));
        }
    }
}
