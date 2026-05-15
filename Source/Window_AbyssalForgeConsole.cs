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
        private string selectedTurretSystemsFilter = TurretFilterAll;

        private const string TurretFilterAll = "All";
        private const string TurretFilterMain = "Main";
        private const string TurretFilterAuxiliary = "Auxiliary";
        private const string TurretFilterPassive = "Passive";

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

        public override void PostClose()
        {
            base.PostClose();

            if (forge?.ProgressComponent != null)
            {
                forge.ProgressComponent.ConsumeRecentUnlocks();
            }
        }

        public override void DoWindowContents(Rect inRect)
        {
            try
            {
                DoWindowContentsSafe(inRect);
            }
            catch (System.Exception ex)
            {
                ABY_UISafetyUtility.DrawWindowFallback(inRect, "Abyssal Forge Console", ex);
            }
        }

        private void DoWindowContentsSafe(Rect inRect)
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
            Rect rightColumnRect = new Rect(patternsRect.xMax + 10f, categoryRect.yMax + 10f, inRect.width - patternsRect.width - 10f, inRect.height - categoryRect.yMax - 10f);
            Rect infrastructureRect = new Rect(rightColumnRect.x, rightColumnRect.y, rightColumnRect.width, 190f);
            Rect billsRect = new Rect(rightColumnRect.x, infrastructureRect.yMax + 10f, rightColumnRect.width, rightColumnRect.height - infrastructureRect.height - 10f);

            DrawHeader(headerRect, progress);
            DrawStatusPanel(statusRect, progress);
            DrawOfferPanel(offerRect, progress);
            DrawNextPanel(nextRect, progress);
            DrawCategoryRow(categoryRect);
            DrawPatternBrowser(patternsRect, progress);
            ABY_ForgeCrucibleInfrastructureCard.Draw(infrastructureRect, forge);
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
            ABY_UIPolishUtility.SafeLabel(new Rect(inner.x, inner.y + 142f, inner.width, inner.height - 142f), enabled ? "ABY_ForgeOfferHintShort".Translate() : "ABY_ForgeOfferNoneAvailable".Translate());
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
            ABY_UIPolishUtility.SafeLabel(new Rect(inner.x, inner.y + 30f, leftWidth, 20f), "ABY_ForgeMilestonesHeader".Translate());
            GUI.color = Color.white;

            float lineY = inner.y + 54f;
            for (int i = 0; i < milestones.Count; i++)
            {
                AbyssalForgeProgressUtility.MilestoneEntry entry = milestones[i];
                GUI.color = entry.satisfied ? new Color(0.72f, 1f, 0.74f, 1f) : Color.white;
                Text.Font = GameFont.Tiny;
                float height = Text.CalcHeight(entry.label + ": " + entry.value, leftWidth);
                ABY_UIPolishUtility.SafeLabel(new Rect(inner.x, lineY, leftWidth, height), entry.label + ": " + entry.value);
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
            ABY_UIPolishUtility.SafeLabel(summaryRect, summary);

            GUI.color = AbyssalForgeConsoleArt.TextDimColor;
            ABY_UIPolishUtility.SafeLabel(new Rect(rightRect.x, rightRect.y + 58f, rightRect.width, 18f), "ABY_ForgeUpcomingPatterns".Translate());
            GUI.color = Color.white;

            List<RecipeDef> locked = progress.GetLockedRecipes(selectedCategory).Take(2).ToList();
            Text.Font = GameFont.Tiny;
            if (locked.Count == 0)
            {
                GUI.color = AbyssalForgeConsoleArt.TextDimColor;
                ABY_UIPolishUtility.SafeLabel(new Rect(rightRect.x, rightRect.y + 76f, rightRect.width, 34f), "ABY_ForgeAllPatternsUnlocked".Translate());
            }
            else
            {
                for (int i = 0; i < locked.Count; i++)
                {
                    RecipeDef recipe = locked[i];
                    string line = "• " + AbyssalForgeProgressUtility.GetRequiredResidue(recipe) + " — " + AbyssalForgeProgressUtility.GetRecipeDisplayLabel(recipe);
                    ABY_UIPolishUtility.SafeLabel(new Rect(rightRect.x, rightRect.y + 76f + i * 24f, rightRect.width, 22f), line);
                }
            }

            Text.Font = GameFont.Small;
            GUI.color = Color.white;

            bool reduced = progress.ReducedVisualEffects;
            bool newReduced = reduced;
            Rect checkboxRect = new Rect(rightRect.x, rightRect.y + 126f, Mathf.Min(220f, rightRect.width), 24f);
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
                    if (selectedCategory != category)
                    {
                        patternScrollPosition = Vector2.zero;
                    }

                    selectedCategory = category;
                    if (selectedCategory == AbyssalForgeProgressUtility.TurretSystemsCategory && selectedTurretSystemsFilter.NullOrEmpty())
                    {
                        selectedTurretSystemsFilter = TurretFilterAll;
                    }

                    SoundDefOf.Tick_Tiny.PlayOneShotOnCamera(null);
                }
            }
        }

        private void DrawPatternBrowser(Rect rect, MapComponent_AbyssalForgeProgress progress)
        {
            AbyssalForgeConsoleArt.DrawPanel(rect, false);
            Rect inner = rect.ContractedBy(10f);
            AbyssalForgeConsoleArt.DrawSectionTitle(new Rect(inner.x, inner.y, inner.width, 22f), "ABY_ForgeConsolePatternsHeader".Translate());

            bool turretSystemsMode = selectedCategory == AbyssalForgeProgressUtility.TurretSystemsCategory;
            List<RecipeDef> recipes = AbyssalForgeProgressUtility.GetForgeRecipes()
                .Where(recipe => AbyssalForgeProgressUtility.RecipeMatchesCategory(recipe, selectedCategory))
                .Where(recipe => !turretSystemsMode || TurretRecipeMatchesFilter(recipe, selectedTurretSystemsFilter))
                .OrderBy(recipe => turretSystemsMode ? GetTurretSystemRecipeOrder(recipe) : AbyssalForgeProgressUtility.GetCategoryOrderIndex(AbyssalForgeProgressUtility.GetCategory(recipe)))
                .ThenBy(AbyssalForgeProgressUtility.GetRequiredResidue)
                .ThenBy(AbyssalForgeProgressUtility.GetRecipeDisplayLabel)
                .ToList();

            float contentTop = inner.y + 28f;
            if (turretSystemsMode)
            {
                Rect filterRect = new Rect(inner.x, contentTop, inner.width, 30f);
                DrawTurretSystemsFilterRow(filterRect);
                contentTop += 38f;
            }

            Rect outRect = new Rect(inner.x, contentTop, inner.width, inner.yMax - contentTop);
            const float scrollbarReserve = 18f;
            float contentWidth = Mathf.Max(120f, outRect.width - scrollbarReserve);
            float cardWidth = (contentWidth - 12f) / 2f;
            float cardHeight = turretSystemsMode ? 196f : 180f;
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
                if (turretSystemsMode && IsTurretSystemRecipe(recipe))
                {
                    DrawTurretSystemPatternCard(cardRect, recipe, unlocked, freshlyUnlocked);
                }
                else
                {
                    DrawPatternCard(cardRect, recipe, unlocked, freshlyUnlocked);
                }
            }
            Widgets.EndScrollView();
        }

        private static bool IsTurretSystemRecipe(RecipeDef recipe)
        {
            ThingDef product = AbyssalForgeProgressUtility.GetPrimaryProduct(recipe);
            return ABY_ModularTurretUtility.GetModuleForThingDef(product) != null
                || product?.GetCompProperties<CompProperties_AbyssalModularTurret>() != null
                || AbyssalForgeProgressUtility.GetCategory(recipe) == AbyssalForgeProgressUtility.TurretSystemsCategory;
        }

        private static int GetTurretSystemRecipeOrder(RecipeDef recipe)
        {
            ThingDef product = AbyssalForgeProgressUtility.GetPrimaryProduct(recipe);
            if (product?.GetCompProperties<CompProperties_AbyssalModularTurret>() != null)
            {
                return 0;
            }

            ABY_TurretModuleDef module = ABY_ModularTurretUtility.GetModuleForThingDef(product);
            if (module == null)
            {
                return 50;
            }

            switch (module.slot)
            {
                case ABY_TurretModuleSlot.MainWeapon:
                    return 10;
                case ABY_TurretModuleSlot.Auxiliary:
                    return 20;
                case ABY_TurretModuleSlot.Passive:
                    return 30;
                default:
                    return 40;
            }
        }

        private void DrawTurretSystemsFilterRow(Rect rect)
        {
            AbyssalForgeConsoleArt.Fill(rect, new Color(0.10f, 0.075f, 0.065f, 0.82f));
            AbyssalForgeConsoleArt.DrawOutline(rect, new Color(1f, 0.36f, 0.13f, 0.35f));

            float buttonHeight = 22f;
            float buttonY = rect.y + (rect.height - buttonHeight) / 2f;
            float gap = 8f;
            float allWidth = 62f;
            float mainWidth = 70f;
            float auxWidth = 62f;
            float passiveWidth = 86f;
            float totalWidth = allWidth + mainWidth + auxWidth + passiveWidth + gap * 3f;
            float x = rect.x + Mathf.Max(10f, (rect.width - totalWidth) / 2f);

            DrawTurretFilterButton(new Rect(x, buttonY, allWidth, buttonHeight), ABY_ModularTurretUtility.TranslateOrFallback("ABY_TurretFilter_All", "ALL"), new Color(0.72f, 0.66f, 0.56f, 1f), TurretFilterAll);
            x += allWidth + gap;
            DrawTurretFilterButton(new Rect(x, buttonY, mainWidth, buttonHeight), ABY_ModularTurretUtility.TranslateOrFallback("ABY_TurretSlotBadge_Main", "MAIN"), ABY_ModularTurretUtility.SlotColor(ABY_TurretModuleSlot.MainWeapon), TurretFilterMain);
            x += mainWidth + gap;
            DrawTurretFilterButton(new Rect(x, buttonY, auxWidth, buttonHeight), ABY_ModularTurretUtility.TranslateOrFallback("ABY_TurretSlotBadge_Aux", "AUX"), ABY_ModularTurretUtility.SlotColor(ABY_TurretModuleSlot.Auxiliary), TurretFilterAuxiliary);
            x += auxWidth + gap;
            DrawTurretFilterButton(new Rect(x, buttonY, passiveWidth, buttonHeight), ABY_ModularTurretUtility.TranslateOrFallback("ABY_TurretSlotBadge_Passive", "PASSIVE"), ABY_ModularTurretUtility.SlotColor(ABY_TurretModuleSlot.Passive), TurretFilterPassive);

            GUI.color = Color.white;
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;
        }

        private void DrawTurretFilterButton(Rect rect, string label, Color color, string filter)
        {
            bool active = selectedTurretSystemsFilter == filter;
            bool hovered = Mouse.IsOver(rect);
            Color fill = active
                ? new Color(color.r * 0.30f, color.g * 0.20f, color.b * 0.14f, 0.98f)
                : hovered
                    ? new Color(color.r * 0.22f, color.g * 0.15f, color.b * 0.12f, 0.96f)
                    : new Color(color.r * 0.14f, color.g * 0.10f, color.b * 0.09f, 0.92f);
            Color outline = active
                ? Color.Lerp(color, Color.white, 0.26f)
                : new Color(color.r, color.g, color.b, hovered ? 0.88f : 0.62f);

            AbyssalForgeConsoleArt.Fill(rect, fill);
            AbyssalForgeConsoleArt.DrawOutline(rect, outline);

            Text.Font = GameFont.Tiny;
            Text.Anchor = TextAnchor.MiddleCenter;
            GUI.color = active ? Color.white : AbyssalForgeConsoleArt.TextSoftColor;
            ABY_UIPolishUtility.SafeLabel(rect.ContractedBy(2f), label);

            if (Widgets.ButtonInvisible(rect, false))
            {
                if (selectedTurretSystemsFilter != filter)
                {
                    selectedTurretSystemsFilter = filter;
                    patternScrollPosition = Vector2.zero;
                    SoundDefOf.Tick_Tiny.PlayOneShotOnCamera(null);
                }
            }

            TooltipHandler.TipRegion(rect, GetTurretFilterTooltip(filter));
            Text.Anchor = TextAnchor.UpperLeft;
            GUI.color = Color.white;
        }

        private static string GetTurretFilterTooltip(string filter)
        {
            switch (filter)
            {
                case TurretFilterMain:
                    return ABY_ModularTurretUtility.TranslateOrFallback("ABY_TurretFilter_MainTooltip", "Show modular turret main weapon cores.");
                case TurretFilterAuxiliary:
                    return ABY_ModularTurretUtility.TranslateOrFallback("ABY_TurretFilter_AuxTooltip", "Show modular turret auxiliary weapon modules.");
                case TurretFilterPassive:
                    return ABY_ModularTurretUtility.TranslateOrFallback("ABY_TurretFilter_PassiveTooltip", "Show modular turret passive upgrade modules.");
                default:
                    return ABY_ModularTurretUtility.TranslateOrFallback("ABY_TurretFilter_AllTooltip", "Show all modular turret chassis and modules.");
            }
        }

        private static bool TurretRecipeMatchesFilter(RecipeDef recipe, string filter)
        {
            if (filter.NullOrEmpty() || filter == TurretFilterAll)
            {
                return true;
            }

            ThingDef product = AbyssalForgeProgressUtility.GetPrimaryProduct(recipe);
            ABY_TurretModuleDef module = ABY_ModularTurretUtility.GetModuleForThingDef(product);
            if (module == null)
            {
                return false;
            }

            switch (filter)
            {
                case TurretFilterMain:
                    return module.slot == ABY_TurretModuleSlot.MainWeapon;
                case TurretFilterAuxiliary:
                    return module.slot == ABY_TurretModuleSlot.Auxiliary;
                case TurretFilterPassive:
                    return module.slot == ABY_TurretModuleSlot.Passive;
                default:
                    return true;
            }
        }

        private void DrawTurretSystemPatternCard(Rect rect, RecipeDef recipe, bool unlocked, bool freshlyUnlocked)
        {
            if (recipe == null)
            {
                AbyssalForgeConsoleArt.DrawPanel(rect, false);
                ABY_UIPolishUtility.SafeLabel(rect.ContractedBy(10f), "Missing turret pattern");
                return;
            }

            AbyssalForgeConsoleArt.DrawPanel(rect, unlocked);
            AbyssalForgeConsoleArt.DrawPatternCardPulse(rect, unlocked, freshlyUnlocked);

            ThingDef product = AbyssalForgeProgressUtility.GetPrimaryProduct(recipe);
            ABY_TurretModuleDef module = ABY_ModularTurretUtility.GetModuleForThingDef(product);
            CompProperties_AbyssalModularTurret chassisProps = product?.GetCompProperties<CompProperties_AbyssalModularTurret>();
            bool isChassis = chassisProps != null;
            Color slotColor = module != null ? ABY_ModularTurretUtility.SlotColor(module.slot) : new Color(0.62f, 0.60f, 0.55f, 1f);

            Rect socketRect = new Rect(rect.x + 10f, rect.y + 12f, 58f, 58f);
            AbyssalForgeConsoleArt.Fill(socketRect, unlocked ? new Color(slotColor.r * 0.16f, slotColor.g * 0.11f, slotColor.b * 0.10f, 0.92f) : new Color(0.05f, 0.05f, 0.055f, 0.82f));
            AbyssalForgeConsoleArt.DrawOutline(socketRect, unlocked ? slotColor : new Color(0.40f, 0.40f, 0.42f, 0.70f));

            Texture2D icon = product != null ? product.uiIcon : AbyssalForgeConsoleArt.GetCategoryIcon(AbyssalForgeProgressUtility.TurretSystemsCategory);
            if (icon != null)
            {
                GUI.color = unlocked ? Color.white : new Color(0.72f, 0.72f, 0.72f, 0.72f);
                GUI.DrawTexture(socketRect.ContractedBy(8f), icon, ScaleMode.ScaleToFit, true);
                GUI.color = Color.white;
            }

            string slotBadge = GetTurretRecipeSlotBadge(module, isChassis);
            Rect badgeRect = new Rect(rect.x + 76f, rect.y + 10f, isChassis ? 72f : 66f, 20f);
            DrawTurretSlotChip(badgeRect, slotBadge, slotColor);

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

            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;
            GUI.color = Color.white;
            ABY_UIPolishUtility.SafeLabel(new Rect(rect.x + 76f, rect.y + 33f, rect.width - 166f, 22f), AbyssalForgeProgressUtility.GetRecipeDisplayLabel(recipe));

            Text.Font = GameFont.Tiny;
            GUI.color = AbyssalForgeConsoleArt.TextDimColor;
            ABY_UIPolishUtility.SafeLabel(new Rect(rect.x + 76f, rect.y + 55f, rect.width - 90f, 18f), BuildTurretCardSubtitle(module, chassisProps));
            GUI.color = Color.white;

            float detailY = rect.y + 78f;
            DrawTurretCardDetailLines(rect, detailY, module, chassisProps);

            string unlockLine = unlocked
                ? "ABY_ForgePatternUnlockedAt".Translate(AbyssalForgeProgressUtility.GetRequiredResidue(recipe))
                : "ABY_ForgePatternLockedAt".Translate(AbyssalForgeProgressUtility.GetRequiredResidue(recipe));
            GUI.color = unlocked ? new Color(1f, 0.78f, 0.58f, 1f) : new Color(0.92f, 0.52f, 0.45f, 1f);
            ABY_UIPolishUtility.SafeLabel(new Rect(rect.x + 10f, rect.y + 126f, rect.width - 20f, 18f), unlockLine);
            GUI.color = Color.white;

            List<AbyssalForgeProgressUtility.IngredientAvailabilityEntry> entries = forge?.Map != null
                ? AbyssalForgeProgressUtility.GetIngredientAvailabilityEntries(forge.Map, recipe)
                : new List<AbyssalForgeProgressUtility.IngredientAvailabilityEntry>();
            int shownEntries = Math.Min(2, entries.Count);
            for (int i = 0; i < shownEntries; i++)
            {
                DrawIngredientStateLine(new Rect(rect.x + 10f, rect.y + 146f + i * 17f, rect.width - 142f, 17f), entries[i]);
            }

            if (entries.Count > shownEntries)
            {
                GUI.color = AbyssalForgeConsoleArt.TextDimColor;
                ABY_UIPolishUtility.SafeLabel(new Rect(rect.x + 10f, rect.y + 146f + shownEntries * 17f, rect.width - 142f, 17f), "ABY_ForgePatternMoreRequirements".Translate(entries.Count - shownEntries));
                GUI.color = Color.white;
            }

            bool hasAllMaterials = entries.All(entry => entry.IsSatisfied);
            bool recipeAvailable = false;
            try
            {
                recipeAvailable = forge != null && recipe.AvailableNow && recipe.AvailableOnNow(forge);
            }
            catch (System.Exception ex)
            {
                ABY_UISafetyUtility.LogUIException("Forge turret pattern availability", ex);
            }

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

            TooltipHandler.TipRegion(rect, BuildTurretRecipeTooltip(recipe, module, chassisProps, freshlyUnlocked));
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;
            GUI.color = Color.white;
        }

        private static void DrawTurretSlotChip(Rect rect, string label, Color color)
        {
            AbyssalForgeConsoleArt.Fill(rect, new Color(color.r * 0.18f, color.g * 0.12f, color.b * 0.10f, 0.95f));
            AbyssalForgeConsoleArt.DrawOutline(rect, new Color(color.r, color.g, color.b, 0.82f));
            Text.Font = GameFont.Tiny;
            Text.Anchor = TextAnchor.MiddleCenter;
            GUI.color = Color.white;
            ABY_UIPolishUtility.SafeLabel(rect.ContractedBy(2f), label);
            Text.Anchor = TextAnchor.UpperLeft;
        }

        private static string GetTurretRecipeSlotBadge(ABY_TurretModuleDef module, bool isChassis)
        {
            if (isChassis)
            {
                return ABY_ModularTurretUtility.TranslateOrFallback("ABY_TurretSlotBadge_Chassis", "CHASSIS");
            }

            if (module == null)
            {
                return ABY_ModularTurretUtility.TranslateOrFallback("ABY_TurretSlotBadge_Module", "MODULE");
            }

            switch (module.slot)
            {
                case ABY_TurretModuleSlot.MainWeapon:
                    return ABY_ModularTurretUtility.TranslateOrFallback("ABY_TurretSlotBadge_Main", "MAIN");
                case ABY_TurretModuleSlot.Auxiliary:
                    return ABY_ModularTurretUtility.TranslateOrFallback("ABY_TurretSlotBadge_Aux", "AUX");
                default:
                    return ABY_ModularTurretUtility.TranslateOrFallback("ABY_TurretSlotBadge_Passive", "PASSIVE");
            }
        }

        private static string BuildTurretCardSubtitle(ABY_TurretModuleDef module, CompProperties_AbyssalModularTurret chassisProps)
        {
            if (module != null)
            {
                return ABY_ModularTurretUtility.TranslateOrFallback("ABY_TurretForgeCardSlotRole", "Slot: {0} · Role: {1}", module.SlotLabel, module.RoleLabel);
            }

            if (chassisProps != null)
            {
                return ABY_ModularTurretUtility.TranslateOrFallback("ABY_TurretForgeCardChassisRole", "Buildable modular defense chassis");
            }

            return ABY_ModularTurretUtility.TranslateOrFallback("ABY_ForgePatternSummary_TurretSystems", "Turret system");
        }

        private void DrawTurretCardDetailLines(Rect rect, float y, ABY_TurretModuleDef module, CompProperties_AbyssalModularTurret chassisProps)
        {
            Text.Font = GameFont.Tiny;
            Text.Anchor = TextAnchor.UpperLeft;
            GUI.color = AbyssalForgeConsoleArt.TextSoftColor;

            if (module != null)
            {
                string effect = ABY_ModularTurretUtility.GetModuleEffectSummary(module);
                ABY_UIPolishUtility.SafeLabel(new Rect(rect.x + 10f, y, rect.width - 20f, 34f), CompactTextForCard(effect, 92));
                GUI.color = AbyssalForgeConsoleArt.TextDimColor;
                ABY_UIPolishUtility.SafeLabel(new Rect(rect.x + 10f, y + 36f, rect.width - 20f, 18f), ABY_ModularTurretUtility.GetModuleStatSummary(module));
                return;
            }

            if (chassisProps != null)
            {
                ABY_UIPolishUtility.SafeLabel(new Rect(rect.x + 10f, y, rect.width - 20f, 18f), ABY_ModularTurretUtility.TranslateOrFallback("ABY_TurretForgeCardChassisSlots", "Slots: {0} main / {1} auxiliary / {2} passive", Mathf.Max(0, chassisProps.mainWeaponSlots), Mathf.Max(0, chassisProps.auxiliarySlots), Mathf.Max(0, chassisProps.passiveSlots)));
                GUI.color = AbyssalForgeConsoleArt.TextDimColor;
                ABY_UIPolishUtility.SafeLabel(new Rect(rect.x + 10f, y + 20f, rect.width - 20f, 18f), ABY_ModularTurretUtility.TranslateOrFallback("ABY_TurretForgeCardChassisStats", "Base power {0} W · base range {1}", chassisProps.basePowerDraw.ToString("0"), chassisProps.baseRange.ToString("0.0")));
            }
        }

        private static string BuildTurretRecipeTooltip(RecipeDef recipe, ABY_TurretModuleDef module, CompProperties_AbyssalModularTurret chassisProps, bool freshlyUnlocked)
        {
            List<string> tooltipLines = new List<string>();
            if (module != null)
            {
                tooltipLines.Add(module.LabelCap);
                tooltipLines.Add(ABY_ModularTurretUtility.GetModuleDetailedTooltip(module));
            }
            else if (chassisProps != null)
            {
                ThingDef product = AbyssalForgeProgressUtility.GetPrimaryProduct(recipe);
                tooltipLines.Add(AbyssalForgeProgressUtility.GetRecipeDisplayLabel(recipe));
                tooltipLines.Add(ABY_ModularTurretUtility.GetChassisDetailedTooltip(product));
            }
            else
            {
                tooltipLines.Add(AbyssalForgeProgressUtility.GetPatternBrowserSummary(recipe));
            }

            string costBlock = AbyssalForgeProgressUtility.GetRecipeIngredientTooltip(recipe);
            if (!costBlock.NullOrEmpty())
            {
                tooltipLines.Add(string.Empty);
                tooltipLines.Add("ABY_ForgePatternRequirementsLabel".Translate());
                tooltipLines.Add(costBlock);
            }

            if (freshlyUnlocked)
            {
                tooltipLines.Add(string.Empty);
                tooltipLines.Add("ABY_ForgeUnlockToast".Translate(AbyssalForgeProgressUtility.GetRecipeDisplayLabel(recipe)));
            }

            return string.Join("\n", tooltipLines.Where(line => line != null).ToArray()).Trim();
        }

        private static string CompactTextForCard(string text, int maxChars)
        {
            if (text.NullOrEmpty() || text.Length <= maxChars)
            {
                return text ?? string.Empty;
            }

            return text.Substring(0, Mathf.Max(1, maxChars - 1)) + "…";
        }

        private void DrawPatternCard(Rect rect, RecipeDef recipe, bool unlocked, bool freshlyUnlocked)
        {
            if (recipe == null)
            {
                AbyssalForgeConsoleArt.DrawPanel(rect, false);
                ABY_UIPolishUtility.SafeLabel(rect.ContractedBy(10f), "Missing forge pattern");
                return;
            }

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
            ABY_UIPolishUtility.SafeLabel(labelRect, AbyssalForgeProgressUtility.GetRecipeDisplayLabel(recipe));

            GUI.color = AbyssalForgeConsoleArt.TextDimColor;
            ABY_UIPolishUtility.SafeLabel(new Rect(rect.x + 60f, rect.y + 31f, rect.width - 100f, 18f), AbyssalForgeProgressUtility.GetPatternBrowserSummary(recipe));

            int primaryProductCount = AbyssalForgeProgressUtility.GetPrimaryProductCount(recipe);
            if (primaryProductCount > 1)
            {
                ABY_UIPolishUtility.SafeLabel(new Rect(rect.x + 60f, rect.y + 48f, rect.width - 100f, 18f), "ABY_ForgePatternOutputCount".Translate(primaryProductCount));
            }
            GUI.color = Color.white;

            string unlockLine = unlocked
                ? "ABY_ForgePatternUnlockedAt".Translate(AbyssalForgeProgressUtility.GetRequiredResidue(recipe))
                : "ABY_ForgePatternLockedAt".Translate(AbyssalForgeProgressUtility.GetRequiredResidue(recipe));
            GUI.color = unlocked ? new Color(1f, 0.78f, 0.58f, 1f) : new Color(0.92f, 0.52f, 0.45f, 1f);
            ABY_UIPolishUtility.SafeLabel(new Rect(rect.x + 10f, rect.y + 66f, rect.width - 20f, 18f), unlockLine);
            GUI.color = Color.white;

            GUI.color = AbyssalForgeConsoleArt.TextDimColor;
            ABY_UIPolishUtility.SafeLabel(new Rect(rect.x + 10f, rect.y + 86f, rect.width - 20f, 18f), "ABY_ForgePatternRequirementsState".Translate());
            GUI.color = Color.white;

            List<AbyssalForgeProgressUtility.IngredientAvailabilityEntry> entries = forge?.Map != null
                ? AbyssalForgeProgressUtility.GetIngredientAvailabilityEntries(forge.Map, recipe)
                : new List<AbyssalForgeProgressUtility.IngredientAvailabilityEntry>();
            int shownEntries = Math.Min(2, entries.Count);
            for (int i = 0; i < shownEntries; i++)
            {
                DrawIngredientStateLine(new Rect(rect.x + 10f, rect.y + 104f + i * 18f, rect.width - 20f, 18f), entries[i]);
            }

            if (entries.Count > shownEntries)
            {
                GUI.color = AbyssalForgeConsoleArt.TextDimColor;
                ABY_UIPolishUtility.SafeLabel(new Rect(rect.x + 10f, rect.y + 104f + shownEntries * 18f, rect.width - 20f, 18f), "ABY_ForgePatternMoreRequirements".Translate(entries.Count - shownEntries));
                GUI.color = Color.white;
            }

            bool hasAllMaterials = entries.All(entry => entry.IsSatisfied);
            bool recipeAvailable = false;
            try
            {
                recipeAvailable = forge != null && recipe.AvailableNow && recipe.AvailableOnNow(forge);
            }
            catch (System.Exception ex)
            {
                ABY_UISafetyUtility.LogUIException("Forge pattern availability", ex);
            }
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

            List<string> tooltipLines = new List<string>
            {
                AbyssalForgeProgressUtility.GetPatternBrowserSummary(recipe)
            };

            string patternDetails = AbyssalForgeProgressUtility.GetPatternBrowserDetails(recipe);
            if (!patternDetails.NullOrEmpty())
            {
                tooltipLines.Add(string.Empty);
                tooltipLines.Add(patternDetails);
            }

            if (primaryProductCount > 1)
            {
                tooltipLines.Add("ABY_ForgePatternOutputCount".Translate(primaryProductCount));
            }

            string costBlock = AbyssalForgeProgressUtility.GetRecipeIngredientTooltip(recipe);
            if (!costBlock.NullOrEmpty())
            {
                tooltipLines.Add(string.Empty);
                tooltipLines.Add("ABY_ForgePatternRequirementsLabel".Translate());
                tooltipLines.Add(costBlock);
            }

            string stateBlock = forge?.Map != null ? AbyssalForgeProgressUtility.GetRecipeAvailabilityTooltip(forge.Map, recipe) : string.Empty;
            if (!stateBlock.NullOrEmpty())
            {
                tooltipLines.Add(string.Empty);
                tooltipLines.Add("ABY_ForgePatternRequirementsState".Translate());
                tooltipLines.Add(stateBlock);
            }

            if (freshlyUnlocked)
            {
                tooltipLines.Add(string.Empty);
                tooltipLines.Add("ABY_ForgeUnlockToast".Translate(AbyssalForgeProgressUtility.GetRecipeDisplayLabel(recipe)));
            }

            string tooltip = string.Join("\n", tooltipLines.Where(line => line != null).ToArray()).Trim();
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
            if (entry == null)
            {
                ABY_UIPolishUtility.SafeLabel(labelRect, "missing ingredient");
                return;
            }

            ABY_UIPolishUtility.SafeLabel(labelRect, ABY_UISafetyUtility.SafeString(entry.label, "ingredient"));
            GUI.color = entry.IsSatisfied ? new Color(0.72f, 1f, 0.74f, 1f) : new Color(1f, 0.58f, 0.52f, 1f);
            Text.Anchor = TextAnchor.UpperRight;
            ABY_UIPolishUtility.SafeLabel(countRect, entry.availableCount + "/" + entry.requiredCount);
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
            List<RecipeDef> availableRecipes = forge?.ProgressComponent != null
                ? forge.ProgressComponent.GetUnlockedRecipes(selectedCategory)
                    .Where(recipe => selectedCategory != AbyssalForgeProgressUtility.TurretSystemsCategory || TurretRecipeMatchesFilter(recipe, selectedTurretSystemsFilter))
                    .ToList()
                : new List<RecipeDef>();

            for (int i = 0; i < availableRecipes.Count; i++)
            {
                RecipeDef recipe = availableRecipes[i];
                bool availableNow = false;
                try
                {
                    availableNow = recipe != null && forge != null && recipe.AvailableNow && recipe.AvailableOnNow(forge);
                }
                catch (System.Exception ex)
                {
                    ABY_UISafetyUtility.LogUIException("Forge bill recipe option", ex);
                }

                if (!availableNow)
                {
                    continue;
                }

                RecipeDef capturedRecipe = recipe;
                options.Add(new FloatMenuOption(AbyssalForgeProgressUtility.GetRecipeDisplayLabel(capturedRecipe), delegate
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

            try
            {
                if (forge?.Map?.mapPawns != null && !forge.Map.mapPawns.FreeColonists.Any(colonist => colonist != null && recipe.PawnSatisfiesSkillRequirements(colonist)))
                {
                    Bill.CreateNoPawnsWithSkillDialog(recipe);
                }

                Bill bill = recipe.MakeNewBill();
                forge?.BillStack?.AddBill(bill);
            }
            catch (System.Exception ex)
            {
                ABY_UISafetyUtility.LogUIException("Forge add bill", ex);
            }

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
