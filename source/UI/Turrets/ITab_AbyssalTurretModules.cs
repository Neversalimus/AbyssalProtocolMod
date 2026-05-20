using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    public sealed class ITab_AbyssalTurretModules : ITab
    {
        private const int MaxVisualAuxiliarySlots = 3;
        private const int MaxVisualPassiveSlots = 5;
        private const float SlotSize = 56f;
        private const float SlotGap = 7f;
        private const float LineHeight = 16f;

        private ABY_TurretModuleSlot selectedSlot = ABY_TurretModuleSlot.MainWeapon;
        private int selectedPassiveIndex = -1;
        private int selectedAuxiliaryIndex;
        private Vector2 detailScrollPosition = Vector2.zero;
        private readonly Dictionary<ABY_TurretModuleSlot, List<ABY_ModularTurretUtility.LooseModuleAvailability>> availableModulesCache = new Dictionary<ABY_TurretModuleSlot, List<ABY_ModularTurretUtility.LooseModuleAvailability>>();

        public ITab_AbyssalTurretModules()
        {
            size = new Vector2(720f, 520f);
            labelKey = "ABY_ModularTurret_Tab";
        }

        protected override void FillTab()
        {
            CompAbyssalModularTurret comp = SelThing?.TryGetComp<CompAbyssalModularTurret>();
            if (comp == null)
            {
                return;
            }

            SanitizeSelection(comp);
            RebuildAvailabilityCache(comp);

            Rect root = new Rect(0f, 4f, size.x - 18f, size.y - 8f).ContractedBy(6f);
            AbyssalForgeConsoleArt.DrawBackground(root);
            Rect inner = root.ContractedBy(10f);

            DrawHeader(new Rect(inner.x, inner.y, inner.width, 66f), comp);

            Rect bodyRect = new Rect(inner.x, inner.y + 74f, inner.width, inner.height - 74f);
            Rect boardRect = new Rect(bodyRect.x, bodyRect.y, 400f, 318f);
            Rect detailRect = new Rect(boardRect.xMax + 10f, bodyRect.y, bodyRect.width - boardRect.width - 10f, 318f);
            Rect statsRect = new Rect(bodyRect.x, boardRect.yMax + 10f, bodyRect.width, bodyRect.height - boardRect.height - 10f);

            DrawSocketGrid(boardRect, comp);
            DrawSelectedSlotPanel(detailRect, comp);
            DrawStatsPanel(statsRect, comp);
        }

        private void SanitizeSelection(CompAbyssalModularTurret comp)
        {
            if (selectedSlot == ABY_TurretModuleSlot.Auxiliary)
            {
                selectedAuxiliaryIndex = Mathf.Clamp(selectedAuxiliaryIndex, 0, MaxVisualAuxiliarySlots - 1);
                if (comp.Props.auxiliarySlots <= 0)
                {
                    selectedSlot = ABY_TurretModuleSlot.MainWeapon;
                    selectedAuxiliaryIndex = 0;
                }
            }
            else if (selectedSlot == ABY_TurretModuleSlot.Passive)
            {
                selectedPassiveIndex = Mathf.Clamp(selectedPassiveIndex, 0, MaxVisualPassiveSlots - 1);
                if (comp.Props.passiveSlots <= 0)
                {
                    selectedSlot = ABY_TurretModuleSlot.MainWeapon;
                    selectedPassiveIndex = -1;
                }
            }
            else
            {
                selectedPassiveIndex = -1;
                selectedAuxiliaryIndex = 0;
            }
        }

        private void RebuildAvailabilityCache(CompAbyssalModularTurret comp)
        {
            availableModulesCache.Clear();
            if (comp?.parent?.Map == null || !comp.FeatureEnabled)
            {
                return;
            }

            availableModulesCache[ABY_TurretModuleSlot.MainWeapon] = ABY_ModularTurretUtility.GetAvailableLooseModulesOnMap(comp.parent.Map, ABY_TurretModuleSlot.MainWeapon, comp.Props.chassisTag)
                .Where(entry => comp.CanInstall(entry.Module, out _))
                .ToList();

            availableModulesCache[ABY_TurretModuleSlot.Auxiliary] = ABY_ModularTurretUtility.GetAvailableLooseModulesOnMap(comp.parent.Map, ABY_TurretModuleSlot.Auxiliary, comp.Props.chassisTag)
                .Where(entry => comp.CanInstall(entry.Module, out _))
                .ToList();

            availableModulesCache[ABY_TurretModuleSlot.Passive] = ABY_ModularTurretUtility.GetAvailableLooseModulesOnMap(comp.parent.Map, ABY_TurretModuleSlot.Passive, comp.Props.chassisTag)
                .Where(entry => comp.CanInstall(entry.Module, out _))
                .ToList();
        }

        private List<ABY_ModularTurretUtility.LooseModuleAvailability> GetAvailableModulesForSlot(ABY_TurretModuleSlot slot)
        {
            return availableModulesCache.TryGetValue(slot, out List<ABY_ModularTurretUtility.LooseModuleAvailability> modules)
                ? modules
                : new List<ABY_ModularTurretUtility.LooseModuleAvailability>();
        }

        private void DrawHeader(Rect rect, CompAbyssalModularTurret comp)
        {
            AbyssalForgeConsoleArt.DrawPanel(rect, comp.FeatureEnabled);
            Rect inner = rect.ContractedBy(8f);

            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.MiddleLeft;
            GUI.color = Color.white;
            DrawClippedLabel(new Rect(inner.x, inner.y + 2f, inner.width, 30f), ABY_ModularTurretUtility.TranslateOrFallback("ABY_ModularTurret_Header", "Abyssal modular turret chassis"));

            Text.Font = GameFont.Tiny;
            Text.Anchor = TextAnchor.UpperLeft;
            GUI.color = comp.FeatureEnabled ? AbyssalForgeConsoleArt.TextSoftColor : new Color(1f, 0.48f, 0.36f, 1f);
            string subtitle = comp.FeatureEnabled
                ? ABY_ModularTurretUtility.TranslateOrFallback("ABY_ModularTurret_GridSubtitle", "Socket board: inspect squares, install modules, and preview turret output.")
                : ABY_ModularTurretUtility.TranslateOrFallback("ABY_TurretDisabledMessage", "Modular turret systems are disabled in mod settings.");
            DrawClippedLabel(new Rect(inner.x + 2f, inner.y + 32f, inner.width - 4f, 17f), subtitle);

            ResetTextState();
        }

        private void DrawSocketGrid(Rect rect, CompAbyssalModularTurret comp)
        {
            AbyssalForgeConsoleArt.DrawPanel(rect, true);
            Rect inner = rect.ContractedBy(10f);

            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;
            GUI.color = Color.white;
            DrawClippedLabel(new Rect(inner.x, inner.y, inner.width, 22f), ABY_ModularTurretUtility.TranslateOrFallback("ABY_TurretGridHeader", "Module sockets"));

            Text.Font = GameFont.Tiny;
            GUI.color = AbyssalForgeConsoleArt.TextDimColor;
            DrawClippedLabel(new Rect(inner.x, inner.y + 23f, inner.width, 18f), ABY_ModularTurretUtility.TranslateOrFallback("ABY_TurretGridHint", "Click a square to inspect or change that slot."));
            GUI.color = Color.white;

            Rect gridRect = new Rect(inner.x, inner.y + 42f, inner.width, inner.height - 42f);
            float mainY = gridRect.y + 2f;
            float auxY = gridRect.y + 64f;
            float passiveTopY = gridRect.y + 128f;
            float passiveBottomY = gridRect.y + 190f;

            float mainX = gridRect.x + (gridRect.width - SlotSize) / 2f;
            DrawSlotSquare(new Rect(mainX, mainY, SlotSize, SlotSize), comp, ABY_TurretModuleSlot.MainWeapon, comp.MainModule, -1, 0, comp.Props.mainWeaponSlots > 0, "MAIN", GetEmptySlotShortHint(ABY_TurretModuleSlot.MainWeapon));

            float auxGroupWidth = MaxVisualAuxiliarySlots * SlotSize + (MaxVisualAuxiliarySlots - 1) * SlotGap;
            float auxX = gridRect.x + (gridRect.width - auxGroupWidth) / 2f;
            for (int i = 0; i < MaxVisualAuxiliarySlots; i++)
            {
                bool active = i < Mathf.Max(0, comp.Props.auxiliarySlots) && i == 0;
                ABY_TurretModuleDef installed = i == 0 ? comp.AuxiliaryModule : null;
                DrawSlotSquare(new Rect(auxX + i * (SlotSize + SlotGap), auxY, SlotSize, SlotSize), comp, ABY_TurretModuleSlot.Auxiliary, installed, -1, i, active, "AUX " + (i + 1), active ? GetEmptySlotShortHint(ABY_TurretModuleSlot.Auxiliary) : GetLockedSlotShortHint());
            }

            float passiveGroupWidth = 3 * SlotSize + 2 * SlotGap;
            float passiveX = gridRect.x + (gridRect.width - passiveGroupWidth) / 2f;
            for (int i = 0; i < 3; i++)
            {
                DrawPassiveSlotSquare(new Rect(passiveX + i * (SlotSize + SlotGap), passiveTopY, SlotSize, SlotSize), comp, i);
            }

            float passiveBottomWidth = 2 * SlotSize + SlotGap;
            float passiveBottomX = gridRect.x + (gridRect.width - passiveBottomWidth) / 2f;
            for (int i = 3; i < MaxVisualPassiveSlots; i++)
            {
                DrawPassiveSlotSquare(new Rect(passiveBottomX + (i - 3) * (SlotSize + SlotGap), passiveBottomY, SlotSize, SlotSize), comp, i);
            }

            ResetTextState();
        }

        private void DrawPassiveSlotSquare(Rect rect, CompAbyssalModularTurret comp, int index)
        {
            bool active = index < Mathf.Max(0, comp.Props.passiveSlots);
            ABY_TurretModuleDef installed = index < comp.PassiveModules.Count ? comp.PassiveModules[index] : null;
            DrawSlotSquare(rect, comp, ABY_TurretModuleSlot.Passive, installed, index, 0, active, "PASS " + (index + 1), active ? GetEmptySlotShortHint(ABY_TurretModuleSlot.Passive) : GetLockedSlotShortHint());
        }

        private void DrawSlotSquare(Rect rect, CompAbyssalModularTurret comp, ABY_TurretModuleSlot slot, ABY_TurretModuleDef installed, int passiveIndex, int auxiliaryIndex, bool active, string badge, string emptyShortHint)
        {
            bool selected = IsSelected(slot, passiveIndex, auxiliaryIndex);
            bool available = active && SlotHasAvailableModule(comp, slot);
            Color slotColor = active ? ABY_ModularTurretUtility.SlotColor(slot) : new Color(0.42f, 0.42f, 0.45f, 1f);
            Color outlineColor = selected ? Color.Lerp(slotColor, Color.white, 0.42f) : new Color(slotColor.r, slotColor.g, slotColor.b, active ? 0.62f : 0.28f);
            Color fillColor = installed != null
                ? new Color(slotColor.r * 0.18f, slotColor.g * 0.13f, slotColor.b * 0.12f, 0.96f)
                : active ? new Color(0.08f, 0.067f, 0.063f, 0.96f) : new Color(0.045f, 0.045f, 0.05f, 0.86f);

            AbyssalForgeConsoleArt.Fill(rect, fillColor);
            AbyssalForgeConsoleArt.DrawOutline(rect, outlineColor);
            AbyssalForgeConsoleArt.Fill(new Rect(rect.x + 2f, rect.y + 2f, rect.width - 4f, 2f), new Color(slotColor.r, slotColor.g, slotColor.b, active ? 0.65f : 0.22f));

            if (!active)
            {
                GUI.color = new Color(0.60f, 0.60f, 0.63f, 0.50f);
                Widgets.DrawLineHorizontal(rect.x + 10f, rect.center.y, rect.width - 20f);
                Text.Font = GameFont.Medium;
                Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.Label(new Rect(rect.x, rect.y + 16f, rect.width, 24f), "×");
                GUI.color = Color.white;
            }

            Text.Font = GameFont.Tiny;
            Text.Anchor = TextAnchor.MiddleCenter;
            GUI.color = active ? Color.white : new Color(0.70f, 0.70f, 0.73f, 0.75f);
            Widgets.Label(new Rect(rect.x + 3f, rect.y + 3f, rect.width - 6f, 15f), badge);

            if (installed?.thingDef?.uiIcon != null)
            {
                Rect iconRect = new Rect(rect.center.x - 15f, rect.y + 21f, 30f, 24f);
                GUI.color = active ? Color.white : new Color(0.72f, 0.72f, 0.72f, 0.58f);
                GUI.DrawTexture(iconRect, installed.thingDef.uiIcon, ScaleMode.ScaleToFit, true);
                GUI.color = Color.white;
            }
            else if (active)
            {
                GUI.color = installed != null ? slotColor : new Color(slotColor.r, slotColor.g, slotColor.b, available ? 0.95f : 0.52f);
                Text.Font = GameFont.Medium;
                Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.Label(new Rect(rect.x, rect.y + 17f, rect.width, 24f), installed != null ? "◆" : "+");
                Text.Font = GameFont.Tiny;
            }

            string bottom = installed != null ? ShortModuleLabel(installed) : emptyShortHint;
            bottom = CompactText(bottom, installed != null ? 8 : 7);
            GUI.color = installed != null ? AbyssalForgeConsoleArt.TextSoftColor : active ? AbyssalForgeConsoleArt.TextDimColor : new Color(0.56f, 0.56f, 0.60f, 0.80f);
            Text.Font = GameFont.Tiny;
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(new Rect(rect.x + 3f, rect.yMax - 16f, rect.width - 6f, 14f), bottom);

            if (selected)
            {
                AbyssalForgeConsoleArt.DrawOutline(rect.ExpandedBy(2f), Color.Lerp(slotColor, Color.white, 0.55f));
            }
            else if (Mouse.IsOver(rect))
            {
                AbyssalForgeConsoleArt.DrawOutline(rect, Color.Lerp(slotColor, Color.white, 0.48f));
            }

            if (Widgets.ButtonInvisible(rect, false))
            {
                selectedSlot = slot;
                selectedPassiveIndex = passiveIndex;
                selectedAuxiliaryIndex = auxiliaryIndex;
                detailScrollPosition = Vector2.zero;
            }

            TooltipHandler.TipRegion(rect, BuildSlotTooltip(comp, slot, installed, passiveIndex, auxiliaryIndex, active, available));
            ResetTextState();
        }

        private bool IsSelected(ABY_TurretModuleSlot slot, int passiveIndex, int auxiliaryIndex)
        {
            if (selectedSlot != slot)
            {
                return false;
            }

            if (slot == ABY_TurretModuleSlot.Passive)
            {
                return selectedPassiveIndex == passiveIndex;
            }

            if (slot == ABY_TurretModuleSlot.Auxiliary)
            {
                return selectedAuxiliaryIndex == auxiliaryIndex;
            }

            return true;
        }

        private bool SlotHasAvailableModule(CompAbyssalModularTurret comp, ABY_TurretModuleSlot slot)
        {
            return comp?.FeatureEnabled == true && GetAvailableModulesForSlot(slot).Count > 0;
        }

        private void DrawSelectedSlotPanel(Rect rect, CompAbyssalModularTurret comp)
        {
            SlotView slotView = GetSelectedSlotView(comp);
            AbyssalForgeConsoleArt.DrawPanel(rect, slotView.Installed != null);
            Rect inner = rect.ContractedBy(12f);
            Color slotColor = ABY_ModularTurretUtility.SlotColor(slotView.Slot);
            AbyssalForgeConsoleArt.Fill(new Rect(inner.x, inner.y + 2f, 5f, 27f), slotColor);

            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;
            GUI.color = Color.white;
            DrawClippedLabel(new Rect(inner.x + 12f, inner.y, inner.width - 12f, 22f), slotView.Title);

            Text.Font = GameFont.Tiny;
            GUI.color = slotView.Active ? AbyssalForgeConsoleArt.TextSoftColor : new Color(1f, 0.58f, 0.48f, 1f);
            DrawClippedLabel(new Rect(inner.x + 12f, inner.y + 23f, inner.width - 12f, 16f), slotView.StateLine);
            GUI.color = Color.white;

            Rect infoRect = new Rect(inner.x, inner.y + 48f, inner.width, inner.height - 124f);
            DrawSelectedSlotInfo(infoRect, comp, slotView);

            DrawSelectedSlotActions(new Rect(inner.x, inner.yMax - 68f, inner.width, 68f), comp, slotView);
            ResetTextState();
        }

        private void DrawSelectedSlotInfo(Rect rect, CompAbyssalModularTurret comp, SlotView slotView)
        {
            Text.Anchor = TextAnchor.UpperLeft;
            Text.Font = GameFont.Tiny;

            Rect outRect = rect;
            Rect viewRect = new Rect(0f, 0f, Mathf.Max(1f, rect.width - 16f), 0f);

            if (!slotView.Active)
            {
                string lockedText = ABY_ModularTurretUtility.TranslateOrFallback("ABY_TurretLockedDetail_New", "This slot is locked on the current chassis. It is not available on this chassis.");
                float height = Mathf.Max(54f, Text.CalcHeight(lockedText, viewRect.width));
                viewRect.height = height;
                AbyssalStyledWidgets.BeginAbyssalScrollView(outRect, ref detailScrollPosition, viewRect);
                GUI.color = AbyssalForgeConsoleArt.TextDimColor;
                DrawWrappedText(new Rect(0f, 0f, viewRect.width, height), lockedText, GameFont.Tiny, AbyssalForgeConsoleArt.TextDimColor);
                AbyssalStyledWidgets.EndAbyssalScrollView(outRect, ref detailScrollPosition, viewRect);
                ResetTextState();
                return;
            }

            float contentY = 0f;
            if (slotView.Installed == null)
            {
                string emptyText = GetEmptySlotHint(slotView.Slot);
                string availableHeader = ABY_ModularTurretUtility.TranslateOrFallback("ABY_TurretGridAvailableHeader_OnMap", "Modules currently on map");
                string availableText = GetAvailableModulesTooltip(comp, slotView.Slot);
                float emptyHeight = Text.CalcHeight(emptyText, viewRect.width);
                float availableHeight = Text.CalcHeight(availableText, viewRect.width);
                viewRect.height = Mathf.Max(outRect.height, emptyHeight + availableHeight + 46f);
                AbyssalStyledWidgets.BeginAbyssalScrollView(outRect, ref detailScrollPosition, viewRect);
                contentY = DrawInfoParagraph(new Rect(0f, 0f, viewRect.width, viewRect.height), contentY, emptyText, AbyssalForgeConsoleArt.TextSoftColor);
                contentY += 6f;
                DrawSectionLine(new Rect(0f, 0f, viewRect.width, viewRect.height), ref contentY, availableHeader);
                DrawInfoParagraph(new Rect(0f, 0f, viewRect.width, viewRect.height), contentY, availableText, AbyssalForgeConsoleArt.TextDimColor);
                AbyssalStyledWidgets.EndAbyssalScrollView(outRect, ref detailScrollPosition, viewRect);
                ResetTextState();
                return;
            }

            ABY_TurretModuleDef module = slotView.Installed;
            float estimatedHeight = 220f;
            string effectText = ABY_ModularTurretUtility.GetModuleEffectSummary(module);
            string[] statLines = BuildReadableModuleStats(module).Split('\n');
            estimatedHeight += Text.CalcHeight(effectText, viewRect.width);
            estimatedHeight += statLines.Length * (LineHeight + 1f);
            viewRect.height = Mathf.Max(outRect.height, estimatedHeight);

            AbyssalStyledWidgets.BeginAbyssalScrollView(outRect, ref detailScrollPosition, viewRect);
            Rect contentRect = new Rect(0f, 0f, viewRect.width, viewRect.height);
            DrawSectionLine(contentRect, ref contentY, module.LocalizedLabelCap);
            DrawInfoLine(contentRect, ref contentY, ABY_ModularTurretUtility.TranslateOrFallback("ABY_TurretModuleLine_Slot", "Slot: {0}", module.SlotLabel));
            DrawInfoLine(contentRect, ref contentY, ABY_ModularTurretUtility.TranslateOrFallback("ABY_TurretModuleLine_Role", "Role: {0}", module.RoleLabel));
            contentY += 5f;
            DrawSectionLine(contentRect, ref contentY, ABY_ModularTurretUtility.TranslateOrFallback("ABY_TurretModuleLine_EffectHeader", "Effect"));
            contentY = DrawInfoParagraph(contentRect, contentY, effectText, AbyssalForgeConsoleArt.TextSoftColor);
            contentY += 5f;
            DrawSectionLine(contentRect, ref contentY, ABY_ModularTurretUtility.TranslateOrFallback("ABY_TurretModuleLine_StatsHeader", "Stats"));
            for (int i = 0; i < statLines.Length; i++)
            {
                DrawInfoLine(contentRect, ref contentY, statLines[i]);
            }
            AbyssalStyledWidgets.EndAbyssalScrollView(outRect, ref detailScrollPosition, viewRect);

            ResetTextState();
        }

        private void DrawSectionLine(Rect rect, ref float y, string text)
        {
            Text.Font = GameFont.Tiny;
            Text.Anchor = TextAnchor.UpperLeft;
            GUI.color = Color.white;
            DrawClippedLabel(new Rect(rect.x, y, rect.width, LineHeight), text);
            y += LineHeight + 1f;
        }

        private void DrawInfoLine(Rect rect, ref float y, string text)
        {
            if (text.NullOrEmpty())
            {
                return;
            }

            Text.Font = GameFont.Tiny;
            Text.Anchor = TextAnchor.UpperLeft;
            GUI.color = AbyssalForgeConsoleArt.TextSoftColor;
            DrawClippedLabel(new Rect(rect.x, y, rect.width, LineHeight), text);
            y += LineHeight;
        }

        private float DrawInfoParagraph(Rect rect, float y, string text, Color color)
        {
            if (text.NullOrEmpty())
            {
                return y;
            }

            Text.Font = GameFont.Tiny;
            Text.Anchor = TextAnchor.UpperLeft;
            GUI.color = color;
            float height = Mathf.Min(Text.CalcHeight(text, rect.width), Mathf.Max(20f, rect.yMax - y));
            Widgets.Label(new Rect(rect.x, y, rect.width, height), text);
            return y + height;
        }

        private void DrawWrappedText(Rect rect, string text, GameFont font, Color color)
        {
            Text.Font = font;
            Text.Anchor = TextAnchor.UpperLeft;
            GUI.color = color;
            Widgets.Label(rect, text ?? string.Empty);
        }

        private void DrawSelectedSlotActions(Rect rect, CompAbyssalModularTurret comp, SlotView slotView)
        {
            if (!slotView.Active)
            {
                return;
            }

            Rect topButton = new Rect(rect.x, rect.y + 3f, rect.width, 27f);
            Rect bottomButton = new Rect(rect.x, rect.y + 38f, rect.width, 25f);

            if (slotView.Installed == null)
            {
                List<ABY_ModularTurretUtility.LooseModuleAvailability> availableModules = GetAvailableModulesForSlot(slotView.Slot);
                string tooltip = comp.FeatureEnabled
                    ? GetAvailableModulesTooltip(comp, slotView.Slot)
                    : ABY_ModularTurretUtility.TranslateOrFallback("ABY_TurretEditDisabled", "Re-enable modular turrets in mod settings before editing installed modules.");
                bool enabled = availableModules.Count > 0;
                if (AbyssalStyledWidgets.TextButton(topButton, ABY_ModularTurretUtility.TranslateOrFallback("ABY_TurretInstallFromMap", "Install from map"), enabled, false, null, tooltip))
                {
                    List<FloatMenuOption> options = new List<FloatMenuOption>();
                    for (int i = 0; i < availableModules.Count; i++)
                    {
                        ABY_ModularTurretUtility.LooseModuleAvailability entry = availableModules[i];
                        string optionLabel = entry.Module.LabelCap + " x" + entry.Count;
                        options.Add(new FloatMenuOption(optionLabel, delegate
                        {
                            if (comp.TryInstallSpecificFromMap(entry.Module, out string message))
                            {
                                detailScrollPosition = Vector2.zero;
                                RebuildAvailabilityCache(comp);
                                Messages.Message(message, comp.parent, MessageTypeDefOf.PositiveEvent);
                            }
                            else
                            {
                                Messages.Message(message ?? ABY_ModularTurretUtility.TranslateOrFallback("ABY_TurretInstallFailed", "Could not install module."), comp.parent, MessageTypeDefOf.RejectInput, false);
                            }
                        }));
                    }

                    if (options.Count > 0)
                    {
                        Find.WindowStack.Add(new FloatMenu(options));
                    }
                }
            }
            else
            {
                string tooltip = comp.FeatureEnabled
                    ? ABY_ModularTurretUtility.TranslateOrFallback("ABY_TurretRemoveTooltip", "Ejects the installed module as an item near the chassis. If no safe nearby cell exists, the module remains installed.")
                    : ABY_ModularTurretUtility.TranslateOrFallback("ABY_TurretEditDisabled", "Re-enable modular turrets in mod settings before editing installed modules.");
                if (AbyssalStyledWidgets.TextButton(topButton, ABY_ModularTurretUtility.TranslateOrFallback("ABY_TurretRemove", "Remove / eject"), comp.FeatureEnabled, false, null, tooltip))
                {
                    bool removed;
                    string message;
                    if (slotView.Slot == ABY_TurretModuleSlot.MainWeapon)
                    {
                        removed = comp.TryRemoveMainModule(out message);
                    }
                    else if (slotView.Slot == ABY_TurretModuleSlot.Auxiliary)
                    {
                        removed = comp.TryRemoveAuxiliaryModule(out message);
                    }
                    else
                    {
                        removed = comp.TryRemovePassiveModule(slotView.PassiveIndex, out message);
                    }

                    if (removed)
                    {
                        detailScrollPosition = Vector2.zero;
                        RebuildAvailabilityCache(comp);
                    }

                    Messages.Message(message ?? ABY_ModularTurretUtility.TranslateOrFallback("ABY_TurretRemoveFailed", "Could not remove module."), comp.parent, removed ? MessageTypeDefOf.PositiveEvent : MessageTypeDefOf.RejectInput, false);
                }
            }

            AbyssalStyledWidgets.TextButton(bottomButton, ABY_ModularTurretUtility.TranslateOrFallback("ABY_TurretOpenForgeHint", "Craft in Forge"), false, false, null, ABY_ModularTurretUtility.TranslateOrFallback("ABY_TurretOpenForgeHintTooltip", "Module recipes are available in the Abyssal Forge Turret Systems category. Direct forge-opening from turret slots is planned."));
        }

        private void DrawStatsPanel(Rect rect, CompAbyssalModularTurret comp)
        {
            AbyssalForgeConsoleArt.DrawPanel(rect, false);
            Rect inner = rect.ContractedBy(10f);

            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;
            GUI.color = Color.white;
            DrawClippedLabel(new Rect(inner.x, inner.y, inner.width, 22f), ABY_ModularTurretUtility.TranslateOrFallback("ABY_TurretStatsHeader_Operational", "Operational summary"));

            Text.Font = GameFont.Tiny;
            GUI.color = AbyssalForgeConsoleArt.TextSoftColor;
            string cooldownText = comp.HasMainWeapon
                ? ABY_ModularTurretUtility.TranslateOrFallback(
                    "ABY_TurretStatsMainResolvedCompact",
                    "Main: range {0}, cooldown {1}",
                    comp.ResolvedRange.ToString("0.0"),
                    ABY_ModularTurretUtility.FormatTicksAsSeconds(comp.ResolvedMainCooldownTicks))
                : ABY_ModularTurretUtility.TranslateOrFallback("ABY_TurretStatsMainMissingCompact", "Main: no weapon core installed");

            List<string> leftLines = new List<string>
            {
                ABY_ModularTurretUtility.TranslateOrFallback("ABY_TurretStatsPower", "Power: {0}", comp.IsPowered ? ABY_ModularTurretUtility.TranslateOrFallback("ABY_StateOnline", "online") : ABY_ModularTurretUtility.TranslateOrFallback("ABY_StateOffline", "offline")),
                cooldownText
            };

            List<string> rightLines = new List<string>
            {
                ABY_ModularTurretUtility.TranslateOrFallback("ABY_TurretStatsPowerDrawBreakdown", "Power draw: chassis {0} W + modules {1} W", comp.ResolvedBasePowerDraw.ToString("0"), comp.ResolvedModulePowerDraw.ToString("0"))
            };

            float columnWidth = (inner.width - 16f) / 2f;
            Rect leftRect = new Rect(inner.x, inner.y + 26f, columnWidth, Mathf.Max(1f, inner.height - 28f));
            Rect rightRect = new Rect(inner.x + columnWidth + 16f, inner.y + 26f, columnWidth, Mathf.Max(1f, inner.height - 28f));
            Widgets.Label(leftRect, string.Join("\n", leftLines.ToArray()));
            Widgets.Label(rightRect, string.Join("\n", rightLines.ToArray()));
            ResetTextState();
        }

        private SlotView GetSelectedSlotView(CompAbyssalModularTurret comp)
        {
            if (selectedSlot == ABY_TurretModuleSlot.Auxiliary)
            {
                bool active = selectedAuxiliaryIndex == 0 && comp.Props.auxiliarySlots > 0;
                return new SlotView(selectedSlot, -1, selectedAuxiliaryIndex, active, active ? comp.AuxiliaryModule : null, GetSlotTitle(selectedSlot, -1, selectedAuxiliaryIndex), active ? ABY_ModularTurretUtility.TranslateOrFallback("ABY_TurretGridActiveSlot", "Active chassis slot") : ABY_ModularTurretUtility.TranslateOrFallback("ABY_TurretGridLockedState", "Locked on this chassis"));
            }

            if (selectedSlot == ABY_TurretModuleSlot.Passive)
            {
                bool active = selectedPassiveIndex >= 0 && selectedPassiveIndex < comp.Props.passiveSlots;
                ABY_TurretModuleDef installed = active && selectedPassiveIndex < comp.PassiveModules.Count ? comp.PassiveModules[selectedPassiveIndex] : null;
                return new SlotView(selectedSlot, selectedPassiveIndex, 0, active, installed, GetSlotTitle(selectedSlot, selectedPassiveIndex, 0), active ? ABY_ModularTurretUtility.TranslateOrFallback("ABY_TurretGridActiveSlot", "Active chassis slot") : ABY_ModularTurretUtility.TranslateOrFallback("ABY_TurretGridLockedState", "Locked on this chassis"));
            }

            bool mainActive = comp.Props.mainWeaponSlots > 0;
            return new SlotView(ABY_TurretModuleSlot.MainWeapon, -1, 0, mainActive, mainActive ? comp.MainModule : null, GetSlotTitle(ABY_TurretModuleSlot.MainWeapon, -1, 0), mainActive ? ABY_ModularTurretUtility.TranslateOrFallback("ABY_TurretGridActiveSlot", "Active chassis slot") : ABY_ModularTurretUtility.TranslateOrFallback("ABY_TurretGridLockedState", "Locked on this chassis"));
        }

        private static string GetSlotTitle(ABY_TurretModuleSlot slot, int passiveIndex, int auxiliaryIndex)
        {
            switch (slot)
            {
                case ABY_TurretModuleSlot.MainWeapon:
                    return ABY_ModularTurretUtility.TranslateOrFallback("ABY_TurretSlotTitle_Main", "Main weapon core");
                case ABY_TurretModuleSlot.Auxiliary:
                    return ABY_ModularTurretUtility.TranslateOrFallback("ABY_TurretSlotTitle_AuxIndexed", "Auxiliary module {0}", auxiliaryIndex + 1);
                default:
                    return ABY_ModularTurretUtility.TranslateOrFallback("ABY_TurretSlotTitle_Passive", "Passive module {0}", passiveIndex + 1);
            }
        }

        private static string GetEmptySlotShortHint(ABY_TurretModuleSlot slot)
        {
            switch (slot)
            {
                case ABY_TurretModuleSlot.MainWeapon:
                    return ABY_ModularTurretUtility.TranslateOrFallback("ABY_TurretGridEmptyMain", "Empty");
                case ABY_TurretModuleSlot.Auxiliary:
                    return ABY_ModularTurretUtility.TranslateOrFallback("ABY_TurretGridEmptyAux", "Empty");
                default:
                    return ABY_ModularTurretUtility.TranslateOrFallback("ABY_TurretGridEmptyPassive", "Empty");
            }
        }

        private static string GetLockedSlotShortHint()
        {
            return ABY_ModularTurretUtility.TranslateOrFallback("ABY_TurretGridLocked", "Locked");
        }

        private static string GetEmptySlotHint(ABY_TurretModuleSlot slot)
        {
            switch (slot)
            {
                case ABY_TurretModuleSlot.MainWeapon:
                    return ABY_ModularTurretUtility.TranslateOrFallback("ABY_TurretEmpty_Main", "Empty. Install a main weapon core before the chassis can fire.");
                case ABY_TurretModuleSlot.Auxiliary:
                    return ABY_ModularTurretUtility.TranslateOrFallback("ABY_TurretEmpty_Aux", "Empty. Auxiliary modules add secondary support fire.");
                default:
                    return ABY_ModularTurretUtility.TranslateOrFallback("ABY_TurretEmpty_Passive", "Empty. Passive modules alter range, cadence, accuracy, or power draw.");
            }
        }

        private static string ShortModuleLabel(ABY_TurretModuleDef module)
        {
            if (module == null)
            {
                return string.Empty;
            }

            string label = module.LocalizedLabel;
            if (label.NullOrEmpty())
            {
                return string.Empty;
            }

            string[] parts = label.Split(' ');
            return parts.Length <= 1 ? label : parts[0];
        }

        private static string BuildSlotTooltip(CompAbyssalModularTurret comp, ABY_TurretModuleSlot slot, ABY_TurretModuleDef installed, int passiveIndex, int auxiliaryIndex, bool active, bool available)
        {
            List<string> lines = new List<string>
            {
                GetSlotTitle(slot, passiveIndex, auxiliaryIndex)
            };

            if (!active)
            {
                lines.Add(ABY_ModularTurretUtility.TranslateOrFallback("ABY_TurretGridLockedTooltip", "Locked on this chassis. This slot is not available on the current chassis."));
                return string.Join("\n", lines.ToArray());
            }

            if (installed != null)
            {
                lines.Add(installed.LabelCap);
                lines.Add(ABY_ModularTurretUtility.GetModuleEffectSummary(installed));
                lines.Add(BuildReadableModuleStats(installed));
            }
            else
            {
                lines.Add(GetEmptySlotHint(slot));
                lines.Add(available ? ABY_ModularTurretUtility.TranslateOrFallback("ABY_TurretGridAvailable", "Compatible loose module available on map.") : ABY_ModularTurretUtility.TranslateOrFallback("ABY_TurretGridUnavailable", "No compatible loose module currently available on map."));
            }

            return string.Join("\n", lines.Where(line => !line.NullOrEmpty()).ToArray());
        }

        private string GetAvailableModulesTooltip(CompAbyssalModularTurret comp, ABY_TurretModuleSlot slot)
        {
            List<ABY_ModularTurretUtility.LooseModuleAvailability> modules = GetAvailableModulesForSlot(slot);
            if (modules.Count == 0)
            {
                return ABY_ModularTurretUtility.TranslateOrFallback("ABY_TurretNoAvailableModule", "No compatible loose module is available on this map.");
            }

            List<string> lines = new List<string>();
            for (int i = 0; i < modules.Count; i++)
            {
                ABY_ModularTurretUtility.LooseModuleAvailability entry = modules[i];
                lines.Add("• " + entry.Module.LabelCap + " x" + entry.Count);
            }

            return string.Join("\n", lines.ToArray());
        }

        private static string BuildReadableModuleStats(ABY_TurretModuleDef module)
        {
            if (module == null)
            {
                return string.Empty;
            }

            List<string> lines = new List<string>();
            if (module.slot == ABY_TurretModuleSlot.MainWeapon)
            {
                lines.Add(ABY_ModularTurretUtility.TranslateOrFallback("ABY_TurretStat_Range", "Range: {0}", module.range.ToString("0.0")));
                lines.Add(ABY_ModularTurretUtility.TranslateOrFallback("ABY_TurretStat_MainCooldown", "Main cooldown: {0}", ABY_ModularTurretUtility.FormatTicksAsSeconds(module.cooldownTicks)));
                lines.Add(ABY_ModularTurretUtility.TranslateOrFallback("ABY_TurretStat_Burst", "Burst: {0}", Mathf.Max(1, module.burstShotCount)));
            }
            else if (module.slot == ABY_TurretModuleSlot.Auxiliary)
            {
                int auxCooldown = module.auxiliaryCooldownTicks > 0 ? module.auxiliaryCooldownTicks : module.cooldownTicks;
                lines.Add(ABY_ModularTurretUtility.TranslateOrFallback("ABY_TurretStat_Range", "Range: {0}", module.range.ToString("0.0")));
                lines.Add(ABY_ModularTurretUtility.TranslateOrFallback("ABY_TurretStat_AuxCooldown", "Aux cooldown: {0}", ABY_ModularTurretUtility.FormatTicksAsSeconds(auxCooldown)));
                lines.Add(ABY_ModularTurretUtility.TranslateOrFallback("ABY_TurretStat_Burst", "Burst: {0}", Mathf.Max(1, module.burstShotCount)));
            }
            else
            {
                lines.Add(ABY_ModularTurretUtility.TranslateOrFallback("ABY_TurretStat_RangeOffset", "Range offset: {0}", ABY_ModularTurretUtility.FormatSignedDecimal(module.rangeOffset)));
                lines.Add(ABY_ModularTurretUtility.TranslateOrFallback("ABY_TurretStat_CooldownEffect", "Cooldown effect: {0}", ABY_ModularTurretUtility.FormatCooldownMultiplierEffect(module.cooldownMultiplier)));
                if (module.cooldownOffsetTicks != 0)
                {
                    lines.Add(ABY_ModularTurretUtility.TranslateOrFallback("ABY_TurretStat_CooldownOffset", "Cooldown offset: {0}", ABY_ModularTurretUtility.FormatTicksAsSeconds(module.cooldownOffsetTicks)));
                }

                if (Mathf.Abs(module.minRangeOffset) > 0.001f)
                {
                    lines.Add(ABY_ModularTurretUtility.TranslateOrFallback("ABY_TurretStat_MinRangeOffset", "Minimum range offset: {0}", ABY_ModularTurretUtility.FormatSignedDecimal(module.minRangeOffset)));
                }

                if (module.idleCooldownRecoveryPerTick > 0.001f)
                {
                    lines.Add(ABY_ModularTurretUtility.TranslateOrFallback("ABY_TurretStat_CooldownRecovery", "Cooldown recovery: {0} ticks/tick", module.idleCooldownRecoveryPerTick.ToString("0.0")));
                }

                if (module.incomingDamageMultiplier > 0f && Mathf.Abs(module.incomingDamageMultiplier - 1f) > 0.001f)
                {
                    lines.Add(ABY_ModularTurretUtility.TranslateOrFallback("ABY_TurretStat_DamageMultiplier", "Incoming damage: {0}%", Mathf.RoundToInt(module.incomingDamageMultiplier * 100f)));
                }

                if (module.turretShieldMax > 0.01f)
                {
                    lines.Add(ABY_ModularTurretUtility.TranslateOrFallback("ABY_TurretStat_ShieldCapacity", "Aegis capacity: {0}", module.turretShieldMax.ToString("0")));
                    lines.Add(ABY_ModularTurretUtility.TranslateOrFallback("ABY_TurretStat_ShieldRecharge", "Aegis recharge: {0}/s after {1}", (module.turretShieldRechargePerTick * 60f).ToString("0.0"), ABY_ModularTurretUtility.FormatTicksAsSeconds(module.turretShieldRechargeDelayTicks)));
                }

                if (module.targetPriorityCombatPowerScale > 0.001f || module.targetPriorityBossBonus > 0.001f || module.targetPriorityConstructBonus > 0.001f || module.targetPriorityMechanoidBonus > 0.001f || module.targetPriorityShieldedBonus > 0.001f || module.targetPriorityWoundedBonus > 0.001f || module.preferClusteredTargets || module.preferLineTargets)
                {
                    lines.Add(ABY_ModularTurretUtility.TranslateOrFallback("ABY_TurretStat_TargetingLogic", "Targeting logic: enhanced"));
                }

                if (Mathf.Abs(module.missRadiusOffset) > 0.001f)
                {
                    lines.Add(ABY_ModularTurretUtility.TranslateOrFallback("ABY_TurretStat_MissRadiusOffset", "Miss radius offset: {0}", ABY_ModularTurretUtility.FormatSignedDecimal(module.missRadiusOffset)));
                }
            }

            if (module.projectileDef != null)
            {
                lines.Add(ABY_ModularTurretUtility.TranslateOrFallback("ABY_TurretStat_Projectile", "Projectile: {0}", module.projectileDef.label));
            }

            if (Mathf.Abs(module.extraPowerDraw) > 0.001f)
            {
                lines.Add(ABY_ModularTurretUtility.TranslateOrFallback("ABY_TurretStat_ExtraPower", "Extra power draw: {0}", ABY_ModularTurretUtility.FormatPowerDelta(module.extraPowerDraw)));
            }

            return string.Join("\n", lines.Where(line => !line.NullOrEmpty()).ToArray());
        }

        private static string CompactText(string text, int maxChars)
        {
            if (text.NullOrEmpty())
            {
                return string.Empty;
            }

            if (text.Length <= maxChars)
            {
                return text;
            }

            return text.Substring(0, Mathf.Max(1, maxChars - 1)) + "…";
        }

        private static void DrawClippedLabel(Rect rect, string text)
        {
            try
            {
                Widgets.Label(rect, text ?? string.Empty);
            }
            catch
            {
            }
        }

        private static void ResetTextState()
        {
            Text.Anchor = TextAnchor.UpperLeft;
            Text.Font = GameFont.Small;
            GUI.color = Color.white;
        }

        private readonly struct SlotView
        {
            public readonly ABY_TurretModuleSlot Slot;
            public readonly int PassiveIndex;
            public readonly int AuxiliaryIndex;
            public readonly bool Active;
            public readonly ABY_TurretModuleDef Installed;
            public readonly string Title;
            public readonly string StateLine;

            public SlotView(ABY_TurretModuleSlot slot, int passiveIndex, int auxiliaryIndex, bool active, ABY_TurretModuleDef installed, string title, string stateLine)
            {
                Slot = slot;
                PassiveIndex = passiveIndex;
                AuxiliaryIndex = auxiliaryIndex;
                Active = active;
                Installed = installed;
                Title = title;
                StateLine = stateLine;
            }
        }
    }
}
