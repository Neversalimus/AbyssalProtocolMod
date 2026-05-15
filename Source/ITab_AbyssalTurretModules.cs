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
        private const float SlotSize = 64f;
        private const float SlotGap = 10f;

        private ABY_TurretModuleSlot selectedSlot = ABY_TurretModuleSlot.MainWeapon;
        private int selectedPassiveIndex = -1;
        private int selectedAuxiliaryIndex;

        public ITab_AbyssalTurretModules()
        {
            size = new Vector2(680f, 540f);
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

            Rect root = new Rect(0f, 0f, size.x, size.y).ContractedBy(10f);
            AbyssalForgeConsoleArt.DrawBackground(root);
            Rect inner = root.ContractedBy(10f);

            DrawHeader(new Rect(inner.x, inner.y, inner.width, 62f), comp);

            Rect bodyRect = new Rect(inner.x, inner.y + 72f, inner.width, inner.height - 72f);
            Rect boardRect = new Rect(bodyRect.x, bodyRect.y, 386f, 346f);
            Rect detailRect = new Rect(boardRect.xMax + 10f, bodyRect.y, bodyRect.width - boardRect.width - 10f, 346f);
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

        private void DrawHeader(Rect rect, CompAbyssalModularTurret comp)
        {
            AbyssalForgeConsoleArt.DrawPanel(rect, comp.FeatureEnabled);
            Text.Font = GameFont.Medium;
            GUI.color = Color.white;
            ABY_UIPolishUtility.SafeLabel(new Rect(rect.x + 12f, rect.y + 6f, rect.width - 24f, 28f), ABY_ModularTurretUtility.TranslateOrFallback("ABY_ModularTurret_Header", "Abyssal modular turret chassis"));

            Text.Font = GameFont.Tiny;
            GUI.color = comp.FeatureEnabled ? AbyssalForgeConsoleArt.TextSoftColor : new Color(1f, 0.48f, 0.36f, 1f);
            string subtitle = comp.FeatureEnabled
                ? ABY_ModularTurretUtility.TranslateOrFallback("ABY_ModularTurret_GridSubtitle", "Socket board prototype: click a square to inspect, install, remove, or craft matching turret modules.")
                : ABY_ModularTurretUtility.TranslateOrFallback("ABY_TurretDisabledMessage", "Modular turret systems are disabled in mod settings.");
            ABY_UIPolishUtility.SafeLabel(new Rect(rect.x + 14f, rect.y + 33f, rect.width - 28f, 22f), subtitle);
            GUI.color = Color.white;
            Text.Font = GameFont.Small;
        }

        private void DrawSocketGrid(Rect rect, CompAbyssalModularTurret comp)
        {
            AbyssalForgeConsoleArt.DrawPanel(rect, true);
            Rect inner = rect.ContractedBy(10f);
            AbyssalForgeConsoleArt.DrawSectionTitle(new Rect(inner.x, inner.y, inner.width, 22f), ABY_ModularTurretUtility.TranslateOrFallback("ABY_TurretGridHeader", "Module socket grid"));

            Text.Font = GameFont.Tiny;
            GUI.color = AbyssalForgeConsoleArt.TextDimColor;
            ABY_UIPolishUtility.SafeLabel(new Rect(inner.x, inner.y + 22f, inner.width, 18f), ABY_ModularTurretUtility.TranslateOrFallback("ABY_TurretGridHint", "Square slots show the chassis layout; grey sockets are reserved for future chassis capacity."));
            GUI.color = Color.white;

            float mainX = inner.x + (inner.width - SlotSize) / 2f;
            DrawSlotSquare(new Rect(mainX, inner.y + 48f, SlotSize, SlotSize), comp, ABY_TurretModuleSlot.MainWeapon, comp.MainModule, -1, 0, comp.Props.mainWeaponSlots > 0, "MAIN", GetEmptySlotShortHint(ABY_TurretModuleSlot.MainWeapon));

            float auxGroupWidth = MaxVisualAuxiliarySlots * SlotSize + (MaxVisualAuxiliarySlots - 1) * SlotGap;
            float auxX = inner.x + (inner.width - auxGroupWidth) / 2f;
            for (int i = 0; i < MaxVisualAuxiliarySlots; i++)
            {
                bool active = i < Mathf.Max(0, comp.Props.auxiliarySlots) && i == 0;
                ABY_TurretModuleDef installed = i == 0 ? comp.AuxiliaryModule : null;
                DrawSlotSquare(new Rect(auxX + i * (SlotSize + SlotGap), inner.y + 122f, SlotSize, SlotSize), comp, ABY_TurretModuleSlot.Auxiliary, installed, -1, i, active, "AUX " + (i + 1), active ? GetEmptySlotShortHint(ABY_TurretModuleSlot.Auxiliary) : ABY_ModularTurretUtility.TranslateOrFallback("ABY_TurretGridFutureAux", "future"));
            }

            float passiveGroupWidth = 3 * SlotSize + 2 * SlotGap;
            float passiveX = inner.x + (inner.width - passiveGroupWidth) / 2f;
            for (int i = 0; i < 3; i++)
            {
                DrawPassiveSlotSquare(new Rect(passiveX + i * (SlotSize + SlotGap), inner.y + 202f, SlotSize, SlotSize), comp, i);
            }

            float passiveBottomWidth = 2 * SlotSize + SlotGap;
            float passiveBottomX = inner.x + (inner.width - passiveBottomWidth) / 2f;
            for (int i = 3; i < MaxVisualPassiveSlots; i++)
            {
                DrawPassiveSlotSquare(new Rect(passiveBottomX + (i - 3) * (SlotSize + SlotGap), inner.y + 276f, SlotSize, SlotSize), comp, i);
            }
        }

        private void DrawPassiveSlotSquare(Rect rect, CompAbyssalModularTurret comp, int index)
        {
            bool active = index < Mathf.Max(0, comp.Props.passiveSlots);
            ABY_TurretModuleDef installed = index < comp.PassiveModules.Count ? comp.PassiveModules[index] : null;
            DrawSlotSquare(rect, comp, ABY_TurretModuleSlot.Passive, installed, index, 0, active, "PASS " + (index + 1), active ? GetEmptySlotShortHint(ABY_TurretModuleSlot.Passive) : ABY_ModularTurretUtility.TranslateOrFallback("ABY_TurretGridFuturePassive", "future"));
        }

        private void DrawSlotSquare(Rect rect, CompAbyssalModularTurret comp, ABY_TurretModuleSlot slot, ABY_TurretModuleDef installed, int passiveIndex, int auxiliaryIndex, bool active, string badge, string emptyShortHint)
        {
            bool selected = IsSelected(slot, passiveIndex, auxiliaryIndex);
            bool available = active && SlotHasAvailableModule(comp, slot);
            Color slotColor = active ? ABY_ModularTurretUtility.SlotColor(slot) : AbyssalForgeConsoleArt.TextDimColor;
            Color fillColor = installed != null
                ? new Color(slotColor.r * 0.20f, slotColor.g * 0.16f, slotColor.b * 0.13f, 0.96f)
                : active ? new Color(0.09f, 0.075f, 0.07f, 0.96f) : new Color(0.055f, 0.055f, 0.06f, 0.82f);

            AbyssalForgeConsoleArt.Fill(rect, fillColor);
            AbyssalForgeConsoleArt.DrawOutline(rect, selected ? Color.Lerp(slotColor, Color.white, 0.35f) : new Color(slotColor.r, slotColor.g, slotColor.b, active ? 0.58f : 0.25f));
            AbyssalForgeConsoleArt.Fill(new Rect(rect.x + 2f, rect.y + 2f, rect.width - 4f, 2f), new Color(slotColor.r, slotColor.g, slotColor.b, active ? 0.65f : 0.25f));

            if (!active)
            {
                GUI.color = new Color(0.55f, 0.55f, 0.58f, 0.45f);
                Widgets.DrawLineHorizontal(rect.x + 8f, rect.center.y, rect.width - 16f);
                Widgets.DrawLineVertical(rect.center.x, rect.y + 8f, rect.height - 16f);
                GUI.color = Color.white;
            }

            Text.Font = GameFont.Tiny;
            Text.Anchor = TextAnchor.UpperCenter;
            GUI.color = active ? Color.white : AbyssalForgeConsoleArt.TextDimColor;
            ABY_UIPolishUtility.SafeLabel(new Rect(rect.x + 4f, rect.y + 5f, rect.width - 8f, 16f), badge);

            if (installed?.thingDef?.uiIcon != null)
            {
                Rect iconRect = new Rect(rect.center.x - 17f, rect.y + 22f, 34f, 26f);
                GUI.color = active ? Color.white : new Color(0.75f, 0.75f, 0.75f, 0.65f);
                GUI.DrawTexture(iconRect, installed.thingDef.uiIcon, ScaleMode.ScaleToFit, true);
                GUI.color = Color.white;
            }
            else
            {
                GUI.color = installed != null ? slotColor : AbyssalForgeConsoleArt.TextDimColor;
                string glyph = installed != null ? "◆" : active ? "+" : "×";
                Text.Font = GameFont.Medium;
                ABY_UIPolishUtility.SafeLabel(new Rect(rect.x, rect.y + 20f, rect.width, 24f), glyph);
                Text.Font = GameFont.Tiny;
            }

            GUI.color = installed != null ? AbyssalForgeConsoleArt.TextSoftColor : active ? AbyssalForgeConsoleArt.TextDimColor : new Color(0.55f, 0.55f, 0.58f, 0.75f);
            string bottom = installed != null ? ShortModuleLabel(installed) : emptyShortHint;
            ABY_UIPolishUtility.SafeLabel(new Rect(rect.x + 4f, rect.yMax - 18f, rect.width - 8f, 16f), bottom);
            Text.Anchor = TextAnchor.UpperLeft;
            GUI.color = Color.white;

            if (selected)
            {
                AbyssalForgeConsoleArt.DrawOutline(rect.ExpandedBy(2f), Color.Lerp(slotColor, Color.white, 0.45f));
            }

            if (Mouse.IsOver(rect))
            {
                AbyssalForgeConsoleArt.DrawOutline(rect, Color.Lerp(slotColor, Color.white, 0.55f));
            }

            if (Widgets.ButtonInvisible(rect, false))
            {
                selectedSlot = slot;
                selectedPassiveIndex = passiveIndex;
                selectedAuxiliaryIndex = auxiliaryIndex;
            }

            TooltipHandler.TipRegion(rect, BuildSlotTooltip(comp, slot, installed, passiveIndex, auxiliaryIndex, active, available));
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
            return comp?.FeatureEnabled == true && comp.parent?.Map != null && ABY_ModularTurretUtility.FindAvailableModuleOnMap(comp.parent.Map, slot, comp.Props.chassisTag) != null;
        }

        private void DrawSelectedSlotPanel(Rect rect, CompAbyssalModularTurret comp)
        {
            SlotView slotView = GetSelectedSlotView(comp);
            AbyssalForgeConsoleArt.DrawPanel(rect, slotView.Installed != null);
            Rect inner = rect.ContractedBy(12f);
            Color slotColor = ABY_ModularTurretUtility.SlotColor(slotView.Slot);
            AbyssalForgeConsoleArt.Fill(new Rect(inner.x, inner.y, 5f, 28f), slotColor);

            Text.Font = GameFont.Small;
            GUI.color = Color.white;
            ABY_UIPolishUtility.SafeLabel(new Rect(inner.x + 12f, inner.y, inner.width - 12f, 24f), slotView.Title);

            Text.Font = GameFont.Tiny;
            GUI.color = slotView.Active ? AbyssalForgeConsoleArt.TextSoftColor : new Color(1f, 0.58f, 0.48f, 1f);
            ABY_UIPolishUtility.SafeLabel(new Rect(inner.x + 12f, inner.y + 24f, inner.width - 12f, 18f), slotView.StateLine);
            GUI.color = Color.white;

            Rect infoRect = new Rect(inner.x, inner.y + 50f, inner.width, inner.height - 124f);
            DrawSelectedSlotInfo(infoRect, comp, slotView);

            DrawSelectedSlotActions(new Rect(inner.x, inner.yMax - 66f, inner.width, 66f), comp, slotView);
        }

        private void DrawSelectedSlotInfo(Rect rect, CompAbyssalModularTurret comp, SlotView slotView)
        {
            if (!slotView.Active)
            {
                Text.Font = GameFont.Tiny;
                GUI.color = AbyssalForgeConsoleArt.TextDimColor;
                ABY_UIPolishUtility.SafeLabel(rect, ABY_ModularTurretUtility.TranslateOrFallback("ABY_TurretGridLockedDetail", "This square is reserved for larger future chassis or later module expansion. It is visible now so the player can understand the full modular layout."));
                GUI.color = Color.white;
                return;
            }

            if (slotView.Installed == null)
            {
                Text.Font = GameFont.Tiny;
                GUI.color = AbyssalForgeConsoleArt.TextSoftColor;
                List<string> lines = new List<string>
                {
                    GetEmptySlotHint(slotView.Slot),
                    string.Empty,
                    ABY_ModularTurretUtility.TranslateOrFallback("ABY_TurretGridAvailableHeader", "Compatible loose modules on map:"),
                    GetAvailableModulesTooltip(comp, slotView.Slot)
                };
                ABY_UIPolishUtility.SafeLabel(rect, string.Join("\n", lines.Where(line => line != null).ToArray()));
                GUI.color = Color.white;
                return;
            }

            ABY_TurretModuleDef module = slotView.Installed;
            List<string> detailLines = new List<string>
            {
                module.LabelCap,
                ABY_ModularTurretUtility.TranslateOrFallback("ABY_TurretModuleLine_Slot", "Slot: {0}", module.SlotLabel),
                ABY_ModularTurretUtility.TranslateOrFallback("ABY_TurretModuleLine_Role", "Role: {0}", module.RoleLabel),
                string.Empty,
                ABY_ModularTurretUtility.TranslateOrFallback("ABY_TurretModuleLine_EffectHeader", "Effect:"),
                ABY_ModularTurretUtility.GetModuleEffectSummary(module),
                string.Empty,
                ABY_ModularTurretUtility.TranslateOrFallback("ABY_TurretModuleLine_StatsHeader", "Stats:"),
                BuildReadableModuleStats(module)
            };

            Text.Font = GameFont.Tiny;
            GUI.color = AbyssalForgeConsoleArt.TextSoftColor;
            ABY_UIPolishUtility.SafeLabel(rect, string.Join("\n", detailLines.Where(line => line != null).ToArray()));
            GUI.color = Color.white;
        }

        private void DrawSelectedSlotActions(Rect rect, CompAbyssalModularTurret comp, SlotView slotView)
        {
            if (!slotView.Active)
            {
                return;
            }

            Rect topButton = new Rect(rect.x, rect.y, rect.width, 28f);
            Rect bottomButton = new Rect(rect.x, rect.y + 36f, rect.width, 26f);

            if (slotView.Installed == null)
            {
                string tooltip = comp.FeatureEnabled
                    ? GetAvailableModulesTooltip(comp, slotView.Slot)
                    : ABY_ModularTurretUtility.TranslateOrFallback("ABY_TurretEditDisabled", "Re-enable modular turrets in mod settings before editing installed modules.");
                bool enabled = SlotHasAvailableModule(comp, slotView.Slot);
                if (AbyssalStyledWidgets.TextButton(topButton, ABY_ModularTurretUtility.TranslateOrFallback("ABY_TurretInstallFromMap", "Install from map"), enabled, false, null, tooltip))
                {
                    if (comp.TryInstallFromMap(slotView.Slot, out string message))
                    {
                        Messages.Message(message, comp.parent, MessageTypeDefOf.PositiveEvent);
                    }
                    else
                    {
                        Messages.Message(message ?? ABY_ModularTurretUtility.TranslateOrFallback("ABY_TurretInstallFailed", "Could not install module."), comp.parent, MessageTypeDefOf.RejectInput, false);
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

                    Messages.Message(message ?? ABY_ModularTurretUtility.TranslateOrFallback("ABY_TurretRemoveFailed", "Could not remove module."), comp.parent, removed ? MessageTypeDefOf.PositiveEvent : MessageTypeDefOf.RejectInput, false);
                }
            }

            AbyssalStyledWidgets.TextButton(bottomButton, ABY_ModularTurretUtility.TranslateOrFallback("ABY_TurretOpenForgeHint", "Craft in Forge"), false, false, null, ABY_ModularTurretUtility.TranslateOrFallback("ABY_TurretOpenForgeHintTooltip", "Package 0 exposes module recipes in the Abyssal Forge Turret Systems category. Direct forge-opening from turret slots is planned for the next UX pass."));
        }

        private void DrawStatsPanel(Rect rect, CompAbyssalModularTurret comp)
        {
            AbyssalForgeConsoleArt.DrawPanel(rect, false);
            Rect inner = rect.ContractedBy(10f);

            Text.Font = GameFont.Small;
            GUI.color = Color.white;
            ABY_UIPolishUtility.SafeLabel(new Rect(inner.x, inner.y, inner.width, 22f), ABY_ModularTurretUtility.TranslateOrFallback("ABY_TurretStatsHeader", "Runtime preview"));

            Text.Font = GameFont.Tiny;
            GUI.color = AbyssalForgeConsoleArt.TextSoftColor;
            string cooldownText = comp.HasMainWeapon
                ? ABY_ModularTurretUtility.TranslateOrFallback(
                    "ABY_TurretStatsCooldownResolved",
                    "Main cooldown: {0} → {1}",
                    ABY_ModularTurretUtility.FormatTicksAsSeconds(comp.BaseMainCooldownTicks),
                    ABY_ModularTurretUtility.FormatTicksAsSeconds(comp.ResolvedMainCooldownTicks))
                : ABY_ModularTurretUtility.TranslateOrFallback("ABY_TurretStatsCooldown", "Main cooldown: {0}", "—");

            List<string> leftLines = new List<string>
            {
                ABY_ModularTurretUtility.TranslateOrFallback("ABY_TurretStatsFeature", "Feature state: {0}", comp.FeatureEnabled ? ABY_ModularTurretUtility.TranslateOrFallback("ABY_StateEnabled", "enabled") : ABY_ModularTurretUtility.TranslateOrFallback("ABY_StateDisabled", "disabled")),
                ABY_ModularTurretUtility.TranslateOrFallback("ABY_TurretStatsPower", "Power: {0}", comp.IsPowered ? ABY_ModularTurretUtility.TranslateOrFallback("ABY_StateOnline", "online") : ABY_ModularTurretUtility.TranslateOrFallback("ABY_StateOffline", "offline")),
                ABY_ModularTurretUtility.TranslateOrFallback("ABY_TurretStatsRange", "Main range: {0}", comp.HasMainWeapon ? comp.ResolvedRange.ToString("0.0") : "—"),
                cooldownText
            };

            List<string> rightLines = new List<string>
            {
                ABY_ModularTurretUtility.TranslateOrFallback("ABY_TurretStatsPowerDraw", "Power draw: {0} W base + {1} W modules = {2} W applied", comp.ResolvedBasePowerDraw.ToString("0"), comp.ResolvedModulePowerDraw.ToString("0"), comp.ResolvedTotalPowerDraw.ToString("0")),
                ABY_ModularTurretUtility.TranslateOrFallback("ABY_TurretStatsTargetingShort", "Targeting: skips downed/dead targets; burst cancels on invalid targets."),
                ABY_ModularTurretUtility.TranslateOrFallback("ABY_TurretStatsKillSwitchShort", "Kill switch: safe disable without deleting installed modules.")
            };

            float columnWidth = (inner.width - 16f) / 2f;
            ABY_UIPolishUtility.SafeLabel(new Rect(inner.x, inner.y + 28f, columnWidth, inner.height - 28f), string.Join("\n", leftLines.ToArray()));
            ABY_UIPolishUtility.SafeLabel(new Rect(inner.x + columnWidth + 16f, inner.y + 28f, columnWidth, inner.height - 28f), string.Join("\n", rightLines.ToArray()));
            GUI.color = Color.white;
            Text.Font = GameFont.Small;
        }

        private SlotView GetSelectedSlotView(CompAbyssalModularTurret comp)
        {
            if (selectedSlot == ABY_TurretModuleSlot.Auxiliary)
            {
                bool active = selectedAuxiliaryIndex == 0 && comp.Props.auxiliarySlots > 0;
                return new SlotView(selectedSlot, -1, selectedAuxiliaryIndex, active, active ? comp.AuxiliaryModule : null, GetSlotTitle(selectedSlot, -1, selectedAuxiliaryIndex), active ? ABY_ModularTurretUtility.TranslateOrFallback("ABY_TurretGridActiveSlot", "Active chassis slot") : ABY_ModularTurretUtility.TranslateOrFallback("ABY_TurretGridReservedSlot", "Reserved future slot"));
            }

            if (selectedSlot == ABY_TurretModuleSlot.Passive)
            {
                bool active = selectedPassiveIndex >= 0 && selectedPassiveIndex < comp.Props.passiveSlots;
                ABY_TurretModuleDef installed = active && selectedPassiveIndex < comp.PassiveModules.Count ? comp.PassiveModules[selectedPassiveIndex] : null;
                return new SlotView(selectedSlot, selectedPassiveIndex, 0, active, installed, GetSlotTitle(selectedSlot, selectedPassiveIndex, 0), active ? ABY_ModularTurretUtility.TranslateOrFallback("ABY_TurretGridActiveSlot", "Active chassis slot") : ABY_ModularTurretUtility.TranslateOrFallback("ABY_TurretGridReservedSlot", "Reserved future slot"));
            }

            bool mainActive = comp.Props.mainWeaponSlots > 0;
            return new SlotView(ABY_TurretModuleSlot.MainWeapon, -1, 0, mainActive, mainActive ? comp.MainModule : null, GetSlotTitle(ABY_TurretModuleSlot.MainWeapon, -1, 0), mainActive ? ABY_ModularTurretUtility.TranslateOrFallback("ABY_TurretGridActiveSlot", "Active chassis slot") : ABY_ModularTurretUtility.TranslateOrFallback("ABY_TurretGridReservedSlot", "Reserved future slot"));
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
                    return ABY_ModularTurretUtility.TranslateOrFallback("ABY_TurretGridEmptyMain", "install");
                case ABY_TurretModuleSlot.Auxiliary:
                    return ABY_ModularTurretUtility.TranslateOrFallback("ABY_TurretGridEmptyAux", "support");
                default:
                    return ABY_ModularTurretUtility.TranslateOrFallback("ABY_TurretGridEmptyPassive", "tune");
            }
        }

        private static string GetEmptySlotHint(ABY_TurretModuleSlot slot)
        {
            switch (slot)
            {
                case ABY_TurretModuleSlot.MainWeapon:
                    return ABY_ModularTurretUtility.TranslateOrFallback("ABY_TurretEmpty_Main", "Empty. The chassis will not fire until a main weapon core is installed.");
                case ABY_TurretModuleSlot.Auxiliary:
                    return ABY_ModularTurretUtility.TranslateOrFallback("ABY_TurretEmpty_Aux", "Empty. Auxiliary modules add a secondary timed support shot in this prototype.");
                default:
                    return ABY_ModularTurretUtility.TranslateOrFallback("ABY_TurretEmpty_Passive", "Empty. Passive modules alter range, cadence, or module power draw.");
            }
        }

        private static string ShortModuleLabel(ABY_TurretModuleDef module)
        {
            if (module == null)
            {
                return string.Empty;
            }

            string label = module.label ?? module.defName;
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
                lines.Add(ABY_ModularTurretUtility.TranslateOrFallback("ABY_TurretGridReservedSlot", "Reserved future slot"));
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

        private static string GetAvailableModulesTooltip(CompAbyssalModularTurret comp, ABY_TurretModuleSlot slot)
        {
            List<ABY_TurretModuleDef> modules = ABY_ModularTurretUtility.GetModulesForSlot(slot, comp.Props.chassisTag);
            if (modules.Count == 0)
            {
                return ABY_ModularTurretUtility.TranslateOrFallback("ABY_TurretNoCompatibleDefs", "No compatible module defs exist for this slot.");
            }

            List<string> lines = new List<string>();
            foreach (ABY_TurretModuleDef module in modules.Take(8))
            {
                int count = comp.parent?.Map != null ? ABY_ModularTurretUtility.GetUsableLooseModuleCount(comp.parent.Map, module) : 0;
                lines.Add("• " + module.LabelCap + " x" + count + " — " + ABY_ModularTurretUtility.GetModuleForgeCardSummary(module));
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
                lines.Add(ABY_ModularTurretUtility.TranslateOrFallback("ABY_TurretStat_AuxCooldown", "Auxiliary cooldown: {0}", ABY_ModularTurretUtility.FormatTicksAsSeconds(auxCooldown)));
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
                lines.Add(ABY_ModularTurretUtility.TranslateOrFallback("ABY_TurretStat_ExtraPower", "Extra power draw: +{0} W", module.extraPowerDraw.ToString("0")));
            }

            return string.Join("\n", lines.Where(line => !line.NullOrEmpty()).ToArray());
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
