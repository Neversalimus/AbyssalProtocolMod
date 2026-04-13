using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace AbyssalProtocol
{
    public class ITab_AbyssalForgeBills : ITab
    {
        private static readonly Vector2 WinSize = new Vector2(640f, 620f);
        private static readonly Dictionary<int, string> SelectedCategoryByThingId = new Dictionary<int, string>();

        private Vector2 scrollPosition = default(Vector2);
        private float viewHeight = 1000f;
        private Bill mouseoverBill;

        protected Building_AbyssalForge SelForge => (Building_AbyssalForge)SelThing;

        public ITab_AbyssalForgeBills()
        {
            size = WinSize;
            labelKey = "ABY_ForgeTabLabel";
            tutorTag = "Bills";
        }

        protected override void FillTab()
        {
            PlayerKnowledgeDatabase.KnowledgeDemonstrated(ConceptDefOf.BillsTab, KnowledgeAmount.FrameDisplayed);

            Rect canvas = new Rect(0f, 0f, size.x, size.y).ContractedBy(10f);
            Rect topRect = new Rect(canvas.x, canvas.y, canvas.width, 148f);
            Rect categoryRect = new Rect(canvas.x, topRect.yMax + 8f, canvas.width, 32f);
            Rect previewRect = new Rect(canvas.x, categoryRect.yMax + 8f, canvas.width, 88f);
            Rect billsRect = new Rect(canvas.x, previewRect.yMax + 8f, canvas.width, canvas.yMax - (previewRect.yMax + 8f));

            DrawProgressSection(topRect);
            DrawCategorySection(categoryRect);
            DrawPreviewSection(previewRect);
            DrawBillsSection(billsRect);
        }

        public override void TabUpdate()
        {
            if (mouseoverBill != null)
            {
                mouseoverBill.TryDrawIngredientSearchRadiusOnMap(SelForge.Position);
                mouseoverBill = null;
            }
        }

        private void DrawProgressSection(Rect rect)
        {
            Widgets.DrawMenuSection(rect);
            Rect inner = rect.ContractedBy(10f);

            MapComponent_AbyssalForgeProgress progress = SelForge.ProgressComponent;
            int availableResidue = progress?.CountAvailableResidue() ?? 0;
            int totalOffered = progress?.TotalResidueOffered ?? 0;
            int nextUnlock = progress?.GetNextUnlockResidue(GetSelectedCategory()) ?? -1;
            RecipeDef nextRecipe = progress?.GetNextUnlockRecipe(GetSelectedCategory());
            int currentTier = progress?.GetCurrentAttunementTier(false) ?? 0;
            float fillPercent = nextUnlock > 0 ? Mathf.Clamp01(totalOffered / (float)nextUnlock) : 1f;

            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(inner.x, inner.y, inner.width, 30f), "ABY_ForgePanelHeader".Translate());
            Text.Font = GameFont.Small;

            Rect barRect = new Rect(inner.x, inner.y + 34f, inner.width - 220f, 18f);
            Widgets.FillableBar(barRect, fillPercent);
            Widgets.DrawBox(barRect);
            Widgets.Label(new Rect(barRect.x + 6f, barRect.y - 2f, barRect.width - 12f, 24f),
                nextUnlock > 0
                    ? "ABY_ForgeProgressBar".Translate(totalOffered, nextUnlock)
                    : "ABY_ForgeProgressComplete".Translate(totalOffered));

            float infoY = barRect.yMax + 10f;
            float leftWidth = inner.width - 220f;
            string nextLabel = nextRecipe != null
                ? AbyssalForgeProgressUtility.GetRecipeDisplayLabel(nextRecipe)
                : "ABY_ForgeNoFurtherUnlocks".Translate();
            string powerLabel = SelForge.IsPowerActive
                ? "ABY_ForgePowerOnline".Translate()
                : "ABY_ForgePowerOffline".Translate();

            Widgets.Label(new Rect(inner.x, infoY, leftWidth, 22f), "ABY_ForgeResidueAvailable".Translate(availableResidue));
            Widgets.Label(new Rect(inner.x, infoY + 22f, leftWidth, 22f), "ABY_ForgeAttunementState".Translate(("ABY_AttunementTier_" + currentTier).Translate()));
            Widgets.Label(new Rect(inner.x, infoY + 44f, leftWidth, 22f), nextUnlock > 0
                ? "ABY_ForgeNextPattern".Translate(nextUnlock, nextLabel)
                : "ABY_ForgeNextPatternDone".Translate());
            Widgets.Label(new Rect(inner.x, infoY + 66f, leftWidth, 22f), powerLabel);

            Rect buttonArea = new Rect(rect.xMax - 198f, rect.y + 16f, 182f, rect.height - 32f);
            DrawOfferButtons(buttonArea, availableResidue);

            TooltipHandler.TipRegion(new Rect(inner.x, infoY + 88f, leftWidth, 22f), "ABY_ForgeOfferTip".Translate());
            Widgets.Label(new Rect(inner.x, infoY + 88f, leftWidth, 22f), "ABY_ForgeOfferHint".Translate());
        }

        private void DrawOfferButtons(Rect rect, int availableResidue)
        {
            GUI.BeginGroup(rect);
            bool enabled = availableResidue > 0;
            GUI.enabled = enabled;

            Rect offer10 = new Rect(0f, 0f, rect.width, 30f);
            Rect offer50 = new Rect(0f, 34f, rect.width, 30f);
            Rect offerAll = new Rect(0f, 68f, rect.width, 30f);

            if (Widgets.ButtonText(offer10, "ABY_ForgeOfferAmount".Translate(10)))
            {
                TryOfferResidue(10);
            }

            if (Widgets.ButtonText(offer50, "ABY_ForgeOfferAmount".Translate(50)))
            {
                TryOfferResidue(50);
            }

            if (Widgets.ButtonText(offerAll, "ABY_ForgeOfferAll".Translate(availableResidue)))
            {
                TryOfferResidue(availableResidue);
            }

            GUI.enabled = true;
            if (!enabled)
            {
                Widgets.Label(new Rect(0f, 104f, rect.width, 24f), "ABY_ForgeOfferNoneAvailable".Translate());
            }

            GUI.EndGroup();
        }

        private void DrawCategorySection(Rect rect)
        {
            Widgets.DrawMenuSection(rect);
            Rect inner = rect.ContractedBy(4f);
            List<string> categories = AbyssalForgeProgressUtility.Categories.ToList();
            float buttonWidth = inner.width / categories.Count;
            string selectedCategory = GetSelectedCategory();

            for (int i = 0; i < categories.Count; i++)
            {
                string category = categories[i];
                Rect buttonRect = new Rect(inner.x + buttonWidth * i, inner.y, buttonWidth - 4f, inner.height);
                bool isSelected = category == selectedCategory;
                Color oldColor = GUI.color;
                if (isSelected)
                {
                    GUI.color = new Color(1f, 0.55f, 0.28f);
                }

                if (Widgets.ButtonText(buttonRect, AbyssalForgeProgressUtility.GetCategoryLabel(category)))
                {
                    SelectedCategoryByThingId[SelForge.thingIDNumber] = category;
                }

                GUI.color = oldColor;
            }
        }

        private void DrawPreviewSection(Rect rect)
        {
            Widgets.DrawMenuSection(rect);
            Rect inner = rect.ContractedBy(8f);

            MapComponent_AbyssalForgeProgress progress = SelForge.ProgressComponent;
            string selectedCategory = GetSelectedCategory();
            List<RecipeDef> unlocked = progress?.GetUnlockedRecipes(selectedCategory) ?? new List<RecipeDef>();
            List<RecipeDef> locked = progress?.GetLockedRecipes(selectedCategory) ?? new List<RecipeDef>();

            Rect leftRect = new Rect(inner.x, inner.y, inner.width * 0.34f, inner.height);
            Rect rightRect = new Rect(leftRect.xMax + 10f, inner.y, inner.width - leftRect.width - 10f, inner.height);

            Widgets.Label(leftRect, "ABY_ForgeUnlockedSummary".Translate(unlocked.Count, unlocked.Count + locked.Count, AbyssalForgeProgressUtility.GetCategoryLabel(selectedCategory)));

            string previewText;
            if (locked.Count == 0)
            {
                previewText = "ABY_ForgeAllPatternsUnlocked".Translate();
            }
            else
            {
                List<string> lines = new List<string>();
                int previewCount = Math.Min(3, locked.Count);
                for (int i = 0; i < previewCount; i++)
                {
                    RecipeDef recipe = locked[i];
                    lines.Add("• " + AbyssalForgeProgressUtility.GetRequiredResidue(recipe) + " — " + AbyssalForgeProgressUtility.GetRecipeDisplayLabel(recipe));
                }

                previewText = "ABY_ForgeUpcomingPatterns".Translate() + "\n" + string.Join("\n", lines.ToArray());
            }

            Widgets.Label(rightRect, previewText);
        }

        private void DrawBillsSection(Rect rect)
        {
            Rect pasteRect = new Rect(rect.xMax - 28f, rect.y + 3f, 24f, 24f);
            DrawPasteButton(pasteRect);

            Func<List<FloatMenuOption>> recipeOptionsMaker = BuildRecipeOptions;
            mouseoverBill = SelForge.BillStack.DoListing(rect, recipeOptionsMaker, ref scrollPosition, ref viewHeight);
        }

        private List<FloatMenuOption> BuildRecipeOptions()
        {
            List<FloatMenuOption> options = new List<FloatMenuOption>();
            List<RecipeDef> availableRecipes = SelForge.ProgressComponent?.GetUnlockedRecipes(GetSelectedCategory()) ?? new List<RecipeDef>();

            for (int i = 0; i < availableRecipes.Count; i++)
            {
                RecipeDef recipe = availableRecipes[i];
                if (!recipe.AvailableNow || !recipe.AvailableOnNow(SelForge))
                {
                    continue;
                }

                RecipeDef capturedRecipe = recipe;
                options.Add(new FloatMenuOption(
                    capturedRecipe.LabelCap,
                    delegate
                    {
                        if (!SelForge.Map.mapPawns.FreeColonists.Any(colonist => capturedRecipe.PawnSatisfiesSkillRequirements(colonist)))
                        {
                            Bill.CreateNoPawnsWithSkillDialog(capturedRecipe);
                        }

                        Bill bill = capturedRecipe.MakeNewBill();
                        SelForge.BillStack.AddBill(bill);

                        if (capturedRecipe.conceptLearned != null)
                        {
                            PlayerKnowledgeDatabase.KnowledgeDemonstrated(capturedRecipe.conceptLearned, KnowledgeAmount.Total);
                        }

                        if (TutorSystem.TutorialMode)
                        {
                            TutorSystem.Notify_Event("AddBill-" + capturedRecipe.LabelCap);
                        }
                    },
                    MenuOptionPriority.Default,
                    null,
                    null,
                    29f,
                    delegate(Rect infoRect)
                    {
                        Widgets.InfoCardButton(infoRect.x + 5f, infoRect.y + (infoRect.height - 24f) / 2f, capturedRecipe);
                        return false;
                    },
                    null));
            }

            if (!options.Any())
            {
                options.Add(new FloatMenuOption("ABY_ForgeNoUnlockedRecipes".Translate(), null, MenuOptionPriority.Default, null, null, 0f, null, null));
            }

            return options;
        }

        private void DrawPasteButton(Rect rect)
        {
            if (BillUtility.Clipboard == null)
            {
                GUI.color = Color.gray;
                Widgets.DrawTextureFitted(rect, TexButton.Paste, 1f);
                GUI.color = Color.white;
                TooltipHandler.TipRegionByKey(rect, "PasteBillTip");
                return;
            }

            RecipeDef clipboardRecipe = BillUtility.Clipboard.recipe;
            if (!CanUseRecipe(clipboardRecipe))
            {
                GUI.color = Color.gray;
                Widgets.DrawTextureFitted(rect, TexButton.Paste, 1f);
                GUI.color = Color.white;
                TooltipHandler.TipRegion(rect, "ABY_ForgeClipboardLocked".Translate());
                return;
            }

            if (SelForge.BillStack.Count >= 15)
            {
                GUI.color = Color.gray;
                Widgets.DrawTextureFitted(rect, TexButton.Paste, 1f);
                GUI.color = Color.white;
                TooltipHandler.TipRegion(rect, "PasteBillTip".Translate() + " (" + "PasteBillTip_LimitReached".Translate() + ")");
                return;
            }

            if (Widgets.ButtonImageFitted(rect, TexButton.Paste, Color.white))
            {
                Bill bill = BillUtility.Clipboard.Clone();
                bill.InitializeAfterClone();
                SelForge.BillStack.AddBill(bill);
                SoundDefOf.Tick_Low.PlayOneShotOnCamera(null);
            }

            TooltipHandler.TipRegionByKey(rect, "PasteBillTip");
        }

        private bool CanUseRecipe(RecipeDef recipe)
        {
            return recipe != null
                && SelForge.def.AllRecipes.Contains(recipe)
                && SelForge.ProgressComponent != null
                && AbyssalForgeProgressUtility.IsRecipeUnlocked(recipe, SelForge.ProgressComponent.TotalResidueOffered)
                && recipe.AvailableNow
                && recipe.AvailableOnNow(SelForge);
        }

        private void TryOfferResidue(int requestedAmount)
        {
            int consumed = SelForge.OfferResidue(requestedAmount);
            if (consumed > 0)
            {
                SoundDefOf.Tick_High.PlayOneShotOnCamera(null);
            }
            else
            {
                Messages.Message("ABY_ForgeOfferNoneAvailable".Translate(), SelForge, MessageTypeDefOf.RejectInput, false);
            }
        }

        private string GetSelectedCategory()
        {
            if (SelectedCategoryByThingId.TryGetValue(SelForge.thingIDNumber, out string category)
                && AbyssalForgeProgressUtility.Categories.Contains(category))
            {
                return category;
            }

            return AbyssalForgeProgressUtility.AllCategory;
        }
    }
}
