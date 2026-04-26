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
            base.Notify_IterationCompleted(billDoer, ingredients);

            if (billDoer == null || billDoer.Map == null || ingredients == null)
            {
                return;
            }

            int targetResidue = 0;
            for (int i = 0; i < ingredients.Count; i++)
            {
                if (ABY_ResidueSinteringUtility.TryGetResidueAmount(ingredients[i], out int residueAmount))
                {
                    targetResidue = Mathf.Max(targetResidue, residueAmount);
                }
            }

            if (targetResidue <= 1)
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
    }
}
