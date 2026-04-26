using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    public class CompProperties_ABY_DominionGateSafePocket : CompProperties
    {
        public CompProperties_ABY_DominionGateSafePocket()
        {
            compClass = typeof(CompABY_DominionGateSafePocket);
        }
    }

    [StaticConstructorOnStartup]
    public class CompABY_DominionGateSafePocket : ThingComp
    {
        private static readonly Texture2D JumpCommandIcon = ContentFinder<Texture2D>.Get("Things/Building/DominionGate/ABY_DominionGate_Core_Frame0", false);
        private static readonly Texture2D ExtractCommandIcon = ContentFinder<Texture2D>.Get("Things/Building/DominionGate/ABY_DominionGate_Ring_Frame0", false);

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            Building_AbyssalDominionGate gate = parent as Building_AbyssalDominionGate;
            if (gate == null || gate.Destroyed || gate.Map == null)
            {
                yield break;
            }

            MapComponent_DominionCrisis crisis = gate.Map.GetComponent<MapComponent_DominionCrisis>();
            if (crisis == null)
            {
                yield break;
            }

            if (crisis.HasActivePocketSession())
            {
                yield return new Command_Action
                {
                    defaultLabel = TranslateOrFallback("ABY_DominionPocketCommand_SafeExtract", "Extract strike team (safe)"),
                    defaultDesc = TranslateOrFallback("ABY_DominionPocketCommand_SafeExtractDesc", "Returns all player pawns from the dominion slice with per-pawn exception isolation. Use this in heavy modpacks instead of the legacy extraction command."),
                    icon = ExtractCommandIcon,
                    action = delegate
                    {
                        if (!AbyssalDominionPocketSafeUtility.TryReturnPocketStrikeTeamFromGate(gate, out string failReason) && !failReason.NullOrEmpty())
                        {
                            Messages.Message(failReason, MessageTypeDefOf.RejectInput, false);
                        }
                    }
                };
                yield break;
            }

            if (crisis.IsGateEntryReady())
            {
                Command_Action enterCommand = new Command_Action
                {
                    defaultLabel = TranslateOrFallback("ABY_DominionPocketCommand_SafeEnter", "Enter dominion slice (safe)"),
                    defaultDesc = TranslateOrFallback("ABY_DominionPocketCommand_SafeEnterDesc", "Creates a sterile dominion pocket map and transfers the entire drafted strike team with per-pawn exception isolation for large modpacks."),
                    icon = JumpCommandIcon,
                    action = delegate
                    {
                        if (!AbyssalDominionPocketSafeUtility.TryOpenPocketSliceFromGate(gate, out string failReason) && !failReason.NullOrEmpty())
                        {
                            Messages.Message(failReason, MessageTypeDefOf.RejectInput, false);
                        }
                    }
                };

                if (AbyssalDominionPocketUtility.GetSelectedColonistsForPocketEntry(gate.Map).Count <= 0)
                {
                    enterCommand.Disable("ABY_DominionPocketFlowFail_NoStrikeTeam".Translate());
                }

                yield return enterCommand;
            }
        }

        private static string TranslateOrFallback(string key, string fallback)
        {
            string translated = key.Translate();
            return translated == key ? fallback : translated;
        }
    }
}
