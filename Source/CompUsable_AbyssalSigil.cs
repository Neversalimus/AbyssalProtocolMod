using System.Collections.Generic;
using RimWorld;
using Verse;

namespace AbyssalProtocol
{
    public class CompUsable_AbyssalSigil : CompUsable
    {
        public override IEnumerable<FloatMenuOption> CompFloatMenuOptions(Pawn selPawn)
        {
            ABY_SigilUseValidator.SigilUseContext context;
            string failReason;
            if (!ABY_SigilUseValidator.TryBuildContext(selPawn, parent, null, true, out context, out failReason))
            {
                yield return new FloatMenuOption(failReason.NullOrEmpty() ? "Cannot invoke this sigil." : failReason, null);
                yield break;
            }

            IEnumerable<FloatMenuOption> baseOptions = base.CompFloatMenuOptions(selPawn);
            if (baseOptions == null)
            {
                yield break;
            }

            foreach (FloatMenuOption option in baseOptions)
            {
                yield return option;
            }
        }
    }
}
