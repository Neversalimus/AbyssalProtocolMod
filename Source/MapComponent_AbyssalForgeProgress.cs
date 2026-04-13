using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace AbyssalProtocol
{
    public class MapComponent_AbyssalForgeProgress : MapComponent
    {
        private const int AttunementSyncIntervalTicks = 180;

        private int totalResidueOffered;
        private int nextAttunementSyncTick;

        public MapComponent_AbyssalForgeProgress(Map map) : base(map)
        {
        }

        public int TotalResidueOffered => totalResidueOffered;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref totalResidueOffered, "totalResidueOffered", 0);
            Scribe_Values.Look(ref nextAttunementSyncTick, "nextAttunementSyncTick", 0);
        }

        public override void MapComponentTick()
        {
            base.MapComponentTick();

            if (Find.TickManager == null)
            {
                return;
            }

            int ticksGame = Find.TickManager.TicksGame;
            if (ticksGame < nextAttunementSyncTick)
            {
                return;
            }

            nextAttunementSyncTick = ticksGame + AttunementSyncIntervalTicks;
            SyncAttunementHediffs();
        }

        public int CountAvailableResidue()
        {
            return AbyssalForgeProgressUtility.CountAvailableResidue(map);
        }

        public int OfferResidue(Building_AbyssalForge forge, int requestedAmount)
        {
            if (requestedAmount <= 0 || map == null)
            {
                return 0;
            }

            int previousTotal = totalResidueOffered;
            int consumed = AbyssalForgeProgressUtility.ConsumeResidue(map, requestedAmount, forge?.Position ?? IntVec3.Zero);
            if (consumed <= 0)
            {
                return 0;
            }

            totalResidueOffered += consumed;
            NotifyOfferComplete(forge, consumed);
            NotifyNewUnlocksIfNeeded(forge, previousTotal, totalResidueOffered);
            SyncAttunementHediffs();
            return consumed;
        }

        public List<RecipeDef> GetUnlockedRecipes(string category = AbyssalForgeProgressUtility.AllCategory)
        {
            return AbyssalForgeProgressUtility.GetForgeRecipes()
                .Where(recipe => AbyssalForgeProgressUtility.RecipeMatchesCategory(recipe, category))
                .Where(recipe => AbyssalForgeProgressUtility.IsRecipeUnlocked(recipe, totalResidueOffered))
                .ToList();
        }

        public List<RecipeDef> GetLockedRecipes(string category = AbyssalForgeProgressUtility.AllCategory)
        {
            return AbyssalForgeProgressUtility.GetForgeRecipes()
                .Where(recipe => AbyssalForgeProgressUtility.RecipeMatchesCategory(recipe, category))
                .Where(recipe => !AbyssalForgeProgressUtility.IsRecipeUnlocked(recipe, totalResidueOffered))
                .ToList();
        }

        public int GetNextUnlockResidue(string category = AbyssalForgeProgressUtility.AllCategory)
        {
            List<RecipeDef> locked = GetLockedRecipes(category);
            return locked.Count > 0
                ? AbyssalForgeProgressUtility.GetRequiredResidue(locked[0])
                : -1;
        }

        public RecipeDef GetNextUnlockRecipe(string category = AbyssalForgeProgressUtility.AllCategory)
        {
            List<RecipeDef> locked = GetLockedRecipes(category);
            return locked.Count > 0 ? locked[0] : null;
        }

        public int GetCurrentAttunementTier(bool requirePoweredForge)
        {
            bool hasForge = HasAnyForge();
            if (!hasForge)
            {
                return 0;
            }

            if (requirePoweredForge && !HasPoweredForge())
            {
                return 0;
            }

            if (totalResidueOffered >= 500)
            {
                return 4;
            }

            if (totalResidueOffered >= 300)
            {
                return 3;
            }

            if (totalResidueOffered >= 150)
            {
                return 2;
            }

            return 1;
        }

        public bool HasAnyForge()
        {
            return GetForges().Count > 0;
        }

        public bool HasPoweredForge()
        {
            List<Building_AbyssalForge> forges = GetForges();
            for (int i = 0; i < forges.Count; i++)
            {
                Building_AbyssalForge forge = forges[i];
                if (forge != null && !forge.Destroyed && forge.IsPowerActive)
                {
                    return true;
                }
            }

            return false;
        }

        private List<Building_AbyssalForge> GetForges()
        {
            ThingDef forgeDef = AbyssalForgeProgressUtility.ForgeDef;
            if (map?.listerThings == null || forgeDef == null)
            {
                return new List<Building_AbyssalForge>();
            }

            List<Thing> things = map.listerThings.ThingsOfDef(forgeDef);
            if (things == null || things.Count == 0)
            {
                return new List<Building_AbyssalForge>();
            }

            List<Building_AbyssalForge> result = new List<Building_AbyssalForge>();
            for (int i = 0; i < things.Count; i++)
            {
                if (things[i] is Building_AbyssalForge forge && !forge.Destroyed)
                {
                    result.Add(forge);
                }
            }

            return result;
        }

        private void NotifyOfferComplete(Building_AbyssalForge forge, int consumed)
        {
            Messages.Message(
                "ABY_ForgeOfferComplete".Translate(consumed, totalResidueOffered),
                forge,
                MessageTypeDefOf.TaskCompletion,
                false);
        }

        private void NotifyNewUnlocksIfNeeded(Building_AbyssalForge forge, int previousTotal, int currentTotal)
        {
            List<RecipeDef> unlockedNow = AbyssalForgeProgressUtility.GetForgeRecipes()
                .Where(recipe => AbyssalForgeProgressUtility.GetRequiredResidue(recipe) > previousTotal)
                .Where(recipe => AbyssalForgeProgressUtility.GetRequiredResidue(recipe) <= currentTotal)
                .ToList();

            if (unlockedNow.Count == 0)
            {
                return;
            }

            string unlockedLines = string.Join("\n", unlockedNow.Select(recipe => "• " + AbyssalForgeProgressUtility.GetRecipeDisplayLabel(recipe)));
            string currentAttunement = "ABY_AttunementTier_" + GetCurrentAttunementTier(false);
            Find.LetterStack.ReceiveLetter(
                "ABY_ForgeUnlockLetterLabel".Translate(currentTotal),
                "ABY_ForgeUnlockLetterDesc".Translate(currentTotal, currentAttunement.Translate(), unlockedLines),
                LetterDefOf.PositiveEvent,
                forge != null ? new LookTargets(forge) : LookTargets.Invalid);
        }

        private void SyncAttunementHediffs()
        {
            HediffDef hediffDef = AbyssalForgeProgressUtility.AttunementHediffDef;
            if (hediffDef == null || map?.mapPawns == null)
            {
                return;
            }

            int tier = GetCurrentAttunementTier(true);
            List<Pawn> colonists = map.mapPawns.FreeColonistsSpawned;
            for (int i = 0; i < colonists.Count; i++)
            {
                Pawn pawn = colonists[i];
                if (pawn?.health?.hediffSet == null || pawn.Dead)
                {
                    continue;
                }

                Hediff existing = pawn.health.hediffSet.GetFirstHediffOfDef(hediffDef);
                if (tier <= 0)
                {
                    if (existing != null)
                    {
                        pawn.health.RemoveHediff(existing);
                    }

                    continue;
                }

                if (existing == null)
                {
                    existing = HediffMaker.MakeHediff(hediffDef, pawn);
                    pawn.health.AddHediff(existing);
                }

                existing.Severity = tier;
            }
        }
    }
}
