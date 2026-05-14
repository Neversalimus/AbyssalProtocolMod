using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    public sealed class ITab_AbyssalTurretModules : ITab
    {
        private Vector2 scrollPosition;

        public ITab_AbyssalTurretModules()
        {
            size = new Vector2(640f, 500f);
            labelKey = "ABY_ModularTurret_Tab";
        }

        protected override void FillTab()
        {
            CompAbyssalModularTurret comp = SelThing?.TryGetComp<CompAbyssalModularTurret>();
            if (comp == null)
            {
                return;
            }

            Rect root = new Rect(0f, 0f, size.x, size.y).ContractedBy(10f);
            AbyssalForgeConsoleArt.DrawBackground(root);
            Rect inner = root.ContractedBy(10f);

            DrawHeader(new Rect(inner.x, inner.y, inner.width, 56f), comp);
            Rect contentRect = new Rect(inner.x, inner.y + 66f, inner.width, inner.height - 66f);

            Rect viewRect = new Rect(0f, 0f, contentRect.width - 18f, 830f);
            Widgets.BeginScrollView(contentRect, ref scrollPosition, viewRect);

            float y = 0f;
            DrawSlotCard(new Rect(0f, y, viewRect.width, 118f), comp, ABY_TurretModuleSlot.MainWeapon, comp.MainModule, -1);
            y += 128f;
            DrawSlotCard(new Rect(0f, y, viewRect.width, 118f), comp, ABY_TurretModuleSlot.Auxiliary, comp.AuxiliaryModule, -1);
            y += 128f;

            for (int i = 0; i < comp.Props.passiveSlots; i++)
            {
                ABY_TurretModuleDef passive = i < comp.PassiveModules.Count ? comp.PassiveModules[i] : null;
                DrawSlotCard(new Rect(0f, y, viewRect.width, 112f), comp, ABY_TurretModuleSlot.Passive, passive, i);
                y += 120f;
            }

            DrawStatsPanel(new Rect(0f, y + 4f, viewRect.width, 162f), comp);
            Widgets.EndScrollView();
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
                ? ABY_ModularTurretUtility.TranslateOrFallback("ABY_ModularTurret_Subtitle", "Install modules from map. Main core fires; auxiliary/passive modules change behavior.")
                : ABY_ModularTurretUtility.TranslateOrFallback("ABY_TurretDisabledMessage", "Modular turret systems are disabled in mod settings.");
            ABY_UIPolishUtility.SafeLabel(new Rect(rect.x + 14f, rect.y + 32f, rect.width - 28f, 20f), subtitle);
            GUI.color = Color.white;
            Text.Font = GameFont.Small;
        }

        private void DrawSlotCard(Rect rect, CompAbyssalModularTurret comp, ABY_TurretModuleSlot slot, ABY_TurretModuleDef installed, int passiveIndex)
        {
            AbyssalForgeConsoleArt.DrawPanel(rect, installed != null);
            Color slotColor = ABY_ModularTurretUtility.SlotColor(slot);
            AbyssalForgeConsoleArt.Fill(new Rect(rect.x + 1f, rect.y + 1f, 5f, rect.height - 2f), slotColor);

            Rect textRect = new Rect(rect.x + 14f, rect.y + 8f, rect.width - 230f, rect.height - 16f);
            Text.Font = GameFont.Small;
            GUI.color = Color.white;
            string title = GetSlotTitle(slot, passiveIndex);
            ABY_UIPolishUtility.SafeLabel(new Rect(textRect.x, textRect.y, textRect.width, 22f), title);

            Text.Font = GameFont.Tiny;
            GUI.color = installed != null ? AbyssalForgeConsoleArt.TextSoftColor : AbyssalForgeConsoleArt.TextDimColor;
            string body = installed != null ? DescribeModule(installed) : GetEmptySlotHint(slot);
            ABY_UIPolishUtility.SafeLabel(new Rect(textRect.x, textRect.y + 25f, textRect.width, textRect.height - 25f), body);
            GUI.color = Color.white;
            Text.Font = GameFont.Small;

            Rect buttonRect = new Rect(rect.xMax - 202f, rect.y + 18f, 188f, 30f);
            if (installed == null)
            {
                string tooltip = comp.FeatureEnabled
                    ? GetAvailableModulesTooltip(comp, slot)
                    : ABY_ModularTurretUtility.TranslateOrFallback("ABY_TurretEditDisabled", "Re-enable modular turrets in mod settings before editing installed modules.");
                bool enabled = comp.FeatureEnabled && ABY_ModularTurretUtility.FindAvailableModuleOnMap(comp.parent.Map, slot, comp.Props.chassisTag) != null;
                if (AbyssalStyledWidgets.TextButton(buttonRect, ABY_ModularTurretUtility.TranslateOrFallback("ABY_TurretInstallFromMap", "Install from map"), enabled, false, null, tooltip))
                {
                    if (comp.TryInstallFromMap(slot, out string message))
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
                if (AbyssalStyledWidgets.TextButton(buttonRect, ABY_ModularTurretUtility.TranslateOrFallback("ABY_TurretRemove", "Remove / eject"), comp.FeatureEnabled, false, null, tooltip))
                {
                    bool removed;
                    string message;
                    if (slot == ABY_TurretModuleSlot.MainWeapon)
                    {
                        removed = comp.TryRemoveMainModule(out message);
                    }
                    else if (slot == ABY_TurretModuleSlot.Auxiliary)
                    {
                        removed = comp.TryRemoveAuxiliaryModule(out message);
                    }
                    else
                    {
                        removed = comp.TryRemovePassiveModule(passiveIndex, out message);
                    }

                    Messages.Message(message ?? ABY_ModularTurretUtility.TranslateOrFallback("ABY_TurretRemoveFailed", "Could not remove module."), comp.parent, removed ? MessageTypeDefOf.PositiveEvent : MessageTypeDefOf.RejectInput, false);
                }
            }

            Rect forgeRect = new Rect(rect.xMax - 202f, rect.y + 58f, 188f, 26f);
            if (AbyssalStyledWidgets.TextButton(forgeRect, ABY_ModularTurretUtility.TranslateOrFallback("ABY_TurretOpenForgeHint", "Craft in Forge"), false, false, null, ABY_ModularTurretUtility.TranslateOrFallback("ABY_TurretOpenForgeHintTooltip", "Package 0 exposes module recipes in the Abyssal Forge Turret Systems category. Direct forge-opening from turret slots is planned for the next UX pass.")))
            {
            }
        }

        private void DrawStatsPanel(Rect rect, CompAbyssalModularTurret comp)
        {
            AbyssalForgeConsoleArt.DrawPanel(rect, false);
            Text.Font = GameFont.Small;
            GUI.color = Color.white;
            ABY_UIPolishUtility.SafeLabel(new Rect(rect.x + 12f, rect.y + 8f, rect.width - 24f, 24f), ABY_ModularTurretUtility.TranslateOrFallback("ABY_TurretStatsHeader", "Runtime preview"));

            Text.Font = GameFont.Tiny;
            GUI.color = AbyssalForgeConsoleArt.TextSoftColor;
            string cooldownText = comp.HasMainWeapon
                ? ABY_ModularTurretUtility.TranslateOrFallback(
                    "ABY_TurretStatsCooldownResolved",
                    "Main cooldown: {0} → {1}",
                    ABY_ModularTurretUtility.FormatTicksAsSeconds(comp.BaseMainCooldownTicks),
                    ABY_ModularTurretUtility.FormatTicksAsSeconds(comp.ResolvedMainCooldownTicks))
                : ABY_ModularTurretUtility.TranslateOrFallback("ABY_TurretStatsCooldown", "Main cooldown: {0}", "—");

            List<string> lines = new List<string>
            {
                ABY_ModularTurretUtility.TranslateOrFallback("ABY_TurretStatsFeature", "Feature state: {0}", comp.FeatureEnabled ? ABY_ModularTurretUtility.TranslateOrFallback("ABY_StateEnabled", "enabled") : ABY_ModularTurretUtility.TranslateOrFallback("ABY_StateDisabled", "disabled")),
                ABY_ModularTurretUtility.TranslateOrFallback("ABY_TurretStatsPower", "Power: {0}", comp.IsPowered ? ABY_ModularTurretUtility.TranslateOrFallback("ABY_StateOnline", "online") : ABY_ModularTurretUtility.TranslateOrFallback("ABY_StateOffline", "offline")),
                ABY_ModularTurretUtility.TranslateOrFallback("ABY_TurretStatsRange", "Main range: {0}", comp.HasMainWeapon ? comp.ResolvedRange.ToString("0.0") : "—"),
                cooldownText,
                ABY_ModularTurretUtility.TranslateOrFallback("ABY_TurretStatsPowerDraw", "Power draw: {0} W base + {1} W modules = {2} W applied", comp.ResolvedBasePowerDraw.ToString("0"), comp.ResolvedModulePowerDraw.ToString("0"), comp.ResolvedTotalPowerDraw.ToString("0")),
                ABY_ModularTurretUtility.TranslateOrFallback("ABY_TurretStatsTargeting", "Targeting: ignores downed/dead targets and cancels burst if the target falls, leaves range, or loses line of sight."),
                ABY_ModularTurretUtility.TranslateOrFallback("ABY_TurretStatsKillSwitch", "Kill switch: disables targeting, firing, placement, and Forge exposure without deleting installed modules.")
            };
            ABY_UIPolishUtility.SafeLabel(new Rect(rect.x + 14f, rect.y + 34f, rect.width - 28f, rect.height - 40f), string.Join("\n", lines.ToArray()));
            GUI.color = Color.white;
            Text.Font = GameFont.Small;
        }

        private static string GetSlotTitle(ABY_TurretModuleSlot slot, int passiveIndex)
        {
            switch (slot)
            {
                case ABY_TurretModuleSlot.MainWeapon:
                    return ABY_ModularTurretUtility.TranslateOrFallback("ABY_TurretSlotTitle_Main", "Main weapon core");
                case ABY_TurretModuleSlot.Auxiliary:
                    return ABY_ModularTurretUtility.TranslateOrFallback("ABY_TurretSlotTitle_Aux", "Auxiliary module");
                default:
                    return ABY_ModularTurretUtility.TranslateOrFallback("ABY_TurretSlotTitle_Passive", "Passive module {0}", passiveIndex + 1);
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

        private static string DescribeModule(ABY_TurretModuleDef module)
        {
            if (module == null)
            {
                return string.Empty;
            }

            List<string> parts = new List<string>
            {
                module.LabelCap + " · " + module.SlotLabel,
                ABY_ModularTurretUtility.TranslateOrFallback("ABY_TurretModuleLine_Role", "Role: {0}", module.RoleLabel),
                ABY_ModularTurretUtility.TranslateOrFallback("ABY_TurretModuleLine_Effect", "Effect: {0}", ABY_ModularTurretUtility.GetModuleEffectSummary(module)),
                ABY_ModularTurretUtility.GetModuleStatSummary(module)
            };

            return string.Join("\n", parts.Where(line => !line.NullOrEmpty()).ToArray());
        }

        private static string GetAvailableModulesTooltip(CompAbyssalModularTurret comp, ABY_TurretModuleSlot slot)
        {
            List<ABY_TurretModuleDef> modules = ABY_ModularTurretUtility.GetModulesForSlot(slot, comp.Props.chassisTag);
            if (modules.Count == 0)
            {
                return ABY_ModularTurretUtility.TranslateOrFallback("ABY_TurretNoCompatibleDefs", "No compatible module defs exist for this slot.");
            }

            List<string> lines = new List<string>
            {
                ABY_ModularTurretUtility.TranslateOrFallback("ABY_TurretCompatibleModules", "Compatible modules:")
            };

            foreach (ABY_TurretModuleDef module in modules.Take(8))
            {
                int count = ABY_ModularTurretUtility.GetUsableLooseModuleCount(comp.parent.Map, module);
                lines.Add("• " + module.LabelCap + " x" + count + " — " + ABY_ModularTurretUtility.GetModuleForgeCardSummary(module));
            }

            return string.Join("\n", lines.ToArray());
        }
    }
}
