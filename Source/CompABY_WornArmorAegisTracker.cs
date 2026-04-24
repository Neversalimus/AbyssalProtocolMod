using System.Collections.Generic;
using Verse;

namespace AbyssalProtocol
{
    /// <summary>
    /// Legacy no-op placeholder.
    ///
    /// The first armor-aegis prototype attached a tracker comp to humanlike pawn
    /// race defs. That does not reliably cover existing pawns in active saves and
    /// is not the correct RimWorld shield interception path. The working system is
    /// now implemented directly on the armor via Apparel_ABY_ArmorAegis and
    /// Apparel.CheckPreAbsorbDamage.
    ///
    /// This class remains intentionally harmless so saves or defs that still refer
    /// to it do not break.
    /// </summary>
    public class CompABY_WornArmorAegisTracker : ThingComp
    {
        public override void PostPreApplyDamage(ref DamageInfo dinfo, out bool absorbed)
        {
            absorbed = false;
        }

        public override string CompInspectStringExtra()
        {
            return null;
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            yield break;
        }
    }
}
