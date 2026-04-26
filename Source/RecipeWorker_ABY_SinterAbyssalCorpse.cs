using System.Collections.Generic;
using RimWorld;
using Verse;

namespace AbyssalProtocol
{
    public class RecipeWorker_ABY_SinterAbyssalCorpse : RecipeWorker
    {
        public override void ConsumeIngredient(Thing ingredient, RecipeDef recipe, Map map)
        {
            // RimWorld products in RecipeDef are static. ABY_SinterAbyssalRemains keeps
            // ABY_AbyssalResidue x1 in XML so vanilla bills/workgivers keep behaving normally.
            //
            // Dynamic corpse value is added here, while the corpse still exists and its
            // InnerPawn can be inspected safely. This avoids relying on the post-completion
            // ingredient list, which may no longer contain a readable corpse or may only
            // preserve enough state for the static XML product.
            TrySpawnDynamicResidueBeforeConsuming(ingredient, map);

            base.ConsumeIngredient(ingredient, recipe, map);
        }

        private static void TrySpawnDynamicResidueBeforeConsuming(Thing ingredient, Map map)
        {
            if (ingredient == null)
            {
                return;
            }

            if (!ABY_ResidueSinteringUtility.TryGetResidueAmount(ingredient, out int targetResidue) || targetResidue <= 1)
            {
                return;
            }

            ThingDef residueDef = DefDatabase<ThingDef>.GetNamed("ABY_AbyssalResidue", false);
            if (residueDef == null)
            {
                return;
            }

            Map targetMap = map ?? ingredient.Map;
            if (targetMap == null)
            {
                return;
            }

            Thing extraResidue = ThingMaker.MakeThing(residueDef);
            extraResidue.stackCount = targetResidue - 1;

            IntVec3 dropCell = ingredient.Position;
            if (!dropCell.IsValid || !dropCell.InBounds(targetMap))
            {
                dropCell = IntVec3.Invalid;
            }

            if (dropCell.IsValid)
            {
                GenPlace.TryPlaceThing(extraResidue, dropCell, targetMap, ThingPlaceMode.Near);
            }
        }
    }
}
