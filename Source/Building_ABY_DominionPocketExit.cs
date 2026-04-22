using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    [StaticConstructorOnStartup]
    public class Building_ABY_DominionPocketExit : Building
    {
        private static readonly Texture2D ExitCommandIcon = ContentFinder<Texture2D>.Get("Things/Building/DominionGate/ABY_DominionGate_Ring", true);
        private string sessionId;

        public void BindSession(string value)
        {
            sessionId = value ?? string.Empty;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref sessionId, "sessionId");
        }

        public override AcceptanceReport ClaimableBy(Faction by)
        {
            return false;
        }

        public override AcceptanceReport DeconstructibleBy(Faction faction)
        {
            return false;
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (Gizmo gizmo in base.GetGizmos())
            {
                yield return gizmo;
            }

            Command_Action returnCommand = new Command_Action
            {
                defaultLabel = "ABY_DominionPocketRuntimeCommand_Return".Translate(),
                defaultDesc = "ABY_DominionPocketRuntimeCommand_ReturnDesc".Translate(),
                icon = ExitCommandIcon,
                action = delegate
                {
                    ABY_DominionPocketRuntimeGameComponent runtime = ABY_DominionPocketRuntimeGameComponent.Get();
                    if (runtime != null && runtime.TryGetSessionById(sessionId, out ABY_DominionPocketSession session))
                    {
                        if (!AbyssalDominionPocketUtility.TryReturnPocketSlice(session, true, out string failReason) && !failReason.NullOrEmpty())
                        {
                            Messages.Message(failReason, MessageTypeDefOf.RejectInput, false);
                        }
                    }
                    else
                    {
                        Messages.Message("ABY_DominionPocketRuntimeFail_NoSession".Translate(), MessageTypeDefOf.RejectInput, false);
                    }
                }
            };

            if (ABY_DominionPocketRuntimeGameComponent.Get() is ABY_DominionPocketRuntimeGameComponent runtimeCheck
                && runtimeCheck.TryGetSessionById(sessionId, out ABY_DominionPocketSession activeSession)
                && AbyssalDominionPocketUtility.GetPocketPlayerCount(Map) <= 0)
            {
                returnCommand.Disable("ABY_DominionPocketRuntimeReturnDisabled".Translate());
            }

            yield return returnCommand;

            if (Prefs.DevMode)
            {
                yield return new Command_Action
                {
                    defaultLabel = "DEV: collapse dominion slice",
                    defaultDesc = "Destroy this temporary dominion slice immediately.",
                    icon = ExitCommandIcon,
                    action = delegate
                    {
                        ABY_DominionPocketRuntimeGameComponent runtime = ABY_DominionPocketRuntimeGameComponent.Get();
                        if (runtime != null && runtime.TryGetSessionById(sessionId, out ABY_DominionPocketSession session))
                        {
                            AbyssalDominionPocketUtility.CollapsePocketSlice(session, Map, false);
                        }
                    }
                };
            }
        }

        public override string GetInspectString()
        {
            List<string> lines = new List<string>();
            string baseText = base.GetInspectString();
            if (!baseText.NullOrEmpty())
            {
                lines.Add(baseText.TrimEnd());
            }

            ABY_DominionPocketRuntimeGameComponent runtime = ABY_DominionPocketRuntimeGameComponent.Get();
            if (runtime != null && runtime.TryGetSessionById(sessionId, out ABY_DominionPocketSession session))
            {
                lines.Add("ABY_DominionPocketRuntimeExitInspect".Translate(AbyssalDominionPocketUtility.GetSourceMapLabel(session)));
                lines.Add("ABY_DominionPocketRuntimeExitInspect_Team".Translate(AbyssalDominionPocketUtility.GetPocketPlayerCount(Map)));
                lines.Add("ABY_DominionPocketRuntimeExitInspect_State".Translate(AbyssalDominionPocketUtility.GetPocketSessionStatusValue(session, Map)));
                lines.Add("ABY_DominionPocketRuntimeExitInspect_Objective".Translate(AbyssalDominionPocketUtility.GetPocketObjectiveValue(session, Map)));
                lines.Add("ABY_DominionPocketRuntimeExitInspect_Rewards".Translate(AbyssalDominionPocketUtility.GetPocketRewardValue(session, Map)));
            }
            else
            {
                lines.Add("ABY_DominionPocketRuntimeExitInspect_Unlinked".Translate());
            }

            return string.Join("\n", lines);
        }
    }
}
