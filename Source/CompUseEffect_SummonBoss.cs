using RimWorld;
using Verse;
using Verse.AI;

namespace AbyssalProtocol
{
    public class CompUseEffect_SummonBoss : CompUseEffect
    {
        public CompProperties_UseEffectSummonBoss Props => (CompProperties_UseEffectSummonBoss)props;

        public override void DoEffect(Pawn usedBy)
        {
            base.DoEffect(usedBy);

            Building_AbyssalSummoningCircle preferredCircle = TryGetPreferredCircle(usedBy);
            ABY_SigilUseValidator.SigilUseContext context;
            string failReason;
            if (!ABY_SigilUseValidator.TryBuildContext(usedBy, parent, preferredCircle, false, out context, out failReason))
            {
                TryEjectFailedSigilFromCircle(usedBy, preferredCircle);
                if (!failReason.NullOrEmpty())
                {
                    Messages.Message(failReason, MessageTypeDefOf.RejectInput, false);
                }
                return;
            }

            if (!context.Circle.TryStartSummonSequence(usedBy, context.Props, out string startFailReason))
            {
                TryEjectFailedSigilFromCircle(usedBy, context.Circle);
                Messages.Message(startFailReason, MessageTypeDefOf.RejectInput, false);
                return;
            }

            ConsumeOneUse();

            Messages.Message(
                "ABY_SigilActivationStarted".Translate(),
                MessageTypeDefOf.PositiveEvent,
                false);
        }

        private Building_AbyssalSummoningCircle TryGetPreferredCircle(Pawn usedBy)
        {
            if (usedBy?.CurJob == null)
            {
                return null;
            }

            LocalTargetInfo targetB = usedBy.CurJob.GetTarget(TargetIndex.B);
            Building_AbyssalSummoningCircle circle = targetB.Thing as Building_AbyssalSummoningCircle;
            if (circle == null || circle.Destroyed || !circle.Spawned || circle.MapHeld != usedBy.MapHeld)
            {
                return null;
            }

            if (!circle.IsReadyForSigil(out _))
            {
                return null;
            }

            return circle;
        }

        private void ConsumeOneUse()
        {
            if (parent == null || parent.Destroyed)
            {
                return;
            }

            if (parent.stackCount > 1)
            {
                Thing one = parent.SplitOff(1);
                one?.Destroy();
            }
            else
            {
                parent.Destroy();
            }
        }

        private void TryEjectFailedSigilFromCircle(Pawn usedBy, Building_AbyssalSummoningCircle circle)
        {
            Thing sigil = parent;
            if (sigil == null || sigil.Destroyed || !sigil.Spawned)
            {
                return;
            }

            Map map = sigil.MapHeld;
            if (map == null)
            {
                return;
            }

            IntVec3 focusCell = circle?.RitualFocusCell ?? IntVec3.Invalid;
            bool onUnsafeCircleCell = focusCell.IsValid && sigil.PositionHeld == focusCell;
            if (!onUnsafeCircleCell && circle != null)
            {
                CellRect occupiedRect = GenAdj.OccupiedRect(circle.Position, circle.Rotation, circle.def.Size);
                if (occupiedRect.Contains(sigil.PositionHeld))
                {
                    onUnsafeCircleCell = true;
                }
            }

            if (!onUnsafeCircleCell)
            {
                return;
            }

            IntVec3 destination = FindFailedSigilDropCell(usedBy, circle, map, focusCell);
            if (!destination.IsValid)
            {
                return;
            }

            sigil.DeSpawn();
            if (!GenPlace.TryPlaceThing(sigil, destination, map, ThingPlaceMode.Near))
            {
                GenSpawn.Spawn(sigil, destination, map);
            }
        }

        private IntVec3 FindFailedSigilDropCell(Pawn usedBy, Building_AbyssalSummoningCircle circle, Map map, IntVec3 focusCell)
        {
            if (usedBy != null)
            {
                IntVec3 actorCell = usedBy.PositionHeld;
                if (IsValidFailedSigilDropCell(actorCell, circle, map, focusCell))
                {
                    return actorCell;
                }
            }

            if (circle != null)
            {
                IntVec3 interactionCell = circle.InteractionCell;
                if (IsValidFailedSigilDropCell(interactionCell, circle, map, focusCell))
                {
                    return interactionCell;
                }

                CellRect occupiedRect = GenAdj.OccupiedRect(circle.Position, circle.Rotation, circle.def.Size);
                for (int i = 0; i < GenRadial.NumCellsInRadius(4.9f); i++)
                {
                    IntVec3 cell = circle.Position + GenRadial.RadialPattern[i];
                    if (!IsValidFailedSigilDropCell(cell, circle, map, focusCell) || occupiedRect.Contains(cell))
                    {
                        continue;
                    }

                    return cell;
                }
            }

            for (int i = 0; i < GenRadial.NumCellsInRadius(6.9f); i++)
            {
                IntVec3 cell = focusCell + GenRadial.RadialPattern[i];
                if (IsValidFailedSigilDropCell(cell, circle, map, focusCell))
                {
                    return cell;
                }
            }

            return IntVec3.Invalid;
        }

        private bool IsValidFailedSigilDropCell(IntVec3 cell, Building_AbyssalSummoningCircle circle, Map map, IntVec3 focusCell)
        {
            if (!cell.IsValid || !cell.InBounds(map) || cell == focusCell || !cell.Standable(map))
            {
                return false;
            }

            if (circle != null)
            {
                CellRect occupiedRect = GenAdj.OccupiedRect(circle.Position, circle.Rotation, circle.def.Size);
                if (occupiedRect.Contains(cell))
                {
                    return false;
                }
            }

            Building edifice = cell.GetEdifice(map);
            if (edifice != null)
            {
                return false;
            }

            return true;
        }
    }
}
