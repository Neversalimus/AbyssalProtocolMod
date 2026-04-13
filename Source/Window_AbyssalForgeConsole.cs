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

        public override Vector2 InitialSize => new Vector2(1180f, 760f);

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

            AbyssalForgeConsoleArt.DrawBackground(inRect);

            Rect headerRect = new Rect(inRect.x, inRect.y, inRect.width, 66f);
            Rect statusRect = new Rect(inRect.x, headerRect.yMax + 10f, 430f, 164f);
            Rect offerRect = new Rect(statusRect.xMax + 10f, headerRect.yMax + 10f, 250f, 164f);
            Rect nextRect = new Rect(offerRect.xMax + 10f, headerRect.yMax + 10f, inRect.width - offerRect.xMax - 10f, 164f);
            Rect categoryRect = new Rect(inRect.x, statusRect.yMax + 10f, inRect.width, 40f);
            Rect patternsRect = new Rect(inRect.x, categoryRect.yMax + 10f, 650f, inRect.height - categoryRect.yMax - 10f);
            Rect billsRect = new Rect(patternsRect.xMax + 10f, categoryRect.yMax + 10f, inRect.width - patternsRect.width - 10f, inRect.height - categoryRect.yMax - 10f);

            DrawHeader(headerRect);
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

        private void DrawHeader(Rect rect)
        {
            AbyssalForgeConsoleArt.DrawHeader(
                rect,
                "ABY_ForgeConsoleTitle".Translate(),
                "ABY_ForgeConsoleSubtitle".Translate());
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
            AbyssalForgeConsoleArt.DrawProgressBar(new Rect(inner.x, inner.y + 26f, inner.width, 22f), fill, progressLabel);

            float metricY = inner.y + 58f;
            float metricWidth = (inner.width - 10f) / 2f;

            AbyssalForgeConsoleArt.DrawMetric(
                new Rect(inner.x, metricY, metricWidth, 44f),
                "ABY_ForgeMetricResidue".Translate(),
                progress.TotalResidueOffered.ToString());
            AbyssalForgeConsoleArt.DrawMetric(
                new Rect(inner.x + metricWidth + 10f, metricY, metricWidth, 44f),
                "ABY_ForgeMetricAvailable".Translate(),
                progress.CountAvailableResidue().ToString());
            AbyssalForgeConsoleArt.DrawMetric(
                new Rect(inner.x, metricY + 48f, metricWidth, 44f),
                "ABY_ForgeMetricAttunement".Translate(),
                ("ABY_AttunementTier_" + progress.GetCurrentAttunementTier(false)).Translate());
            AbyssalForgeConsoleArt.DrawMetric(
                new Rect(inner.x + metricWidth + 10f, metricY + 48f, metricWidth, 44f),
                "ABY_ForgeMetricPower".Translate(),
                forge.IsPowerActive ? "ABY_ForgePowerOnlineShort".Translate() : "ABY_ForgePowerOfflineShort".Translate());
        }

        private void DrawOfferPanel(Rect rect, MapComponent_AbyssalForgeProgress progress)
        {
            AbyssalForgeConsoleArt.DrawPanel(rect, true);
            Rect inner = rect.ContractedBy(12f);
            int availableResidue = progress.CountAvailableResidue();

            AbyssalForgeConsoleArt.DrawSectionTitle(new Rect(inner.x, inner.y, inner.width, 22f), "ABY_ForgeOfferHeader".Translate());

            bool enabled = availableResidue > 0;
            bool oldEnabled = GUI.enabled;
            GUI.enabled = enabled;

            if (Widgets.ButtonText(new Rect(inner.x, inner.y + 30f, inner.width, 30f), "ABY_ForgeOfferAmount".Translate(10)))
            {
                TryOfferResidue(10);
            }

            if (Widgets.ButtonText(new Rect(inner.x, inner.y + 66f, inner.width, 30f), "ABY_ForgeOfferAmount".Translate(50)))
            {
                TryOfferResidue(50);
            }

            if (Widgets.ButtonText(new Rect(inner.x, inner.y + 102f, inner.width, 30f), "ABY_ForgeOfferAll".Translate(availableResidue)))
            {
                TryOfferResidue(availableResidue);
            }

            GUI.enabled = oldEnabled;

            GUI.color = AbyssalForgeConsoleArt.TextDimColor;
            Widgets.Label(new Rect(inner.x, inner.y + 136f, inner.width, 20f), enabled ? "ABY_ForgeOfferHintShort".Translate() : "ABY_ForgeOfferNoneAvailable".Translate());
            GUI.color = Color.white;
        }

        private void DrawNextPanel(Rect rect, MapComponent_AbyssalForgeProgress progress)
        {
            AbyssalForgeConsoleArt.DrawPanel(rect, false);
            Rect inner = rect.ContractedBy(12f);
            AbyssalForgeConsoleArt.DrawSectionTitle(new Rect(inner.x, inner.y, inner.width, 22f), "ABY_ForgeNextHeader".Translate());

            List<RecipeDef> recipes = AbyssalForgeProgressUtility.GetForgeRecipes()
                .Where(recipe => AbyssalForgeProgressUtility.RecipeMatchesCategory(recipe, selectedCategory))
                .Take(3)
                .ToList();
            List<RecipeDef> locked = progress.GetLockedRecipes(selectedCategory).Take(3).ToList();

            float leftWidth = inner.width * 0.46f;
            GUI.color = AbyssalForgeConsoleArt.TextDimColor;
            Widgets.Label(new Rect(inner.x, inner.y + 30f, leftWidth, 20f), "ABY_ForgeUpcomingPatterns".Translate());
            GUI.color = Color.white;

            if (locked.Count == 0)
            {
                Widgets.Label(new Rect(inner.x, inner.y + 54f, leftWidth, inner.height - 54f), "ABY_ForgeAllPatternsUnlocked".Translate());
            }
            else
            {
                for (int i = 0; i < locked.Count; i++)
                {
                    RecipeDef recipe = locked[i];
                    Widgets.Label(
                        new Rect(inner.x, inner.y + 52f + i * 28f, leftWidth, 26f),
                        "• " + AbyssalForgeProgressUtility.GetRequiredResidue(recipe) + " — " + AbyssalForgeProgressUtility.GetRecipeDisplayLabel(recipe));
                }
            }

            Rect rightRect = new Rect(inner.x + leftWidth + 14f, inner.y + 28f, inner.width - leftWidth - 14f, inner.height - 28f);
            string categoryLabel = AbyssalForgeProgressUtility.GetCategoryLabel(selectedCategory);
            string summary = "ABY_ForgeUnlockedSummary".Translate(
                progress.GetUnlockedRecipes(selectedCategory).Count,
                progress.GetUnlockedRecipes(selectedCategory).Count + progress.GetLockedRecipes(selectedCategory).Count,
                categoryLabel);
            Widgets.Label(rightRect, summary + "\n\n" + "ABY_ForgePreviewHint".Translate());
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
                AbyssalForgeConsoleArt.DrawCategoryButton(buttonRect, category, category == selectedCategory);
                if (Widgets.ButtonInvisible(buttonRect))
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
            float cardWidth = (outRect.width - 12f) / 2f;
            float cardHeight = 96f;
            int rows = Mathf.CeilToInt(recipes.Count / 2f);
            float viewHeight = Math.Max(outRect.height, rows * (cardHeight + 8f));
            Rect viewRect = new Rect(0f, 0f, outRect.width - 16f, viewHeight);

            Widgets.BeginScrollView(outRect, ref patternScrollPosition, viewRect, true);
            for (int i = 0; i < recipes.Count; i++)
            {
                int column = i % 2;
                int row = i / 2;
                Rect cardRect = new Rect(column * (cardWidth + 12f), row * (cardHeight + 8f), cardWidth, cardHeight);
                bool unlocked = AbyssalForgeProgressUtility.IsRecipeUnlocked(recipes[i], progress.TotalResidueOffered);
                DrawPatternCard(cardRect, recipes[i], unlocked);
            }
            Widgets.EndScrollView();
        }

        private void DrawPatternCard(Rect rect, RecipeDef recipe, bool unlocked)
        {
            AbyssalForgeConsoleArt.DrawPanel(rect, unlocked);

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

            Rect labelRect = new Rect(rect.x + 60f, rect.y + 10f, rect.width - 70f, 22f);
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;
            Widgets.Label(labelRect, AbyssalForgeProgressUtility.GetRecipeDisplayLabel(recipe));

            GUI.color = AbyssalForgeConsoleArt.TextDimColor;
            Widgets.Label(new Rect(rect.x + 60f, rect.y + 32f, rect.width - 70f, 20f), AbyssalForgeProgressUtility.GetCategoryLabel(AbyssalForgeProgressUtility.GetCategory(recipe)));
            GUI.color = Color.white;

            string unlockLine = unlocked
                ? "ABY_ForgePatternUnlockedAt".Translate(AbyssalForgeProgressUtility.GetRequiredResidue(recipe))
                : "ABY_ForgePatternLockedAt".Translate(AbyssalForgeProgressUtility.GetRequiredResidue(recipe));
            GUI.color = unlocked ? new Color(1f, 0.78f, 0.58f, 1f) : new Color(0.92f, 0.52f, 0.45f, 1f);
            Widgets.Label(new Rect(rect.x + 10f, rect.y + 58f, rect.width - 20f, 20f), unlockLine);
            GUI.color = Color.white;

            Rect buttonRect = new Rect(rect.x + rect.width - 112f, rect.y + rect.height - 32f, 100f, 24f);
            if (unlocked && recipe.AvailableNow && recipe.AvailableOnNow(forge))
            {
                if (Widgets.ButtonText(buttonRect, "ABY_ForgePatternAddBill".Translate()))
                {
                    AddBill(recipe);
                }
            }
            else
            {
                GUI.color = Color.gray;
                Widgets.ButtonText(buttonRect, "ABY_ForgePatternLocked".Translate());
                GUI.color = Color.white;
            }

            string description = product != null && !product.description.NullOrEmpty() ? product.description : recipe.description;
            if (!description.NullOrEmpty())
            {
                TooltipHandler.TipRegion(rect, description);
            }
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

            if (forge.BillStack.Count >= 15)
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
                forge.BillStack.AddBill(bill);
                SoundDefOf.Tick_Low.PlayOneShotOnCamera(null);
            }

            TooltipHandler.TipRegionByKey(rect, "PasteBillTip");
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
