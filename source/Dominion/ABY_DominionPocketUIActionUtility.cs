using Verse;
using RimWorld;

namespace AbyssalProtocol
{
    /// <summary>
    /// Dominion pocket entry/exit UI shim.
    ///
    /// These helpers intentionally defer heavy map-transfer operations by one frame. This avoids
    /// running pocket entry/return/collapse from inside the same IMGUI event that is drawing gizmos,
    /// ITabs, custom consoles, or third-party scroll views in large modpacks.
    /// </summary>
    public static class ABY_DominionPocketUIActionUtility
    {
        public static bool QueueJumpFromCrisis(MapComponent_DominionCrisis crisis, out string failReason)
        {
            failReason = null;
            if (crisis == null)
            {
                failReason = "ABY_DominionPocketRuntimeFail_NoSession".Translate();
                return false;
            }

            return ABY_DeferredUIActionGameComponent.TryQueue("Dominion pocket jump", delegate
            {
                if (!crisis.TryJumpToPocketSlice(out string reason) && !reason.NullOrEmpty())
                {
                    Messages.Message(reason, MessageTypeDefOf.RejectInput, false);
                }
            }, out failReason);
        }

        public static bool QueueReturnFromCrisis(MapComponent_DominionCrisis crisis, out string failReason)
        {
            failReason = null;
            if (crisis == null)
            {
                failReason = "ABY_DominionPocketRuntimeFail_NoSession".Translate();
                return false;
            }

            return ABY_DeferredUIActionGameComponent.TryQueue("Dominion pocket return", delegate
            {
                if (!crisis.TryReturnPocketStrikeTeam(out string reason) && !reason.NullOrEmpty())
                {
                    Messages.Message(reason, MessageTypeDefOf.RejectInput, false);
                }
            }, out failReason);
        }

        public static bool QueueOpenFromCrisis(MapComponent_DominionCrisis crisis, out string failReason)
        {
            failReason = null;
            if (crisis == null)
            {
                failReason = "ABY_DominionPocketFlowFail_NotReady".Translate();
                return false;
            }

            return ABY_DeferredUIActionGameComponent.TryQueue("Dominion pocket open", delegate
            {
                if (!crisis.TryOpenPocketSliceFromPlayerFlow(out string reason) && !reason.NullOrEmpty())
                {
                    Messages.Message(reason, MessageTypeDefOf.RejectInput, false);
                }
            }, out failReason);
        }

        public static bool QueueSafeOpenFromGate(Building_AbyssalDominionGate gate, out string failReason)
        {
            failReason = null;
            if (gate == null || gate.Destroyed || gate.Map == null)
            {
                failReason = "ABY_DominionPocketRuntimeFail_NoGate".Translate();
                return false;
            }

            return ABY_DeferredUIActionGameComponent.TryQueue("Dominion safe pocket open", delegate
            {
                if (!AbyssalDominionPocketSafeUtility.TryOpenPocketSliceFromGate(gate, out string reason) && !reason.NullOrEmpty())
                {
                    Messages.Message(reason, MessageTypeDefOf.RejectInput, false);
                }
            }, out failReason);
        }

        public static bool QueueSafeReturnFromGate(Building_AbyssalDominionGate gate, out string failReason)
        {
            failReason = null;
            if (gate == null || gate.Destroyed || gate.Map == null)
            {
                failReason = "ABY_DominionPocketRuntimeFail_NoGate".Translate();
                return false;
            }

            return ABY_DeferredUIActionGameComponent.TryQueue("Dominion safe pocket return", delegate
            {
                if (!AbyssalDominionPocketSafeUtility.TryReturnPocketStrikeTeamFromGate(gate, out string reason) && !reason.NullOrEmpty())
                {
                    Messages.Message(reason, MessageTypeDefOf.RejectInput, false);
                }
            }, out failReason);
        }

        public static bool QueueJumpSession(ABY_DominionPocketSession session, out string failReason)
        {
            failReason = null;
            if (session == null || session.sessionId.NullOrEmpty())
            {
                failReason = "ABY_DominionPocketRuntimeFail_NoSession".Translate();
                return false;
            }

            return ABY_DeferredUIActionGameComponent.TryQueue("Dominion session jump", delegate
            {
                if (!AbyssalDominionPocketUtility.TryJumpToPocketSlice(session, out string reason) && !reason.NullOrEmpty())
                {
                    Messages.Message(reason, MessageTypeDefOf.RejectInput, false);
                }
            }, out failReason);
        }

        public static bool QueueReturnSession(ABY_DominionPocketSession session, bool destroyPocketMap, out string failReason)
        {
            failReason = null;
            if (session == null || session.sessionId.NullOrEmpty())
            {
                failReason = "ABY_DominionPocketRuntimeFail_NoSession".Translate();
                return false;
            }

            return ABY_DeferredUIActionGameComponent.TryQueue("Dominion session return", delegate
            {
                if (!AbyssalDominionPocketUtility.TryReturnPocketSlice(session, destroyPocketMap, out string reason) && !reason.NullOrEmpty())
                {
                    Messages.Message(reason, MessageTypeDefOf.RejectInput, false);
                }
            }, out failReason);
        }

        public static bool QueueOpenLegacy(Building_AbyssalDominionGate gate, System.Collections.Generic.IEnumerable<Pawn> pawns, out string failReason)
        {
            failReason = null;
            if (gate == null || gate.Destroyed || gate.Map == null)
            {
                failReason = "ABY_DominionPocketRuntimeFail_NoGate".Translate();
                return false;
            }

            return ABY_DeferredUIActionGameComponent.TryQueue("Dominion legacy pocket open", delegate
            {
                if (AbyssalDominionPocketUtility.TryOpenPocketSlice(gate, pawns, out _, out string reason))
                {
                    Messages.Message("Dominion slice runtime opened.", MessageTypeDefOf.PositiveEvent, false);
                }
                else if (!reason.NullOrEmpty())
                {
                    Messages.Message(reason, MessageTypeDefOf.RejectInput, false);
                }
            }, out failReason);
        }
    }
}
