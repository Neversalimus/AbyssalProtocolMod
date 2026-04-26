using System.Collections.Generic;
using RimWorld;
using Verse;

namespace AbyssalProtocol
{
    /// <summary>
    /// Drop-in gate class for large modpack playtests.
    /// It preserves the original dominion gate behavior but hides the old unsafe pocket enter/extract gizmos.
    /// The replacement safe gizmos are supplied by CompABY_DominionGateSafePocket so the same flow also works
    /// on already-spawned gates if RimWorld attaches the newly-added comp on load.
    /// </summary>
    public class Building_AbyssalDominionGate_Safe : Building_AbyssalDominionGate
    {
        public override IEnumerable<Gizmo> GetGizmos()
        {
            string unsafeEnterLabel = "ABY_DominionPocketCommand_Enter".Translate();
            string unsafeExtractLabel = "ABY_DominionPocketCommand_Extract".Translate();

            foreach (Gizmo gizmo in base.GetGizmos())
            {
                if (gizmo is Command_Action command)
                {
                    if (command.defaultLabel == unsafeEnterLabel || command.defaultLabel == unsafeExtractLabel)
                    {
                        continue;
                    }
                }

                yield return gizmo;
            }
        }
    }
}
