using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace AbyssalProtocol
{
    public class MapComponent_AbyssalForgeProgress : MapComponent
    {
        private const int AttunementSyncIntervalTicks = 180;
        private const int RecentUnlockDurationTicks = 180000;

        private int totalResidueOffered;
        private int nextAttunementSyncTick;
        private bool reducedVisualEffects;
        private int recentUnlockTick = -999999;
        private List<string> recentUnlockRecipeDefNames = new List<string>();

        public MapComponent_AbyssalForgeProgress(Map map) : base(map)
        {
        }

        public int TotalResidueOffered => totalResidueOffered;
        public bool ReducedVisualEffects => reducedVisualEffects;

        public bool HasRecentUnlocks
        {
            get
            {
                if (recentUnlockRecipeDefNames == null || recentUnlockRecipeDefNames.Count == 0)
                {
                    return false;
                }

                int currentTick = Find.TickManager != null ? Find.TickManager.TicksGame : 0;
                return currentTick - recentUnlockTick <= RecentUnlockDurationTicks;
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref totalResidueOffered, "totalResidueOffered", 0);
            Scribe_Values.Look(ref nextAttunementSyncTick, "nextAttunementSyncTick", 0);
            Scribe_Values.Look(ref reducedVisualEffects, "reducedVisualEffects", false);
            Scribe_Values.Look(ref recentUnlockTick, "recentUnlockTick", -999999);
            Scribe_Collections.Look(ref recentUnlockRecipeDefNames, "recentUnlockRecipeDefNames", LookMode.Value);

            if (Scribe.mode == LoadSaveMode.PostLoadInit && recentUnlockRecipeDefNames == null)
            {
                recentUnlockRecipeDefNames = new List<string>();
            }
        }

        public override void MapComponentTick()
        {
            base.MapComponentTick();

            if (Find.TickManager == null)
            {
                return;
            }

            int ticksGame = Find.TickManager.TicksGame;
            if (ticksGame >= nextAttunementSyncTick)
            {
                nextAttunementSyncTick = ticksGame + AttunementSyncIntervalTicks;
                SyncAttunementHediffs();
            }

            if (recentUnlockRecipeDefNames != null && recentUnlockRecipeDefNames.Count > 0 && ticksGame - recentUnlockTick > RecentUnlockDurationTicks)
            {
                recentUnlockRecipeDefNames.Clear();
            }
        }

        public int CountAvailableResidue()
        {
            return AbyssalForgeProgressUtility.CountAvailableResidue(map);
        }

        public void SetReducedVisualEffects(bool value)
        {
            reducedVisualEffects = value;
        }

        public bool IsRecentlyUnlocked(RecipeDef recipe)
        {
            return recipe != null
                && HasRecentUnlocks
                && recentUnlockRecipeDefNames != null
                && recentUnlockRecipeDefNames.Contains(recipe.defName);
        }

        public void ConsumeRecentUnlocks()
        {
            if (recentUnlockRecipeDefNames == null || recentUnlockRecipeDefNames.Count == 0)
            {
                return;
            }

            recentUnlockRecipeDefNames.Clear();
            recentUnlockTick = -999999;
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



        public int DebugGrantResidue(Building_AbyssalForge forge, int amount)
        {
            if (amount <= 0)
            {
                return 0;
            }

            int previousTotal = totalResidueOffered;
            totalResidueOffered += amount;
            NotifyNewUnlocksIfNeeded(forge, previousTotal, totalResidueOffered);
            SyncAttunementHediffs();

            Messages.Message(
                "ABY_ForgeDevResidueApplied".Translate(amount, totalResidueOffered),
                forge,
                MessageTypeDefOf.TaskCompletion,
                false);

            return amount;
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

            return Mathf.Max(1, AbyssalForgeProgressUtility.GetAttunementTierForResidue(totalResidueOffered));
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

            recentUnlockTick = Find.TickManager != null ? Find.TickManager.TicksGame : 0;
            recentUnlockRecipeDefNames = unlockedNow.Where(recipe => recipe != null && !recipe.defName.NullOrEmpty()).Select(recipe => recipe.defName).ToList();

            string unlockedLines = string.Join("\n", unlockedNow.Select(recipe => "• " + AbyssalForgeProgressUtility.GetRecipeDisplayLabel(recipe)).ToArray());
            string currentAttunement = "ABY_AttunementTier_" + GetCurrentAttunementTier(false);
            Find.LetterStack.ReceiveLetter(
                "ABY_ForgeUnlockLetterLabel".Translate(currentTotal),
                "ABY_ForgeUnlockLetterDesc".Translate(currentTotal, currentAttunement.Translate(), unlockedLines),
                LetterDefOf.PositiveEvent,
                forge != null ? new LookTargets(forge) : LookTargets.Invalid);

            Messages.Message(
                "ABY_ForgeUnlockToast".Translate(BuildUnlockToastLabel(unlockedNow)),
                forge,
                MessageTypeDefOf.PositiveEvent,
                false);

            SoundDefOf.Tick_High.PlayOneShotOnCamera(null);
        }

        private static string BuildUnlockToastLabel(List<RecipeDef> unlockedNow)
        {
            if (unlockedNow == null || unlockedNow.Count == 0)
            {
                return "?";
            }

            if (unlockedNow.Count == 1)
            {
                return AbyssalForgeProgressUtility.GetRecipeDisplayLabel(unlockedNow[0]);
            }

            string combined = string.Join(", ", unlockedNow.Take(2).Select(recipe => AbyssalForgeProgressUtility.GetRecipeDisplayLabel(recipe)).ToArray());
            if (unlockedNow.Count > 2)
            {
                combined += " +" + (unlockedNow.Count - 2);
            }

            return combined;
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
