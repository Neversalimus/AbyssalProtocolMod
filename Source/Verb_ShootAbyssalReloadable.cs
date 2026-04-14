using RimWorld;
using Verse;

namespace AbyssalProtocol
{
    public class Verb_ShootAbyssalReloadable : Verb_Shoot
    {
        protected override bool TryCastShot()
        {
            CompAbyssalReloadable reloadable = EquipmentSource?.GetComp<CompAbyssalReloadable>();
            if (reloadable != null && !reloadable.CanFireNow(out string reason))
            {
                if (CasterPawn != null && CasterPawn.IsColonistPlayerControlled)
                {
                    Messages.Message(reason ?? (EquipmentSource.LabelCap + " cannot fire."), CasterPawn, MessageTypeDefOf.RejectInput, false);
                }
                return false;
            }

            bool result = base.TryCastShot();
            if (result)
            {
                reloadable?.NotifyShotFired(CasterPawn);
            }

            return result;
        }
    }
}
