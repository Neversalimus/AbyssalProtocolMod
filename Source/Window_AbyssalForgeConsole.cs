using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace AbyssalProtocol
{
    public class Window_AbyssalForgeConsole : Window
    {
        private readonly Building_AbyssalForge forge;

        private Vector2 patternScrollPosition = Vector2.zero;
        private Vector2 billScrollPosition = Vector2.zero;
        private float billViewHeight = 1000f;
        private Bill mouseoverBill;
        private string selectedCategory = AbyssalForgeProgressUtility.AllCategory;

        public Window_AbyssalForgeConsole(Building_AbyssalForge forge)
        {
            this.forge = forge;
            absorbInputAroundWindow = true;
            closeOnClickedOutside = false;
            doCloseX = true;
            draggable = true;
            forcePause = false;
            preventCameraMotion = false;
            onlyOneOfTypeAllowed = false;
            resizeable = false;
        }

        public override Vector2 InitialSize => new Vector2(1228f, 812f);

        public override void DoWindowContents(Rect inRect)
        {
            if (forge == null || forge.Destroyed || forge.Map == null)
            {
                Close();
                return;
            }

            MapComponent_AbyssalForgeProgress progress = forge.ProgressComponent;
            if (progress == null)
            {
                Close();
                return;
            }

            AbyssalForgeConsoleArt.ReducedEffects = progress.ReducedVisualEffects;
            AbyssalForgeConsoleArt.DrawBackground(inRect);

            Rect headerRect = new Rect(inRect.x, inRect.y, inRect.width, 74f);
            Rect statusRect = new Rect(inRect.x, headerRect.yMax + 10f, 492f, 210f);
            Rect offerRect = new Rect(statusRect.xMax + 10f, headerRect.yMax + 10f, 248f, 210f);
            Rect nextRect = new Rect(offerRect.xMax + 10f, headerRect.yMax + 10f, inRect.width - offerRect.xMax - 10f, 210f);
            Rect categoryRect = new Rect(inRect.x, statusRect.yMax + 10f, inRect.width, 40f);
            Rect patternsRect = new Rect(inRect.x, categoryRect.yMax + 10f, 804f, inRect.height - categoryRect.yMax - 10f);
            Rect billsRect = new Rect(patternsRect.xMax + 10f, categoryRect.yMax + 10f, inRect.width - patternsRect.width - 10f, inRect.height - categoryRect.yMax - 10f);

            DrawHeader(headerRect, progress);
            DrawStatusPanel(statusRect, progress);
            DrawOfferPanel(offerRect, progress);
            DrawNextPanel(nextRect, progress);
            DrawCategoryRow(categoryRect);
            DrawPatternBrowser(patternsRect, progress);
            DrawBillsPanel(billsRect);

            if (mouseoverBill != null)
            {
                mouseoverBill.TryDrawIngredientSearchRadiusOnMap(forge.Position);
                mouseoverBill = null;
            }
        }

        private void DrawHeader(Rect rect, MapComponent_AbyssalForgeProgress progress)
        {
            AbyssalForgeConsoleArt.DrawHeader(
                rect,
                "ABY_ForgeConsoleTitle".Translate(),
                "ABY_ForgeConsoleSubtitle".Translate(),
                progress.HasRecentUnlocks);
        }

        private void DrawStatusPanel(Rect rect, MapComponent_AbyssalForgeProgress progress)
        {
            AbyssalForgeConsoleArt.DrawPanel(rect, false);
            Rect inner = rect.ContractedBy(12f);

            int total = progress.TotalResidueOffered;
            int nextThreshold = progress.GetNextUnlockResidue(selectedCategory);
            int previousThreshold = GetPreviousUnlockThreshold(progress, selectedCategory, total);
            float fill = 1f;
            string progressLabel;

            if (nextThreshold > 0)
            {
                int bandSize = Math.Max(1, nextThreshold - previousThreshold);
                fill = Mathf.Clamp01((total - previousThreshold) / (float)bandSize);
                progressLabel = "ABY_ForgeProgressBand".Translate(total, previousThreshold, nextThreshold);
            }
            else
            {
                progressLabel = "ABY_ForgeProgressComplete".Translate(total);
            }

            AbyssalForgeConsoleArt.DrawSectionTitle(new Rect(inner.x, inner.y, inner.width, 22f), "ABY_ForgeStatusHeader".Translate());
            AbyssalForgeConsoleArt.DrawProgressBar(new Rect(inner.x, inner.y + 26f, inner.width, 24f), fill, progressLabel, progress.HasRecentUnlocks);

            int attunementTier = progress.GetCurrentAttunementTier(false);
            Rect attunementBarRect = new Rect(inner.x, inner.y + 58f, inner.width, 22f);
            AbyssalForgeConsoleArt.DrawProgressBar(attunementBarRect, AbyssalForgeProgressUtility.GetAttunementLevelFill(attunementTier), AbyssalForgeProgressUtility.GetAttunementBarLabel(attunementTier), false);

            float metricY = inner.y + 90f;
            float metricWidth = (inner.width - 10f) / 2f;

            Rect residueRect = new Rect(inner.x, metricY, metricWidth, 42f);
            Rect availableRect = new Rect(inner.x + metricWidth + 10f, metricY, metricWidth, 42f);
            Rect attunementRect = new Rect(inner.x, metricY + 46f, metricWidth, 42f);
            Rect powerRect = new Rect(inner.x + metricWidth + 10f, metricY + 46f, metricWidth, 42f);

            AbyssalForgeConsoleArt.DrawMetric(residueRect, "ABY_ForgeMetricResidue".Translate(), progress.TotalResidueOffered.ToString());
            AbyssalForgeConsoleArt.DrawMetric(availableRect, "ABY_ForgeMetricAvailable".Translate(), progress.CountAvailableResidue().ToString());
            AbyssalForgeConsoleArt.DrawMetric(attunementRect, "ABY_ForgeMetricAttunement".Translate(), AbyssalForgeProgressUtility.GetAttunementMetricLabel(attunementTier));
            AbyssalForgeConsoleArt.DrawMetric(powerRect, "ABY_ForgeMetricPower".Translate(), forge.IsPowerActive ? "ABY_ForgePowerOnlineShort".Translate() : "ABY_ForgePowerOfflineShort".Translate());

            TooltipHandler.TipRegion(new Rect(attunementBarRect.x, attunementBarRect.y, attunementBarRect.width, attunementBarRect.height + 88f), AbyssalForgeProgressUtility.GetAttunementTooltip(attunementTier, progress.TotalResidueOffered, progress.HasPoweredForge()));

            if (progress.HasRecentUnlocks)
            {
                Rect tagRect = new Rect(inner.xMax - 78f, inner.y + 2f, 68f, 18f);
                AbyssalForgeConsoleArt.DrawTag(tagRect, "ABY_ForgePatternNew".Translate(), true);
            }
        }

        private void DrawOfferPanel(Rect rect, MapComponent_AbyssalForgeProgress progress)
        {
            AbyssalForgeConsoleArt.DrawPanel(rect, true);
            Rect inner = rect.ContractedBy(12f);
            int availableResidue = progress.CountAvailableResidue();

            AbyssalForgeConsoleArt.DrawSectionTitle(new Rect(inner.x, inner.y, inner.width, 22f), "ABY_ForgeOfferHeader".Translate());

            bool enabled = availableResidue > 0;

            if (AbyssalStyledWidgets.TextButton(new Rect(inner.x, inner.y + 30f, inner.width, 30f), "ABY_ForgeOfferAmount".Translate(10), enabled))
            {
                TryOfferResidue(10);
            }

            if (AbyssalStyledWidgets.TextButton(new Rect(inner.x, inner.y + 66f, inner.width, 30f), "ABY_ForgeOfferAmount".Translate(50), enabled))
            {
                TryOfferResidue(50);
            }

            if (AbyssalStyledWidgets.TextButton(new Rect(inner.x, inner.y + 102f, inner.width, 32f), "ABY_ForgeOfferAll".Translate(availableResidue), enabled))
            {
                TryOfferResidue(availableResidue);
            }

            GUI.color = AbyssalForgeConsoleArt.TextDimColor;
            Widgets.Label(new Rect(inner.x, inner.y + 142f, inner.width, inner.height - 142f), enabled ? "ABY_ForgeOfferHintShort".Translate() : "ABY_ForgeOfferNoneAvailable".Translate());
            GUI.color = Color.white;
        }

        private void DrawNextPanel(Rect rect, MapComponent_AbyssalForgeProgress progress)
        {
            AbyssalForgeConsoleArt.DrawPanel(rect, false);
            Rect inner = rect.ContractedBy(12f);
            AbyssalForgeConsoleArt.DrawSectionTitle(new Rect(inner.x, inner.y, inner.width, 22f), "ABY_ForgeNextHeader".Translate());

            List<AbyssalForgeProgressUtility.MilestoneEntry> milestones = AbyssalForgeProgressUtility.GetMilestoneEntries(progress, selectedCategory);
            float leftWidth = inner.width * 0.58f;
            GUI.color = AbyssalForgeConsoleArt.TextDimColor;
            Widgets.Label(new Rect(inner.x, inner.y + 30f, leftWidth, 20f), "ABY_ForgeMilestonesHeader".Translate());
            GUI.color = Color.white;

            float lineY = inner.y + 54f;
            for (int i = 0; i < milestones.Count; i++)
            {
                AbyssalForgeProgressUtility.MilestoneEntry entry = milestones[i];
                GUI.color = entry.satisfied ? new Color(0.72f, 1f, 0.74f, 1f) : Color.white;
                Text.Font = GameFont.Tiny;
                float height = Text.CalcHeight(entry.label + ": " + entry.value, leftWidth);
                Widgets.Label(new Rect(inner.x, lineY, leftWidth, height), entry.label + ": " + entry.value);
                lineY += height + 8f;
            }
            Text.Font = GameFont.Small;
            GUI.color = Color.white;

            Rect rightRect = new Rect(inner.x + leftWidth + 18f, inner.y + 28f, inner.width - leftWidth - 18f, inner.height - 28f);
            string categoryLabel = AbyssalForgeProgressUtility.GetCategoryLabel(selectedCategory);
            List<RecipeDef> unlocked = progress.GetUnlockedRecipes(selectedCategory);
            List<RecipeDef> lockedAll = progress.GetLockedRecipes(selectedCategory);
            string summary = "ABY_ForgeUnlockedSummary".Translate(unlocked.Count, unlocked.Count + lockedAll.Count, categoryLabel);

            Rect summaryRect = new Rect(rightRect.x, rightRect.y, rightRect.width, 52f);
            Widgets.Label(summaryRect, summary);

            GUI.color = AbyssalForgeConsoleArt.TextDimColor;
            Widgets.Label(new Rect(rightRect.x, rightRect.y + 58f, rightRect.width, 18f), "ABY_ForgeUpcomingPatterns".Translate());
            GUI.color = Color.white;

            List<RecipeDef> locked = progress.GetLockedRecipes(selectedCategory).Take(2).ToList();
            Text.Font = GameFont.Tiny;
            if (locked.Count == 0)
            {
                GUI.color = AbyssalForgeConsoleArt.TextDimColor;
                Widgets.Label(new Rect(rightRect.x, rightRect.y + 76f, rightRect.width, 34f), "ABY_ForgeAllPatternsUnlocked".Translate());
            }
            else
            {
                for (int i = 0; i < locked.Count; i++)
                {
                    RecipeDef recipe = locked[i];
                    string line = "• " + AbyssalForgeProgressUtility.GetRequiredResidue(recipe) + " — " + AbyssalForgeProgressUtility.GetRecipeDisplayLabel(recipe);
                    Widgets.Label(new Rect(rightRect.x, rightRect.y + 76f + i * 24f, rightRect.width, 22f), line);
                }
            }
            Text.Font = GameFont.Small;
            GUI.color = AbyssalForgeConsoleArt.TextDimColor;
            Widgets.Label(new Rect(rightRect.x, rightRect.y + 126f, rightRect.width, 28f), "ABY_ForgeReducedEffectsDesc".Translate());
            GUI.color = Color.white;

            bool reduced = progress.ReducedVisualEffects;
            bool newReduced = reduced;
            Rect checkboxRect = new Rect(rightRect.x, rightRect.y + 156f, Mathf.Min(186f, rightRect.width), 24f);
            Widgets.CheckboxLabeled(checkboxRect, "ABY_ForgeReducedEffectsToggle".Translate(), ref newReduced, false, null, null, false);
            if (newReduced != reduced)
            {
                progress.SetReducedVisualEffects(newReduced);
                SoundDefOf.Tick_Tiny.PlayOneShotOnCamera(null);
            }
            TooltipHandler.TipRegion(checkboxRect, "ABY_ForgeReducedEffectsDesc".Translate());
        }

        private void DrawCategoryRow(Rect rect)
        {
            AbyssalForgeConsoleArt.DrawPanel(rect, false);
            Rect inner = rect.ContractedBy(4f);
            List<string> categories = AbyssalForgeProgressUtility.Categories.ToList();
            float width = inner.width / categories.Count;

            for (int i = 0; i < categories.Count; i++)
            {
                string category = categories[i];
                Rect buttonRect = new Rect(inner.x + width * i, inner.y, width - 4f, inner.height);
                if (AbyssalStyledWidgets.TabButton(buttonRect, AbyssalForgeProgressUtility.GetCategoryLabel(category), AbyssalForgeConsoleArt.GetCategoryIcon(category), category == selectedCategory))
                {
                    selectedCategory = category;
                    SoundDefOf.Tick_Tiny.PlayOneShotOnCamera(null);
                }
            }
        }

        private void DrawPatternBrowser(Rect rect, MapComponent_AbyssalForgeProgress progress)
        {
            AbyssalForgeConsoleArt.DrawPanel(rect, false);
            Rect inner = rect.ContractedBy(10f);
            AbyssalForgeConsoleArt.DrawSectionTitle(new Rect(inner.x, inner.y, inner.width, 22f), "ABY_ForgeConsolePatternsHeader".Translate());

            List<RecipeDef> recipes = AbyssalForgeProgressUtility.GetForgeRecipes()
                .Where(recipe => AbyssalForgeProgressUtility.RecipeMatchesCategory(recipe, selectedCategory))
                .ToList();

            Rect outRect = new Rect(inner.x, inner.y + 28f, inner.width, inner.height - 28f);
            const float scrollbarReserve = 18f;
            float contentWidth = Mathf.Max(120f, outRect.width - scrollbarReserve);
            float cardWidth = (contentWidth - 12f) / 2f;
            float cardHeight = 180f;
            int rows = Mathf.CeilToInt(recipes.Count / 2f);
            float viewHeight = Math.Max(outRect.height, rows * (cardHeight + 8f));
            Rect viewRect = new Rect(0f, 0f, contentWidth, viewHeight);

            Widgets.BeginScrollView(outRect, ref patternScrollPosition, viewRect, true);
            for (int i = 0; i < recipes.Count; i++)
            {
                int column = i % 2;
                int row = i / 2;
                Rect cardRect = new Rect(column * (cardWidth + 12f), row * (cardHeight + 8f), cardWidth, cardHeight);
                RecipeDef recipe = recipes[i];
                bool unlocked = AbyssalForgeProgressUtility.IsRecipeUnlocked(recipe, progress.TotalResidueOffered);
                bool freshlyUnlocked = progress.IsRecentlyUnlocked(recipe);
                DrawPatternCard(cardRect, recipe, unlocked, freshlyUnlocked);
            }
            Widgets.EndScrollView();
        }

        private void DrawPatternCard(Rect rect, RecipeDef recipe, bool unlocked, bool freshlyUnlocked)
        {
            AbyssalForgeConsoleArt.DrawPanel(rect, unlocked);
            AbyssalForgeConsoleArt.DrawPatternCardPulse(rect, unlocked, freshlyUnlocked);

            ThingDef product = AbyssalForgeProgressUtility.GetPrimaryProduct(recipe);
            Texture2D icon = product != null ? product.uiIcon : null;
            Rect iconRect = new Rect(rect.x + 10f, rect.y + 12f, 42f, 42f);
            if (icon != null)
            {
                GUI.color = unlocked ? Color.white : new Color(0.72f, 0.72f, 0.72f, 0.72f);
                GUI.DrawTexture(iconRect, icon, ScaleMode.ScaleToFit, true);
                GUI.color = Color.white;
            }
            else
            {
                Texture2D categoryIcon = AbyssalForgeConsoleArt.GetCategoryIcon(AbyssalForgeProgressUtility.GetCategory(recipe));
                if (categoryIcon != null)
                {
                    GUI.color = unlocked ? Color.white : new Color(0.72f, 0.72f, 0.72f, 0.72f);
                    GUI.DrawTexture(iconRect, categoryIcon, ScaleMode.ScaleToFit, true);
                    GUI.color = Color.white;
                }
            }

            if (freshlyUnlocked)
            {
                Rect newRect = new Rect(rect.xMax - 54f, rect.y + 10f, 44f, 18f);
                AbyssalForgeConsoleArt.DrawTag(newRect, "ABY_ForgePatternNew".Translate(), true);
            }

            Def infoDef = (Def)product ?? recipe;
            Rect infoRect = new Rect(rect.xMax - 82f, rect.y + 10f, 24f, 24f);
            if (infoDef != null)
            {
                Widgets.InfoCardButton(infoRect.x, infoRect.y, infoDef);
                TooltipHandler.TipRegion(infoRect, "ABY_ForgePatternOpenInfo".Translate());
            }

            Rect labelRect = new Rect(rect.x + 60f, rect.y + 10f, rect.width - 154f, 22f);
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;
            Widgets.Label(labelRect, AbyssalForgeProgressUtility.GetRecipeDisplayLabel(recipe));

            GUI.color = AbyssalForgeConsoleArt.TextDimColor;
            Widgets.Label(new Rect(rect.x + 60f, rect.y + 31f, rect.width - 100f, 18f), AbyssalForgeProgressUtility.GetCategoryLabel(AbyssalForgeProgressUtility.GetCategory(recipe)));

            int primaryProductCount = AbyssalForgeProgressUtility.GetPrimaryProductCount(recipe);
            if (primaryProductCount > 1)
            {
                Widgets.Label(new Rect(rect.x + 60f, rect.y + 48f, rect.width - 100f, 18f), "ABY_ForgePatternOutputCount".Translate(primaryProductCount));
            }
            GUI.color = Color.white;

            string unlockLine = unlocked
                ? "ABY_ForgePatternUnlockedAt".Translate(AbyssalForgeProgressUtility.GetRequiredResidue(recipe))
                : "ABY_ForgePatternLockedAt".Translate(AbyssalForgeProgressUtility.GetRequiredResidue(recipe));
            GUI.color = unlocked ? new Color(1f, 0.78f, 0.58f, 1f) : new Color(0.92f, 0.52f, 0.45f, 1f);
            Widgets.Label(new Rect(rect.x + 10f, rect.y + 66f, rect.width - 20f, 18f), unlockLine);
            GUI.color = Color.white;

            GUI.color = AbyssalForgeConsoleArt.TextDimColor;
            Widgets.Label(new Rect(rect.x + 10f, rect.y + 86f, rect.width - 20f, 18f), "ABY_ForgePatternRequirementsState".Translate());
            GUI.color = Color.white;

            List<AbyssalForgeProgressUtility.IngredientAvailabilityEntry> entries = AbyssalForgeProgressUtility.GetIngredientAvailabilityEntries(forge.Map, recipe);
            int shownEntries = Math.Min(2, entries.Count);
            for (int i = 0; i < shownEntries; i++)
            {
                DrawIngredientStateLine(new Rect(rect.x + 10f, rect.y + 104f + i * 18f, rect.width - 20f, 18f), entries[i]);
            }

            if (entries.Count > shownEntries)
            {
                GUI.color = AbyssalForgeConsoleArt.TextDimColor;
                Widgets.Label(new Rect(rect.x + 10f, rect.y + 104f + shownEntries * 18f, rect.width - 20f, 18f), "ABY_ForgePatternMoreRequirements".Translate(entries.Count - shownEntries));
                GUI.color = Color.white;
            }

            bool hasAllMaterials = entries.All(entry => entry.IsSatisfied);
            bool recipeAvailable = recipe.AvailableNow && recipe.AvailableOnNow(forge);
            string actionLabel;
            if (!unlocked)
            {
                actionLabel = "ABY_ForgePatternLocked".Translate();
            }
            else if (recipeAvailable)
            {
                actionLabel = "ABY_ForgePatternAddBill".Translate();
            }
            else if (!hasAllMaterials)
            {
                actionLabel = "ABY_ForgePatternMissingMaterials".Translate();
            }
            else
            {
                actionLabel = "ABY_ForgePatternResearchRequired".Translate();
            }

            Rect buttonRect = new Rect(rect.x + rect.width - 120f, rect.y + rect.height - 34f, 108f, 28f);
            if (unlocked && recipeAvailable)
            {
                if (AbyssalStyledWidgets.TextButton(buttonRect, actionLabel))
                {
                    AddBill(recipe);
                }
            }
            else
            {
                AbyssalStyledWidgets.TextButton(buttonRect, actionLabel, false);
            }

            string description = product != null && !product.description.NullOrEmpty() ? product.description : recipe.description;
            string tooltip = description;
            string costBlock = AbyssalForgeProgressUtility.GetRecipeIngredientTooltip(recipe);
            string stateBlock = AbyssalForgeProgressUtility.GetRecipeAvailabilityTooltip(forge.Map, recipe);
            if (!costBlock.NullOrEmpty())
            {
                if (!tooltip.NullOrEmpty())
                {
                    tooltip += "\n\n";
                }
                tooltip += "ABY_ForgePatternRequirementsLabel".Translate() + "\n" + costBlock;
            }
            if (!stateBlock.NullOrEmpty())
            {
                if (!tooltip.NullOrEmpty())
                {
                    tooltip += "\n\n";
                }
                tooltip += "ABY_ForgePatternRequirementsState".Translate() + "\n" + stateBlock;
            }
            if (freshlyUnlocked)
            {
                if (!tooltip.NullOrEmpty())
                {
                    tooltip += "\n\n";
                }
                tooltip += "ABY_ForgeUnlockToast".Translate(AbyssalForgeProgressUtility.GetRecipeDisplayLabel(recipe));
            }

            if (!tooltip.NullOrEmpty())
            {
                TooltipHandler.TipRegion(rect, tooltip);
            }
        }

        private void DrawIngredientStateLine(Rect rect, AbyssalForgeProgressUtility.IngredientAvailabilityEntry entry)
        {
            Rect labelRect = new Rect(rect.x, rect.y, rect.width - 72f, rect.height);
            Rect countRect = new Rect(rect.xMax - 70f, rect.y, 70f, rect.height);

            GUI.color = Color.white;
            Widgets.Label(labelRect, entry.label);
            GUI.color = entry.IsSatisfied ? new Color(0.72f, 1f, 0.74f, 1f) : new Color(1f, 0.58f, 0.52f, 1f);
            Text.Anchor = TextAnchor.UpperRight;
            Widgets.Label(countRect, entry.availableCount + "/" + entry.requiredCount);
            Text.Anchor = TextAnchor.UpperLeft;
            GUI.color = Color.white;
        }

        private void DrawBillsPanel(Rect rect)
        {
            AbyssalForgeConsoleArt.DrawPanel(rect, false);
            Rect inner = rect.ContractedBy(10f);
            AbyssalForgeConsoleArt.DrawSectionTitle(new Rect(inner.x, inner.y, inner.width, 22f), "ABY_ForgeConsoleBillsHeader".Translate());

            Rect pasteRect = new Rect(inner.xMax - 24f, inner.y, 24f, 24f);
            DrawPasteButton(pasteRect);

            Rect listRect = new Rect(inner.x, inner.y + 28f, inner.width, inner.height - 28f);
            mouseoverBill = forge.BillStack.DoListing(listRect, BuildRecipeOptions, ref billScrollPosition, ref billViewHeight);
        }

        private List<FloatMenuOption> BuildRecipeOptions()
        {
            List<FloatMenuOption> options = new List<FloatMenuOption>();
            List<RecipeDef> availableRecipes = forge.ProgressComponent.GetUnlockedRecipes(selectedCategory);

            for (int i = 0; i < availableRecipes.Count; i++)
            {
                RecipeDef recipe = availableRecipes[i];
                if (!recipe.AvailableNow || !recipe.AvailableOnNow(forge))
                {
                    continue;
                }

                RecipeDef capturedRecipe = recipe;
                options.Add(new FloatMenuOption(capturedRecipe.LabelCap, delegate
                {
                    AddBill(capturedRecipe);
                }));
            }

            if (options.Count == 0)
            {
                options.Add(new FloatMenuOption("ABY_ForgeNoUnlockedRecipes".Translate(), null));
            }

            return options;
        }

        private void AddBill(RecipeDef recipe)
        {
            if (recipe == null)
            {
                return;
            }

            if (!forge.Map.mapPawns.FreeColonists.Any(colonist => recipe.PawnSatisfiesSkillRequirements(colonist)))
            {
                Bill.CreateNoPawnsWithSkillDialog(recipe);
            }

            Bill bill = recipe.MakeNewBill();
            forge.BillStack.AddBill(bill);

            if (recipe.conceptLearned != null)
            {
                PlayerKnowledgeDatabase.KnowledgeDemonstrated(recipe.conceptLearned, KnowledgeAmount.Total);
            }

            SoundDefOf.Tick_High.PlayOneShotOnCamera(null);
        }

        private void DrawPasteButton(Rect rect)
        {
            if (BillUtility.Clipboard == null)
            {
                AbyssalStyledWidgets.IconButton(rect, TexButton.Paste, false, false, "PasteBillTip".Translate());
                return;
            }

            RecipeDef clipboardRecipe = BillUtility.Clipboard.recipe;
            if (!CanUseRecipe(clipboardRecipe))
            {
                AbyssalStyledWidgets.IconButton(rect, TexButton.Paste, false, false, "ABY_ForgeClipboardLocked".Translate());
                return;
            }

            if (forge.BillStack.Count >= 15)
            {
                AbyssalStyledWidgets.IconButton(rect, TexButton.Paste, false, false, "PasteBillTip".Translate() + " (" + "PasteBillTip_LimitReached".Translate() + ")");
                return;
            }

            if (AbyssalStyledWidgets.IconButton(rect, TexButton.Paste, true, false, "PasteBillTip".Translate()))
            {
                Bill bill = BillUtility.Clipboard.Clone();
                bill.InitializeAfterClone();
                forge.BillStack.AddBill(bill);
                SoundDefOf.Tick_Low.PlayOneShotOnCamera(null);
            }
        }

        private bool CanUseRecipe(RecipeDef recipe)
        {
            return recipe != null
                && forge.def.AllRecipes.Contains(recipe)
                && forge.ProgressComponent != null
                && AbyssalForgeProgressUtility.IsRecipeUnlocked(recipe, forge.ProgressComponent.TotalResidueOffered)
                && recipe.AvailableNow
                && recipe.AvailableOnNow(forge);
        }

        private void TryOfferResidue(int requestedAmount)
        {
            int consumed = forge.OfferResidue(requestedAmount);
            if (consumed > 0)
            {
                SoundDefOf.Tick_High.PlayOneShotOnCamera(null);
            }
            else
            {
                Messages.Message("ABY_ForgeOfferNoneAvailable".Translate(), forge, MessageTypeDefOf.RejectInput, false);
            }
        }

        private int GetPreviousUnlockThreshold(MapComponent_AbyssalForgeProgress progress, string category, int total)
        {
            List<RecipeDef> unlocked = progress.GetUnlockedRecipes(category);
            int value = 0;
            for (int i = 0; i < unlocked.Count; i++)
            {
                int required = AbyssalForgeProgressUtility.GetRequiredResidue(unlocked[i]);
                if (required <= total && required > value)
                {
                    value = required;
                }
            }

            return value;
        }
    }
}
