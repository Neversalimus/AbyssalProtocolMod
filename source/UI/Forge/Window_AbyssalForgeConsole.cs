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
        private Vector2 selectedPatternScrollPosition = Vector2.zero;
        private Vector2 billScrollPosition = Vector2.zero;
        private float billViewHeight = 1000f;
        private Bill mouseoverBill;
        private string patternSearchText = string.Empty;
        private RecipeDef selectedPattern;
        private string selectedCategory = AbyssalForgeProgressUtility.AllCategory;
        private string selectedCoreFilter = CoreFilterAll;
        private string selectedWeaponsFilter = WeaponsFilterAll;
        private string selectedArmorFilter = ArmorFilterAll;
        private string selectedImplantsFilter = ImplantsFilterAll;
        private string selectedTurretSystemsFilter = TurretFilterAll;
        private string selectedStatusFilter = StatusFilterAll;

        private readonly List<ForgePatternEntry> patternIndex = new List<ForgePatternEntry>();
        private readonly List<RecipeDef> patternIndexSnapshot = new List<RecipeDef>();
        private readonly List<ForgePatternEntry> categoryPatternScratch = new List<ForgePatternEntry>();
        private readonly List<ForgePatternEntry> searchPatternScratch = new List<ForgePatternEntry>();
        private readonly List<ForgePatternEntry> visiblePatternScratch = new List<ForgePatternEntry>();
        private readonly List<ForgePatternEntry> billOptionScratch = new List<ForgePatternEntry>();
        private readonly Dictionary<RecipeDef, CachedPatternStatus> patternStatusCache = new Dictionary<RecipeDef, CachedPatternStatus>();
        private readonly Dictionary<RecipeDef, ForgePatternStatus> statusScratch = new Dictionary<RecipeDef, ForgePatternStatus>();
        private bool patternIndexDirty = true;
        private int patternIndexVersion;
        private int lastStatusResidueSnapshot = -1;
        private int filteredPatternCacheIndexVersion = -1;
        private int filteredPatternCacheResidue = int.MinValue;
        private int filteredPatternCacheTickBucket = -1;
        private string filteredPatternCacheCategory = string.Empty;
        private string filteredPatternCacheSubfilter = string.Empty;
        private string filteredPatternCacheStatus = string.Empty;
        private string filteredPatternCacheSearch = string.Empty;

        private const int PatternStatusCacheRefreshTicks = 180;
        private const int PatternListCacheRefreshTicks = 60;
        private const int PatternStatusRefreshBudgetPerPass = 14;

        private const string CoreFilterAll = "All";
        private const string CoreFilterResidue = "Residue";
        private const string CoreFilterCapacitor = "Capacitor";
        private const string CoreFilterStabilizer = "Stabilizer";

        private const string WeaponsFilterAll = "All";
        private const string WeaponsFilterMelee = "Melee";
        private const string WeaponsFilterRanged = "Ranged";
        private const string WeaponsFilterHerald = "Herald";

        private const string ArmorFilterAll = "All";
        private const string ArmorFilterArmor = "Armor";
        private const string ArmorFilterHelmet = "Helmet";
        private const string ArmorFilterGloves = "Gloves";
        private const string ArmorFilterVambraces = "Vambraces";
        private const string ArmorFilterPack = "Pack";
        private const string ArmorFilterBoots = "Boots";

        private const string ImplantsFilterAll = "All";
        private const string ImplantsFilterBrain = "Brain";
        private const string ImplantsFilterEyes = "Eyes";
        private const string ImplantsFilterBody = "Body";
        private const string ImplantsFilterArms = "Arms";
        private const string ImplantsFilterLegs = "Legs";
        private const string ImplantsFilterNeck = "Neck";
        private const string ImplantsFilterSpine = "Spine";
        private const string ImplantsFilterOrgans = "Organs";

        private const string TurretFilterAll = "All";
        private const string TurretFilterMain = "Main";
        private const string TurretFilterAuxiliary = "Auxiliary";
        private const string TurretFilterPassive = "Passive";

        private const string StatusFilterAll = "All";
        private const string StatusFilterCraftable = "Craftable";
        private const string StatusFilterMissing = "NeedsResources";
        private const string StatusFilterLocked = "Locked";
        private const string StatusFilterNexus = "Nexus";

        private enum ForgePatternStatus
        {
            Craftable,
            MissingMaterials,
            Locked,
            NexusLocked
        }

        private class CachedPatternStatus
        {
            public ForgePatternStatus status;
            public int tick;
            public int residue;
        }

        private struct ForgePatternEntry
        {
            public RecipeDef recipe;
            public ThingDef product;
            public string category;
            public string displayLabel;
            public string searchText;
            public string identityText;
            public int requiredResidue;
            public string coreFilterId;
            public int coreFilterOrder;
            public bool weaponMelee;
            public bool weaponRanged;
            public bool weaponHerald;
            public int weaponFilterOrder;
            public string armorFilterId;
            public int armorFilterOrder;
            public string implantFilterId;
            public int implantFilterOrder;
            public bool isTurretSystem;
            public string turretFilterId;
            public int turretOrder;
        }

        private struct ForgeFilterOption
        {
            public string id;
            public string label;
            public Color color;

            public ForgeFilterOption(string id, string label, Color color)
            {
                this.id = id;
                this.label = label;
                this.color = color;
            }
        }

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

            bool enhancedLayout = AbyssalStyledWidgets.UseEnhancedTheme;
            float gap = enhancedLayout ? 8f : 10f;
            float headerHeight = enhancedLayout ? 64f : 74f;
            float summaryHeight = enhancedLayout ? 206f : 210f;
            float categoryHeight = enhancedLayout ? 38f : 40f;
            float statusWidth = enhancedLayout ? 486f : 492f;
            float offerWidth = enhancedLayout ? 246f : 248f;
            float patternWidth = enhancedLayout ? 812f : 804f;
            float selectedPatternHeight = enhancedLayout ? 258f : 252f;

            Rect headerRect = new Rect(inRect.x, inRect.y, inRect.width, headerHeight);
            Rect statusRect = new Rect(inRect.x, headerRect.yMax + gap, statusWidth, summaryHeight);
            Rect offerRect = new Rect(statusRect.xMax + gap, headerRect.yMax + gap, offerWidth, summaryHeight);
            Rect nextRect = new Rect(offerRect.xMax + gap, headerRect.yMax + gap, inRect.width - offerRect.xMax - gap, summaryHeight);
            Rect categoryRect = new Rect(inRect.x, statusRect.yMax + gap, inRect.width, categoryHeight);
            Rect patternsRect = new Rect(inRect.x, categoryRect.yMax + gap, patternWidth, inRect.height - categoryRect.yMax - gap);
            Rect rightColumnRect = new Rect(patternsRect.xMax + gap, categoryRect.yMax + gap, inRect.width - patternWidth - gap, inRect.height - categoryRect.yMax - gap);
            Rect selectedPatternRect = new Rect(rightColumnRect.x, rightColumnRect.y, rightColumnRect.width, Mathf.Min(selectedPatternHeight, rightColumnRect.height * 0.62f));
            Rect billsRect = new Rect(rightColumnRect.x, selectedPatternRect.yMax + gap, rightColumnRect.width, rightColumnRect.height - selectedPatternRect.height - gap);

            DrawHeader(headerRect, progress);
            DrawStatusPanel(statusRect, progress);
            DrawOfferPanel(offerRect, progress);
            DrawNextPanel(nextRect, progress);
            DrawCategoryRow(categoryRect);
            DrawPatternBrowser(patternsRect, progress);
            DrawSelectedPatternPanel(selectedPatternRect, progress);
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
            string currentBandLabel;

            if (nextThreshold > 0)
            {
                int bandSize = Math.Max(1, nextThreshold - previousThreshold);
                fill = Mathf.Clamp01((total - previousThreshold) / (float)bandSize);
                currentBandLabel = "ABY_ForgeCurrentBandShort".Translate(previousThreshold, nextThreshold);
            }
            else
            {
                currentBandLabel = "ABY_ForgeCurrentBandCompleteShort".Translate();
            }

            int attunementTier = progress.GetCurrentAttunementTier(false);
            string attunementTierLabel = "ABY_ForgeAttunementTierShort".Translate(attunementTier, AbyssalForgeProgressUtility.MaxAttunementTier);

            float titleWidth = progress.HasRecentUnlocks ? inner.width - 164f : inner.width - 8f;
            AbyssalForgeConsoleArt.DrawSectionTitle(new Rect(inner.x, inner.y, Mathf.Max(120f, titleWidth), 22f), "ABY_ForgeStatusHeader".Translate());
            Text.Anchor = TextAnchor.MiddleRight;
            Text.Font = GameFont.Tiny;
            GUI.color = AbyssalForgeConsoleArt.TextSoftColor;
            ABY_UIPolishUtility.SafeLabel(new Rect(inner.x + 220f, inner.y + 1f, inner.width - 220f, 20f), currentBandLabel, 0f, 1f);
            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperLeft;
            Text.Font = GameFont.Small;

            Rect communionBarRect = new Rect(inner.x, inner.y + 26f, inner.width, 24f);
            AbyssalForgeConsoleArt.DrawProgressBar(communionBarRect, fill, string.Empty, progress.HasRecentUnlocks, AbyssalForgeConsoleArt.ProgressBarStyle.Communion);

            Rect attunementBarRect = new Rect(inner.x, inner.y + 58f, inner.width, 22f);
            AbyssalForgeConsoleArt.DrawProgressBar(attunementBarRect, AbyssalForgeProgressUtility.GetAttunementLevelFill(attunementTier), string.Empty, false, AbyssalForgeConsoleArt.ProgressBarStyle.Attunement);

            Text.Anchor = TextAnchor.MiddleRight;
            Text.Font = GameFont.Tiny;
            GUI.color = AbyssalForgeConsoleArt.TextSoftColor;
            ABY_UIPolishUtility.SafeLabel(new Rect(inner.x + 220f, attunementBarRect.yMax + 2f, inner.width - 220f, 18f), attunementTierLabel, 0f, 1f);
            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperLeft;
            Text.Font = GameFont.Small;

            float metricY = inner.y + 100f;
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
            float gutter = 18f;
            Rect leftRect = new Rect(inner.x, inner.y + 30f, leftWidth, inner.height - 30f);
            Rect rightRect = new Rect(inner.x + leftWidth + gutter, inner.y + 28f, inner.width - leftWidth - gutter, inner.height - 28f);

            GUI.color = AbyssalForgeConsoleArt.TextDimColor;
            ABY_UIPolishUtility.SafeLabel(new Rect(leftRect.x, leftRect.y, leftRect.width, 22f), "ABY_ForgeMilestonesHeader".Translate());
            GUI.color = Color.white;

            GameFont oldFont = Text.Font;
            Text.Font = GameFont.Tiny;
            float lineY = leftRect.y + 28f;
            for (int i = 0; i < milestones.Count; i++)
            {
                AbyssalForgeProgressUtility.MilestoneEntry entry = milestones[i];
                string milestoneLine = entry.label + ": " + entry.value;
                float height = ABY_UIPolishUtility.WrappedHeight(milestoneLine, leftRect.width, GameFont.Tiny, 24f, 10f);
                if (lineY + height > leftRect.yMax - 2f)
                {
                    break;
                }

                GUI.color = entry.satisfied ? new Color(0.72f, 1f, 0.74f, 1f) : Color.white;
                ABY_UIPolishUtility.SafeLabel(new Rect(leftRect.x, lineY, leftRect.width, height), milestoneLine, 0f, 3f);
                lineY += height + 8f;
            }
            Text.Font = oldFont;
            GUI.color = Color.white;

            string categoryLabel = AbyssalForgeProgressUtility.GetCategoryLabel(selectedCategory);
            List<RecipeDef> unlocked = progress.GetUnlockedRecipes(selectedCategory);
            List<RecipeDef> lockedAll = progress.GetLockedRecipes(selectedCategory);
            string summary = "ABY_ForgeUnlockedSummary".Translate(unlocked.Count, unlocked.Count + lockedAll.Count, categoryLabel);

            Text.Font = GameFont.Small;
            float summaryHeight = ABY_UIPolishUtility.WrappedHeight(summary, rightRect.width, GameFont.Small, 48f, 8f);
            ABY_UIPolishUtility.SafeLabel(new Rect(rightRect.x, rightRect.y, rightRect.width, summaryHeight), summary, 0f, 3f);

            float rightY = rightRect.y + summaryHeight + 8f;
            GUI.color = AbyssalForgeConsoleArt.TextDimColor;
            ABY_UIPolishUtility.SafeLabel(new Rect(rightRect.x, rightY, rightRect.width, 22f), "ABY_ForgeUpcomingPatterns".Translate(), 0f, 3f);
            GUI.color = Color.white;
            rightY += 24f;

            List<RecipeDef> locked = progress.GetLockedRecipes(selectedCategory).Take(2).ToList();
            Text.Font = GameFont.Tiny;
            if (locked.Count == 0)
            {
                GUI.color = AbyssalForgeConsoleArt.TextDimColor;
                string doneLine = "ABY_ForgeAllPatternsUnlocked".Translate();
                float doneHeight = ABY_UIPolishUtility.WrappedHeight(doneLine, rightRect.width, GameFont.Tiny, 28f, 8f);
                ABY_UIPolishUtility.SafeLabel(new Rect(rightRect.x, rightY, rightRect.width, doneHeight), doneLine, 0f, 3f);
                rightY += doneHeight + 4f;
            }
            else
            {
                for (int i = 0; i < locked.Count; i++)
                {
                    RecipeDef recipe = locked[i];
                    const float badgeWidth = 62f;
                    const float badgeGap = 6f;
                    string line = AbyssalForgeProgressUtility.GetRequiredResidue(recipe) + " — " + ABY_ProtocolResearchGateUtility.GetForgeDisplayLabel(recipe);
                    float lineWidth = Mathf.Max(40f, rightRect.width - badgeWidth - badgeGap);
                    float height = Mathf.Max(18f, ABY_UIPolishUtility.WrappedHeight(line, lineWidth, GameFont.Tiny, 24f, 8f));
                    if (rightY + height > rightRect.yMax - 30f)
                    {
                        break;
                    }

                    DrawForgeTierBadge(new Rect(rightRect.x, rightY + 1f, badgeWidth, 17f), recipe, true);
                    ABY_UIPolishUtility.SafeLabel(new Rect(rightRect.x + badgeWidth + badgeGap, rightY, lineWidth, height), line, 0f, 3f);
                    rightY += height + 4f;
                }
            }

            Text.Font = GameFont.Small;
            GUI.color = Color.white;

            bool reduced = progress.ReducedVisualEffects;
            bool newReduced = reduced;
            float checkboxY = Mathf.Min(rightRect.yMax - 26f, Mathf.Max(rightY + 8f, rightRect.y + 126f));
            Rect checkboxRect = new Rect(rightRect.x, checkboxY, Mathf.Min(220f, rightRect.width), 24f);
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
            if (selectedCategory == AbyssalForgeProgressUtility.HeraldCategory)
            {
                selectedCategory = AbyssalForgeProgressUtility.WeaponsCategory;
                selectedWeaponsFilter = WeaponsFilterHerald;
            }

            AbyssalForgeConsoleArt.DrawPanel(rect, false);
            if (AbyssalStyledWidgets.UseEnhancedTheme)
            {
                AbyssalStyledWidgets.DrawDividerHorizontal(new Rect(rect.x + 8f, rect.yMax - 6f, rect.width - 16f, 5f), 0.34f);
            }
            Rect inner = rect.ContractedBy(4f);
            List<string> categories = GetVisibleCategories().ToList();
            float width = inner.width / categories.Count;

            for (int i = 0; i < categories.Count; i++)
            {
                string category = categories[i];
                Rect buttonRect = new Rect(inner.x + width * i, inner.y, width - 4f, inner.height);
                bool active = category == selectedCategory;
                bool clicked = AbyssalStyledWidgets.TabButton(buttonRect, string.Empty, null, active);
                DrawCategoryTabContent(buttonRect, category, active, Mouse.IsOver(buttonRect));
                if (clicked)
                {
                    if (selectedCategory != category)
                    {
                        patternScrollPosition = Vector2.zero;
                        SetSelectedPattern(null);
                    }

                    selectedCategory = category;
                    EnsureSelectedFilterForCategory();

                    SoundDefOf.Tick_Tiny.PlayOneShotOnCamera(null);
                }
            }
        }

        private static void DrawCategoryTabContent(Rect rect, string category, bool active, bool hovered)
        {
            string label = AbyssalForgeProgressUtility.GetCategoryLabel(category);
            Texture2D icon = AbyssalForgeConsoleArt.GetCategoryIcon(category);

            TextAnchor oldAnchor = Text.Anchor;
            GameFont oldFont = Text.Font;
            Color oldColor = GUI.color;

            Text.Font = GameFont.Small;
            if (Text.CalcSize(label).x > rect.width - 34f)
            {
                Text.Font = GameFont.Tiny;
            }

            float iconSize = icon != null ? Mathf.Min(15f, rect.height - 14f) : 0f;
            float spacing = iconSize > 0f ? 5f : 0f;
            Vector2 labelSize = Text.CalcSize(label);
            float totalWidth = iconSize + spacing + labelSize.x;
            if (totalWidth > rect.width - 10f && iconSize > 0f)
            {
                iconSize = 0f;
                spacing = 0f;
                totalWidth = labelSize.x;
            }

            float startX = rect.center.x - totalWidth * 0.5f;
            if (iconSize > 0f)
            {
                Rect iconRect = new Rect(startX, rect.center.y - iconSize * 0.5f, iconSize, iconSize);
                GUI.color = active
                    ? new Color(1f, 0.74f, 0.45f, 0.96f)
                    : hovered
                        ? new Color(1f, 0.82f, 0.58f, 0.86f)
                        : new Color(0.92f, 0.76f, 0.62f, 0.78f);
                GUI.DrawTexture(iconRect, icon, ScaleMode.ScaleToFit, true);
                startX += iconSize + spacing;
            }

            Text.Anchor = TextAnchor.MiddleLeft;
            GUI.color = active
                ? new Color(1f, 0.88f, 0.70f, 1f)
                : hovered
                    ? Color.white
                    : new Color(0.90f, 0.84f, 0.76f, 0.96f);
            Rect labelRect = new Rect(startX, rect.y, Mathf.Min(labelSize.x + 6f, rect.xMax - startX - 4f), rect.height);
            ABY_UIPolishUtility.SafeLabel(labelRect, label, 0f, rect.height <= 30f ? 9f : 8f);

            GUI.color = oldColor;
            Text.Font = oldFont;
            Text.Anchor = oldAnchor;
        }

        private static IEnumerable<string> GetVisibleCategories()
        {
            foreach (string category in AbyssalForgeProgressUtility.Categories)
            {
                if (category == AbyssalForgeProgressUtility.HeraldCategory)
                {
                    continue;
                }

                yield return category;
            }
        }

        private void EnsureSelectedFilterForCategory()
        {
            if (selectedCoreFilter.NullOrEmpty()) selectedCoreFilter = CoreFilterAll;
            if (selectedWeaponsFilter.NullOrEmpty()) selectedWeaponsFilter = WeaponsFilterAll;
            if (selectedArmorFilter.NullOrEmpty()) selectedArmorFilter = ArmorFilterAll;
            if (selectedImplantsFilter.NullOrEmpty()) selectedImplantsFilter = ImplantsFilterAll;
            if (selectedTurretSystemsFilter.NullOrEmpty()) selectedTurretSystemsFilter = TurretFilterAll;
        }

        private static bool ShouldDrawSubfilterRow(string category)
        {
            return category == AbyssalForgeProgressUtility.CoreCategory
                || category == AbyssalForgeProgressUtility.WeaponsCategory
                || category == AbyssalForgeProgressUtility.ArmorCategory
                || category == AbyssalForgeProgressUtility.ImplantsCategory
                || category == AbyssalForgeProgressUtility.TurretSystemsCategory;
        }

        private void DrawSubfilterRow(Rect rect)
        {
            EnsureSelectedFilterForCategory();
            AbyssalForgeConsoleArt.Fill(rect, new Color(0.10f, 0.075f, 0.065f, 0.82f));
            AbyssalForgeConsoleArt.DrawOutline(rect, new Color(1f, 0.36f, 0.13f, 0.35f));

            List<ForgeFilterOption> options = GetFilterOptionsForSelectedCategory();
            if (options.Count == 0)
            {
                return;
            }

            GameFont oldFont = Text.Font;
            Text.Font = GameFont.Tiny;
            float gap = 8f;
            float totalWidth = -gap;
            float[] widths = new float[options.Count];
            for (int i = 0; i < options.Count; i++)
            {
                float labelWidth = Text.CalcSize(options[i].label).x + 24f;
                widths[i] = Mathf.Clamp(labelWidth, 54f, 124f);
                totalWidth += widths[i] + gap;
            }

            float buttonHeight = 22f;
            float buttonY = rect.y + (rect.height - buttonHeight) / 2f;
            float x = rect.x + Mathf.Max(10f, (rect.width - totalWidth) / 2f);
            for (int i = 0; i < options.Count; i++)
            {
                ForgeFilterOption option = options[i];
                DrawForgeFilterButton(new Rect(x, buttonY, widths[i], buttonHeight), option);
                x += widths[i] + gap;
            }

            Text.Font = oldFont;
            Text.Anchor = TextAnchor.UpperLeft;
            GUI.color = Color.white;
        }

        private List<ForgeFilterOption> GetFilterOptionsForSelectedCategory()
        {
            Color neutral = new Color(0.72f, 0.66f, 0.56f, 1f);
            Color forge = new Color(0.92f, 0.48f, 0.22f, 1f);
            Color capacitor = new Color(0.95f, 0.62f, 0.28f, 1f);
            Color stabilizer = new Color(0.76f, 0.64f, 0.36f, 1f);
            Color weapon = new Color(0.92f, 0.36f, 0.22f, 1f);
            Color armor = new Color(0.78f, 0.64f, 0.50f, 1f);
            Color implant = new Color(0.82f, 0.52f, 0.84f, 1f);
            Color herald = new Color(0.94f, 0.74f, 0.30f, 1f);

            if (selectedCategory == AbyssalForgeProgressUtility.CoreCategory)
            {
                return new List<ForgeFilterOption>
                {
                    new ForgeFilterOption(CoreFilterAll, GetSubfilterLabel(AbyssalForgeProgressUtility.CoreCategory, CoreFilterAll), neutral),
                    new ForgeFilterOption(CoreFilterResidue, GetSubfilterLabel(AbyssalForgeProgressUtility.CoreCategory, CoreFilterResidue), forge),
                    new ForgeFilterOption(CoreFilterCapacitor, GetSubfilterLabel(AbyssalForgeProgressUtility.CoreCategory, CoreFilterCapacitor), capacitor),
                    new ForgeFilterOption(CoreFilterStabilizer, GetSubfilterLabel(AbyssalForgeProgressUtility.CoreCategory, CoreFilterStabilizer), stabilizer)
                };
            }

            if (selectedCategory == AbyssalForgeProgressUtility.WeaponsCategory)
            {
                return new List<ForgeFilterOption>
                {
                    new ForgeFilterOption(WeaponsFilterAll, GetSubfilterLabel(AbyssalForgeProgressUtility.WeaponsCategory, WeaponsFilterAll), neutral),
                    new ForgeFilterOption(WeaponsFilterMelee, GetSubfilterLabel(AbyssalForgeProgressUtility.WeaponsCategory, WeaponsFilterMelee), weapon),
                    new ForgeFilterOption(WeaponsFilterRanged, GetSubfilterLabel(AbyssalForgeProgressUtility.WeaponsCategory, WeaponsFilterRanged), weapon),
                    new ForgeFilterOption(WeaponsFilterHerald, GetSubfilterLabel(AbyssalForgeProgressUtility.WeaponsCategory, WeaponsFilterHerald), herald)
                };
            }

            if (selectedCategory == AbyssalForgeProgressUtility.ArmorCategory)
            {
                return new List<ForgeFilterOption>
                {
                    new ForgeFilterOption(ArmorFilterAll, GetSubfilterLabel(AbyssalForgeProgressUtility.ArmorCategory, ArmorFilterAll), neutral),
                    new ForgeFilterOption(ArmorFilterArmor, GetSubfilterLabel(AbyssalForgeProgressUtility.ArmorCategory, ArmorFilterArmor), armor),
                    new ForgeFilterOption(ArmorFilterHelmet, GetSubfilterLabel(AbyssalForgeProgressUtility.ArmorCategory, ArmorFilterHelmet), armor),
                    new ForgeFilterOption(ArmorFilterGloves, GetSubfilterLabel(AbyssalForgeProgressUtility.ArmorCategory, ArmorFilterGloves), armor),
                    new ForgeFilterOption(ArmorFilterVambraces, GetSubfilterLabel(AbyssalForgeProgressUtility.ArmorCategory, ArmorFilterVambraces), armor),
                    new ForgeFilterOption(ArmorFilterPack, GetSubfilterLabel(AbyssalForgeProgressUtility.ArmorCategory, ArmorFilterPack), armor),
                    new ForgeFilterOption(ArmorFilterBoots, GetSubfilterLabel(AbyssalForgeProgressUtility.ArmorCategory, ArmorFilterBoots), armor)
                };
            }

            if (selectedCategory == AbyssalForgeProgressUtility.ImplantsCategory)
            {
                return new List<ForgeFilterOption>
                {
                    new ForgeFilterOption(ImplantsFilterAll, GetSubfilterLabel(AbyssalForgeProgressUtility.ImplantsCategory, ImplantsFilterAll), neutral),
                    new ForgeFilterOption(ImplantsFilterBrain, GetSubfilterLabel(AbyssalForgeProgressUtility.ImplantsCategory, ImplantsFilterBrain), implant),
                    new ForgeFilterOption(ImplantsFilterEyes, GetSubfilterLabel(AbyssalForgeProgressUtility.ImplantsCategory, ImplantsFilterEyes), implant),
                    new ForgeFilterOption(ImplantsFilterBody, GetSubfilterLabel(AbyssalForgeProgressUtility.ImplantsCategory, ImplantsFilterBody), implant),
                    new ForgeFilterOption(ImplantsFilterArms, GetSubfilterLabel(AbyssalForgeProgressUtility.ImplantsCategory, ImplantsFilterArms), implant),
                    new ForgeFilterOption(ImplantsFilterLegs, GetSubfilterLabel(AbyssalForgeProgressUtility.ImplantsCategory, ImplantsFilterLegs), implant),
                    new ForgeFilterOption(ImplantsFilterNeck, GetSubfilterLabel(AbyssalForgeProgressUtility.ImplantsCategory, ImplantsFilterNeck), implant),
                    new ForgeFilterOption(ImplantsFilterSpine, GetSubfilterLabel(AbyssalForgeProgressUtility.ImplantsCategory, ImplantsFilterSpine), implant),
                    new ForgeFilterOption(ImplantsFilterOrgans, GetSubfilterLabel(AbyssalForgeProgressUtility.ImplantsCategory, ImplantsFilterOrgans), implant)
                };
            }

            if (selectedCategory == AbyssalForgeProgressUtility.TurretSystemsCategory)
            {
                return new List<ForgeFilterOption>
                {
                    new ForgeFilterOption(TurretFilterAll, ABY_ModularTurretUtility.TranslateOrFallback("ABY_TurretFilter_All", "ALL"), neutral),
                    new ForgeFilterOption(TurretFilterMain, ABY_ModularTurretUtility.TranslateOrFallback("ABY_TurretSlotBadge_Main", "MAIN"), ABY_ModularTurretUtility.SlotColor(ABY_TurretModuleSlot.MainWeapon)),
                    new ForgeFilterOption(TurretFilterAuxiliary, ABY_ModularTurretUtility.TranslateOrFallback("ABY_TurretSlotBadge_Aux", "AUX"), ABY_ModularTurretUtility.SlotColor(ABY_TurretModuleSlot.Auxiliary)),
                    new ForgeFilterOption(TurretFilterPassive, ABY_ModularTurretUtility.TranslateOrFallback("ABY_TurretSlotBadge_Passive", "PASSIVE"), ABY_ModularTurretUtility.SlotColor(ABY_TurretModuleSlot.Passive))
                };
            }

            return new List<ForgeFilterOption>();
        }

        private string GetSelectedSubfilter()
        {
            if (selectedCategory == AbyssalForgeProgressUtility.CoreCategory) return selectedCoreFilter;
            if (selectedCategory == AbyssalForgeProgressUtility.WeaponsCategory) return selectedWeaponsFilter;
            if (selectedCategory == AbyssalForgeProgressUtility.ArmorCategory) return selectedArmorFilter;
            if (selectedCategory == AbyssalForgeProgressUtility.ImplantsCategory) return selectedImplantsFilter;
            if (selectedCategory == AbyssalForgeProgressUtility.TurretSystemsCategory) return selectedTurretSystemsFilter;
            return string.Empty;
        }

        private static string GetSubfilterLabel(string category, string filter)
        {
            if (category == AbyssalForgeProgressUtility.CoreCategory)
            {
                if (filter == CoreFilterResidue) return TranslateOrFallback("ABY_ForgeSubfilter_Residue", "Residue");
                if (filter == CoreFilterCapacitor) return TranslateOrFallback("ABY_ForgeSubfilter_Capacitor", "Capacitor");
                if (filter == CoreFilterStabilizer) return TranslateOrFallback("ABY_ForgeSubfilter_Stabilizer", "Stabilizer");
                return TranslateOrFallback("ABY_ForgeSubfilter_All", "All");
            }

            if (category == AbyssalForgeProgressUtility.WeaponsCategory)
            {
                if (filter == WeaponsFilterMelee) return TranslateOrFallback("ABY_ForgeSubfilter_Melee", "Melee");
                if (filter == WeaponsFilterRanged) return TranslateOrFallback("ABY_ForgeSubfilter_Ranged", "Ranged");
                if (filter == WeaponsFilterHerald) return TranslateOrFallback("ABY_ForgeSubfilter_Herald", "Herald");
                return TranslateOrFallback("ABY_ForgeSubfilter_All", "All");
            }

            if (category == AbyssalForgeProgressUtility.ArmorCategory)
            {
                if (filter == ArmorFilterArmor) return TranslateOrFallback("ABY_ForgeSubfilter_Armor", "Armor");
                if (filter == ArmorFilterHelmet) return TranslateOrFallback("ABY_ForgeSubfilter_Helmet", "Helmet");
                if (filter == ArmorFilterGloves) return TranslateOrFallback("ABY_ForgeSubfilter_Gloves", "Gloves");
                if (filter == ArmorFilterVambraces) return TranslateOrFallback("ABY_ForgeSubfilter_Vambraces", "Vambraces");
                if (filter == ArmorFilterPack) return TranslateOrFallback("ABY_ForgeSubfilter_Pack", "Pack");
                if (filter == ArmorFilterBoots) return TranslateOrFallback("ABY_ForgeSubfilter_Boots", "Boots");
                return TranslateOrFallback("ABY_ForgeSubfilter_All", "All");
            }

            if (category == AbyssalForgeProgressUtility.ImplantsCategory)
            {
                if (filter == ImplantsFilterBrain) return TranslateOrFallback("ABY_ForgeSubfilter_Brain", "Brain");
                if (filter == ImplantsFilterEyes) return TranslateOrFallback("ABY_ForgeSubfilter_Eyes", "Eyes");
                if (filter == ImplantsFilterBody) return TranslateOrFallback("ABY_ForgeSubfilter_Body", "Body");
                if (filter == ImplantsFilterArms) return TranslateOrFallback("ABY_ForgeSubfilter_Arms", "Arms");
                if (filter == ImplantsFilterLegs) return TranslateOrFallback("ABY_ForgeSubfilter_Legs", "Legs");
                if (filter == ImplantsFilterNeck) return TranslateOrFallback("ABY_ForgeSubfilter_Neck", "Neck");
                if (filter == ImplantsFilterSpine) return TranslateOrFallback("ABY_ForgeSubfilter_Spine", "Spine");
                if (filter == ImplantsFilterOrgans) return TranslateOrFallback("ABY_ForgeSubfilter_Organs", "Organs");
                return TranslateOrFallback("ABY_ForgeSubfilter_All", "All");
            }

            return filter ?? string.Empty;
        }

        private void SetSelectedSubfilter(string filter)
        {
            if (selectedCategory == AbyssalForgeProgressUtility.CoreCategory) selectedCoreFilter = filter;
            else if (selectedCategory == AbyssalForgeProgressUtility.WeaponsCategory) selectedWeaponsFilter = filter;
            else if (selectedCategory == AbyssalForgeProgressUtility.ArmorCategory) selectedArmorFilter = filter;
            else if (selectedCategory == AbyssalForgeProgressUtility.ImplantsCategory) selectedImplantsFilter = filter;
            else if (selectedCategory == AbyssalForgeProgressUtility.TurretSystemsCategory) selectedTurretSystemsFilter = filter;
        }

        private void DrawForgeFilterButton(Rect rect, ForgeFilterOption option)
        {
            string current = GetSelectedSubfilter();
            bool active = current == option.id;
            bool hovered = Mouse.IsOver(rect);
            Color color = option.color;
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
            ABY_UIPolishUtility.SafeLabel(rect.ContractedBy(2f), option.label);

            if (Widgets.ButtonInvisible(rect, false))
            {
                if (current != option.id)
                {
                    SetSelectedSubfilter(option.id);
                    patternScrollPosition = Vector2.zero;
                    SoundDefOf.Tick_Tiny.PlayOneShotOnCamera(null);
                }
            }

            TooltipHandler.TipRegion(rect, GetSubfilterTooltip(selectedCategory, option.id));
            Text.Anchor = TextAnchor.UpperLeft;
            GUI.color = Color.white;
        }

        private static string GetSubfilterTooltip(string category, string filter)
        {
            if (category == AbyssalForgeProgressUtility.CoreCategory)
            {
                if (filter == CoreFilterResidue) return TranslateOrFallback("ABY_ForgeSubfilterTooltip_Residue", "Show residue processing and residue-routing forge infrastructure.");
                if (filter == CoreFilterCapacitor) return TranslateOrFallback("ABY_ForgeSubfilterTooltip_Capacitor", "Show capacitor and energy storage modules.");
                if (filter == CoreFilterStabilizer) return TranslateOrFallback("ABY_ForgeSubfilterTooltip_Stabilizer", "Show circle stabilizer modules.");
                return TranslateOrFallback("ABY_ForgeSubfilterTooltip_CoreAll", "Show all forge core infrastructure patterns.");
            }

            if (category == AbyssalForgeProgressUtility.WeaponsCategory)
            {
                if (filter == WeaponsFilterMelee) return TranslateOrFallback("ABY_ForgeSubfilterTooltip_Melee", "Show melee weapons.");
                if (filter == WeaponsFilterRanged) return TranslateOrFallback("ABY_ForgeSubfilterTooltip_Ranged", "Show ranged weapons.");
                if (filter == WeaponsFilterHerald) return TranslateOrFallback("ABY_ForgeSubfilterTooltip_Herald", "Show Herald-grade weapons moved into the Weapons category.");
                return TranslateOrFallback("ABY_ForgeSubfilterTooltip_WeaponsAll", "Show all weapon patterns, including Herald-grade weapons.");
            }

            if (category == AbyssalForgeProgressUtility.ArmorCategory)
            {
                return TranslateOrFallback("ABY_ForgeSubfilterTooltip_ArmorGeneric", "Show {0} armor slot patterns.", GetSubfilterLabel(category, filter).ToLowerInvariant());
            }

            if (category == AbyssalForgeProgressUtility.ImplantsCategory)
            {
                return TranslateOrFallback("ABY_ForgeSubfilterTooltip_ImplantGeneric", "Show {0} implant procedures.", GetSubfilterLabel(category, filter).ToLowerInvariant());
            }

            if (category == AbyssalForgeProgressUtility.TurretSystemsCategory)
            {
                return GetTurretFilterTooltip(filter);
            }

            return string.Empty;
        }

        private bool RecipeMatchesSelectedCategoryAndFilter(RecipeDef recipe)
        {
            if (recipe == null)
            {
                return false;
            }

            string category = AbyssalForgeProgressUtility.GetCategory(recipe);
            if (selectedCategory == AbyssalForgeProgressUtility.AllCategory)
            {
                return true;
            }

            if (selectedCategory == AbyssalForgeProgressUtility.WeaponsCategory)
            {
                if (category != AbyssalForgeProgressUtility.WeaponsCategory && category != AbyssalForgeProgressUtility.HeraldCategory)
                {
                    return false;
                }

                return WeaponRecipeMatchesFilter(recipe, selectedWeaponsFilter);
            }

            if (category != selectedCategory)
            {
                return false;
            }

            if (selectedCategory == AbyssalForgeProgressUtility.CoreCategory) return CoreRecipeMatchesFilter(recipe, selectedCoreFilter);
            if (selectedCategory == AbyssalForgeProgressUtility.ArmorCategory) return ArmorRecipeMatchesFilter(recipe, selectedArmorFilter);
            if (selectedCategory == AbyssalForgeProgressUtility.ImplantsCategory) return ImplantRecipeMatchesFilter(recipe, selectedImplantsFilter);
            if (selectedCategory == AbyssalForgeProgressUtility.TurretSystemsCategory) return TurretRecipeMatchesFilter(recipe, selectedTurretSystemsFilter);
            return true;
        }

        private int GetActiveRecipeOrder(RecipeDef recipe)
        {
            if (recipe == null)
            {
                return 9999;
            }

            if (selectedCategory == AbyssalForgeProgressUtility.AllCategory)
            {
                string category = AbyssalForgeProgressUtility.GetCategory(recipe);
                if (category == AbyssalForgeProgressUtility.HeraldCategory)
                {
                    category = AbyssalForgeProgressUtility.WeaponsCategory;
                }

                return AbyssalForgeProgressUtility.GetCategoryOrderIndex(category) * 100 + GetSubfilterOrderForRecipe(recipe);
            }

            return GetSubfilterOrderForRecipe(recipe);
        }

        private int GetSubfilterOrderForRecipe(RecipeDef recipe)
        {
            if (selectedCategory == AbyssalForgeProgressUtility.CoreCategory) return GetCoreFilterOrder(recipe);
            if (selectedCategory == AbyssalForgeProgressUtility.WeaponsCategory) return GetWeaponFilterOrder(recipe);
            if (selectedCategory == AbyssalForgeProgressUtility.ArmorCategory) return GetArmorFilterOrder(recipe);
            if (selectedCategory == AbyssalForgeProgressUtility.ImplantsCategory) return GetImplantFilterOrder(recipe);
            if (selectedCategory == AbyssalForgeProgressUtility.TurretSystemsCategory) return GetTurretSystemRecipeOrder(recipe);
            return AbyssalForgeProgressUtility.GetCategoryOrderIndex(AbyssalForgeProgressUtility.GetCategory(recipe));
        }

        private static bool CoreRecipeMatchesFilter(RecipeDef recipe, string filter)
        {
            if (filter.NullOrEmpty() || filter == CoreFilterAll) return true;
            return GetCoreFilterId(recipe) == filter;
        }

        private static int GetCoreFilterOrder(RecipeDef recipe)
        {
            string id = GetCoreFilterId(recipe);
            if (id == CoreFilterResidue) return 10;
            if (id == CoreFilterCapacitor) return 20;
            if (id == CoreFilterStabilizer) return 30;
            return 90;
        }

        private static string GetCoreFilterId(RecipeDef recipe)
        {
            string text = BuildRecipeSearchText(recipe);
            if (text.Contains("stabilizer")) return CoreFilterStabilizer;
            if (text.Contains("capacitor") || text.Contains("condenser") || text.Contains("condensation") || text.Contains("cell")) return CoreFilterCapacitor;
            if (text.Contains("residue") || text.Contains("sinter") || text.Contains("crucible") || text.Contains("processing")) return CoreFilterResidue;
            return CoreFilterResidue;
        }

        private static bool WeaponRecipeMatchesFilter(RecipeDef recipe, string filter)
        {
            if (filter.NullOrEmpty() || filter == WeaponsFilterAll) return true;
            string category = AbyssalForgeProgressUtility.GetCategory(recipe);
            if (filter == WeaponsFilterHerald) return category == AbyssalForgeProgressUtility.HeraldCategory || IsHeraldWeaponRecipe(recipe);
            if (filter == WeaponsFilterMelee) return IsMeleeWeaponRecipe(recipe);
            if (filter == WeaponsFilterRanged) return IsRangedWeaponRecipe(recipe);
            return true;
        }

        private static int GetWeaponFilterOrder(RecipeDef recipe)
        {
            if (IsMeleeWeaponRecipe(recipe)) return 10;
            if (IsRangedWeaponRecipe(recipe)) return 20;
            if (AbyssalForgeProgressUtility.GetCategory(recipe) == AbyssalForgeProgressUtility.HeraldCategory || IsHeraldWeaponRecipe(recipe)) return 30;
            return 90;
        }

        private static bool IsRangedWeaponRecipe(RecipeDef recipe)
        {
            string text = BuildRecipeSearchText(recipe);
            if (IsForcedRangedWeaponRecipe(text))
            {
                return true;
            }

            return !IsMeleeWeaponRecipe(recipe);
        }

        private static bool IsMeleeWeaponRecipe(RecipeDef recipe)
        {
            string text = BuildRecipeSearchText(recipe);
            if (IsForcedRangedWeaponRecipe(text))
            {
                return false;
            }

            return text.Contains("blade")
                || text.Contains("dagger")
                || text.Contains("halberd")
                || text.Contains("maul")
                || text.Contains("glaive");
        }

        private static bool IsForcedRangedWeaponRecipe(string text)
        {
            return text.Contains("ashen pike")
                || text.Contains("canticle driver")
                || text.Contains("anchor spiker")
                || text.Contains("phalanx driver")
                || text.Contains("gatebreaker spiker");
        }

        private static bool IsHeraldWeaponRecipe(RecipeDef recipe)
        {
            string text = BuildRecipeSearchText(recipe);
            return text.Contains("herald");
        }

        private static bool ArmorRecipeMatchesFilter(RecipeDef recipe, string filter)
        {
            if (filter.NullOrEmpty() || filter == ArmorFilterAll) return true;
            return GetArmorFilterId(recipe) == filter;
        }

        private static int GetArmorFilterOrder(RecipeDef recipe)
        {
            string id = GetArmorFilterId(recipe);
            if (id == ArmorFilterArmor) return 10;
            if (id == ArmorFilterHelmet) return 20;
            if (id == ArmorFilterGloves) return 30;
            if (id == ArmorFilterVambraces) return 40;
            if (id == ArmorFilterPack) return 50;
            if (id == ArmorFilterBoots) return 60;
            return 90;
        }

        private static string GetArmorFilterId(RecipeDef recipe)
        {
            string text = BuildRecipeSearchText(recipe);
            if (RecipeIdentityContains(recipe, "infernal combat frame")) return ArmorFilterArmor;
            if (RecipeIdentityContains(recipe, "ashen vambraces")) return ArmorFilterVambraces;
            if (text.Contains("pack")) return ArmorFilterPack;
            if (text.Contains("vambrace")) return ArmorFilterVambraces;
            if (text.Contains("glove") || text.Contains("gauntlet")) return ArmorFilterGloves;
            if (text.Contains("boot") || text.Contains("greave") || text.Contains("sabatons")) return ArmorFilterBoots;
            if (text.Contains("helm") || text.Contains("cowl") || text.Contains("veil")) return ArmorFilterHelmet;
            return ArmorFilterArmor;
        }

        private static bool ImplantRecipeMatchesFilter(RecipeDef recipe, string filter)
        {
            if (filter.NullOrEmpty() || filter == ImplantsFilterAll) return true;
            return GetImplantFilterId(recipe) == filter;
        }

        private static int GetImplantFilterOrder(RecipeDef recipe)
        {
            string id = GetImplantFilterId(recipe);
            if (id == ImplantsFilterBrain) return 10;
            if (id == ImplantsFilterEyes) return 20;
            if (id == ImplantsFilterBody) return 30;
            if (id == ImplantsFilterArms) return 40;
            if (id == ImplantsFilterLegs) return 50;
            if (id == ImplantsFilterNeck) return 60;
            if (id == ImplantsFilterSpine) return 70;
            if (id == ImplantsFilterOrgans) return 80;
            return 90;
        }

        private static string GetImplantFilterId(RecipeDef recipe)
        {
            string text = BuildRecipeSearchText(recipe);
            if (RecipeIdentityContains(recipe, "herald carapace mesh", "harmonic mesh", "lawwoven carapace mesh")) return ImplantsFilterBody;
            if (RecipeIdentityContains(recipe, "archon tendon spine", "verdict tendon spine")) return ImplantsFilterSpine;
            if (RecipeIdentityContains(recipe, "cinder mandible seal")) return ImplantsFilterOrgans;
            if (RecipeIdentityContains(recipe, "null chorus collar")) return ImplantsFilterNeck;
            if (RecipeIdentityContains(recipe, "breach tendon weave")) return ImplantsFilterLegs;
            if (text.Contains("eye")) return ImplantsFilterEyes;
            if (text.Contains("cortex") || text.Contains("subcore") || text.Contains("brain")) return ImplantsFilterBrain;
            if (text.Contains("collar") || text.Contains("neck")) return ImplantsFilterNeck;
            if (text.Contains("spine")) return ImplantsFilterSpine;
            if (text.Contains("heart") || text.Contains("kidney") || text.Contains("liver") || text.Contains("lung") || text.Contains("mandible")) return ImplantsFilterOrgans;
            if (text.Contains("leg") || text.Contains("tendon")) return ImplantsFilterLegs;
            if (text.Contains("carapace mesh") || text.Contains("mesh")) return ImplantsFilterBody;
            if (text.Contains("arm") || text.Contains("claw") || text.Contains("servo") || text.Contains("hand")) return ImplantsFilterArms;
            return ImplantsFilterBody;
        }

        private static bool RecipeIdentityContains(RecipeDef recipe, params string[] fragments)
        {
            if (fragments == null || fragments.Length == 0)
            {
                return false;
            }

            string identity = BuildRecipeIdentityText(recipe);
            for (int i = 0; i < fragments.Length; i++)
            {
                string fragment = fragments[i];
                if (!fragment.NullOrEmpty() && identity.Contains(fragment.ToLowerInvariant()))
                {
                    return true;
                }
            }

            return false;
        }

        private static string BuildRecipeIdentityText(RecipeDef recipe)
        {
            return BuildRecipeIdentityText(recipe, AbyssalForgeProgressUtility.GetPrimaryProduct(recipe));
        }

        private static string BuildRecipeIdentityText(RecipeDef recipe, ThingDef product)
        {
            return ((recipe?.defName ?? string.Empty) + " "
                + (recipe?.label ?? string.Empty) + " "
                + (product?.defName ?? string.Empty) + " "
                + (product?.label ?? string.Empty)).ToLowerInvariant();
        }

        private static string BuildRecipeSearchText(RecipeDef recipe)
        {
            ThingDef product = AbyssalForgeProgressUtility.GetPrimaryProduct(recipe);
            string category = AbyssalForgeProgressUtility.GetCategory(recipe);
            return BuildRecipeSearchText(recipe, product, category);
        }

        private static string BuildRecipeSearchText(RecipeDef recipe, ThingDef product, string category)
        {
            string productLabel = product != null ? product.label : string.Empty;
            string productDef = product != null ? product.defName : string.Empty;
            string categoryLabel = AbyssalForgeProgressUtility.GetCategoryLabel(category);
            string summary = AbyssalForgeProgressUtility.GetPatternBrowserSummary(recipe);
            string details = AbyssalForgeProgressUtility.GetPatternBrowserDetails(recipe);
            string ingredients = AbyssalForgeProgressUtility.GetRecipeIngredientTooltip(recipe);
            return ((recipe?.defName ?? string.Empty) + " "
                + (recipe?.label ?? string.Empty) + " "
                + productDef + " "
                + productLabel + " "
                + category + " "
                + categoryLabel + " "
                + summary + " "
                + details + " "
                + ingredients).ToLowerInvariant();
        }

        private static bool RecipeMatchesSearch(RecipeDef recipe, string searchText)
        {
            return SearchTextMatches(BuildRecipeSearchText(recipe), searchText);
        }

        private static bool SearchTextMatches(string haystack, string searchText)
        {
            if (searchText.NullOrEmpty())
            {
                return true;
            }

            string query = searchText.Trim().ToLowerInvariant();
            if (query.Length == 0)
            {
                return true;
            }

            string safeHaystack = haystack ?? string.Empty;
            string[] parts = query.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < parts.Length; i++)
            {
                if (!safeHaystack.Contains(parts[i]))
                {
                    return false;
                }
            }

            return true;
        }

        private List<ForgePatternEntry> GetForgePatternIndex()
        {
            List<RecipeDef> recipes = AbyssalForgeProgressUtility.GetForgeRecipes();
            if (!patternIndexDirty && PatternIndexMatches(recipes))
            {
                return patternIndex;
            }

            patternIndex.Clear();
            patternIndexSnapshot.Clear();
            if (recipes != null)
            {
                for (int i = 0; i < recipes.Count; i++)
                {
                    RecipeDef recipe = recipes[i];
                    if (recipe == null)
                    {
                        continue;
                    }

                    patternIndex.Add(BuildForgePatternEntry(recipe));
                    patternIndexSnapshot.Add(recipe);
                }
            }

            patternIndexDirty = false;
            patternIndexVersion++;
            ClearPatternStatusCache();
            InvalidateFilteredPatternCache();
            return patternIndex;
        }

        private bool PatternIndexMatches(List<RecipeDef> recipes)
        {
            if (recipes == null || recipes.Count != patternIndexSnapshot.Count)
            {
                return false;
            }

            for (int i = 0; i < recipes.Count; i++)
            {
                if (recipes[i] != patternIndexSnapshot[i])
                {
                    return false;
                }
            }

            return true;
        }

        private ForgePatternEntry BuildForgePatternEntry(RecipeDef recipe)
        {
            ThingDef product = AbyssalForgeProgressUtility.GetPrimaryProduct(recipe);
            string category = AbyssalForgeProgressUtility.GetCategory(recipe);
            string identity = BuildRecipeIdentityText(recipe, product);
            string search = BuildRecipeSearchText(recipe, product, category);

            bool forcedRanged = IsForcedRangedWeaponRecipe(search);
            bool melee = !forcedRanged && IsMeleeWeaponText(search);
            bool ranged = forcedRanged || !melee;
            bool herald = category == AbyssalForgeProgressUtility.HeraldCategory || search.Contains("herald");

            string coreFilter = ResolveCoreFilterId(search);
            string armorFilter = ResolveArmorFilterId(search, identity);
            string implantFilter = ResolveImplantFilterId(search, identity);
            string turretFilter = ResolveTurretFilterId(product);

            return new ForgePatternEntry
            {
                recipe = recipe,
                product = product,
                category = category,
                displayLabel = AbyssalForgeProgressUtility.GetRecipeDisplayLabel(recipe),
                searchText = search,
                identityText = identity,
                requiredResidue = AbyssalForgeProgressUtility.GetRequiredResidue(recipe),
                coreFilterId = coreFilter,
                coreFilterOrder = ResolveCoreFilterOrder(coreFilter),
                weaponMelee = melee,
                weaponRanged = ranged,
                weaponHerald = herald,
                weaponFilterOrder = ResolveWeaponFilterOrder(melee, ranged, herald),
                armorFilterId = armorFilter,
                armorFilterOrder = ResolveArmorFilterOrder(armorFilter),
                implantFilterId = implantFilter,
                implantFilterOrder = ResolveImplantFilterOrder(implantFilter),
                isTurretSystem = IsTurretSystemProduct(recipe, product, category),
                turretFilterId = turretFilter,
                turretOrder = ResolveTurretSystemOrder(product)
            };
        }

        private static string ResolveCoreFilterId(string text)
        {
            string safe = text ?? string.Empty;
            if (safe.Contains("stabilizer")) return CoreFilterStabilizer;
            if (safe.Contains("capacitor") || safe.Contains("condenser") || safe.Contains("condensation") || safe.Contains("cell")) return CoreFilterCapacitor;
            if (safe.Contains("residue") || safe.Contains("sinter") || safe.Contains("crucible") || safe.Contains("processing")) return CoreFilterResidue;
            return CoreFilterResidue;
        }

        private static int ResolveCoreFilterOrder(string id)
        {
            if (id == CoreFilterResidue) return 10;
            if (id == CoreFilterCapacitor) return 20;
            if (id == CoreFilterStabilizer) return 30;
            return 90;
        }

        private static bool IsMeleeWeaponText(string text)
        {
            string safe = text ?? string.Empty;
            return safe.Contains("blade")
                || safe.Contains("dagger")
                || safe.Contains("halberd")
                || safe.Contains("maul")
                || safe.Contains("glaive");
        }

        private static int ResolveWeaponFilterOrder(bool melee, bool ranged, bool herald)
        {
            if (melee) return 10;
            if (ranged) return 20;
            if (herald) return 30;
            return 90;
        }

        private static string ResolveArmorFilterId(string searchText, string identityText)
        {
            string text = searchText ?? string.Empty;
            string identity = identityText ?? string.Empty;
            if (identity.Contains("infernal combat frame")) return ArmorFilterArmor;
            if (identity.Contains("ashen vambraces")) return ArmorFilterVambraces;
            if (text.Contains("pack")) return ArmorFilterPack;
            if (text.Contains("vambrace")) return ArmorFilterVambraces;
            if (text.Contains("glove") || text.Contains("gauntlet")) return ArmorFilterGloves;
            if (text.Contains("boot") || text.Contains("greave") || text.Contains("sabatons")) return ArmorFilterBoots;
            if (text.Contains("helm") || text.Contains("cowl") || text.Contains("veil")) return ArmorFilterHelmet;
            return ArmorFilterArmor;
        }

        private static int ResolveArmorFilterOrder(string id)
        {
            if (id == ArmorFilterArmor) return 10;
            if (id == ArmorFilterHelmet) return 20;
            if (id == ArmorFilterGloves) return 30;
            if (id == ArmorFilterVambraces) return 40;
            if (id == ArmorFilterPack) return 50;
            if (id == ArmorFilterBoots) return 60;
            return 90;
        }

        private static string ResolveImplantFilterId(string searchText, string identityText)
        {
            string text = searchText ?? string.Empty;
            string identity = identityText ?? string.Empty;
            if (IdentityTextContains(identity, "herald carapace mesh", "harmonic mesh", "lawwoven carapace mesh")) return ImplantsFilterBody;
            if (IdentityTextContains(identity, "archon tendon spine", "verdict tendon spine")) return ImplantsFilterSpine;
            if (IdentityTextContains(identity, "cinder mandible seal")) return ImplantsFilterOrgans;
            if (IdentityTextContains(identity, "null chorus collar")) return ImplantsFilterNeck;
            if (IdentityTextContains(identity, "breach tendon weave")) return ImplantsFilterLegs;
            if (text.Contains("eye")) return ImplantsFilterEyes;
            if (text.Contains("cortex") || text.Contains("subcore") || text.Contains("brain")) return ImplantsFilterBrain;
            if (text.Contains("collar") || text.Contains("neck")) return ImplantsFilterNeck;
            if (text.Contains("spine")) return ImplantsFilterSpine;
            if (text.Contains("heart") || text.Contains("kidney") || text.Contains("liver") || text.Contains("lung") || text.Contains("mandible")) return ImplantsFilterOrgans;
            if (text.Contains("leg") || text.Contains("tendon")) return ImplantsFilterLegs;
            if (text.Contains("carapace mesh") || text.Contains("mesh")) return ImplantsFilterBody;
            if (text.Contains("arm") || text.Contains("claw") || text.Contains("servo") || text.Contains("hand")) return ImplantsFilterArms;
            return ImplantsFilterBody;
        }

        private static int ResolveImplantFilterOrder(string id)
        {
            if (id == ImplantsFilterBrain) return 10;
            if (id == ImplantsFilterEyes) return 20;
            if (id == ImplantsFilterBody) return 30;
            if (id == ImplantsFilterArms) return 40;
            if (id == ImplantsFilterLegs) return 50;
            if (id == ImplantsFilterNeck) return 60;
            if (id == ImplantsFilterSpine) return 70;
            if (id == ImplantsFilterOrgans) return 80;
            return 90;
        }

        private static bool IdentityTextContains(string identity, params string[] fragments)
        {
            if (fragments == null || fragments.Length == 0)
            {
                return false;
            }

            string safe = identity ?? string.Empty;
            for (int i = 0; i < fragments.Length; i++)
            {
                string fragment = fragments[i];
                if (!fragment.NullOrEmpty() && safe.Contains(fragment.ToLowerInvariant()))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsTurretSystemProduct(RecipeDef recipe, ThingDef product, string category)
        {
            return ABY_ModularTurretUtility.GetModuleForThingDef(product) != null
                || product?.GetCompProperties<CompProperties_AbyssalModularTurret>() != null
                || category == AbyssalForgeProgressUtility.TurretSystemsCategory;
        }

        private static string ResolveTurretFilterId(ThingDef product)
        {
            ABY_TurretModuleDef module = ABY_ModularTurretUtility.GetModuleForThingDef(product);
            if (module == null)
            {
                return TurretFilterAll;
            }

            switch (module.slot)
            {
                case ABY_TurretModuleSlot.MainWeapon:
                    return TurretFilterMain;
                case ABY_TurretModuleSlot.Auxiliary:
                    return TurretFilterAuxiliary;
                case ABY_TurretModuleSlot.Passive:
                    return TurretFilterPassive;
                default:
                    return TurretFilterAll;
            }
        }

        private static int ResolveTurretSystemOrder(ThingDef product)
        {
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

        private bool EntryMatchesSelectedCategoryAndFilter(ForgePatternEntry entry)
        {
            RecipeDef recipe = entry.recipe;
            if (recipe == null)
            {
                return false;
            }

            string category = entry.category;
            if (selectedCategory == AbyssalForgeProgressUtility.AllCategory)
            {
                return true;
            }

            if (selectedCategory == AbyssalForgeProgressUtility.WeaponsCategory)
            {
                if (category != AbyssalForgeProgressUtility.WeaponsCategory && category != AbyssalForgeProgressUtility.HeraldCategory)
                {
                    return false;
                }

                return WeaponEntryMatchesFilter(entry, selectedWeaponsFilter);
            }

            if (category != selectedCategory)
            {
                return false;
            }

            if (selectedCategory == AbyssalForgeProgressUtility.CoreCategory) return selectedCoreFilter.NullOrEmpty() || selectedCoreFilter == CoreFilterAll || entry.coreFilterId == selectedCoreFilter;
            if (selectedCategory == AbyssalForgeProgressUtility.ArmorCategory) return selectedArmorFilter.NullOrEmpty() || selectedArmorFilter == ArmorFilterAll || entry.armorFilterId == selectedArmorFilter;
            if (selectedCategory == AbyssalForgeProgressUtility.ImplantsCategory) return selectedImplantsFilter.NullOrEmpty() || selectedImplantsFilter == ImplantsFilterAll || entry.implantFilterId == selectedImplantsFilter;
            if (selectedCategory == AbyssalForgeProgressUtility.TurretSystemsCategory) return TurretEntryMatchesFilter(entry, selectedTurretSystemsFilter);
            return true;
        }

        private static bool WeaponEntryMatchesFilter(ForgePatternEntry entry, string filter)
        {
            if (filter.NullOrEmpty() || filter == WeaponsFilterAll) return true;
            if (filter == WeaponsFilterHerald) return entry.weaponHerald;
            if (filter == WeaponsFilterMelee) return entry.weaponMelee;
            if (filter == WeaponsFilterRanged) return entry.weaponRanged;
            return true;
        }

        private static bool TurretEntryMatchesFilter(ForgePatternEntry entry, string filter)
        {
            if (filter.NullOrEmpty() || filter == TurretFilterAll)
            {
                return true;
            }

            return entry.turretFilterId == filter;
        }

        private bool EntryMatchesSearch(ForgePatternEntry entry, string searchText)
        {
            return SearchTextMatches(entry.searchText, searchText);
        }

        private int ComparePatternEntriesForCurrentView(ForgePatternEntry left, ForgePatternEntry right)
        {
            int result = GetActiveRecipeOrder(left).CompareTo(GetActiveRecipeOrder(right));
            if (result != 0) return result;
            result = left.requiredResidue.CompareTo(right.requiredResidue);
            if (result != 0) return result;
            return string.Compare(left.displayLabel, right.displayLabel, StringComparison.OrdinalIgnoreCase);
        }

        private int GetActiveRecipeOrder(ForgePatternEntry entry)
        {
            if (entry.recipe == null)
            {
                return 9999;
            }

            if (selectedCategory == AbyssalForgeProgressUtility.AllCategory)
            {
                string category = entry.category;
                if (category == AbyssalForgeProgressUtility.HeraldCategory)
                {
                    category = AbyssalForgeProgressUtility.WeaponsCategory;
                }

                return AbyssalForgeProgressUtility.GetCategoryOrderIndex(category) * 100 + AbyssalForgeProgressUtility.GetCategoryOrderIndex(entry.category);
            }

            return GetSubfilterOrderForEntry(entry);
        }

        private int GetSubfilterOrderForEntry(ForgePatternEntry entry)
        {
            if (selectedCategory == AbyssalForgeProgressUtility.CoreCategory) return entry.coreFilterOrder;
            if (selectedCategory == AbyssalForgeProgressUtility.WeaponsCategory) return entry.weaponFilterOrder;
            if (selectedCategory == AbyssalForgeProgressUtility.ArmorCategory) return entry.armorFilterOrder;
            if (selectedCategory == AbyssalForgeProgressUtility.ImplantsCategory) return entry.implantFilterOrder;
            if (selectedCategory == AbyssalForgeProgressUtility.TurretSystemsCategory) return entry.turretOrder;
            return AbyssalForgeProgressUtility.GetCategoryOrderIndex(entry.category);
        }

        private static bool ContainsRecipe(List<ForgePatternEntry> entries, RecipeDef recipe)
        {
            if (entries == null || recipe == null)
            {
                return false;
            }

            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i].recipe == recipe)
                {
                    return true;
                }
            }

            return false;
        }

        private void DrawPatternBrowser(Rect rect, MapComponent_AbyssalForgeProgress progress)
        {
            AbyssalForgeConsoleArt.DrawPanel(rect, false);
            Rect inner = rect.ContractedBy(10f);

            BuildFilteredPatternLists(progress);

            if (visiblePatternScratch.Count == 0)
            {
                SetSelectedPattern(null);
            }
            else if (selectedPattern == null || !ContainsRecipe(visiblePatternScratch, selectedPattern))
            {
                SetSelectedPattern(visiblePatternScratch[0].recipe);
            }

            float contentTop = inner.y;
            Rect searchRect = new Rect(inner.x, contentTop, inner.width, 30f);
            DrawPatternSearchRow(searchRect, visiblePatternScratch.Count, categoryPatternScratch.Count);
            contentTop += 36f;

            if (ShouldDrawSubfilterRow(selectedCategory))
            {
                Rect filterRect = new Rect(inner.x, contentTop, inner.width, 30f);
                DrawSubfilterRow(filterRect);
                contentTop += 38f;
            }

            Rect statusRect = new Rect(inner.x, contentTop, inner.width, 28f);
            DrawStatusFilterRow(statusRect, searchPatternScratch, statusScratch);
            contentTop += 34f;

            Rect outRect = new Rect(inner.x, contentTop, inner.width, inner.yMax - contentTop);
            DrawVirtualizedPatternCards(outRect, visiblePatternScratch, progress);
        }

        private void BuildFilteredPatternLists(MapComponent_AbyssalForgeProgress progress)
        {
            List<ForgePatternEntry> index = GetForgePatternIndex();
            int totalResidue = progress != null ? progress.TotalResidueOffered : -1;
            int tickBucket = CurrentGameTick() / PatternListCacheRefreshTicks;
            string currentCategory = selectedCategory ?? string.Empty;
            string currentSubfilter = GetSelectedSubfilter() ?? string.Empty;
            string currentStatus = selectedStatusFilter ?? string.Empty;
            string currentSearch = patternSearchText ?? string.Empty;

            if (filteredPatternCacheIndexVersion == patternIndexVersion
                && filteredPatternCacheResidue == totalResidue
                && filteredPatternCacheTickBucket == tickBucket
                && filteredPatternCacheCategory == currentCategory
                && filteredPatternCacheSubfilter == currentSubfilter
                && filteredPatternCacheStatus == currentStatus
                && filteredPatternCacheSearch == currentSearch)
            {
                return;
            }

            filteredPatternCacheIndexVersion = patternIndexVersion;
            filteredPatternCacheResidue = totalResidue;
            filteredPatternCacheTickBucket = tickBucket;
            filteredPatternCacheCategory = currentCategory;
            filteredPatternCacheSubfilter = currentSubfilter;
            filteredPatternCacheStatus = currentStatus;
            filteredPatternCacheSearch = currentSearch;

            categoryPatternScratch.Clear();
            searchPatternScratch.Clear();
            visiblePatternScratch.Clear();

            for (int i = 0; i < index.Count; i++)
            {
                ForgePatternEntry entry = index[i];
                if (EntryMatchesSelectedCategoryAndFilter(entry))
                {
                    categoryPatternScratch.Add(entry);
                }
            }

            categoryPatternScratch.Sort(ComparePatternEntriesForCurrentView);

            for (int i = 0; i < categoryPatternScratch.Count; i++)
            {
                ForgePatternEntry entry = categoryPatternScratch[i];
                if (EntryMatchesSearch(entry, currentSearch))
                {
                    searchPatternScratch.Add(entry);
                }
            }

            BuildStatusCache(searchPatternScratch, progress);

            for (int i = 0; i < searchPatternScratch.Count; i++)
            {
                ForgePatternEntry entry = searchPatternScratch[i];
                if (EntryMatchesSelectedStatus(entry, statusScratch))
                {
                    visiblePatternScratch.Add(entry);
                }
            }
        }

        private void DrawVirtualizedPatternCards(Rect outRect, List<ForgePatternEntry> entries, MapComponent_AbyssalForgeProgress progress)
        {
            const float scrollbarReserve = 18f;
            const int columns = 2;
            float contentWidth = Mathf.Max(120f, outRect.width - scrollbarReserve);
            float cardWidth = (contentWidth - 12f) / columns;
            bool turretSystemsMode = selectedCategory == AbyssalForgeProgressUtility.TurretSystemsCategory;
            float cardHeight = turretSystemsMode ? 196f : 180f;
            float rowPitch = cardHeight + 8f;
            int rows = Mathf.CeilToInt((entries?.Count ?? 0) / (float)columns);
            float viewHeight = Math.Max(outRect.height, rows * rowPitch);
            Rect viewRect = new Rect(0f, 0f, contentWidth, viewHeight);

            AbyssalStyledWidgets.BeginAbyssalScrollView(outRect, ref patternScrollPosition, viewRect);
            if (entries == null || entries.Count == 0)
            {
                Rect emptyRect = new Rect(0f, 0f, contentWidth, 70f);
                GUI.color = AbyssalForgeConsoleArt.TextDimColor;
                Text.Font = GameFont.Small;
                ABY_UIPolishUtility.SafeLabel(emptyRect.ContractedBy(12f), TranslateOrFallback("ABY_ForgeNoPatternsMatch", "No forge patterns match the current search/filter."));
                GUI.color = Color.white;
            }
            else
            {
                int firstVisibleRow = Mathf.Max(0, Mathf.FloorToInt(patternScrollPosition.y / rowPitch) - 1);
                int lastVisibleRow = Mathf.Min(rows - 1, Mathf.CeilToInt((patternScrollPosition.y + outRect.height) / rowPitch) + 1);
                int firstIndex = Mathf.Clamp(firstVisibleRow * columns, 0, entries.Count - 1);
                int lastIndexExclusive = Mathf.Min(entries.Count, (lastVisibleRow + 1) * columns);

                for (int i = firstIndex; i < lastIndexExclusive; i++)
                {
                    int column = i % columns;
                    int row = i / columns;
                    Rect cardRect = new Rect(column * (cardWidth + 12f), row * rowPitch, cardWidth, cardHeight);
                    ForgePatternEntry entry = entries[i];
                    RecipeDef recipe = entry.recipe;
                    bool decoded = ABY_ProtocolResearchGateUtility.IsDecodedForForge(recipe);
                    bool unlocked = progress != null && AbyssalForgeProgressUtility.IsRecipeUnlocked(recipe, progress.TotalResidueOffered);
                    bool freshlyUnlocked = progress != null && progress.IsRecentlyUnlocked(recipe);
                    if (!decoded)
                    {
                        DrawUnknownPatternCard(cardRect, recipe, turretSystemsMode && entry.isTurretSystem);
                    }
                    else if (turretSystemsMode && entry.isTurretSystem)
                    {
                        DrawTurretSystemPatternCard(cardRect, recipe, unlocked, freshlyUnlocked);
                    }
                    else
                    {
                        DrawPatternCard(cardRect, recipe, unlocked, freshlyUnlocked);
                    }
                }
            }
            AbyssalStyledWidgets.EndAbyssalScrollView(outRect, ref patternScrollPosition, viewRect);
        }

        private void BuildStatusCache(List<ForgePatternEntry> entries, MapComponent_AbyssalForgeProgress progress)
        {
            statusScratch.Clear();
            int totalResidue = progress != null ? progress.TotalResidueOffered : -1;
            if (lastStatusResidueSnapshot != totalResidue)
            {
                patternStatusCache.Clear();
                lastStatusResidueSnapshot = totalResidue;
            }

            if (entries == null)
            {
                return;
            }

            int refreshBudget = PatternStatusRefreshBudgetPerPass;
            for (int i = 0; i < entries.Count; i++)
            {
                RecipeDef recipe = entries[i].recipe;
                if (recipe == null || statusScratch.ContainsKey(recipe))
                {
                    continue;
                }

                ForgePatternStatus status = GetCachedPatternStatus(recipe, progress, false);
                if (refreshBudget > 0 && NeedsPatternStatusRefresh(recipe, progress))
                {
                    status = GetCachedPatternStatus(recipe, progress, true);
                    refreshBudget--;
                }

                statusScratch[recipe] = status;
            }
        }

        private bool EntryMatchesSelectedStatus(ForgePatternEntry entry, Dictionary<RecipeDef, ForgePatternStatus> statusByRecipe)
        {
            if (entry.recipe == null)
            {
                return false;
            }

            if (selectedStatusFilter.NullOrEmpty() || selectedStatusFilter == StatusFilterAll)
            {
                return true;
            }

            ForgePatternStatus status;
            if (statusByRecipe == null || !statusByRecipe.TryGetValue(entry.recipe, out status))
            {
                status = GetCachedPatternStatus(entry.recipe, forge?.ProgressComponent, false);
            }

            if (selectedStatusFilter == StatusFilterCraftable) return status == ForgePatternStatus.Craftable;
            if (selectedStatusFilter == StatusFilterMissing) return status == ForgePatternStatus.MissingMaterials;
            if (selectedStatusFilter == StatusFilterLocked) return status == ForgePatternStatus.Locked;
            if (selectedStatusFilter == StatusFilterNexus) return status == ForgePatternStatus.NexusLocked;
            return true;
        }

        private ForgePatternStatus GetCachedPatternStatus(RecipeDef recipe, MapComponent_AbyssalForgeProgress progress, bool forceRefresh)
        {
            if (recipe == null)
            {
                return ForgePatternStatus.Locked;
            }

            int tick = CurrentGameTick();
            int totalResidue = progress != null ? progress.TotalResidueOffered : -1;
            CachedPatternStatus cached;
            if (!forceRefresh && patternStatusCache.TryGetValue(recipe, out cached) && cached.residue == totalResidue)
            {
                return cached.status;
            }

            ForgePatternStatus status = ResolvePatternStatus(recipe, progress);
            patternStatusCache[recipe] = new CachedPatternStatus
            {
                status = status,
                tick = tick,
                residue = totalResidue
            };
            return status;
        }

        private bool NeedsPatternStatusRefresh(RecipeDef recipe, MapComponent_AbyssalForgeProgress progress)
        {
            if (recipe == null)
            {
                return false;
            }

            int totalResidue = progress != null ? progress.TotalResidueOffered : -1;
            CachedPatternStatus cached;
            if (!patternStatusCache.TryGetValue(recipe, out cached) || cached.residue != totalResidue)
            {
                return true;
            }

            return CurrentGameTick() - cached.tick >= PatternStatusCacheRefreshTicks;
        }

        private void InvalidateFilteredPatternCache()
        {
            filteredPatternCacheIndexVersion = -1;
            filteredPatternCacheResidue = int.MinValue;
            filteredPatternCacheTickBucket = -1;
            filteredPatternCacheCategory = string.Empty;
            filteredPatternCacheSubfilter = string.Empty;
            filteredPatternCacheStatus = string.Empty;
            filteredPatternCacheSearch = string.Empty;
        }

        private static int CurrentGameTick()
        {
            try
            {
                return Find.TickManager != null ? Find.TickManager.TicksGame : Environment.TickCount;
            }
            catch
            {
                return Environment.TickCount;
            }
        }

        private static string TranslateOrFallback(string key, string fallback)
        {
            if (key.NullOrEmpty())
            {
                return fallback ?? string.Empty;
            }

            try
            {
                string translated = key.Translate();
                if (!translated.NullOrEmpty() && translated != key)
                {
                    return translated;
                }
            }
            catch
            {
            }

            return fallback ?? key;
        }

        private static string TranslateOrFallback(string key, string fallbackFormat, params object[] args)
        {
            string format = TranslateOrFallback(key, fallbackFormat);
            try
            {
                return string.Format(format, args ?? new object[0]);
            }
            catch
            {
                return fallbackFormat ?? key ?? string.Empty;
            }
        }

        private static string GetRussianPluralAwareRequirementKey(string baseKey, int count)
        {
            int abs = Math.Abs(count);
            int lastTwo = abs % 100;
            int last = abs % 10;
            if (lastTwo >= 11 && lastTwo <= 14)
            {
                return baseKey + "_Many";
            }

            if (last == 1)
            {
                return baseKey + "_One";
            }

            if (last >= 2 && last <= 4)
            {
                return baseKey + "_Few";
            }

            return baseKey + "_Many";
        }

        private static string FormatRequirementCount(int count)
        {
            return TranslateOrFallback(GetRussianPluralAwareRequirementKey("ABY_ForgeRequirementCount", count), "{0} requirements", count);
        }

        private static string FormatMoreRequirements(int count)
        {
            return TranslateOrFallback(GetRussianPluralAwareRequirementKey("ABY_ForgeMoreRequirements", count), "+{0} more requirements", count);
        }

        private void ClearPatternStatusCache()
        {
            patternStatusCache.Clear();
            statusScratch.Clear();
            lastStatusResidueSnapshot = -1;
            InvalidateFilteredPatternCache();
        }

        private ForgePatternStatus ResolvePatternStatus(RecipeDef recipe, MapComponent_AbyssalForgeProgress progress)
        {
            if (recipe == null)
            {
                return ForgePatternStatus.Locked;
            }

            if (!ABY_ProtocolResearchGateUtility.IsDecodedForForge(recipe))
            {
                return ForgePatternStatus.NexusLocked;
            }

            if (progress == null || !AbyssalForgeProgressUtility.IsRecipeUnlocked(recipe, progress.TotalResidueOffered))
            {
                return ForgePatternStatus.Locked;
            }

            bool recipeAvailable = false;
            try
            {
                recipeAvailable = forge != null && recipe.AvailableNow && recipe.AvailableOnNow(forge);
            }
            catch (System.Exception ex)
            {
                ABY_UISafetyUtility.LogUIException("Forge pattern status availability", ex);
            }

            if (!recipeAvailable)
            {
                return ForgePatternStatus.Locked;
            }

            List<AbyssalForgeProgressUtility.IngredientAvailabilityEntry> entries = forge?.Map != null
                ? AbyssalForgeProgressUtility.GetIngredientAvailabilityEntries(forge.Map, recipe)
                : new List<AbyssalForgeProgressUtility.IngredientAvailabilityEntry>();
            bool hasAllMaterials = entries.All(entry => entry.IsSatisfied);
            return hasAllMaterials ? ForgePatternStatus.Craftable : ForgePatternStatus.MissingMaterials;
        }
        private static string BuildRecipeUnavailableButtonLabel(RecipeDef recipe)
        {
            string researchSummary = BuildMissingResearchSummary(recipe);
            if (!researchSummary.NullOrEmpty())
            {
                return CompactTextForCard(TranslateOrFallback("ABY_ForgePatternResearchShort", "Research: {0}", researchSummary), 24);
            }

            return TranslateOrFallback("ABY_ForgePatternUnavailable", "Unavailable");
        }

        private static string BuildRecipeUnavailableDetailLine(RecipeDef recipe)
        {
            string researchSummary = BuildMissingResearchSummary(recipe);
            if (!researchSummary.NullOrEmpty())
            {
                return TranslateOrFallback("ABY_ForgePatternResearchRequiredDetail", "Research required: {0}", researchSummary);
            }

            return TranslateOrFallback("ABY_ForgePatternUnavailableOnForge", "Unavailable on this forge");
        }

        private static string BuildRecipeUnavailableTooltip(RecipeDef recipe)
        {
            string researchSummary = BuildMissingResearchSummary(recipe);
            if (!researchSummary.NullOrEmpty())
            {
                return TranslateOrFallback("ABY_ForgePatternRequiredResearchTooltip", "Required research:\n{0}", researchSummary);
            }

            return TranslateOrFallback("ABY_ForgePatternUnavailableTooltip", "This pattern is unlocked, but the recipe is not currently available on this Forge.");
        }

        private static string BuildMissingResearchSummary(RecipeDef recipe)
        {
            List<string> labels = new List<string>();
            if (recipe == null)
            {
                return string.Empty;
            }

            AddMissingResearchLabel(labels, recipe.researchPrerequisite);

            if (recipe.researchPrerequisites != null)
            {
                for (int i = 0; i < recipe.researchPrerequisites.Count; i++)
                {
                    AddMissingResearchLabel(labels, recipe.researchPrerequisites[i]);
                }
            }

            return labels.Count > 0 ? string.Join(", ", labels.ToArray()) : string.Empty;
        }

        private static void AddMissingResearchLabel(List<string> labels, ResearchProjectDef project)
        {
            if (labels == null || project == null || project.IsFinished)
            {
                return;
            }

            string label = project.LabelCap.ToString();
            if (!label.NullOrEmpty() && !labels.Contains(label))
            {
                labels.Add(label);
            }
        }


        private void DrawStatusFilterRow(Rect rect, List<ForgePatternEntry> searchEntries, Dictionary<RecipeDef, ForgePatternStatus> statusByRecipe)
        {
            AbyssalForgeConsoleArt.Fill(rect, new Color(0.085f, 0.062f, 0.055f, 0.76f));
            AbyssalForgeConsoleArt.DrawOutline(rect, new Color(1f, 0.36f, 0.13f, 0.24f));

            List<ForgeFilterOption> options = GetStatusFilterOptions(searchEntries, statusByRecipe);
            if (options.Count == 0)
            {
                return;
            }

            GameFont oldFont = Text.Font;
            Text.Font = GameFont.Tiny;
            float gap = 6f;
            float totalWidth = -gap;
            float[] widths = new float[options.Count];
            for (int i = 0; i < options.Count; i++)
            {
                widths[i] = Mathf.Clamp(Text.CalcSize(options[i].label).x + 18f, 46f, 138f);
                totalWidth += widths[i] + gap;
            }

            float x = rect.x + Mathf.Max(8f, (rect.width - totalWidth) * 0.5f);
            float buttonHeight = 20f;
            float y = rect.y + (rect.height - buttonHeight) * 0.5f;
            for (int i = 0; i < options.Count; i++)
            {
                DrawStatusFilterChip(new Rect(x, y, widths[i], buttonHeight), options[i]);
                x += widths[i] + gap;
            }

            Text.Font = oldFont;
            Text.Anchor = TextAnchor.UpperLeft;
            GUI.color = Color.white;
        }

        private List<ForgeFilterOption> GetStatusFilterOptions(List<ForgePatternEntry> searchEntries, Dictionary<RecipeDef, ForgePatternStatus> statusByRecipe)
        {
            int all = searchEntries?.Count ?? 0;
            int craftable = CountStatus(statusByRecipe, ForgePatternStatus.Craftable);
            int missing = CountStatus(statusByRecipe, ForgePatternStatus.MissingMaterials);
            int locked = CountStatus(statusByRecipe, ForgePatternStatus.Locked);
            int nexus = CountStatus(statusByRecipe, ForgePatternStatus.NexusLocked);
            return new List<ForgeFilterOption>
            {
                new ForgeFilterOption(StatusFilterAll, TranslateOrFallback("ABY_ForgeStatus_All", "All {0}", all), new Color(0.72f, 0.66f, 0.56f, 1f)),
                new ForgeFilterOption(StatusFilterCraftable, TranslateOrFallback("ABY_ForgeStatus_Craftable", "Craftable {0}", craftable), new Color(0.45f, 0.88f, 0.48f, 1f)),
                new ForgeFilterOption(StatusFilterMissing, TranslateOrFallback("ABY_ForgeStatus_NeedsResources", "Needs: {0}", FormatRequirementCount(missing)), new Color(0.95f, 0.62f, 0.28f, 1f)),
                new ForgeFilterOption(StatusFilterLocked, TranslateOrFallback("ABY_ForgeStatus_Locked", "Locked {0}", locked), new Color(0.82f, 0.38f, 0.30f, 1f)),
                new ForgeFilterOption(StatusFilterNexus, TranslateOrFallback("ABY_ForgeStatus_Nexus", "Nexus {0}", nexus), new Color(0.74f, 0.50f, 0.92f, 1f))
            };
        }

        private static int CountStatus(Dictionary<RecipeDef, ForgePatternStatus> statusByRecipe, ForgePatternStatus status)
        {
            if (statusByRecipe == null || statusByRecipe.Count == 0)
            {
                return 0;
            }

            int count = 0;
            foreach (ForgePatternStatus value in statusByRecipe.Values)
            {
                if (value == status)
                {
                    count++;
                }
            }

            return count;
        }

        private void DrawStatusFilterChip(Rect rect, ForgeFilterOption option)
        {
            bool active = selectedStatusFilter == option.id;
            bool hovered = Mouse.IsOver(rect);
            Color color = option.color;
            Color fill = active
                ? new Color(color.r * 0.26f, color.g * 0.18f, color.b * 0.13f, 0.96f)
                : hovered
                    ? new Color(color.r * 0.18f, color.g * 0.13f, color.b * 0.10f, 0.88f)
                    : new Color(color.r * 0.10f, color.g * 0.08f, color.b * 0.07f, 0.72f);
            Color outline = active
                ? Color.Lerp(color, Color.white, 0.20f)
                : new Color(color.r, color.g, color.b, hovered ? 0.58f : 0.34f);

            AbyssalForgeConsoleArt.Fill(rect, fill);
            AbyssalForgeConsoleArt.DrawOutline(rect, outline);

            Text.Font = GameFont.Tiny;
            Text.Anchor = TextAnchor.MiddleCenter;
            GUI.color = active ? Color.white : AbyssalForgeConsoleArt.TextSoftColor;
            ABY_UIPolishUtility.SafeLabel(rect.ContractedBy(2f), option.label, 0f, 8f);

            if (Widgets.ButtonInvisible(rect, false))
            {
                if (selectedStatusFilter != option.id)
                {
                    selectedStatusFilter = option.id;
                    patternScrollPosition = Vector2.zero;
                    SetSelectedPattern(null);
                    SoundDefOf.Tick_Tiny.PlayOneShotOnCamera(null);
                }
            }

            TooltipHandler.TipRegion(rect, GetStatusFilterTooltip(option.id));
            Text.Anchor = TextAnchor.UpperLeft;
            GUI.color = Color.white;
        }

        private static string GetStatusFilterTooltip(string filter)
        {
            if (filter == StatusFilterCraftable) return TranslateOrFallback("ABY_ForgeStatusTooltip_Craftable", "Show decoded, unlocked patterns with all currently counted ingredients available.");
            if (filter == StatusFilterMissing) return TranslateOrFallback("ABY_ForgeStatusTooltip_NeedsResources", "Show unlocked patterns that still need ingredients. This is informational only: bills can still be queued and RimWorld will resolve materials through the normal bill system.");
            if (filter == StatusFilterLocked) return TranslateOrFallback("ABY_ForgeStatusTooltip_Locked", "Show decoded patterns blocked by residue, research, facility, or normal recipe availability.");
            if (filter == StatusFilterNexus) return TranslateOrFallback("ABY_ForgeStatusTooltip_Nexus", "Show patterns that still need Protocol Nexus decoding.");
            return TranslateOrFallback("ABY_ForgeStatusTooltip_All", "Show every pattern matching the current category, subfilter, and search.");
        }

        private void DrawPatternSearchRow(Rect rect, int shownCount, int totalCount)
        {
            AbyssalForgeConsoleArt.Fill(rect, new Color(0.10f, 0.075f, 0.065f, 0.82f));
            AbyssalForgeConsoleArt.DrawOutline(rect, new Color(1f, 0.36f, 0.13f, 0.35f));

            float labelWidth = 54f;
            Rect labelRect = new Rect(rect.x + 10f, rect.y + 5f, labelWidth, rect.height - 10f);
            Text.Font = GameFont.Tiny;
            Text.Anchor = TextAnchor.MiddleLeft;
            GUI.color = AbyssalForgeConsoleArt.TextDimColor;
            ABY_UIPolishUtility.SafeLabel(labelRect, TranslateOrFallback("ABY_ForgeSearchLabel", "Search"));

            Rect fieldRect = new Rect(labelRect.xMax + 4f, rect.y + 5f, Mathf.Max(120f, rect.width - labelWidth - 176f), rect.height - 10f);
            string previous = patternSearchText ?? string.Empty;
            patternSearchText = Widgets.TextField(fieldRect, previous);
            if (patternSearchText != previous)
            {
                patternScrollPosition = Vector2.zero;
                SetSelectedPattern(null);
            }

            if (patternSearchText.NullOrEmpty() && !Mouse.IsOver(fieldRect) && GUI.GetNameOfFocusedControl() != "ABYForgeSearch")
            {
                GUI.color = new Color(0.70f, 0.62f, 0.56f, 0.58f);
                ABY_UIPolishUtility.SafeLabel(fieldRect.ContractedBy(4f, 1f), TranslateOrFallback("ABY_ForgeSearchPlaceholder", "name, role, material…"));
                GUI.color = Color.white;
            }

            Rect clearRect = new Rect(fieldRect.xMax + 8f, rect.y + 4f, 58f, rect.height - 8f);
            if (AbyssalStyledWidgets.TextButton(clearRect, TranslateOrFallback("ABY_ForgeSearchClear", "Clear"), !patternSearchText.NullOrEmpty()))
            {
                patternSearchText = string.Empty;
                patternScrollPosition = Vector2.zero;
                SetSelectedPattern(null);
                SoundDefOf.Tick_Tiny.PlayOneShotOnCamera(null);
            }

            Rect countRect = new Rect(clearRect.xMax + 8f, rect.y + 5f, rect.xMax - clearRect.xMax - 16f, rect.height - 10f);
            Text.Anchor = TextAnchor.MiddleRight;
            GUI.color = AbyssalForgeConsoleArt.TextDimColor;
            ABY_UIPolishUtility.SafeLabel(countRect, shownCount + " / " + totalCount);
            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperLeft;
            Text.Font = GameFont.Small;
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


        private static void DrawForgeTierRail(Rect cardRect, RecipeDef recipe, bool unlocked, bool decoded, bool emphasized = false)
        {
            if (recipe == null)
            {
                return;
            }

            AbyssalForgeProgressUtility.ForgeTierBand tier = AbyssalForgeProgressUtility.GetForgeTierBand(recipe);
            Color tierColor = GetForgeTierColor(tier, unlocked, decoded);
            Color hotColor = GetForgeTierHotColor(tier, unlocked, decoded);
            Rect railRect = new Rect(cardRect.x + 3f, cardRect.y + 5f, 5f, Mathf.Max(4f, cardRect.height - 10f));

            float glowIntensity = emphasized ? (decoded ? 0.31f : 0.17f) : (decoded ? 0.18f : 0.09f);
            float stateMultiplier = emphasized ? (unlocked ? 1f : 0.70f) : (unlocked ? 0.72f : 0.46f);
            float railAlpha = emphasized ? (decoded ? 0.78f : 0.54f) : (decoded ? 0.64f : 0.42f);
            float coreAlpha = emphasized ? tierColor.a : tierColor.a * 0.78f;
            float hotAlpha = emphasized ? hotColor.a : hotColor.a * 0.58f;
            float outlineAlpha = emphasized ? (decoded ? 0.74f : 0.42f) : (decoded ? 0.48f : 0.24f);

            DrawForgeTierGlow(railRect, hotColor, glowIntensity, stateMultiplier);
            AbyssalForgeConsoleArt.Fill(railRect, new Color(tierColor.r * 0.40f, tierColor.g * 0.30f, tierColor.b * 0.30f, railAlpha));
            AbyssalForgeConsoleArt.Fill(new Rect(railRect.x + 1f, railRect.y + 1f, 3f, railRect.height - 2f), new Color(tierColor.r, tierColor.g, tierColor.b, coreAlpha));
            Color innerHot = Color.Lerp(hotColor, Color.white, emphasized ? 0.30f : 0.14f);
            innerHot.a = decoded ? hotAlpha : hotAlpha * 0.72f;
            AbyssalForgeConsoleArt.Fill(new Rect(railRect.x + 2f, railRect.y + 2f, 1f, railRect.height - 4f), innerHot);
            AbyssalForgeConsoleArt.DrawOutline(new Rect(railRect.x - 1f, railRect.y, railRect.width + 2f, railRect.height), new Color(hotColor.r, hotColor.g, hotColor.b, outlineAlpha));
            TooltipHandler.TipRegion(railRect.ExpandedBy(6f), AbyssalForgeProgressUtility.GetForgeTierTooltip(recipe));
        }

        private static void DrawForgeTierBadge(Rect rect, RecipeDef recipe, bool compact)
        {
            if (recipe == null)
            {
                return;
            }

            AbyssalForgeProgressUtility.ForgeTierBand tier = AbyssalForgeProgressUtility.GetForgeTierBand(recipe);
            Color tierColor = GetForgeTierColor(tier, true, true);
            Color hotColor = GetForgeTierHotColor(tier, true, true);
            Color fill = new Color(tierColor.r * 0.15f, tierColor.g * 0.10f, tierColor.b * 0.11f, compact ? 0.90f : 0.94f);
            Color outline = new Color(hotColor.r, hotColor.g, hotColor.b, compact ? 0.88f : 0.96f);

            DrawForgeTierGlow(rect, hotColor, compact ? 0.16f : 0.24f, 0.86f);
            AbyssalForgeConsoleArt.Fill(rect, fill);
            AbyssalForgeConsoleArt.Fill(new Rect(rect.x + 1f, rect.y + 1f, 3f, Mathf.Max(1f, rect.height - 2f)), new Color(hotColor.r, hotColor.g, hotColor.b, compact ? 0.86f : 0.96f));
            AbyssalForgeConsoleArt.DrawOutline(rect, outline);
            AbyssalForgeConsoleArt.DrawOutline(rect.ContractedBy(1f), new Color(tierColor.r, tierColor.g, tierColor.b, compact ? 0.28f : 0.38f));

            GameFont oldFont = Text.Font;
            TextAnchor oldAnchor = Text.Anchor;
            Color oldColor = GUI.color;
            Text.Font = GameFont.Tiny;
            Text.Anchor = TextAnchor.MiddleCenter;
            GUI.color = Color.Lerp(hotColor, Color.white, compact ? 0.54f : 0.64f);
            ABY_UIPolishUtility.SafeLabel(new Rect(rect.x + 4f, rect.y, rect.width - 5f, rect.height).ContractedBy(2f), AbyssalForgeProgressUtility.GetForgeTierLabel(tier));
            Text.Font = oldFont;
            Text.Anchor = oldAnchor;
            GUI.color = oldColor;

            TooltipHandler.TipRegion(rect.ExpandedBy(2f), AbyssalForgeProgressUtility.GetForgeTierTooltip(recipe));
        }

        private static void DrawForgeTierGlow(Rect rect, Color color, float intensity, float stateMultiplier)
        {
            if (intensity <= 0f || stateMultiplier <= 0f)
            {
                return;
            }

            float alpha = Mathf.Clamp01(color.a * intensity * stateMultiplier);
            AbyssalForgeConsoleArt.Fill(rect.ExpandedBy(7f), new Color(color.r, color.g, color.b, alpha * 0.16f));
            AbyssalForgeConsoleArt.Fill(rect.ExpandedBy(4f), new Color(color.r, color.g, color.b, alpha * 0.24f));
            AbyssalForgeConsoleArt.Fill(rect.ExpandedBy(2f), new Color(color.r, color.g, color.b, alpha * 0.32f));
        }

        private static Color GetForgeTierColor(AbyssalForgeProgressUtility.ForgeTierBand tier, bool unlocked, bool decoded)
        {
            Color color;
            switch (tier)
            {
                case AbyssalForgeProgressUtility.ForgeTierBand.Signal:
                    color = new Color(0.95f, 0.42f, 0.16f, 0.96f);
                    break;
                case AbyssalForgeProgressUtility.ForgeTierBand.Breach:
                    color = new Color(1.00f, 0.16f, 0.07f, 0.97f);
                    break;
                case AbyssalForgeProgressUtility.ForgeTierBand.Archon:
                    color = new Color(0.78f, 0.04f, 0.27f, 0.97f);
                    break;
                case AbyssalForgeProgressUtility.ForgeTierBand.Reactor:
                    color = new Color(1.00f, 0.80f, 0.25f, 0.98f);
                    break;
                case AbyssalForgeProgressUtility.ForgeTierBand.Dominion:
                    color = new Color(0.44f, 0.24f, 1.00f, 0.97f);
                    break;
                default:
                    color = new Color(0.92f, 0.90f, 0.68f, 0.99f);
                    break;
            }

            if (!decoded)
            {
                color = Color.Lerp(color, new Color(0.34f, 0.31f, 0.30f, color.a), 0.50f);
            }
            else if (!unlocked)
            {
                color = Color.Lerp(color, new Color(0.43f, 0.31f, 0.27f, color.a), 0.24f);
            }

            return color;
        }

        private static Color GetForgeTierHotColor(AbyssalForgeProgressUtility.ForgeTierBand tier, bool unlocked, bool decoded)
        {
            Color hot;
            switch (tier)
            {
                case AbyssalForgeProgressUtility.ForgeTierBand.Signal:
                    hot = new Color(1.00f, 0.58f, 0.24f, 0.98f);
                    break;
                case AbyssalForgeProgressUtility.ForgeTierBand.Breach:
                    hot = new Color(1.00f, 0.32f, 0.16f, 0.98f);
                    break;
                case AbyssalForgeProgressUtility.ForgeTierBand.Archon:
                    hot = new Color(1.00f, 0.10f, 0.38f, 0.98f);
                    break;
                case AbyssalForgeProgressUtility.ForgeTierBand.Reactor:
                    hot = new Color(1.00f, 0.94f, 0.55f, 0.99f);
                    break;
                case AbyssalForgeProgressUtility.ForgeTierBand.Dominion:
                    hot = new Color(0.66f, 0.44f, 1.00f, 0.98f);
                    break;
                default:
                    hot = new Color(1.00f, 0.98f, 0.78f, 0.99f);
                    break;
            }

            if (!decoded)
            {
                hot = Color.Lerp(hot, new Color(0.42f, 0.38f, 0.36f, hot.a), 0.52f);
            }
            else if (!unlocked)
            {
                hot = Color.Lerp(hot, new Color(0.50f, 0.36f, 0.31f, hot.a), 0.26f);
            }

            return hot;
        }

        private static void DrawProductPreviewIcon(Rect rect, ThingDef product, float alpha = 0.96f)
        {
            if (product?.uiIcon == null)
            {
                return;
            }

            Color oldColor = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, alpha);
            GUI.DrawTexture(rect, product.uiIcon, ScaleMode.ScaleToFit, true);
            GUI.color = oldColor;
            TooltipHandler.TipRegion(rect, product.LabelCap);
        }

        private void DrawUnknownPatternCard(Rect rect, RecipeDef recipe, bool turretCard)
        {
            AbyssalForgeConsoleArt.DrawPanel(rect, false);
            if (AbyssalStyledWidgets.UseEnhancedTheme)
            {
                Rect inset = rect.ContractedBy(3f);
                AbyssalForgeConsoleArt.Fill(inset, new Color(0.018f, 0.014f, 0.012f, 0.38f));
                AbyssalForgeConsoleArt.DrawOutline(inset, new Color(0.92f, 0.25f, 0.08f, 0.34f));
            }
            else
            {
                AbyssalForgeConsoleArt.Fill(rect.ContractedBy(3f), new Color(0.012f, 0.010f, 0.010f, 0.46f));
                AbyssalForgeConsoleArt.DrawOutline(rect.ContractedBy(3f), new Color(0.92f, 0.28f, 0.10f, 0.42f));
            }

            DrawForgeTierRail(rect, recipe, false, true, Mouse.IsOver(rect));
            string category = AbyssalForgeProgressUtility.GetCategory(recipe);

            Rect tagRect = new Rect(rect.xMax - 86f, rect.y + 10f, 76f, 18f);
            AbyssalForgeConsoleArt.DrawTag(tagRect, "ABY_ForgeUnknownTag".Translate(), false);

            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;
            GUI.color = new Color(1f, 0.82f, 0.62f, 1f);
            ABY_UIPolishUtility.SafeLabel(new Rect(rect.x + 12f, rect.y + 12f, rect.width - 108f, 24f), ABY_ProtocolResearchGateUtility.GetForgeDisplayLabel(recipe));

            Text.Font = GameFont.Tiny;
            GUI.color = AbyssalForgeConsoleArt.TextDimColor;
            ABY_UIPolishUtility.SafeLabel(new Rect(rect.x + 12f, rect.y + 36f, rect.width - 24f, 36f), ABY_ProtocolResearchGateUtility.GetUnknownHint(recipe));

            GUI.color = new Color(0.92f, 0.62f, 0.42f, 1f);
            ABY_UIPolishUtility.SafeLabel(new Rect(rect.x + 10f, rect.y + (turretCard ? 96f : 70f), rect.width - 20f, 18f), "ABY_ForgeUnknownResidueLine".Translate(AbyssalForgeProgressUtility.GetRequiredResidue(recipe)));

            GUI.color = AbyssalForgeConsoleArt.TextDimColor;
            ABY_UIPolishUtility.SafeLabel(new Rect(rect.x + 10f, rect.y + (turretCard ? 118f : 92f), rect.width - 20f, 40f), "ABY_ForgeUnknownMaskedDetails".Translate(AbyssalForgeProgressUtility.GetCategoryLabel(category)));
            GUI.color = Color.white;

            Rect buttonRect = new Rect(rect.x + rect.width - 120f, rect.y + rect.height - 34f, 108f, 28f);
            AbyssalStyledWidgets.TextButton(buttonRect, "ABY_ForgeUnknownDecodeButton".Translate(), false);

            DrawSelectedPatternOutline(rect, recipe);
            HandlePatternCardSelection(rect, recipe);
            TooltipHandler.TipRegion(rect, ABY_ProtocolResearchGateUtility.GetUnknownHint(recipe) + "\n\n" + "ABY_ForgeUnknownTooltip".Translate());
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;
            GUI.color = Color.white;
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
            DrawForgeTierRail(rect, recipe, unlocked, true, freshlyUnlocked || IsSelectedPattern(recipe) || Mouse.IsOver(rect));

            ThingDef product = AbyssalForgeProgressUtility.GetPrimaryProduct(recipe);
            ABY_TurretModuleDef module = ABY_ModularTurretUtility.GetModuleForThingDef(product);
            CompProperties_AbyssalModularTurret chassisProps = product?.GetCompProperties<CompProperties_AbyssalModularTurret>();
            bool isChassis = chassisProps != null;
            Color slotColor = module != null ? ABY_ModularTurretUtility.SlotColor(module.slot) : new Color(0.62f, 0.60f, 0.55f, 1f);
            Rect productIconRect = new Rect(rect.x + 12f, rect.y + 36f, 38f, 38f);
            DrawProductPreviewIcon(productIconRect, product, unlocked ? 0.98f : 0.74f);
            float contentX = product != null && product.uiIcon != null ? rect.x + 58f : rect.x + 12f;

            string slotBadge = GetTurretRecipeSlotBadge(module, isChassis);
            Rect badgeRect = new Rect(rect.x + 12f, rect.y + 10f, isChassis ? 72f : 66f, 20f);
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
            ABY_UIPolishUtility.SafeLabel(new Rect(contentX, rect.y + 33f, rect.xMax - contentX - 88f, 22f), AbyssalForgeProgressUtility.GetRecipeDisplayLabel(recipe));

            Text.Font = GameFont.Tiny;
            GUI.color = AbyssalForgeConsoleArt.TextDimColor;
            ABY_UIPolishUtility.SafeLabel(new Rect(contentX, rect.y + 55f, rect.xMax - contentX - 18f, 18f), BuildTurretCardSubtitle(module, chassisProps));
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
                ABY_UIPolishUtility.SafeLabel(new Rect(rect.x + 10f, rect.y + 146f + shownEntries * 17f, rect.width - 142f, 17f), FormatMoreRequirements(entries.Count - shownEntries));
                GUI.color = Color.white;
            }

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
            string actionTooltip = null;
            if (!unlocked)
            {
                actionLabel = "ABY_ForgePatternLocked".Translate();
            }
            else if (recipeAvailable)
            {
                actionLabel = "ABY_ForgePatternAddBill".Translate();
            }
            else
            {
                actionLabel = BuildRecipeUnavailableButtonLabel(recipe);
                actionTooltip = BuildRecipeUnavailableTooltip(recipe);
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
            if (!actionTooltip.NullOrEmpty())
            {
                TooltipHandler.TipRegion(buttonRect, actionTooltip);
            }

            DrawSelectedPatternOutline(rect, recipe);
            HandlePatternCardSelection(rect, recipe);
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
                tooltipLines.Add(module.LocalizedLabelCap);
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

            tooltipLines.Add(AbyssalForgeProgressUtility.GetForgeTierDisplayLine(recipe));

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
                ABY_UIPolishUtility.SafeLabel(rect.ContractedBy(10f), TranslateOrFallback("ABY_ForgeMissingPattern", "Missing forge pattern"));
                return;
            }

            AbyssalForgeConsoleArt.DrawPanel(rect, unlocked);
            AbyssalForgeConsoleArt.DrawPatternCardPulse(rect, unlocked, freshlyUnlocked);
            DrawForgeTierRail(rect, recipe, unlocked, true, freshlyUnlocked || IsSelectedPattern(recipe) || Mouse.IsOver(rect));

            ThingDef product = AbyssalForgeProgressUtility.GetPrimaryProduct(recipe);
            Rect productIconRect = new Rect(rect.x + 12f, rect.y + 12f, 42f, 42f);
            DrawProductPreviewIcon(productIconRect, product, unlocked ? 0.98f : 0.74f);
            float contentX = product != null && product.uiIcon != null ? rect.x + 62f : rect.x + 12f;

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

            Rect labelRect = new Rect(contentX, rect.y + 10f, rect.xMax - contentX - 88f, 22f);
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;
            ABY_UIPolishUtility.SafeLabel(labelRect, AbyssalForgeProgressUtility.GetRecipeDisplayLabel(recipe));

            GUI.color = AbyssalForgeConsoleArt.TextDimColor;
            ABY_UIPolishUtility.SafeLabel(new Rect(contentX, rect.y + 31f, rect.xMax - contentX - 18f, 18f), AbyssalForgeProgressUtility.GetPatternBrowserSummary(recipe));

            int primaryProductCount = AbyssalForgeProgressUtility.GetPrimaryProductCount(recipe);
            if (primaryProductCount > 1)
            {
                ABY_UIPolishUtility.SafeLabel(new Rect(contentX, rect.y + 48f, rect.xMax - contentX - 18f, 18f), "ABY_ForgePatternOutputCount".Translate(primaryProductCount));
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
                ABY_UIPolishUtility.SafeLabel(new Rect(rect.x + 10f, rect.y + 104f + shownEntries * 18f, rect.width - 20f, 18f), FormatMoreRequirements(entries.Count - shownEntries));
                GUI.color = Color.white;
            }

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
            string actionTooltip = null;
            if (!unlocked)
            {
                actionLabel = "ABY_ForgePatternLocked".Translate();
            }
            else if (recipeAvailable)
            {
                actionLabel = "ABY_ForgePatternAddBill".Translate();
            }
            else
            {
                actionLabel = BuildRecipeUnavailableButtonLabel(recipe);
                actionTooltip = BuildRecipeUnavailableTooltip(recipe);
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
            if (!actionTooltip.NullOrEmpty())
            {
                TooltipHandler.TipRegion(buttonRect, actionTooltip);
            }

            List<string> tooltipLines = new List<string>
            {
                AbyssalForgeProgressUtility.GetPatternBrowserSummary(recipe),
                AbyssalForgeProgressUtility.GetForgeTierDisplayLine(recipe)
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

            DrawSelectedPatternOutline(rect, recipe);
            HandlePatternCardSelection(rect, recipe);

            string tooltip = string.Join("\n", tooltipLines.Where(line => line != null).ToArray()).Trim();
            if (!tooltip.NullOrEmpty())
            {
                TooltipHandler.TipRegion(rect, tooltip);
            }
        }

        private void DrawIngredientStateLine(Rect rect, AbyssalForgeProgressUtility.IngredientAvailabilityEntry entry)
        {
            GUI.color = Color.white;
            if (entry == null)
            {
                ABY_UIPolishUtility.SafeLabel(rect, "missing ingredient");
                return;
            }

            Rect labelRect;
            Rect countRect = new Rect(rect.xMax - 70f, rect.y, 70f, rect.height);
            if (AbyssalStyledWidgets.UseEnhancedTheme)
            {
                Rect badgeRect = new Rect(rect.x, rect.y + 1f, 14f, 14f);
                AbyssalStyledWidgets.DrawCategoryIcon(badgeRect, entry.IsSatisfied ? AbyssalStyledWidgets.AbyssalCategoryIcon.Ready : AbyssalStyledWidgets.AbyssalCategoryIcon.Forbidden, Color.white, entry.IsSatisfied ? 0.82f : 0.72f);
                labelRect = new Rect(rect.x + 18f, rect.y, rect.width - 90f, rect.height);
            }
            else
            {
                labelRect = new Rect(rect.x, rect.y, rect.width - 72f, rect.height);
            }

            GUI.color = Color.white;
            ABY_UIPolishUtility.SafeLabel(labelRect, ABY_UISafetyUtility.SafeString(entry.label, "ingredient"));
            GUI.color = entry.IsSatisfied ? new Color(0.72f, 1f, 0.74f, 1f) : new Color(1f, 0.58f, 0.52f, 1f);
            Text.Anchor = TextAnchor.UpperRight;
            ABY_UIPolishUtility.SafeLabel(countRect, entry.availableCount + "/" + entry.requiredCount);
            Text.Anchor = TextAnchor.UpperLeft;
            GUI.color = Color.white;
        }

        private void DrawSelectedPatternPanel(Rect rect, MapComponent_AbyssalForgeProgress progress)
        {
            AbyssalForgeConsoleArt.DrawPanel(rect, false);
            Rect inner = rect.ContractedBy(10f);
            AbyssalForgeConsoleArt.DrawSectionTitle(new Rect(inner.x, inner.y, inner.width, 22f), TranslateOrFallback("ABY_ForgeSelectedPatternHeader", "Selected pattern"));
            if (AbyssalStyledWidgets.UseEnhancedTheme)
            {
                AbyssalStyledWidgets.DrawDividerHorizontal(new Rect(inner.x, inner.y + 22f, inner.width, 5f), 0.34f);
            }

            if (selectedPattern == null)
            {
                GUI.color = AbyssalForgeConsoleArt.TextDimColor;
                Text.Font = GameFont.Small;
                ABY_UIPolishUtility.SafeLabel(new Rect(inner.x, inner.y + 34f, inner.width, 52f), TranslateOrFallback("ABY_ForgeSelectedPatternEmpty", "Select a pattern in the browser to inspect materials, lock state, output and action."));
                GUI.color = Color.white;
                return;
            }

            RecipeDef recipe = selectedPattern;
            ThingDef product = AbyssalForgeProgressUtility.GetPrimaryProduct(recipe);
            bool decoded = ABY_ProtocolResearchGateUtility.IsDecodedForForge(recipe);
            bool unlocked = progress != null && AbyssalForgeProgressUtility.IsRecipeUnlocked(recipe, progress.TotalResidueOffered);
            bool freshlyUnlocked = progress != null && progress.IsRecentlyUnlocked(recipe);

            bool recipeAvailable = false;
            try
            {
                recipeAvailable = decoded && forge != null && recipe.AvailableNow && recipe.AvailableOnNow(forge);
            }
            catch (System.Exception ex)
            {
                ABY_UISafetyUtility.LogUIException("Forge selected pattern availability", ex);
            }

            string actionLabel;
            string actionDetailLine = null;
            string actionTooltip = null;
            bool actionEnabled = false;
            if (!decoded)
            {
                actionLabel = TranslateOrFallback("ABY_ForgeUnknownDecodeButton", "Decode in Nexus");
            }
            else if (!unlocked)
            {
                actionLabel = "ABY_ForgePatternLocked".Translate();
            }
            else if (recipeAvailable)
            {
                actionLabel = "ABY_ForgePatternAddBill".Translate();
                actionEnabled = true;
            }
            else
            {
                actionLabel = BuildRecipeUnavailableButtonLabel(recipe);
                actionDetailLine = BuildRecipeUnavailableDetailLine(recipe);
                actionTooltip = BuildRecipeUnavailableTooltip(recipe);
            }

            Rect footerRect = new Rect(inner.x, inner.yMax - 34f, inner.width, 32f);
            Rect scrollOutRect = new Rect(inner.x, inner.y + 30f, inner.width, Mathf.Max(44f, footerRect.y - inner.y - 36f));
            const float scrollbarReserve = 18f;
            float contentWidth = Mathf.Max(80f, scrollOutRect.width - scrollbarReserve);
            float y = 4f;

            Text.Font = GameFont.Small;
            float summaryHeight = Text.CalcHeight(decoded ? AbyssalForgeProgressUtility.GetPatternBrowserSummary(recipe) : ABY_ProtocolResearchGateUtility.GetUnknownHint(recipe), contentWidth);
            float detailHeight = 0f;
            string details = decoded ? AbyssalForgeProgressUtility.GetPatternBrowserDetails(recipe) : TranslateOrFallback("ABY_ForgeUnknownTooltip", "Open the Protocol Nexus and decode the linked project to reveal this pattern.");
            if (!details.NullOrEmpty())
            {
                detailHeight = Text.CalcHeight(details, contentWidth);
            }

            List<AbyssalForgeProgressUtility.IngredientAvailabilityEntry> entries = forge?.Map != null
                ? AbyssalForgeProgressUtility.GetIngredientAvailabilityEntries(forge.Map, recipe)
                : new List<AbyssalForgeProgressUtility.IngredientAvailabilityEntry>();
            float requirementsHeight = 22f + Mathf.Max(20f, entries.Count * 17f + (entries.Count > 0 ? 0f : 2f));
            float contentHeight = Mathf.Max(scrollOutRect.height, 94f + Mathf.Clamp(summaryHeight, 34f, 120f) + (details.NullOrEmpty() ? 0f : Mathf.Clamp(detailHeight, 34f, 130f) + 8f) + requirementsHeight + 18f);
            Rect viewRect = new Rect(0f, 0f, contentWidth, contentHeight);

            AbyssalStyledWidgets.BeginAbyssalScrollView(scrollOutRect, ref selectedPatternScrollPosition, viewRect);

            Rect iconRect = new Rect(0f, y, 54f, 54f);
            if (product?.uiIcon != null)
            {
                DrawProductPreviewIcon(iconRect, product, unlocked ? 1f : 0.76f);
            }
            else
            {
                AbyssalForgeConsoleArt.Fill(iconRect, new Color(0.08f, 0.055f, 0.045f, 0.92f));
                AbyssalForgeConsoleArt.DrawOutline(iconRect, new Color(0.9f, 0.32f, 0.12f, 0.52f));
            }

            Rect infoRect = new Rect(contentWidth - 26f, y, 24f, 24f);
            Def infoDef = (Def)product ?? recipe;
            if (infoDef != null)
            {
                Widgets.InfoCardButton(infoRect.x, infoRect.y, infoDef);
            }

            Rect titleRect = new Rect(iconRect.xMax + 10f, y - 2f, contentWidth - 94f, 24f);
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;
            GUI.color = decoded ? Color.white : new Color(1f, 0.82f, 0.62f, 1f);
            ABY_UIPolishUtility.SafeLabel(titleRect, decoded ? AbyssalForgeProgressUtility.GetRecipeDisplayLabel(recipe) : ABY_ProtocolResearchGateUtility.GetForgeDisplayLabel(recipe));

            Text.Font = GameFont.Tiny;
            GUI.color = AbyssalForgeConsoleArt.TextDimColor;
            string categoryLine = AbyssalForgeProgressUtility.GetCategoryLabel(AbyssalForgeProgressUtility.GetCategory(recipe));
            string subfilter = GetSelectedSubfilter();
            if (!subfilter.NullOrEmpty() && subfilter != "All")
            {
                categoryLine += " • " + GetSubfilterLabel(selectedCategory, subfilter);
            }
            const float selectedTierBadgeWidth = 76f;
            DrawForgeTierBadge(new Rect(iconRect.xMax + 10f, y + 22f, selectedTierBadgeWidth, 17f), recipe, false);
            ABY_UIPolishUtility.SafeLabel(new Rect(iconRect.xMax + 10f + selectedTierBadgeWidth + 8f, y + 22f, Mathf.Max(40f, contentWidth - 102f - selectedTierBadgeWidth), 18f), categoryLine);

            string lockLine = unlocked
                ? "ABY_ForgePatternUnlockedAt".Translate(AbyssalForgeProgressUtility.GetRequiredResidue(recipe)).ToString()
                : "ABY_ForgePatternLockedAt".Translate(AbyssalForgeProgressUtility.GetRequiredResidue(recipe)).ToString();
            GUI.color = unlocked ? new Color(1f, 0.78f, 0.58f, 1f) : new Color(0.92f, 0.52f, 0.45f, 1f);
            ABY_UIPolishUtility.SafeLabel(new Rect(iconRect.xMax + 10f, y + 40f, contentWidth - 94f, 18f), decoded ? lockLine : TranslateOrFallback("ABY_ForgeUnknownDecodeShort", "Decode required in Protocol Nexus"));

            y += 68f;
            GUI.color = decoded ? AbyssalForgeConsoleArt.TextSoftColor : AbyssalForgeConsoleArt.TextDimColor;
            Text.Font = GameFont.Tiny;
            string summary = decoded ? AbyssalForgeProgressUtility.GetPatternBrowserSummary(recipe) : ABY_ProtocolResearchGateUtility.GetUnknownHint(recipe);
            float clampedSummaryHeight = Mathf.Clamp(Text.CalcHeight(summary, contentWidth), 34f, 120f);
            ABY_UIPolishUtility.SafeLabel(new Rect(0f, y, contentWidth, clampedSummaryHeight), summary);
            y += clampedSummaryHeight + 8f;

            if (!details.NullOrEmpty())
            {
                GUI.color = AbyssalForgeConsoleArt.TextDimColor;
                float clampedDetailHeight = Mathf.Clamp(Text.CalcHeight(details, contentWidth), 34f, 130f);
                ABY_UIPolishUtility.SafeLabel(new Rect(0f, y, contentWidth, clampedDetailHeight), details);
                y += clampedDetailHeight + 10f;
            }

            GUI.color = Color.white;
            Text.Font = GameFont.Tiny;
            ABY_UIPolishUtility.SafeLabel(new Rect(0f, y, contentWidth, 18f), "ABY_ForgePatternRequirementsLabel".Translate());
            y += 18f;

            if (entries.Count == 0)
            {
                GUI.color = AbyssalForgeConsoleArt.TextDimColor;
                ABY_UIPolishUtility.SafeLabel(new Rect(0f, y, contentWidth, 18f), "ABY_ForgePatternNoMaterialData".Translate());
                y += 20f;
            }
            else
            {
                for (int i = 0; i < entries.Count; i++)
                {
                    DrawIngredientStateLine(new Rect(0f, y, contentWidth - 6f, 17f), entries[i]);
                    y += 17f;
                }
            }

            AbyssalStyledWidgets.EndAbyssalScrollView(scrollOutRect, ref selectedPatternScrollPosition, viewRect);

            if (!actionDetailLine.NullOrEmpty())
            {
                float blockerX = freshlyUnlocked ? footerRect.x + 58f : footerRect.x;
                float blockerWidth = Mathf.Max(80f, footerRect.xMax - 132f - blockerX);
                Rect blockerRect = new Rect(blockerX, footerRect.y + 1f, blockerWidth, 30f);
                GUI.color = new Color(0.98f, 0.62f, 0.36f, 1f);
                Text.Font = GameFont.Tiny;
                ABY_UIPolishUtility.SafeLabel(blockerRect, CompactTextForCard(actionDetailLine, 90));
                GUI.color = Color.white;
            }

            Rect buttonRect = new Rect(footerRect.xMax - 124f, footerRect.y + 2f, 118f, 28f);
            if (AbyssalStyledWidgets.TextButton(buttonRect, actionLabel, actionEnabled))
            {
                AddBill(recipe);
            }
            if (!actionTooltip.NullOrEmpty())
            {
                TooltipHandler.TipRegion(buttonRect, actionTooltip);
            }

            if (freshlyUnlocked)
            {
                Rect newRect = new Rect(footerRect.x, footerRect.y + 4f, 54f, 18f);
                AbyssalForgeConsoleArt.DrawTag(newRect, "ABY_ForgePatternNew".Translate(), true);
            }

            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;
            GUI.color = Color.white;
        }

        private void SetSelectedPattern(RecipeDef recipe)
        {
            if (selectedPattern == recipe)
            {
                return;
            }

            selectedPattern = recipe;
            selectedPatternScrollPosition = Vector2.zero;
        }

        private bool IsSelectedPattern(RecipeDef recipe)
        {
            return recipe != null && selectedPattern == recipe;
        }

        private void HandlePatternCardSelection(Rect rect, RecipeDef recipe)
        {
            if (recipe == null)
            {
                return;
            }

            Rect hitRect = new Rect(rect.x, rect.y, rect.width, Mathf.Max(1f, rect.height - 38f));
            if (Widgets.ButtonInvisible(hitRect, false))
            {
                if (selectedPattern != recipe)
                {
                    SetSelectedPattern(recipe);
                    SoundDefOf.Tick_Tiny.PlayOneShotOnCamera(null);
                }
            }
        }

        private void DrawSelectedPatternOutline(Rect rect, RecipeDef recipe)
        {
            if (!IsSelectedPattern(recipe))
            {
                return;
            }

            Rect outlineRect = rect.ContractedBy(2f);
            AbyssalForgeConsoleArt.DrawOutline(outlineRect, new Color(1f, 0.60f, 0.24f, 0.92f));
            AbyssalForgeConsoleArt.DrawOutline(outlineRect.ContractedBy(2f), new Color(1f, 0.33f, 0.10f, 0.42f));
        }

        private void DrawBillsPanel(Rect rect)
        {
            AbyssalForgeConsoleArt.DrawPanel(rect, false);
            Rect inner = rect.ContractedBy(10f);
            AbyssalForgeConsoleArt.DrawSectionTitle(new Rect(inner.x, inner.y, inner.width, 22f), "ABY_ForgeConsoleBillsHeader".Translate());
            if (AbyssalStyledWidgets.UseEnhancedTheme)
            {
                AbyssalStyledWidgets.DrawDividerHorizontal(new Rect(inner.x, inner.y + 22f, inner.width - 28f, 5f), 0.34f);
            }

            Rect pasteRect = new Rect(inner.xMax - 24f, inner.y, 24f, 24f);
            DrawPasteButton(pasteRect);

            Rect listRect = new Rect(inner.x, inner.y + 28f, inner.width, inner.height - 28f);
            mouseoverBill = forge.BillStack.DoListing(listRect, BuildRecipeOptions, ref billScrollPosition, ref billViewHeight);
            AbyssalStyledWidgets.DrawAbyssalVerticalScrollbar(listRect, ref billScrollPosition, new Rect(0f, 0f, listRect.width, billViewHeight));
        }

        private List<FloatMenuOption> BuildRecipeOptions()
        {
            List<FloatMenuOption> options = new List<FloatMenuOption>();
            billOptionScratch.Clear();

            MapComponent_AbyssalForgeProgress progress = forge?.ProgressComponent;
            if (progress != null)
            {
                List<ForgePatternEntry> index = GetForgePatternIndex();
                for (int i = 0; i < index.Count; i++)
                {
                    ForgePatternEntry entry = index[i];
                    RecipeDef recipe = entry.recipe;
                    if (recipe == null)
                    {
                        continue;
                    }

                    if (!EntryMatchesSelectedCategoryAndFilter(entry) || !EntryMatchesSearch(entry, patternSearchText))
                    {
                        continue;
                    }

                    if (selectedStatusFilter != StatusFilterAll && !EntryMatchesSelectedStatus(entry, null))
                    {
                        continue;
                    }

                    if (!AbyssalForgeProgressUtility.IsRecipeUnlocked(recipe, progress.TotalResidueOffered) || !ABY_ProtocolResearchGateUtility.IsDecodedForForge(recipe))
                    {
                        continue;
                    }

                    bool availableNow = false;
                    try
                    {
                        availableNow = forge != null && recipe.AvailableNow && recipe.AvailableOnNow(forge);
                    }
                    catch (System.Exception ex)
                    {
                        ABY_UISafetyUtility.LogUIException("Forge bill recipe option", ex);
                    }

                    if (availableNow)
                    {
                        billOptionScratch.Add(entry);
                    }
                }
            }

            billOptionScratch.Sort(ComparePatternEntriesForCurrentView);
            for (int i = 0; i < billOptionScratch.Count; i++)
            {
                RecipeDef capturedRecipe = billOptionScratch[i].recipe;
                options.Add(new FloatMenuOption(billOptionScratch[i].displayLabel, delegate
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
                if (!ABY_ProtocolResearchGateUtility.IsDecodedForForge(recipe))
                {
                    Messages.Message("ABY_ForgeUnknownCannotQueue".Translate(), forge, MessageTypeDefOf.RejectInput, false);
                    return;
                }

                if (forge?.Map?.mapPawns != null && !forge.Map.mapPawns.FreeColonists.Any(colonist => colonist != null && recipe.PawnSatisfiesSkillRequirements(colonist)))
                {
                    Bill.CreateNoPawnsWithSkillDialog(recipe);
                }

                Bill bill = recipe.MakeNewBill();
                forge?.BillStack?.AddBill(bill);
                ClearPatternStatusCache();
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
                ClearPatternStatusCache();
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
                ClearPatternStatusCache();
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
