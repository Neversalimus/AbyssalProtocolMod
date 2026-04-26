using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    public class RecipeWorker_ABY_SinterAbyssalCorpse : RecipeWorker
    {
        public override void Notify_IterationCompleted(Pawn billDoer, List<Thing> ingredients)
        {
            // Cache the dynamic corpse yield before base completion consumes/destroys the corpse.
            // If this is done after base.Notify_IterationCompleted, corpse.InnerPawn can already be
            // unavailable and the recipe falls back to the static XML product: 1 residue.
            int targetResidue = GetHighestResidueYield(ingredients);

            base.Notify_IterationCompleted(billDoer, ingredients);

            if (billDoer == null || billDoer.Map == null || targetResidue <= 1)
            {
                return;
            }

            ThingDef residueDef = DefDatabase<ThingDef>.GetNamed("ABY_AbyssalResidue", false);
            if (residueDef == null)
            {
                return;
            }

            Thing extraResidue = ThingMaker.MakeThing(residueDef);
            extraResidue.stackCount = targetResidue - 1;
            GenPlace.TryPlaceThing(extraResidue, billDoer.Position, billDoer.Map, ThingPlaceMode.Near);
        }

        private static int GetHighestResidueYield(List<Thing> ingredients)
        {
            if (ingredients == null)
            {
                return 0;
            }

            int targetResidue = 0;
            for (int i = 0; i < ingredients.Count; i++)
            {
                if (ABY_ResidueSinteringUtility.TryGetResidueAmount(ingredients[i], out int residueAmount))
                {
                    targetResidue = Mathf.Max(targetResidue, residueAmount);
                }
            }

            return targetResidue;
        }
    }
}
